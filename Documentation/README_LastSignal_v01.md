# 最後訊號 Last Signal v01

專案：`C:\Users\User\Documents\GPT6 for unity\hw1012`

場景：`Assets/CourseGame/Generated/LastSignal_v01.unity`

## 遊玩

1. Unity 選單 **Course Game → Open Last Signal**。
2. 按 Play，點 Game 畫面，再按 Enter 或「開始演練」。
3. WASD 移動，兩個武器槽自動攻擊最近的敵人。
4. 生存 180 秒即撤離成功。每擊殺 8 隻敵人升級、提升射速並回復 18 點生命。
5. ESC 暫停／繼續；R 重新開始；M 開關音效。

## 教材對應

| 教材 | 完成內容 |
| --- | --- |
| 20260914 | Universal 3D、CharacterController、WASD、重力與接地 |
| 20260921 / 20260917 package | 武器槽環狀排列、Enemy Tag、最近目標索敵、Rigidbody 投射物、傷害與死亡 |
| 20261005 | characterMedium 骨架模型、Idle / Run、依實際速度轉向、C# CrossFade，沒有 Animator Parameter / Transition |
| 20261012 | CameraFollow、3×3 無限地板、EnemySpawner、EnemyPool、ProjectilePool、多模板子彈、生命與冷卻重設、死亡及超出地板範圍回收 |

教材文字與 package 檔案清單保存在 `Documentation/CourseInventory_v01.txt`。原始教材位於使用者提供的 E 槽資料夾，未變更。

## 檔案

- `Assets/CourseGame/Generated`：可遊玩場景、材質、Animator Controller 與 Prefab。
- `Assets/CourseGame/SourceAnimations`：教材動畫的專案內副本，已設定循環。原始 Art 動畫保留。
- `Assets/CourseGame/Scripts`：開始／暫停／勝敗／重開、HUD、升級與音效，以及驗證元件。
- `Assets/CourseGame/Editor/CourseBuilder.cs`：可重建場景的 Editor 工具。
- `Documentation/OriginalScripts_v01`：修改前原始腳本備份，採 `.cs.txt` 避免重複編譯。
- `CourseGameVerification`：Play Mode 測試、狀態與畫面證據。

原本 `Assets/Scenes/SampleScene.unity` 保留。Build Settings 將 Last Signal 設為第一個場景，保留原 SampleScene。

## 主要設定

- Player：生命 100、移速 5、角色高 1.7 公尺。
- Enemy：生命 30、移速 2.55、接觸傷害 9、攻擊冷卻 1.2 秒。
- Weapon：2 個模板、傷害 15、射程 9.5、速度 18、初始間隔 0.46 / 0.58 秒。
- Ground：12 公尺地板，共 9 塊重複使用。
- EnemyPool：預建 30、最多 40；場上敵人上限隨時間增加但不超過 30。
- ProjectilePool：每模板預建 24、最多 80；命中或 1.6 秒壽命結束回收。

CourseSession 每 30 秒提高生成頻率。升級最低攻擊間隔為 0.08 秒。

## 修正

- DeadModule 的玩家死亡路徑通知 GameManager，保留玩家引用供結算使用。
- WeaponSlot 停止於暫停與死亡狀態，使用 NonAlloc 搜尋，瞄準 Collider 中心。
- PlayerMovement 在 GameManager 結束狀態停止移動。
- 教材 Generic 動畫以骨架 Root 為根，Animator 掛在骨架 Root，模型縮放位於 Animator 之外，避免動畫重寫縮放。

## 素材狀態

目前教材足以完成可玩的課堂遊戲。教材未提供配樂、專用命中／死亡動畫與主選單插圖。此版本使用簡短合成音效與現有 Idle／Run 動畫；這些項目可作為後續美術與音效擴充。
