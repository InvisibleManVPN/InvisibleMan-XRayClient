//go:build android

package xray

/*
#include <stdbool.h>
#include <stdlib.h>
*/
import "C"

import (
	"errors"
	"fmt"
	"io"
	"net"
	"os"
	"strings"
	"sync"
	"time"

	tcore "github.com/eycorsican/go-tun2socks/core"
)

const androidTunReadBufferSize = 64 * 1024

var androidTunMutex sync.Mutex
var androidTunState *androidTunBridge
var androidTunLastError string

type androidTunBridge struct {
	tunFile *os.File
	lwip    tcore.LWIPStack
	done    chan struct{}
	stop    chan struct{}
}

type disabledUDPHandler struct{}

func (disabledUDPHandler) Connect(conn tcore.UDPConn, target *net.UDPAddr) error {
	return errors.New("udp is disabled")
}

func (disabledUDPHandler) ReceiveTo(conn tcore.UDPConn, data []byte, addr *net.UDPAddr) error {
	conn.Close()
	return errors.New("udp is disabled")
}

//export StartAndroidTun2Socks
func StartAndroidTun2Socks(fd C.int, proxyPort C.int, isUdpEnabled C.bool, username *C.char, password *C.char) (errPtr *C.char) {
	defer func() {
		if recovered := recover(); recovered != nil {
			message := fmt.Sprintf("android tun2socks panic: %v", recovered)
			androidTunMutex.Lock()
			stopAndroidTunLocked()
			androidTunLastError = message
			androidTunMutex.Unlock()
			errPtr = C.CString(message)
		}
	}()

	androidTunMutex.Lock()
	defer androidTunMutex.Unlock()

	androidTunLastError = ""
	stopAndroidTunLocked()

	if fd <= 0 {
		message := "invalid Android TUN file descriptor"
		androidTunLastError = message
		return C.CString(message)
	}

	if proxyPort <= 0 {
		message := "invalid Android SOCKS proxy port"
		androidTunLastError = message
		return C.CString(message)
	}

	auth := newLocalSocksAuth(C.GoString(username), C.GoString(password))
	if auth == nil {
		message := "missing Android SOCKS credentials"
		androidTunLastError = message
		return C.CString(message)
	}

	tunFile := os.NewFile(uintptr(fd), fmt.Sprintf("android-tun-%d", int(fd)))
	if tunFile == nil {
		message := "failed to open Android TUN file descriptor"
		androidTunLastError = message
		return C.CString(message)
	}

	lwip := tcore.NewLWIPStack()
	bridge := &androidTunBridge{
		tunFile: tunFile,
		lwip:    lwip,
		done:    make(chan struct{}),
		stop:    make(chan struct{}),
	}

	tcore.RegisterTCPConnHandler(newAndroidTCPHandler("127.0.0.1", uint16(proxyPort), auth))
	if bool(isUdpEnabled) {
		tcore.RegisterUDPConnHandler(newAndroidUDPHandler("127.0.0.1", uint16(proxyPort), auth, 30*time.Second))
	} else {
		tcore.RegisterUDPConnHandler(disabledUDPHandler{})
	}

	tcore.RegisterOutputFn(func(data []byte) (int, error) {
		return writeAndroidTunPacket(bridge, data)
	})

	androidTunState = bridge
	go runAndroidTunLoop(bridge)
	return nil
}

//export StopAndroidTun2Socks
func StopAndroidTun2Socks() {
	androidTunMutex.Lock()
	defer androidTunMutex.Unlock()
	stopAndroidTunLocked()
}

//export IsAndroidTun2SocksRunning
func IsAndroidTun2SocksRunning() C.bool {
	androidTunMutex.Lock()
	defer androidTunMutex.Unlock()
	return C.bool(androidTunState != nil)
}

//export GetAndroidTun2SocksLastError
func GetAndroidTun2SocksLastError() *C.char {
	androidTunMutex.Lock()
	defer androidTunMutex.Unlock()

	if androidTunLastError == "" {
		return nil
	}

	return C.CString(androidTunLastError)
}

func runAndroidTunLoop(bridge *androidTunBridge) {
	defer close(bridge.done)
	defer func() {
		androidTunMutex.Lock()
		if androidTunState == bridge {
			androidTunState = nil
		}
		androidTunMutex.Unlock()
	}()

	buffer := make([]byte, androidTunReadBufferSize)
	for {
		tunFile := bridge.tunFile
		if tunFile == nil {
			return
		}

		packetLength, err := tunFile.Read(buffer)
		if packetLength > 0 {
			if isBridgeStopping(bridge) {
				return
			}

			lwip := bridge.lwip
			if lwip == nil {
				return
			}

			if _, writeErr := lwip.Write(buffer[:packetLength]); writeErr != nil && !isIgnorableTunInputError(writeErr) {
				if isBridgeStopping(bridge) {
					return
				}

				setAndroidTunLastError(fmt.Sprintf("tun2socks packet handling failed: %v", writeErr))
				return
			}
		}

		if err != nil {
			if !isExpectedTunClose(err) {
				setAndroidTunLastError(fmt.Sprintf("tun2socks read failed: %v", err))
			}
			return
		}

		select {
		case <-bridge.stop:
			return
		default:
		}
	}
}

func writeAndroidTunPacket(bridge *androidTunBridge, data []byte) (int, error) {
	androidTunMutex.Lock()
	defer androidTunMutex.Unlock()

	if androidTunState != bridge || bridge.tunFile == nil {
		return 0, io.ErrClosedPipe
	}

	return bridge.tunFile.Write(data)
}

func stopAndroidTunLocked() {
	bridge := androidTunState
	androidTunState = nil

	if bridge == nil {
		return
	}

	select {
	case <-bridge.stop:
	default:
		close(bridge.stop)
	}

	tunFile := bridge.tunFile
	lwip := bridge.lwip

	// Release the global bridge lock before closing the TUN file and LWIP stack.
	// Close() may synchronously trigger callbacks that also need androidTunMutex.
	androidTunMutex.Unlock()
	defer androidTunMutex.Lock()

	// Close the TUN FD first so the packet reader unblocks before lwip teardown.
	if tunFile != nil {
		_ = tunFile.Close()
	}

	// Give the read loop a chance to exit cleanly before tearing down lwip.
	select {
	case <-bridge.done:
	case <-time.After(2 * time.Second):
	}

	if lwip != nil {
		_ = lwip.Close()
	}
}

func isExpectedTunClose(err error) bool {
	if err == nil {
		return true
	}

	if errors.Is(err, os.ErrClosed) || errors.Is(err, io.EOF) || errors.Is(err, io.ErrClosedPipe) {
		return true
	}

	message := err.Error()
	return strings.Contains(message, "file already closed") ||
		strings.Contains(message, "bad file descriptor") ||
		strings.Contains(message, "use of closed file") ||
		strings.Contains(message, "closed pipe")
}

func isIgnorableTunInputError(err error) bool {
	if err == nil {
		return false
	}

	return isExpectedTunClose(err) || strings.Contains(err.Error(), "packet not handled")
}

func setAndroidTunLastError(message string) {
	androidTunMutex.Lock()
	defer androidTunMutex.Unlock()
	androidTunLastError = message
}

func isBridgeStopping(bridge *androidTunBridge) bool {
	select {
	case <-bridge.stop:
		return true
	default:
		return false
	}
}
