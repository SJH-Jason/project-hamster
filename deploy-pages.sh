#!/bin/bash
# 把 Blazor WASM 網頁版 publish 成 GitHub Pages 可用的靜態檔。
# 用法：./deploy-pages.sh <repo-name>   例如 ./deploy-pages.sh project-hamster
# 產出在 ./publish/wwwroot ，可直接推到 gh-pages 分支 / Pages。
set -e
REPO="${1:-project-hamster}"
export DOTNET_ROOT="/opt/homebrew/opt/dotnet/libexec"
export PATH="$PATH:/opt/homebrew/opt/dotnet/libexec"
cd "$(dirname "$0")"

# 同步最新 data 進 wwwroot(避免用到舊副本)
cp data/*.json src/HamsterRace.Web/wwwroot/data/ 2>/dev/null
rm -rf publish
dotnet publish src/HamsterRace.Web -c Release -o publish --nologo

OUT=publish/wwwroot
# GitHub Pages 專案站在 /<repo>/ 底下 → base href 要對
sed -i '' "s|<base href=\"/\" />|<base href=\"/$REPO/\" />|" "$OUT/index.html"
# SPA fallback：找不到路徑時回 index
cp "$OUT/index.html" "$OUT/404.html"
# 讓 Pages 不要用 Jekyll 處理（否則 _framework 底線開頭資料夾會被忽略）
touch "$OUT/.nojekyll"

echo "✅ 靜態檔在 $OUT （base href = /$REPO/）"
echo "   本機預覽：cd $OUT && python3 -m http.server 8080  → http://localhost:8080/$REPO/ 不適用，本機用 dotnet run 測即可"
