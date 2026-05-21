//go:build android

package xray

import (
	"errors"
	"fmt"
	"io"
	"net"
	"sync"
	"time"

	"golang.org/x/net/proxy"

	"github.com/eycorsican/go-tun2socks/common/log"
	tcore "github.com/eycorsican/go-tun2socks/core"
	tsocks "github.com/eycorsican/go-tun2socks/proxy/socks"
)

const (
	androidSocks5MethodNoAuth           = 0x00
	androidSocks5MethodUsernamePassword = 0x02
	androidSocks5MethodNoAcceptable     = 0xff
	androidSocks5Version                = 0x05
	androidSocks5UDPAssociate           = 0x03

	// max IP packet size - min IP header size - min UDP header size - min SOCKS5 header size
	androidMaxUdpPayloadSize = 65535 - 20 - 8 - 7
)

type androidSocksTCPHandler struct {
	proxyHost string
	proxyPort uint16
	auth      *localSocksAuth
}

type androidSocksUDPHandler struct {
	sync.Mutex

	proxyHost   string
	proxyPort   uint16
	auth        *localSocksAuth
	udpConns    map[tcore.UDPConn]net.PacketConn
	tcpConns    map[tcore.UDPConn]net.Conn
	remoteAddrs map[tcore.UDPConn]*net.UDPAddr
	timeout     time.Duration
}

type tcpRelayDirection byte

const (
	relayUplink tcpRelayDirection = iota
	relayDownlink
)

type duplexConn interface {
	net.Conn
	CloseRead() error
	CloseWrite() error
}

func newAndroidTCPHandler(proxyHost string, proxyPort uint16, auth *localSocksAuth) tcore.TCPConnHandler {
	return &androidSocksTCPHandler{
		proxyHost: proxyHost,
		proxyPort: proxyPort,
		auth:      auth,
	}
}

func newAndroidUDPHandler(proxyHost string, proxyPort uint16, auth *localSocksAuth, timeout time.Duration) tcore.UDPConnHandler {
	return &androidSocksUDPHandler{
		proxyHost:   proxyHost,
		proxyPort:   proxyPort,
		auth:        auth,
		udpConns:    make(map[tcore.UDPConn]net.PacketConn, 8),
		tcpConns:    make(map[tcore.UDPConn]net.Conn, 8),
		remoteAddrs: make(map[tcore.UDPConn]*net.UDPAddr, 8),
		timeout:     timeout,
	}
}

func (h *androidSocksTCPHandler) Handle(conn net.Conn, target *net.TCPAddr) error {
	var proxyAuth *proxy.Auth
	if h.auth != nil && h.auth.enabled() {
		proxyAuth = &proxy.Auth{
			User:     h.auth.Username,
			Password: h.auth.Password,
		}
	}

	dialer, err := proxy.SOCKS5("tcp", tcore.ParseTCPAddr(h.proxyHost, h.proxyPort).String(), proxyAuth, nil)
	if err != nil {
		return err
	}

	upstreamConn, err := dialer.Dial(target.Network(), target.String())
	if err != nil {
		return err
	}

	go relayTCP(conn, upstreamConn)
	log.Infof("new proxy connection to %v", target)
	return nil
}

func relayTCP(lhs net.Conn, rhs net.Conn) {
	upCh := make(chan struct{})

	closeConn := func(dir tcpRelayDirection, interrupt bool) {
		lhsDConn, lhsOk := lhs.(duplexConn)
		rhsDConn, rhsOk := rhs.(duplexConn)
		if !interrupt && lhsOk && rhsOk {
			switch dir {
			case relayUplink:
				lhsDConn.CloseRead()
				rhsDConn.CloseWrite()
			case relayDownlink:
				lhsDConn.CloseWrite()
				rhsDConn.CloseRead()
			default:
				panic("unexpected TCP relay direction")
			}
		} else {
			lhs.Close()
			rhs.Close()
		}
	}

	go func() {
		if _, err := io.Copy(rhs, lhs); err != nil {
			closeConn(relayUplink, true)
		} else {
			closeConn(relayUplink, false)
		}
		upCh <- struct{}{}
	}()

	if _, err := io.Copy(lhs, rhs); err != nil {
		closeConn(relayDownlink, true)
	} else {
		closeConn(relayDownlink, false)
	}

	<-upCh
}

func (h *androidSocksUDPHandler) Connect(conn tcore.UDPConn, target *net.UDPAddr) error {
	if target == nil {
		return h.connectInternal(conn, "")
	}
	return h.connectInternal(conn, target.String())
}

func (h *androidSocksUDPHandler) connectInternal(conn tcore.UDPConn, dest string) error {
	tcpConn, err := net.DialTimeout("tcp", tcore.ParseTCPAddr(h.proxyHost, h.proxyPort).String(), 4*time.Second)
	if err != nil {
		return err
	}

	if err := authenticateSocks5Connection(tcpConn, h.auth); err != nil {
		tcpConn.Close()
		return err
	}

	if _, err := tcpConn.Write(append([]byte{androidSocks5Version, androidSocks5UDPAssociate, 0}, []byte{1, 0, 0, 0, 0, 0, 0}...)); err != nil {
		tcpConn.Close()
		return err
	}

	buf := make([]byte, tsocks.MaxAddrLen)
	if _, err := io.ReadFull(tcpConn, buf[:3]); err != nil {
		tcpConn.Close()
		return err
	}

	rep := buf[1]
	if rep != 0 {
		tcpConn.Close()
		return fmt.Errorf("SOCKS UDP associate failed with code %d", rep)
	}

	remoteAddr, err := readAndroidSocksAddr(tcpConn, buf)
	if err != nil {
		tcpConn.Close()
		return err
	}

	resolvedRemoteAddr, err := net.ResolveUDPAddr("udp", remoteAddr.String())
	if err != nil {
		tcpConn.Close()
		return errors.New("failed to resolve SOCKS UDP relay address")
	}

	packetConn, err := net.ListenPacket("udp", "")
	if err != nil {
		tcpConn.Close()
		return err
	}

	h.Lock()
	h.tcpConns[conn] = tcpConn
	h.udpConns[conn] = packetConn
	h.remoteAddrs[conn] = resolvedRemoteAddr
	h.Unlock()

	go h.handleTCP(conn, tcpConn)
	go h.fetchUDPInput(conn, packetConn)

	log.Infof("new proxy connection to %v", dest)
	return nil
}

func (h *androidSocksUDPHandler) ReceiveTo(conn tcore.UDPConn, data []byte, addr *net.UDPAddr) error {
	h.Lock()
	packetConn, ok1 := h.udpConns[conn]
	remoteAddr, ok2 := h.remoteAddrs[conn]
	h.Unlock()

	if ok1 && ok2 {
		buf := append([]byte{0, 0, 0}, tsocks.ParseAddr(addr.String())...)
		buf = append(buf, data[:]...)
		if _, err := packetConn.WriteTo(buf, remoteAddr); err != nil {
			h.Close(conn)
			return fmt.Errorf("write remote failed: %w", err)
		}
		return nil
	}

	h.Close(conn)
	return fmt.Errorf("proxy connection %v->%v does not exist", conn.LocalAddr(), addr)
}

func (h *androidSocksUDPHandler) Close(conn tcore.UDPConn) {
	conn.Close()

	h.Lock()
	defer h.Unlock()

	if tcpConn, ok := h.tcpConns[conn]; ok {
		tcpConn.Close()
		delete(h.tcpConns, conn)
	}

	if packetConn, ok := h.udpConns[conn]; ok {
		packetConn.Close()
		delete(h.udpConns, conn)
	}

	delete(h.remoteAddrs, conn)
}

func (h *androidSocksUDPHandler) handleTCP(conn tcore.UDPConn, tcpConn net.Conn) {
	buf := tcore.NewBytes(tcore.BufSize)
	defer func() {
		h.Close(conn)
		tcore.FreeBytes(buf)
	}()

	for {
		tcpConn.SetDeadline(time.Time{})
		if _, err := tcpConn.Read(buf); err != nil {
			return
		}
	}
}

func (h *androidSocksUDPHandler) fetchUDPInput(conn tcore.UDPConn, input net.PacketConn) {
	buf := tcore.NewBytes(androidMaxUdpPayloadSize)
	defer func() {
		h.Close(conn)
		tcore.FreeBytes(buf)
	}()

	for {
		input.SetDeadline(time.Now().Add(h.timeout))
		n, _, err := input.ReadFrom(buf)
		if err != nil {
			return
		}
		if n < 3 {
			continue
		}

		addr := tsocks.SplitAddr(buf[3:n])
		if addr == nil {
			continue
		}

		resolvedAddr, err := net.ResolveUDPAddr("udp", addr.String())
		if err != nil {
			continue
		}

		if _, err = conn.WriteFrom(buf[int(3+len(addr)):n], resolvedAddr); err != nil {
			log.Warnf("write local failed: %v", err)
			return
		}
	}
}

func authenticateSocks5Connection(conn net.Conn, auth *localSocksAuth) error {
	if auth != nil && auth.enabled() {
		if _, err := conn.Write([]byte{androidSocks5Version, 1, androidSocks5MethodUsernamePassword}); err != nil {
			return err
		}

		reply := make([]byte, 2)
		if _, err := io.ReadFull(conn, reply); err != nil {
			return err
		}
		if reply[0] != androidSocks5Version {
			return fmt.Errorf("unexpected SOCKS version in method reply: %d", reply[0])
		}
		if reply[1] == androidSocks5MethodNoAcceptable {
			return errors.New("SOCKS server rejected all authentication methods")
		}
		if reply[1] != androidSocks5MethodUsernamePassword {
			return fmt.Errorf("SOCKS server selected unexpected auth method: %d", reply[1])
		}

		username := []byte(auth.Username)
		password := []byte(auth.Password)
		if len(username) == 0 || len(username) > 255 || len(password) > 255 {
			return errors.New("invalid SOCKS credential lengths")
		}

		authRequest := make([]byte, 0, 3+len(username)+len(password))
		authRequest = append(authRequest, 1, byte(len(username)))
		authRequest = append(authRequest, username...)
		authRequest = append(authRequest, byte(len(password)))
		authRequest = append(authRequest, password...)
		if _, err := conn.Write(authRequest); err != nil {
			return err
		}

		authReply := make([]byte, 2)
		if _, err := io.ReadFull(conn, authReply); err != nil {
			return err
		}
		if authReply[1] != 0x00 {
			return errors.New("SOCKS username/password authentication failed")
		}

		return nil
	}

	if _, err := conn.Write([]byte{androidSocks5Version, 1, androidSocks5MethodNoAuth}); err != nil {
		return err
	}

	reply := make([]byte, 2)
	if _, err := io.ReadFull(conn, reply); err != nil {
		return err
	}
	if reply[0] != androidSocks5Version {
		return fmt.Errorf("unexpected SOCKS version in method reply: %d", reply[0])
	}
	if reply[1] != androidSocks5MethodNoAuth {
		return fmt.Errorf("SOCKS server selected unsupported auth method: %d", reply[1])
	}

	return nil
}

func readAndroidSocksAddr(r io.Reader, buffer []byte) (tsocks.Addr, error) {
	if len(buffer) < tsocks.MaxAddrLen {
		return nil, io.ErrShortBuffer
	}

	if _, err := io.ReadFull(r, buffer[:1]); err != nil {
		return nil, err
	}

	switch buffer[0] {
	case 0x03:
		if _, err := io.ReadFull(r, buffer[1:2]); err != nil {
			return nil, err
		}
		if _, err := io.ReadFull(r, buffer[2:2+int(buffer[1])+2]); err != nil {
			return nil, err
		}
		return buffer[:1+1+int(buffer[1])+2], nil
	case 0x01:
		if _, err := io.ReadFull(r, buffer[1:1+net.IPv4len+2]); err != nil {
			return nil, err
		}
		return buffer[:1+net.IPv4len+2], nil
	case 0x04:
		if _, err := io.ReadFull(r, buffer[1:1+net.IPv6len+2]); err != nil {
			return nil, err
		}
		return buffer[:1+net.IPv6len+2], nil
	default:
		return nil, errors.New("unsupported SOCKS address type")
	}
}
