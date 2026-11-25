# ESurfingTray

ESurfingClient 的 Windows 系统托盘工具，提供托盘入口、状态监控与快捷操作。

## 功能

- **打开管理页面**：双击托盘图标或菜单项，打开 http://127.0.0.1:8888
- **强制重认证**：调用 `POST /api/restartAuth` 重启认证进程
- **开机自启**：写入 `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`
- **状态窗口**：3 秒轮询认证 / 联网状态
- **退出**：优雅停止 + 强杀兜底

## 编译

运行 `build.ps1`，使用 Windows 自带的 .NET Framework 4.x `csc.exe`，无需安装任何 SDK。

```powershell
./build.ps1
```

产物 `ESurfingTray.exe`（约 60 KB）生成在本目录，单文件、零依赖，任何 Windows 可直接运行。

## 目录结构

```
tray/
├── Program.cs      # 托盘主程序（WinForms）
├── icon.ico        # 托盘图标（dashboard.svg 渲染）
├── open.png        # 「打开管理页面」菜单图标
├── exit.png        # 「退出」菜单图标
├── build.ps1       # 一键编译脚本
└── tools/          # 图标生成工具链
    ├── icon.html           # SVG → 256px PNG（Edge headless 渲染）
    ├── icon256.png         # 256px 中间产物
    ├── make-ico.ps1        # icon256.png → 多尺寸 icon.ico
    └── make-menu-icons.ps1 # 生成菜单图标（open / exit）
```

## 重新生成图标

```powershell
# 1. 菜单图标（需要本机安装 Edge）
./tools/make-menu-icons.ps1

# 2. 托盘图标
#    先用 Edge headless 渲染 tools/icon.html 得到 tools/icon256.png
#    再运行：
./tools/make-ico.ps1
```

## 依赖

仅 .NET Framework 4.x（WinForms），无第三方库。
