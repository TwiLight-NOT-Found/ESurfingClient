#!/usr/bin/env bash
# ESurfingTray (Linux) 一键编译脚本
set -e
cd "$(dirname "$0")"

go build -ldflags "-s -w" -o esurfing-tray .
echo "编译完成: $(pwd)/esurfing-tray"
