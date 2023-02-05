package xray

import (
	"C"
	"encoding/json"
	"fmt"
	"os"
	"os/signal"
	"runtime"
	"runtime/debug"
	"syscall"

	"github.com/xtls/xray-core/core"

	_ "github.com/xtls/xray-core/main/distro/all"
)

//export StartServer
func StartServer(config *C.char) {
	configJson := C.GoString(config)
	configObj := &core.Config{}
	json.Unmarshal([]byte(configJson), configObj)

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
