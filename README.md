# ESurfingClient

> 纯 C11 编写的天翼校园网（ESurfing）认证客户端 —— 超轻量、跨平台、跨架构。

[![GitHub release](https://img.shields.io/github/v/release/TwiLight-NOT-Found/ESurfingClient)](https://github.com/TwiLight-NOT-Found/ESurfingClient/releases/latest)
[![License](https://img.shields.io/badge/license-Apache--2.0-blue.svg)](https://github.com/TwiLight-NOT-Found/ESurfingClient/blob/main/LICENSE)
[![GitHub stars](https://img.shields.io/github/stars/TwiLight-NOT-Found/ESurfingClient)](https://github.com/TwiLight-NOT-Found/ESurfingClient)

## 简介

ESurfingClient 是一个跨平台的**广东**天翼校园网认证客户端，参考 [Rsplwe](https://github.com/Rsplwe) 的 Kotlin 源码思路，用**纯 C11** 重写，约 2.3 万行代码。桌面端采用 `supervisor` 多进程架构并自带 Web 面板，OpenWrt 端采用 `procd` 多实例并自带 LuCI。

核心优势：

- 🪶 **超轻量** —— 主程序仅 200–300 KB
- 🌐 **跨平台跨架构** —— 见下方支持列表
- 📦 **开箱即用** —— 桌面端 Web 面板 / OpenWrt 端 LuCI
- 🔀 **多 WAN 多拨** —— 每账号独立 mark，nftables 按 mark 分流
- 🧵 **低占用** —— 单账号常驻仅约 500 KB 内存

## 特性

- 自动认证 / 心跳保活 / 主动登出（`auth.cgi` → `ticket.cgi` → `keep.cgi` → `term.cgi`）
- 多账号多实例，互不影响
- Web 控制面板（桌面端）+ LuCI 面板（OpenWrt）
- 配置热重载，日志轮转
- `generate_204` 并发探测，固定 IP 直连（不发 DNS）

## 支持平台与架构

### 主程序

|   系统   |               架构                | 包管理器  |      理论最低支持版本      |        推荐版本         |
|:--------:|:---------------------------------:|:---------:|:--------------------------:|:-----------------------:|
| Windows  |        x86_64 / x86 / aarch64     |  portable |     Windows Vista x64      |     Windows 10 x64      |
|  Linux   | x86_64 / aarch64 / armv7 / loongarch64 | portable |        glibc 2.36          |       glibc 2.39        |
| Android  |   x86_64 / arm64-v8a / armeabi-v7a | portable |         Android 7          |       Android 10        |
|  macOS   |              x86_64              |  portable |         macOS 12           |        macOS 13         |
|  macOS   |             aarch64              |  portable |         macOS 13           |        macOS 14         |
| OpenWRT  | [All](docs/OpenWRT_targets.md)   |  opkg/apk | OpenWRT 15.05 / 25.12.0-rc1 | OpenWRT 19.07.0 / 25.12.0 |

### OpenWrt LuCI 插件包

|   系统   |              架构              | 包管理器 |      理论最低支持版本      |        推荐版本         |
|:--------:|:------------------------------:|:--------:|:--------------------------:|:-----------------------:|
| OpenWRT  | [All](docs/OpenWRT_targets.md) | opkg/apk | OpenWRT 15.05 / 25.12.0-rc1 | OpenWRT 19.07.0 / 25.12.0 |

### OpenWrt 架构自查

**opkg 包管理器**

```shell
opkg print-architecture
# 示例输出
arch all 1
arch noarch 1
arch mipsel_24kc 10   # mipsel_24kc 即为要下载的架构
```

**apk 包管理器**

```shell
cat /etc/apk/arch
# 示例输出
mipsel_24kc   # mipsel_24kc 即为要下载的架构
```

> 实在找不到可以在这里查 → [OpenWRT 目标平台自查教程](docs/OpenWRT_targets.md)

## 源码构建

依赖 libcurl。版本号唯一真相源是 `esurfingclient/app/CMakeLists.txt`。

```bash
# 桌面端（Windows / Linux / macOS / Android）
cmake -S esurfingclient/app -B build -DCMAKE_BUILD_TYPE=Release
cmake --build build -j

# 仅验证 OpenWrt 代码路径
cmake -S esurfingclient/app -B build-openwrt -D__OPENWRT__=1
cmake --build build-openwrt -j
```

> 改完版本号后请执行 `scripts/sync-version.sh`，前端构建使用 `scripts/build-portal.sh`。

## 文档

| 文档 | 说明 |
|------|------|
| [Windows / Linux / macOS 使用教程](docs/Desktop.md) | 桌面端环境 |
| [OpenWRT 使用教程](docs/OpenWRT.md) | 路由端环境 |
| [OpenWRT 多拨教程](docs/OpenWRT_mwan3.md) | MWAN3 进阶 |
| [自行编译教程](docs/Compile.md) | 源码构建 |
| [目标平台自查教程](docs/OpenWRT_targets.md) | 架构查询 |
| [Q&A](docs/Q&A.md) | 常见问题 |

## 更新日志

见 [CHANGELOG.md](CHANGELOG.md)。

## 资源占用

实测自用 K2P 路由器（单账号，另装 MWAN3）：

|    阶段    | VmPeak | VmSize | VmHWM | VmRSS |
|:----------:|:------:|:------:|:-----:|:-----:|
|   初运行   | 912 kB | 800 kB | 536 kB | 536 kB |
| 运行十天后 | 1088 kB | 884 kB | 668 kB | 564 kB |

运行十天后内存占用仅增加约 28 KB，4 级日志轮换后约 1 MB。

## 联系方式

- GitHub: [TwiLight-NOT-Found](https://github.com/TwiLight-NOT-Found)
- QQ: [点此查看二维码](https://twilight-cdisk.site:8443/f/MWIl/Image_1795600400738_280.png)
- 微信: [点此查看二维码](https://twilight-cdisk.site:8443/f/0WC5/mmqrcode1795600324955.png)

## 免责声明

本程序仅用于在不同平台登录校园网，仅供学习参考。

> [!WARNING]
> - 不负责包括但不限于无视用户数限制登录等不合规操作
> - `非广东` 省地区因认证流程不同，不一定可用
> - 请勿用于任何商业贩卖行为

## 致谢

- [Rsplwe](https://github.com/Rsplwe) —— Kotlin 源码思路
- [cJSON](https://github.com/DaveGamble/cJSON) —— 配置文件解析
- [mongoose](https://github.com/cesanta/mongoose) —— Web 服务器
- [libcurl](https://github.com/curl/curl) —— 网络请求
- [Ironjhin](https://github.com/Ironjhin) —— [EsurfingClient_Android](https://github.com/Ironjhin/EsurfingClient_Android)（安卓安装包/面具模块）
