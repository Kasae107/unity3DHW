# Last Signal v02 verification - November 23 update

- Scene: `Assets/CourseGame/Generated/LastSignal_v02.unity`
- Unity: 6000.6.2f1, open hw1012 editor.
- Current gameplay verification: 107 PASS, 0 FAIL in `PlayMode-results-v02.txt`, completed in the open Unity Editor on November 23.
- November 23 checks: one event per gained level; native three-choice buttons; configurable and repeatable health/damage/movement rewards; queued levels; freeze and exact prior-time-scale restoration; manual-pause deferral; victory/death cancellation; unchanged projectile templates and in-flight shots; correct damage after projectile pool reuse. Previous automatic healing/fire-rate bonuses were removed.
- Native mouse smoke check: clicked the damage reward in the actual Game view; the panel closed, the selected-reward notice appeared and battle resumed. Reset to the welcome screen afterward.
- November 9 checks: level-1 baseline and explicit level-2 through level-10 HP/damage/speed values; no healing when existing enemies grow; zero Inspector deltas; recalculation after restoring deltas; no accumulated bonuses after 60 pool reuses; full upgraded HP on spawn; upgraded health bar normalization; no EXP from recycling.
- Timer checks: welcome stays at zero, explicit run start, one shared time source, scaled frame-time accumulation, MM:SS minute rollover, pause freezes both time and text, resume preserves time, victory/death stop once, and scene restart resets timer and EXP.
- New checks cover three obstacle types, collision, 49 pooled/preloaded grounds, separate combat bounds, retained overlap cells, deterministic revisits, bounded per-prefab caches, HP/EXP Slider geometry and values, enemy health bars/camera facing, exact EXP rewards, 5/11-kill level boundaries, no EXP from recycling, and restart resetting EXP.
- Existing movement, animation, projectile collision, contact damage, pooling, pause, victory (short timer), death and restart checks also pass.
- November 9 live UI smoke check: started through the actual menu button; elapsed time and countdown appeared together. Standing still reached level 4 with 19 kills, then died at 00:16; the live timer and result time matched and stopped. No runtime errors were captured. This was not a complete three-minute survival run.
- Current pools: 30 prewarmed enemies, 48 prewarmed projectiles, 49 ground tiles. Active enemy cap: 30.
- No errors captured by the Play Mode error collector during final verification/endurance.
- Existing visual evidence is historical: `Game-preview-v01.png` and `Victory-preview-v01.png`. The November 23 native UI smoke check was inspected directly in the Editor.
- Updated existing LastSignal_v02, scripts and documentation in place. Added PlayerUpgradeStats, LevelUpRewardController, a connected uGUI reward panel, GraphicRaycaster and Input System EventSystem. Existing scene/material adjustments were preserved; no new scene version was created.

The earlier `Runtime-errors-v01.txt` preserves development errors. The 180-second endurance result in `Endurance-v01.txt` / `Endurance-v02.txt` belongs to October 12 and is not claimed as a new full-duration validation of the obstacle/EXP update.

This verifies the Unity Editor experience on the current Windows machine. A standalone executable, mobile input, and other target devices were not built or tested in this request.
