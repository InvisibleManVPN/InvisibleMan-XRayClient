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

	"github.com/xtls/xray-core/common/net"
	"github.com/xtls/xray-core/core"

	_ "github.com/xtls/xray-core/main/distro/all"
)

//export StartServer
func StartServer(config *C.char, port int) {
	configObj := convertJsonToObject(config)
	configObj.Inbound = overrideInbound(net.Port(port))

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
		osSignals := make(chan os.Signal, 1)
		signal.Notify(osSignals, os.Interrupt, syscall.SIGTERM)
		<-osSignals
	}
}

//export TestConnection
func TestConnection(config *C.char, port int) bool {
	configObj := convertJsonToObject(config)
	configObj.Inbound = overrideInbound(net.Port(port))

	server, err := core.New(configObj)
	if err != nil {
		return false
	}

	if err := server.Start(); err != nil {
		return false
	}

	proxyUrl, err := url.Parse("http://127.0.0.1:" + strconv.Itoa(port))
	if err != nil {
		return false
	}

	http.DefaultTransport = &http.Transport{Proxy: http.ProxyURL(proxyUrl)}
	response, err := http.Head("https://www.gstatic.com/generate_204")

	if err != nil {
		return false
	}

	server.Close()
	fmt.Println("info | response code >", response.StatusCode)

	if response.StatusCode == 204 {
		return true
	}

	return false
}
