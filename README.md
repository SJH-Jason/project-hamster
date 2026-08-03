# 🐹 Project Hamster

**▶️ 線上試玩（手機/電腦，免安裝）：https://sjh-jason.github.io/project-hamster/**


鼠鼠競賽模擬遊戲。玩家扮演教練:配牌、判讀環境（賽道×天氣×風向×時段）、設定技能自動施放條件，讓鼠鼠自動跑完比賽，再從文字戰報分析輸贏。

> **現況:MVP 開發中（Stage 1 完成）。** 目標是先用純文字、確定性模擬驗證「配牌×環境判讀×戰報分析」好不好玩，之後才做養成/劇情/美術/主機版。
> 完整規劃見 Sandbox `works/active/TASK-002-project-hamster.md`。

## 技術

- **C# / .NET 10**（選 C# 是為了日後引擎能零重寫搬進 Unity 上 PC/Switch）
- 架構:核心與表現分離、規則資料化（JSON）、確定性模擬（同 seed 同結果）

## 專案結構

```
src/
  HamsterRace.Domain/       型別:Hamster、RaceConfig、RaceResult、確定性亂數
  HamsterRace.Simulation/   引擎:RaceSimulator(tick)、TextReport、DataLoader
  HamsterRace.Console/      CLI 進入點
tests/
  HamsterRace.Simulation.Tests/   確定性與完賽測試
data/
  hamsters.json             4 隻原型鼠數值(data-driven)
```

## 跑跑看

```bash
# 跑一場 100m,seed 42
dotnet run --project src/HamsterRace.Console -- 42 100

# 測試
dotnet test
```

## 開發階段（Stage）

- ✅ **Stage 0** 地基:資料模型＋確定性亂數
- ✅ **Stage 1** 純模擬核心:tick 引擎、基礎跑速、名次＋文字戰報
- ✅ **Stage 2** 卡牌與資源:卡牌依階段觸發、HP/MP 消耗與補給、體力衰減、資源不足連續遞減
- ✅ **Stage 3** 環境系統:賽道/天氣/風向/時段適性、預報→鎖牌抽實際、環境條件卡（押天氣）
- ✅ **Stage 4** 技能系統:2 主動技能、自動觸發條件、冷卻、親密度成功率（熟練度/親密度先固定，突破待做）
- ✅ **Stage 5** 批次平衡:10,000 場統計勝率（依天氣拆分）、平衡參數 rules.json 資料驅動
- ✅ **Stage 6** 互動介面:教練模式（看情報→選鼠→手動配 6 張牌→開賽→戰報→重玩）

> **🎉 MVP（Stage 0–6）完成。** 核心玩法（配牌 × 資源 × 環境/押天氣 × 技能）已驗證好玩且平衡。

## 玩玩看（手動組牌）

```bash
dotnet run --project src/HamsterRace.Console -- play
```
選一隻鼠當教練、從 12 張牌手動配 6 張、開賽看戰報，換牌或賭天氣再來一場。

## 批次平衡

```bash
dotnet run -c Release --project src/HamsterRace.Console -- batch 10000 100
```
輸出各鼠總勝率＋依天氣拆分的勝率。目前平衡:泡芙(一般王)/麻糬(大雨王)/黑糖(大晴天王)/花生(穩定第二)，無全環境最強鼠。調 `data/rules.json` 與各鼠數值即可重新平衡。
