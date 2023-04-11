package xray

import (
	"C"
	"fmt"
)

func RunTest(path string, port int, isSocks bool) {
	fmt.Println("start testing...")
	cpath := C.CString(path)

	format := GetConfigFormat(cpath)
	fmt.Println("format:", C.GoString(format))

	if !IsFileExists(cpath) {
		fmt.Println("error | file doesn't exist.")
		return
	}

	file := cpath
	fmt.Println("file:", C.GoString(file))

	config := LoadConfig(format, file)
	fmt.Println("config:", C.GoString(config))

	StartServer(config, port, isSocks)
	fmt.Println("end of test.")
}
