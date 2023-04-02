package main

import (
	"fmt"

	_ "github.com/invisiblemanvpn/xray-wrapper/xray"
)

func main() {
	fmt.Println("xray-core wrapper")
	fmt.Println("created by: invisiblemanvpn")
	fmt.Println("https://github.com/invisiblemanvpn")

	// for testing, you can use xray.RunTest(path, port, isSocks) function.
	// uncomment these lines and replace the "path" variable with your config path,
	// and replace the "port" variable with the port that you want to listen to it.
	// If you want to run it in socks mode, set "isSocks" to true, else false.

	// path := "for example: C:/config.json"
	// port := "for example: 10801"
	// isSocks := "for example: true"
	// xray.RunTest(path, port, isSocks)
}
