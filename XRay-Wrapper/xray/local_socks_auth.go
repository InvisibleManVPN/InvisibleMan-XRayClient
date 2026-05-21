package xray

import "strings"

type localSocksAuth struct {
	Username string
	Password string
}

func newLocalSocksAuth(username string, password string) *localSocksAuth {
	username = strings.TrimSpace(username)
	password = strings.TrimSpace(password)
	if username == "" || password == "" {
		return nil
	}

	return &localSocksAuth{
		Username: username,
		Password: password,
	}
}

func (auth *localSocksAuth) enabled() bool {
	return auth != nil && auth.Username != "" && auth.Password != ""
}

func (auth *localSocksAuth) accounts() map[string]string {
	if !auth.enabled() {
		return nil
	}

	return map[string]string{
		auth.Username: auth.Password,
	}
}
