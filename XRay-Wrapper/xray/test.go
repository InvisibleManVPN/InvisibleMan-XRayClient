package xray

import (
	"C"
	"fmt"
)

func RunTest(content string, format string, port int, logLevel string, logPath string, isSocks bool, isUdpEnabled bool) {
	fmt.Println("start testing...")
	cContent := C.CString(content)
	cFormat := C.CString(format)
	cLogLevel := C.CString(logLevel)
	cLogPath := C.CString(logPath)

	config := LoadConfig(cFormat, cContent)
	fmt.Println("config:", C.GoString(config))

	StartServer(config, port, cLogLevel, cLogPath, isSocks, isUdpEnabled)
	fmt.Println("end of test.")
}
