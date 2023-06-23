package xray

import (
	"C"
	"encoding/json"
	"fmt"
	"os"
	"path/filepath"
	"strings"

	"github.com/xtls/xray-core/app/log"
	"github.com/xtls/xray-core/app/proxyman"
	"github.com/xtls/xray-core/common/cmdarg"
	"github.com/xtls/xray-core/common/net"
	"github.com/xtls/xray-core/common/serial"
	"github.com/xtls/xray-core/core"
	"github.com/xtls/xray-core/proxy/http"
	"github.com/xtls/xray-core/proxy/socks"

	clog "github.com/xtls/xray-core/common/log"
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

func convertJsonToObject(config *C.char) *core.Config {
	configJson := C.GoString(config)
	configObj := &core.Config{}

	json.Unmarshal([]byte(configJson), configObj)
	return configObj
}

func convertLogLevelToSeverity(logLevel *C.char) clog.Severity {
	switch level := strings.ToLower(C.GoString(logLevel)); level {
	case "debug":
		return clog.Severity_Debug
	case "info":
		return clog.Severity_Info
	case "warning":
		return clog.Severity_Warning
	case "error":
		return clog.Severity_Error
	default:
		return clog.Severity_Unknown
	}
}

func insertElementToConfigApp(element *serial.TypedMessage, configApp []*serial.TypedMessage) {
	for i := 0; i < len(configApp); i++ {
		if configApp[i].Type == element.Type {
			configApp[i] = element
			return
		}
	}

	configApp = append(configApp, element)
}

func overrideLog(logLevel clog.Severity, logPath *C.char) *serial.TypedMessage {
	path := C.GoString(logPath)

	return serial.ToTypedMessage(&log.Config{
		ErrorLogType:  log.LogType_File,
		ErrorLogPath:  path + "/error.log",
		ErrorLogLevel: logLevel,
		AccessLogType: log.LogType_File,
		AccessLogPath: path + "/access.log",
	})
}

func overrideInbound(port net.Port, isSocks bool, isUdpEnabled bool) []*core.InboundHandlerConfig {
	if isSocks == false {
		return overrideInboundToHttp(port)
	} else {
		return overrideInboundToSocks(port, isUdpEnabled)
	}
}

func overrideInboundToHttp(port net.Port) []*core.InboundHandlerConfig {
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

func overrideInboundToSocks(port net.Port, isUdpEnabled bool) []*core.InboundHandlerConfig {
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
			ProxySettings: serial.ToTypedMessage(&socks.ServerConfig{UdpEnabled: isUdpEnabled}),
		},
	}
}
