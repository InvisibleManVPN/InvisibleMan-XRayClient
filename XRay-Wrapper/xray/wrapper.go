package xray

import (
	"C"
	"fmt"
	"net/http"
	"net/url"
	"os"
	"os/signal"
	"runtime"
	"runtime/debug"
	"strconv"
	"syscall"
	"time"

	"github.com/xtls/xray-core/common/net"
	"github.com/xtls/xray-core/core"

	_ "github.com/xtls/xray-core/main/distro/all"
	clog "github.com/xtls/xray-core/common/log"
)

const (
	PingTimeout int = -1
	PingError   int = -2
)

var osSignals = make(chan os.Signal, 1)

//export StartServer
func StartServer(config *C.char, port int, logLevel *C.char, logPath *C.char, isSocks bool, isUdpEnabled bool) {
	logSeverity := convertLogLevelToSeverity(logLevel)
	configObj := convertJsonToObject(config)
	configObj.Inbound = overrideInbound(net.Port(port), isSocks, isUdpEnabled)

	if logSeverity != clog.Severity_Unknown {
		log := overrideLog(logSeverity, logPath)
		insertElementToConfigApp(log, configObj.App)
		tryMakingDirectory(logPath)
	}

	server, err := core.New(configObj)
	if err != nil {
		fmt.Println("error | failed to initialize the server >", err)
		return
	}

	if err := server.Start(); err != nil {
		fmt.Println("error | failed to start server >", err)
		return
	}

	defer server.Close()

	runtime.GC()
	debug.FreeOSMemory()

	{
		signal.Notify(osSignals, os.Interrupt, syscall.SIGTERM)
		<-osSignals
	}
}

//export StopServer
func StopServer() {
	osSignals <- syscall.SIGTERM
}

//export TestConnection
func TestConnection(config *C.char, port int) int {
	configObj := convertJsonToObject(config)
	configObj.Inbound = overrideInbound(net.Port(port), false, false)

	server, err := core.New(configObj)
	if err != nil {
		return PingError
	}

	if err := server.Start(); err != nil {
		server.Close()
		return PingError
	}

	proxyUrl, err := url.Parse("http://127.0.0.1:" + strconv.Itoa(port))
	if err != nil {
		server.Close()
		return PingError
	}

	start := time.Now()
	http.DefaultTransport = &http.Transport{
		Proxy:               http.ProxyURL(proxyUrl),
		TLSHandshakeTimeout: time.Second * 5,
	}
	response, err := http.Head("https://www.gstatic.com/generate_204")

	if err != nil {
		server.Close()
		return PingTimeout
	}

	server.Close()
	fmt.Println("info | response code >", response.StatusCode)

	if response.StatusCode == 204 {
		return int(time.Since(start).Milliseconds())
	}

	return PingTimeout
}

//export GetXrayCoreVersion
func GetXrayCoreVersion() *C.char {
	return C.CString(core.Version())
}
