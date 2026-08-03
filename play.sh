#!/bin/bash
# 🐹 一鍵開玩 Project Hamster（教練模式，手動組牌）
# 用法：./play.sh
export DOTNET_ROOT="/opt/homebrew/opt/dotnet/libexec"
export PATH="$PATH:/opt/homebrew/opt/dotnet/libexec"
cd "$(dirname "$0")"
dotnet run -c Release --project src/HamsterRace.Console -- play
