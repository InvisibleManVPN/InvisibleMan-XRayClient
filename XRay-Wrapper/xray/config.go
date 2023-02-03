package xray

import (
	"os"
	"path/filepath"
	"strings"

	"github.com/xtls/xray-core/common/cmdarg"
	"github.com/xtls/xray-core/core"
)

func getConfigFormat(path string) string {
	ext := strings.TrimPrefix(filepath.Ext(path), ".")
	f := core.GetFormatByExtension(ext)
	if f == "" {
		f = "auto"
	}
	return f
}

func getConfigFile(path string) cmdarg.Arg {
	if isFileExists(path) {
		return cmdarg.Arg{path}
	}

	return nil
}

func isFileExists(file string) bool {
	if file == "" {
		return false
	}

	info, err := os.Stat(file)
	return err == nil && !info.IsDir()
}
