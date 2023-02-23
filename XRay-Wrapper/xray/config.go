package xray

import (
	"C"
	"encoding/json"
	"fmt"
	"os"
	"path/filepath"
	"strings"

	"github.com/xtls/xray-core/app/proxyman"
	"github.com/xtls/xray-core/common/cmdarg"
	"github.com/xtls/xray-core/common/net"
	"github.com/xtls/xray-core/common/serial"
	"github.com/xtls/xray-core/core"
	"github.com/xtls/xray-core/proxy/http"
)

//export GetConfigFormat
func GetConfigFormat(path *C.char) *C.char {
	file := C.GoString(path)
	ext := strings.TrimPrefix(filepath.Ext(file), ".")
	format := core.GetFormatByExtension(ext)

	if format == "" {
		format = "auto"
	}

	return C.CString(format)
}

//export IsFileExists
func IsFileExists(path *C.char) bool {
	file := C.GoString(path)
	if file == "" {
		return false
	}

	info, err := os.Stat(file)
	return err == nil && !info.IsDir()
}

//export LoadConfig
func LoadConfig(ext *C.char, path *C.char) *C.char {
	format := C.GoString(ext)
	file := cmdarg.Arg{C.GoString(path)}

	config, err := core.LoadConfig(format, file)
	if err != nil {
		fmt.Println("error | failed to load config file >", err)
		return C.CString("")
	}

	configJson, err := json.Marshal(config)
	if err != nil {
		fmt.Println("error | failed to encode config to json >", err)
		return C.CString("")
	}

	return C.CString(string(configJson))
}

func overrideInbound(port net.Port) []*core.InboundHandlerConfig {
	return []*core.InboundHandlerConfig{
		{
			ReceiverSettings: serial.ToTypedMessage(&proxyman.ReceiverConfig{
				PortList: &net.PortList{
					Range: []*net.PortRange{
						net.SinglePortRange(port),
					},
				},
				Listen: &net.IPOrDomain{
					Address: &net.IPOrDomain_Ip{
						Ip: []byte{127, 0, 0, 1},
					},
				},
			}),
			ProxySettings: serial.ToTypedMessage(&http.ServerConfig{}),
		},
	}
}
