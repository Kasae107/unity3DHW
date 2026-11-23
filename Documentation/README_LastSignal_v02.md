# 最後訊號 Last Signal v02

完整且最新的專案說明請見 [`../README.md`](../README.md)。11/09 已加入敵人等級強化與生存計時器；11/23 已加入升級三選一。目前主場景仍為 LastSignal_v02。

專案：`C:\Users\User\Documents\GPT6 for unity\hw1012`

場景：`Assets/CourseGame/Generated/LastSignal_v02.unity`

## 遊玩

1. Unity 選單 **Course Game → Open Last Signal**。
2. 按 Play，點 Game 畫面，再按 Enter 或「開始演練」。
3. WASD 移動，兩個武器槽自動攻擊最近的敵人。
4. 生存 180 秒即撤離成功。每擊殺 1 隻敵人取得 1 EXP；首次升級需 5 EXP，之後需求向上取整增加 10%。每次升級暫停戰鬥，三選一：生命上限與目前生命 +20、新子彈傷害 +2、移動速度 +0.5。連升多級會依序選擇。
5. ESC 暫停／繼續；R 重新開始；M 開關音效。

## 教材對應

| 教材 | 完成內容 |
| --- | --- |
| 20260914 | Universal 3D、CharacterController、WASD、重力與接地 |
| 20260921 / 20260917 package | 武器槽環狀排列、Enemy Tag、最近目標索敵、Rigidbody 投射物、傷害與死亡 |
| 20261005 | characterMedium 骨架模型、Idle / Run、依實際速度轉向、C# CrossFade，沒有 Animator Parameter / Transition |
| 20261012 | CameraFollow、無限地板、EnemySpawner、EnemyPool、ProjectilePool、多模板子彈、生命與冷卻重設、死亡及超出地板範圍回收 |
| 20261019 / MultiPrefab_Obstacle_Lesson | 三種障礙物 Prefab、固定世界格子種子、重疊格子維持原位、外圈預載、依種類快取重用 |
| 20261019 / Unity3D_HP_EXP | PlayerExperience、每次擊殺 1 EXP、5/6/7…升級門檻、PlayerHUD 的 HP／EXP Slider、Legacy Text、敵人 World Space 血條與鏡頭朝向 |

教材文字與 package 檔案清單保存在 `Documentation/CourseInventory_v01.txt`。原始教材位於使用者提供的 E 槽資料夾，未變更。

## 檔案

- `Assets/CourseGame/Generated`：可遊玩場景、材質、Animator Controller 與 Prefab。
- `Assets/CourseGame/SourceAnimations`：教材動畫的專案內副本，已設定循環。原始 Art 動畫保留。
- `Assets/CourseGame/Scripts`：開始／暫停／勝敗／重開、HUD、升級與音效，以及驗證元件。
- `Assets/CourseGame/Editor/CourseBuilder.cs`：可重建場景的 Editor 工具。
- `Documentation/OriginalScripts_v01`：修改前原始腳本備份，採 `.cs.txt` 避免重複編譯。
- `CourseGameVerification`：Play Mode 測試、狀態與畫面證據。

原本 `Assets/Scenes/SampleScene.unity` 保留。Build Settings 將 Last Signal 設為第一個場景，保留原 SampleScene。

`LastSignal_v01.unity` 是製作過程保留的早期場景；請使用 v02。v02 修正了教材動畫的根節點配置與角色比例。

## 已完成驗證

11/09 更新後，`CourseGameVerification/PlayMode-results-v02.txt` 的 86 項檢查全部通過。涵蓋原有的移動、動畫、戰鬥、生成、回收、暫停、勝敗、重開，以及三種障礙物、預載範圍、世界格子往返一致性、快取上限、子彈撞障礙物、HP／EXP Slider、敵人血條、EXP、1～10 級敵人成長與生存計時器。

功能測試內的倒數測試使用縮短的目標時間。`Endurance-v01.txt` 是 10/12 版本正常時間完整 180 秒的遊玩測試紀錄，未重複用來宣稱 10/19 擴充版通過完整三分鐘實玩。

`Runtime-errors-v01.txt` 保留早期製作時的 Animator 配置錯誤，修正後的測試沒有這些錯誤。Play Mode 的錯誤另由 `PlayMode-errors-v01.txt` 收集，沒有錯誤時不產生該檔。

## 主要設定

- Player：生命 100、移速 5、角色高 1.7 公尺。
- Enemy：生命 30、移速 2.55、接觸傷害 9、攻擊冷卻 1.2 秒。
- Weapon：2 個模板、傷害 15、射程 9.5、速度 18、初始間隔 0.46 / 0.58 秒。
- Ground：12 公尺地板，內圈半徑 2（5×5）供鏡頭與戰鬥使用，額外預載 1 圈，共 49 塊重複使用；預載外圈不計入敵人生成範圍。
- EnemyPool：預建 30、最多 40；場上敵人上限隨時間增加但不超過 30。
- ProjectilePool：每模板預建 24、最多 80；命中或 1.6 秒壽命結束回收。

CourseSession 每 30 秒提高生成頻率。升級不再自動增加射速或回復生命；依所選獎勵強化。

## 修正

- 10/19 直接更新現有 `LastSignal_v02.unity`、Ground／Enemy Prefab 與腳本，沒有另存新版場景。保留現有場景與材質調整。
- 新增 SupplyCrate、Barricade、SignalPillar 三種障礙物 Prefab。每個節點同時啟用一種，離開後返回同一世界格子，種類、位置與旋轉維持一致。
- 障礙物具備實體碰撞；敵人出生點避開障礙物，子彈碰撞障礙物會回池。敵人仍沿用教材的直接追蹤方式，這次教材未加入障礙物尋路。
- 經驗值由 GameManager 監聽敵人死亡後發放；單純離場回收不發放，避免將擊殺與回收混淆。

- DeadModule 的玩家死亡路徑通知 GameManager，保留玩家引用供結算使用。
- WeaponSlot 停止於暫停與死亡狀態，使用 NonAlloc 搜尋，瞄準 Collider 中心。
- PlayerMovement 在 GameManager 結束狀態停止移動。
- 教材 Generic 動畫以骨架 Root 為根，Animator 掛在骨架 Root，模型縮放位於 Animator 之外，避免動畫重寫縮放。

## 素材狀態

目前教材足以完成可玩的課堂遊戲。教材未提供配樂、專用命中／死亡動畫與主選單插圖。此版本使用簡短合成音效與現有 Idle／Run 動畫；這些項目可作為後續美術與音效擴充。

