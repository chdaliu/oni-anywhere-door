#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "$0")"

export DOTNET_ROOT="${DOTNET_ROOT:-$HOME/.dotnet}"
export PATH="$DOTNET_ROOT:$PATH"

# 生成建筑动画（仅依赖 Pillow）。如果还没有安装 Pillow，可先执行：
#   python3 -m pip install pillow
# 没有 Pillow 时跳过生成，直接使用仓库内已经生成的动画资源。
if command -v python3 >/dev/null 2>&1 && python3 -c "import PIL" >/dev/null 2>&1; then
    python3 tools/generate_door_anim.py
else
    echo "跳过动画生成：未找到 python3 或 Pillow。"
fi

dotnet build -c Release "$@"

echo
echo "已部署到：$HOME/Library/Application Support/unity.Klei.Oxygen Not Included/mods/Local/ONI-AnywhereDoor"
