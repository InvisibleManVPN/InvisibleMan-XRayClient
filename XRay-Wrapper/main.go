package main

import (
	"fmt"

	_ "github.com/invisiblemanvpn/xray-wrapper/xray"
)

func main() {
	fmt.Println("xray-core wrapper")
	fmt.Println("created by: invisiblemanvpn")
	fmt.Println("https://github.com/invisiblemanvpn")

	// for testing, you can use xray.RunTest(content, format, port, logLevel, logPath isSocks, isUdpEnabled) function.
	// uncomment these lines and replace the "content" variable with your config content,
	// and replace the "port" variable with the port that you want to listen to it.
	// For accessing the log information, set "logLevel" and "logPath"
	// If you want to run it in socks mode, set "isSocks" to true, else false.
	// If you want to proxy UDP traffic, set "isUdpEnabled" to true, else false. (only for socks)

	// content := "for example: {\"log\":{\"access\": ... }}"
	// format := "for example: json"
	// port := "for example: 10801"
	// logLevel := "between [none, debug, info, warning, error]"
	// logPath := "for example: C:/Log"
	// isSocks := "for example: true"
	// isUdpEnabled := "for example: true"
	// xray.RunTest(content, format, port, logLevel, logPath, isSocks, isUdpEnabled)
}
