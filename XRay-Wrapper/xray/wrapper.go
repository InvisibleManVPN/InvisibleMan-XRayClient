package xray

import (
	"C"
	"fmt"
	"os"
	"os/signal"
	"runtime"
	"runtime/debug"
	"syscall"

	"github.com/xtls/xray-core/core"

	_ "github.com/xtls/xray-core/main/distro/all"
)

func RunTest(path string) {
	cpath := C.CString(path)
	Run(cpath)
}

//export Run
func Run(path *C.char) {
	fmt.Println("Running xray-core...")

	pathStr := C.GoString(path)
	format := getConfigFormat(pathStr)

	file := getConfigFile(pathStr)
	if file == nil {
		fmt.Println("error | failed to get config file.")
		os.Exit(1)
	}

	c, err := core.LoadConfig(format, file)
	if err != nil {
		fmt.Println("error | failed to load config file >", err)
		os.Exit(2)
	}

	server, err := core.New(c)
	if err != nil {
		fmt.Println("error | failed to start server >", err)
		os.Exit(3)
	}

	if err := server.Start(); err != nil {
		fmt.Println("error | failed to start server >", err)
		os.Exit(4)
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
