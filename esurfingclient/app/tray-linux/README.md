# ESurfingTray (Linux)

ESurfingClient 的 Linux 系统托盘工具，功能与 Windows 版 [tray](../tray/) 对齐。

## 功能

- **打开管理页面**：点击菜单项，用默认浏览器打开 http://127.0.0.1:8888
- **强制重认证**：调用 `POST /api/restartAuth` 重启认证进程
- **开机自启**：写入 `~/.config/autostart/esurfing-tray.desktop`
- **状态轮询**：每 3 秒轮询认证 / 联网状态，更新托盘悬浮提示
- **退出**：`pkill -f ESurfingClient` 停止客户端

## 依赖

- Go 1.21+
- Linux 桌面托盘后端之一（按发行版安装）：
  - Debian/Ubuntu: `sudo apt install libayatana-appindicator3-dev`
  - Fedora: `sudo dnf install libappindicator-gtk3-devel`
  - Arch: `sudo pacman -S libappindicator-gtk3`
- 桌面通知：`notify-send`（libnotify，可选）

## 编译

```bash
./build.sh
# 或手动：
go mod tidy
go build -ldflags "-s -w" -o esurfing-tray .
```

产物 `esurfing-tray`（约 3~4 MB，单二进制）生成在本目录。

## 运行

```bash
./esurfing-tray
```

## 交叉编译（在其他平台编 Linux 二进制）

```bash
CGO_ENABLED=0 GOOS=linux GOARCH=amd64 go build -ldflags "-s -w" -o esurfing-tray-linux-amd64 .
CGO_ENABLED=0 GOOS=linux GOARCH=arm64 go build -ldflags "-s -w" -o esurfing-tray-linux-arm64 .
```

> 注意：托盘后端需要 CGO + appindicator 库，`CGO_ENABLED=0` 仅当使用纯 Go 后端时才可；实际 Linux 桌面通常需保留 CGO 并在目标平台编译。
