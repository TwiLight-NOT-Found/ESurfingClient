package main

import (
	_ "embed"
	"encoding/json"
	"fmt"
	"net/http"
	"os"
	"os/exec"
	"path/filepath"
	"time"

	"github.com/getlantern/systray"
)

//go:embed icon.png
var iconData []byte

const manageURL = "http://127.0.0.1:8888"

type authResp struct {
	Status    bool   `json:"status"`
	Reachable bool   `json:"reachable"`
	Reason    string `json:"reason"`
}

func main() {
	systray.Run(onReady, onExit)
}

func onReady() {
	systray.SetIcon(iconData)
	systray.SetTitle("ESurfingTray")
	systray.SetTooltip("ESurfingClient 托盘")

	mOpen := systray.AddMenuItem("打开管理页面", "打开 "+manageURL)
	mReauth := systray.AddMenuItem("强制重认证", "重启认证进程")
	systray.AddSeparator()
	mAuto := systray.AddMenuItemCheckbox("开机自启", "登录时自动运行", isAutoStart())
	systray.AddSeparator()
	mQuit := systray.AddMenuItem("退出", "退出托盘并停止客户端")

	go pollStatus()

	for {
		select {
		case <-mOpen.ClickedCh:
			openManagePage()
		case <-mReauth.ClickedCh:
			reauthenticate()
		case <-mAuto.ClickedCh:
			toggleAutoStart(mAuto)
		case <-mQuit.ClickedCh:
			stopClient()
			systray.Quit()
		}
	}
}

func onExit() {}

// openManagePage 用默认浏览器打开管理页
func openManagePage() {
	exec.Command("xdg-open", manageURL).Start()
}

// reauthenticate 调用 POST /api/restartAuth 重启认证进程
func reauthenticate() {
	req, _ := http.NewRequest("POST", manageURL+"/api/restartAuth", nil)
	req.Header.Set("User-Agent", "ESurfingTray")
	resp, err := (&http.Client{Timeout: 5 * time.Second}).Do(req)
	if err != nil {
		notify("强制重认证", "无法连接管理页，客户端可能未运行")
		return
	}
	defer resp.Body.Close()
	switch resp.StatusCode {
	case 204:
		notify("强制重认证", "认证进程已重启，正在重新检测网络")
	case 503:
		notify("强制重认证", "认证进程不可达，请求被拒绝")
	default:
		notify("强制重认证", fmt.Sprintf("请求失败，状态码 %d", resp.StatusCode))
	}
}

// pollStatus 每 3 秒轮询状态，更新托盘悬浮提示
func pollStatus() {
	for {
		code := statusCode(manageURL + "/api/status/online")
		auth, reachable := authStatus()

		tip := "客户端未运行"
		if reachable {
			if auth {
				tip = "已认证"
			} else {
				switch code {
				case 204:
					tip = "已联网 · 无需认证"
				case 302:
					tip = "待认证"
				default:
					tip = "未认证"
				}
			}
		}
		systray.SetTooltip("ESurfingClient · " + tip)
		time.Sleep(3 * time.Second)
	}
}

// statusCode 返回 HTTP 状态码（不跟随重定向），失败返回 -1
func statusCode(url string) int {
	req, _ := http.NewRequest("GET", url, nil)
	client := &http.Client{
		Timeout: 3 * time.Second,
		CheckRedirect: func(*http.Request, []*http.Request) error {
			return http.ErrUseLastResponse
		},
	}
	resp, err := client.Do(req)
	if err != nil {
		return -1
	}
	defer resp.Body.Close()
	return resp.StatusCode
}

// authStatus 读取 /api/status/auth，返回 (是否已认证, 是否可达)
func authStatus() (bool, bool) {
	req, _ := http.NewRequest("GET", manageURL+"/api/status/auth", nil)
	resp, err := (&http.Client{Timeout: 3 * time.Second}).Do(req)
	if err != nil {
		return false, false
	}
	defer resp.Body.Close()

	var a authResp
	if err := json.NewDecoder(resp.Body).Decode(&a); err != nil {
		return false, true
	}
	return a.Status, a.Reachable
}

// autostartPath 返回 XDG autostart desktop 文件路径
func autostartPath() string {
	home, _ := os.UserHomeDir()
	return filepath.Join(home, ".config", "autostart", "esurfing-tray.desktop")
}

func isAutoStart() bool {
	_, err := os.Stat(autostartPath())
	return err == nil
}

func enableAutoStart() {
	exe, _ := os.Executable()
	exe, _ = filepath.Abs(exe)
	content := fmt.Sprintf(`[Desktop Entry]
Type=Application
Name=ESurfingTray
Comment=ESurfingClient 系统托盘
Exec=%s
X-GNOME-Autostart-enabled=true
`, exe)
	os.MkdirAll(filepath.Dir(autostartPath()), 0o755)
	os.WriteFile(autostartPath(), []byte(content), 0o644)
	notify("开机自启", "已启用")
}

func disableAutoStart() {
	os.Remove(autostartPath())
	notify("开机自启", "已关闭")
}

func toggleAutoStart(m *systray.MenuItem) {
	if m.Checked() {
		disableAutoStart()
		m.Uncheck()
	} else {
		enableAutoStart()
		m.Check()
	}
}

// stopClient 停止 ESurfingClient 进程
func stopClient() {
	exec.Command("pkill", "-f", "ESurfingClient").Run()
}

// notify 发送桌面通知（若 notify-send 存在）
func notify(title, msg string) {
	exec.Command("notify-send", title, msg).Start()
}
