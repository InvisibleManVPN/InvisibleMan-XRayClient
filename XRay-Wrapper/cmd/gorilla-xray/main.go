package main

import (
	"encoding/json"
	"flag"
	"fmt"
	"net/http"
	"net/url"
	"os"
	"os/signal"
	"path/filepath"
	"runtime"
	"runtime/debug"
	"strings"
	"syscall"
	"time"

	"github.com/xtls/xray-core/app/log"
	"github.com/xtls/xray-core/app/proxyman"
	"github.com/xtls/xray-core/common/cmdarg"
	clog "github.com/xtls/xray-core/common/log"
	"github.com/xtls/xray-core/common/net"
	"github.com/xtls/xray-core/common/serial"
	"github.com/xtls/xray-core/core"
	xhttp "github.com/xtls/xray-core/proxy/http"
	"github.com/xtls/xray-core/proxy/socks"

	_ "github.com/xtls/xray-core/main/distro/all"
)

var version = "3.2.5.0"

func main() {
	configPath := flag.String("config", "", "Path to xray config file (JSON/TOML/YAML)")
	port := flag.Int("port", 10801, "Local proxy listen port")
	logLevel := flag.String("log-level", "none", "Log level: none, debug, info, warning, error")
	logPath := flag.String("log-path", "", "Directory for log files (access.log, error.log)")
	useSocks := flag.Bool("socks", false, "Use SOCKS5 instead of HTTP proxy")
	udpEnabled := flag.Bool("udp", false, "Enable UDP proxying (SOCKS5 only)")
	socksUsername := flag.String("socks-user", "", "SOCKS5 username for the local listener")
	socksPassword := flag.String("socks-pass", "", "SOCKS5 password for the local listener")
	showVersion := flag.Bool("version", false, "Show version and exit")
	testConn := flag.Bool("test", false, "Test connection and exit (prints latency in ms)")

	flag.Usage = func() {
		fmt.Fprintf(os.Stderr, "Invisible Gorilla XRay Client v%s\n", version)
		fmt.Fprintf(os.Stderr, "Xray-core %s | %s/%s\n\n", core.Version(), runtime.GOOS, runtime.GOARCH)
		fmt.Fprintf(os.Stderr, "Usage: %s -config <path> [options]\n\n", os.Args[0])
		fmt.Fprintf(os.Stderr, "Options:\n")
		flag.PrintDefaults()
		fmt.Fprintf(os.Stderr, "\nExamples:\n")
		fmt.Fprintf(os.Stderr, "  %s -config config.json\n", os.Args[0])
		fmt.Fprintf(os.Stderr, "  %s -config config.json -port 1080 -socks\n", os.Args[0])
		fmt.Fprintf(os.Stderr, "  %s -config config.json -log-level debug -log-path ./logs\n", os.Args[0])
		fmt.Fprintf(os.Stderr, "  %s -config config.json -test\n", os.Args[0])
	}

	flag.Parse()

	if *showVersion {
		fmt.Printf("Invisible Gorilla XRay Client v%s\n", version)
		fmt.Printf("Xray-core %s\n", core.Version())
		fmt.Printf("Platform: %s/%s\n", runtime.GOOS, runtime.GOARCH)
		os.Exit(0)
	}

	if *configPath == "" {
		fmt.Fprintln(os.Stderr, "error: -config is required")
		fmt.Fprintln(os.Stderr, "")
		flag.Usage()
		os.Exit(1)
	}

	if (*socksUsername == "") != (*socksPassword == "") {
		fmt.Fprintln(os.Stderr, "error: -socks-user and -socks-pass must be provided together")
		os.Exit(1)
	}

	configObj, err := loadConfig(*configPath)
	if err != nil {
		fmt.Fprintf(os.Stderr, "error: failed to load config: %v\n", err)
		os.Exit(1)
	}

	configObj.Inbound = buildInbound(net.Port(*port), *useSocks, *udpEnabled, *socksUsername, *socksPassword)

	severity := parseSeverity(*logLevel)
	if severity != clog.Severity_Unknown && *logPath != "" {
		logMsg := buildLogConfig(severity, *logPath)
		insertLogConfig(logMsg, configObj.App)
		os.MkdirAll(*logPath, os.ModePerm)
	}

	if *testConn {
		ms := testConnection(configObj, *port)
		if ms >= 0 {
			fmt.Printf("%d\n", ms)
			os.Exit(0)
		}
		os.Exit(1)
	}

	runServer(configObj)
}

func loadConfig(path string) (*core.Config, error) {
	info, err := os.Stat(path)
	if err != nil || info.IsDir() {
		return nil, fmt.Errorf("config file not found: %s", path)
	}

	ext := strings.TrimPrefix(filepath.Ext(path), ".")
	format := core.GetFormatByExtension(ext)
	if format == "" {
		format = "auto"
	}

	config, err := core.LoadConfig(format, cmdarg.Arg{path})
	if err != nil {
		return nil, fmt.Errorf("load config: %w", err)
	}

	configJSON, err := json.Marshal(config)
	if err != nil {
		return nil, fmt.Errorf("encode config: %w", err)
	}

	configObj := &core.Config{}
	if err := json.Unmarshal(configJSON, configObj); err != nil {
		return nil, fmt.Errorf("decode config: %w", err)
	}

	return configObj, nil
}

func buildInbound(port net.Port, isSocks bool, udpEnabled bool, socksUsername string, socksPassword string) []*core.InboundHandlerConfig {
	receiver := &proxyman.ReceiverConfig{
		PortList: &net.PortList{
			Range: []*net.PortRange{net.SinglePortRange(port)},
		},
		Listen: &net.IPOrDomain{
			Address: &net.IPOrDomain_Ip{Ip: []byte{127, 0, 0, 1}},
		},
	}

	var proxy *serial.TypedMessage
	if isSocks {
		serverConfig := &socks.ServerConfig{UdpEnabled: udpEnabled}
		if socksUsername != "" && socksPassword != "" {
			serverConfig.AuthType = socks.AuthType_PASSWORD
			serverConfig.Accounts = map[string]string{
				socksUsername: socksPassword,
			}
		}
		proxy = serial.ToTypedMessage(serverConfig)
	} else {
		proxy = serial.ToTypedMessage(&xhttp.ServerConfig{})
	}

	return []*core.InboundHandlerConfig{{
		ReceiverSettings: serial.ToTypedMessage(receiver),
		ProxySettings:    proxy,
	}}
}

func buildLogConfig(severity clog.Severity, logDir string) *serial.TypedMessage {
	return serial.ToTypedMessage(&log.Config{
		ErrorLogType:  log.LogType_File,
		ErrorLogPath:  filepath.Join(logDir, "error.log"),
		ErrorLogLevel: severity,
		AccessLogType: log.LogType_File,
		AccessLogPath: filepath.Join(logDir, "access.log"),
	})
}

func insertLogConfig(logMsg *serial.TypedMessage, apps []*serial.TypedMessage) {
	for i, app := range apps {
		if app.Type == logMsg.Type {
			apps[i] = logMsg
			return
		}
	}
}

func parseSeverity(level string) clog.Severity {
	switch strings.ToLower(level) {
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

func runServer(config *core.Config) {
	server, err := core.New(config)
	if err != nil {
		fmt.Fprintf(os.Stderr, "error: failed to create server: %v\n", err)
		os.Exit(1)
	}

	if err := server.Start(); err != nil {
		fmt.Fprintf(os.Stderr, "error: failed to start server: %v\n", err)
		os.Exit(1)
	}
	defer server.Close()

	fmt.Printf("Invisible Gorilla XRay started (xray-core %s)\n", core.Version())
	fmt.Println("Press Ctrl+C to stop")

	runtime.GC()
	debug.FreeOSMemory()

	sig := make(chan os.Signal, 1)
	signal.Notify(sig, os.Interrupt, syscall.SIGTERM)
	<-sig

	fmt.Println("\nShutting down...")
}

func testConnection(config *core.Config, port int) int {
	server, err := core.New(config)
	if err != nil {
		fmt.Fprintf(os.Stderr, "error: %v\n", err)
		return -2
	}

	if err := server.Start(); err != nil {
		server.Close()
		fmt.Fprintf(os.Stderr, "error: %v\n", err)
		return -2
	}

	proxyURL, _ := url.Parse(fmt.Sprintf("http://127.0.0.1:%d", port))
	client := &http.Client{
		Transport: &http.Transport{
			Proxy:               http.ProxyURL(proxyURL),
			TLSHandshakeTimeout: 5 * time.Second,
		},
		Timeout: 10 * time.Second,
	}

	start := time.Now()
	resp, err := client.Head("https://www.gstatic.com/generate_204")
	server.Close()

	if err != nil {
		fmt.Fprintf(os.Stderr, "timeout: %v\n", err)
		return -1
	}
	if resp.Body != nil {
		resp.Body.Close()
	}

	if resp.StatusCode == 204 {
		return int(time.Since(start).Milliseconds())
	}

	fmt.Fprintf(os.Stderr, "unexpected status: %d\n", resp.StatusCode)
	return -1
}
