# Phase 0 共用契約定案

> 本文件是 4 位 Agent（A1 Ballistics／A2 Combat+Terrain／A3 AI+Economy／A4 Turn Flow+Data）
> 共同討論後的定案結果，作為 Phase 1 平行開發的依據。任何人若要修改本文件定義的共用型別，
> 必須先知會其他 3 位 Agent，因為變更會同時影響多個模組。

## 1. 基礎慣例

- 座標系統：`UnityEngine.Vector2`，世界座標，X 軸向右為正、Y 軸向上為正，1 unit = 1 公尺。
- 角度：以水平軸（X 軸正方向）為基準，範圍 0°~180°（對應 Spec US-04）。
- 地圖 X 座標範圍固定從 `0` 到 `MapWidth`（不置中），簡化 `IMapScaleCalculator` /
  `ITankSpawnDistributor` 的回傳值語意。
- 亂數：全專案共用 `IRandomSource`（見 §2.2），由「對戰協調層」（Turn/Match Flow，最終會在
  Gameplay 層的 BattleCoordinator）建立一顆帶種子的實例，分派給 Wind、AI、行動順序、地形生成等
  模組使用。任何 Core 邏輯類別**禁止**在內部 `new System.Random()`。
- 設定注入：所有 Core 邏輯類別一律以建構子/方法參數注入設定資料，不得在 Core 內直接
  `Resources.Load` 或引用 ScriptableObject 靜態實例，以利 NUnit 測試 mock。

## 2. 模組邊界與資料流向

### 2.1 Ballistics（A1）
- 對外提供「正向模擬」為主：`IBallisticsSimulator.Advance(...)`（逐步積分，供 Projectile 播放
  飛行動畫）與 `Simulate(...)`（一次算完，供 AI 試算候選解）。
- 額外提供 `IBallisticsEstimator.EstimateNoWind(...)`：無風力解析解，作為 AI 搜尋的初始猜測值，
  加速收斂，不保證精確。
- **只判斷地形碰撞**（透過 `ITerrainQuery`），**不判斷坦克碰撞**——坦克碰撞由 Gameplay 層每幀用
  `ITankState.Position` 做簡單距離/半徑檢查，避免 Ballistics 依賴「取得所有坦克」的介面。
- 風力產生時機：由 Turn/Match Flow 在「輪到某坦克發射前」呼叫 `IWindProvider.GenerateNewWind()`
  一次；Ballistics 本身是無狀態純函式，只接受 `WindData` 參數。

### 2.2 Combat + Terrain（A2）
- Terrain 採 **Heightmap**（一維高度陣列）而非分割碰撞體，理由：圓形炸坑對高度陣列操作簡單、
  效能可預期、序列化容易。
- Terrain 對外提供 `ITerrainQuery`（唯讀，供 Ballistics/AI/Gameplay 查詢）與 `ITerrainCarver`
  （唯一的破壞地形入口，`CarveCrater` 邊界情況：超出地圖邊界直接 clamp、半徑 ≤0 為 no-op、
  重複挖同一區域取聯集，皆不拋例外）。
- **爆炸結算採單一入口**：Combat 提供 `IExplosionResolver.Resolve(request)`，內部依序執行
  「炸地形 → 找出範圍內坦克 → 算傷害 → 套用 TakeDamage」，Gameplay 層偵測到 Ballistics 回報
  命中後，只需呼叫這一個方法，不必自行協調呼叫順序（解決「誰先炸地形、誰先算傷害」的疑慮）。
- `ITankHealth.OnEliminated` 為同步、冪等事件（跨越 0 時觸發一次，重複呼叫 `TakeDamage` 不會
  重複觸發）。Turn Flow 的事件處理者只應該「登記/移除」，不應該在事件中反過來遍歷仍在使用中的
  集合。
- 傷害公式的火力倍率由呼叫端（ExplosionResolver）透過 `IUpgradeEffectResolver`（Economy 提供）
  查出後，寫入 `DamageContext.FirepowerMultiplier` 傳入，Combat 不反查 Economy 服務。

### 2.3 AI + Economy（A3）
- AI 決策依賴 Ballistics 的**正向模擬**（`IBallisticsSimulator`）自行做搜尋（如角度固定對威力做
  二分搜尋），並用 `IBallisticsEstimator.EstimateNoWind` 當初始猜測值加速收斂；限制最大迭代次數
  以確保限時內完成。
- 限時控制採 `System.Threading.CancellationToken`：Turn/Match Flow 呼叫 `IAIStrategy.DecideAim`
  時傳入 token，AI 定期檢查是否取消，逾時回傳 `AimResult.Success = false`（由呼叫端視為跳過）。
- Economy 只管等級與金錢；等級換算成實際效果數值（火力倍率、移動速度倍率）由 Economy 提供的
  `IUpgradeEffectResolver` 統一負責，Combat/Gameplay 只消費，不重算。
- 持久化：`ISaveDataRepository` 介面放在 `Core/Economy`；實際呼叫 `PlayerPrefs` +
  `JsonUtility` 的實作放在 `Scripts/Gameplay`（Unity 平台 API，不放 Core），NUnit 測試用
  in-memory fake 取代。

### 2.4 Turn/Match Flow + Data（A4）
- `ITurnParticipant` 包裹 `ITankState`，不重新定義「存活」語意。
- 玩家淘汰即可提前判定 `PlayerDefeat`，不必等到只剩最後一個 AI（效能優化，需與 Combat 的淘汰
  事件時機一致）。
- `BalanceConfig`（ScriptableObject）**只放跨模組協調用欄位**：回合限時秒數、地圖基礎寬度、
  單位間距、最小安全間距、AI 人數上下限。其餘（風力範圍、傷害係數、升級花費曲線、地形隨機參數、
  AI 難度誤差參數）由各自模組擁有獨立 Config，降低平行開發衝突面。

## 3. 共用型別清單（實作於 `Assets/Scripts/Core/Shared`）

| 型別 | 說明 |
|---|---|
| `Faction` | `Player` / `AI` |
| `ITankState` | 唯讀坦克狀態：Id、Faction、Position、CurrentHp、MaxHp、IsAlive |
| `IRandomSource` / `SeededRandomSource` | 全域亂數注入介面與預設實作 |
| `WindData` | 單一 `SignedValue`（-10~10），不拆方向/強度 |
| `IWindProvider` | `GenerateNewWind()` / `CurrentWind` |
| `LaunchParameters` | 角度/威力/砲口位置/初速，含 `Clamp` |
| `TrajectoryPoint` / `TrajectoryState` | 逐步模擬狀態 |
| `ImpactType` / `ImpactInfo` | 命中類型（Terrain/Tank/OutOfBounds）與命中資訊 |
| `ITerrainQuery` | 唯讀地形查詢（IsSolidAt、GetSurfaceHeight、TryGetCollision、GetWorldBounds） |
| `ITerrainCarver` | 破壞地形唯一入口（CarveCrater） |
| `TerrainModificationResult` | 破壞地形後的受影響範圍回報 |

各模組專屬介面（Phase 1 由對應 Agent 實作）放在各自資料夾：`Core/Ballistics`、
`Core/Combat`、`Core/Terrain`（實作 `ITerrainQuery`/`ITerrainCarver` 的具體類別）、
`Core/AI`、`Core/Economy`、`Core/TurnFlow`。

## 4. 資產夾/命名慣例

- asmdef：`TankBattle.Core`（涵蓋 `Scripts/Core/**` 與 `Scripts/Data/**`）、
  `TankBattle.Gameplay`、`TankBattle.UI`、`TankBattle.Tests.EditMode`、
  `TankBattle.Tests.PlayMode`。
- ScriptableObject `CreateAssetMenu` 統一前綴 `TankBattle/Data/...`，避免 Inspector 選單混亂。
- 各模組獨立 Config 命名慣例：`<Module>Config`（例如 `WindConfig`、`DamageConfig`、
  `UpgradeConfig`、`AIDifficultyConfig`、`TerrainConfig`）。

## 5. Git Worktree 分工（一路跟到底）

| 分支 | Worktree 目錄 | 負責 Agent | Phase 1 範圍 | Phase 2 範圍 | Phase 3 範圍 |
|---|---|---|---|---|---|
| `feature/agentA-ballistics` | `../TankBattle_AI-agentA` | A1 | Core/Ballistics | Gameplay/Projectile | UI/戰鬥HUD |
| `feature/agentB-combat-terrain` | `../TankBattle_AI-agentB` | A2 | Core/Combat, Core/Terrain | Gameplay/Tank, 地形渲染 | UI/結算畫面 |
| `feature/agentC-ai-economy` | `../TankBattle_AI-agentC` | A3 | Core/AI, Core/Economy | Gameplay/AIController, 商店串接, 存讀檔 | UI/商店UI |
| `feature/agentD-turnflow-data` | `../TankBattle_AI-agentD` | A4 | Core/TurnFlow, Data(BalanceConfig) | Gameplay/TurnManager, 地圖生成 | UI/主選單+場景切換 |
