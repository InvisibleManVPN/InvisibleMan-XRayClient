package xray

import (
	"C"
	"fmt"
)

func RunTest(path string, port int, logLevel string, logPath string, isSocks bool, isUdpEnabled bool) {
	fmt.Println("start testing...")
	cPath := C.CString(path)
	cLogLevel := C.CString(logLevel)
	cLogPath := C.CString(logPath)

	format := GetConfigFormat(cPath)
	fmt.Println("format:", C.GoString(format))

	if !IsFileExists(cPath) {
		fmt.Println("error | file doesn't exist.")
		return
	}

	file := cPath
	fmt.Println("file:", C.GoString(file))

	config := LoadConfig(format, file)
	fmt.Println("config:", C.GoString(config))

	StartServer(config, port, cLogLevel, cLogPath, isSocks, isUdpEnabled)
	fmt.Println("end of test.")
}
