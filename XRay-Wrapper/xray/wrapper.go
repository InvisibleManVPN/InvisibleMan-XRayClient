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
	"sync"
	"syscall"
	"time"

	"github.com/xtls/xray-core/common/net"
	"github.com/xtls/xray-core/core"

	clog "github.com/xtls/xray-core/common/log"
	_ "github.com/xtls/xray-core/main/distro/all"
)

const (
	PingTimeout int = -1
	PingError   int = -2
)

var serverStopMutex sync.Mutex
var serverStopChannel chan struct{}
var serverLifecycleActive bool
var serverStopRequested bool

//export StartServer
func StartServer(config *C.char, port int, logLevel *C.char, logPath *C.char, isSocks bool, isUdpEnabled bool, username *C.char, password *C.char) {
	serverStopMutex.Lock()
	serverLifecycleActive = true
	serverStopRequested = false
	serverStopChannel = nil
	serverStopMutex.Unlock()

	logSeverity := convertLogLevelToSeverity(logLevel)
	configObj := convertJsonToObject(config)
	configObj.Inbound = overrideInbound(
		net.Port(port),
		isSocks,
		isUdpEnabled,
		newLocalSocksAuth(C.GoString(username), C.GoString(password)))

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
	defer clearServerStopState()

	runtime.GC()
	debug.FreeOSMemory()

	stopChannel := make(chan struct{})
	serverStopMutex.Lock()
	serverStopChannel = stopChannel
	pendingStop := serverStopRequested
	serverStopRequested = false
	serverStopMutex.Unlock()

	if pendingStop {
		closeServerStopChannel(stopChannel)
	}

	osSignalChannel := make(chan os.Signal, 1)
	signal.Notify(osSignalChannel, os.Interrupt, syscall.SIGTERM)
	defer signal.Stop(osSignalChannel)

	select {
	case <-osSignalChannel:
	case <-stopChannel:
	}
}

//export StopServer
func StopServer() {
	serverStopMutex.Lock()
	stopChannel := serverStopChannel
	if stopChannel != nil {
		serverStopChannel = nil
		serverStopMutex.Unlock()
		closeServerStopChannel(stopChannel)
		return
	}

	if serverLifecycleActive {
		serverStopRequested = true
	}
	serverStopMutex.Unlock()
}

//export TestConnection
func TestConnection(config *C.char, port int) int {
	configObj := convertJsonToObject(config)
	configObj.Inbound = overrideInbound(net.Port(port), false, false, nil)

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

	if response.Body != nil {
		response.Body.Close()
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

func clearServerStopState() {
	serverStopMutex.Lock()
	serverStopChannel = nil
	serverLifecycleActive = false
	serverStopRequested = false
	serverStopMutex.Unlock()
}

func closeServerStopChannel(channel chan struct{}) {
	defer func() {
		_ = recover()
	}()

	close(channel)
}
