package xray

import (
	"C"
	"os"
)

func tryMakingDirectory(directory *C.char) {
	dir := C.GoString(directory)

	if _, err := os.Stat(dir); os.IsNotExist(err) {
		_ = os.MkdirAll(dir, os.ModePerm)
	}
}
