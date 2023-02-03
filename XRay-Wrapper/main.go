package main

import (
	"fmt"

	_ "github.com/invisiblemanvpn/xray-wrapper/xray"
)

func main() {
	fmt.Println("xray-core wrapper")
	fmt.Println("created by: invisiblemanvpn")
	fmt.Println("https://github.com/invisiblemanvpn")

	// for testing, you can use xray.RunTest(path) function.
	// uncomment these lines and replace the "path" variable with your config path.

	// path := "for example: C:/config.json"
	// xray.RunTest(path)
}
