# 三类眼动案例底图

这三个案例使用用户提供的真实截图，并保留原始分辨率。采集时使用项目现有的 `ReadingViewForm` 全屏适配，不要拉伸或二次裁切；导出的 CSV 会记录屏幕尺寸，热力图和扫视图再按相同 FitRect 映射回原图。

## 正式采集底图

- `assets/taobao-home.png`（2511×1462）：淘宝首页。
- `assets/xiaomi-su7-home.png`（2536×1387）：小米 SU7 官网首屏。
- `assets/game-training.png`（2509×1566）：游戏训练场景。

## 仿真 Demo

`demo/` 中的 PNG 与 CSV 只用于验证叠图效果，不代表真实实验结果。三组 CSV 与现有 `GazeX`、`GazeY`、`DeviceTimestamp` 和 `#screen` 数据结构兼容。

- `taobao-scanpath-demo.png`：搜索栏 → 纸巾商品 → 电脑广告 → 搜索栏 → 搜索按钮，并加入局部复看。
- `xiaomi-su7-heatmap-demo.png`：车体、人脸、SU7 标题为主要热点，同时保留品牌签名、价格与按钮等弱热点。
- `xiaomi-su7-scanpath-demo.png`：第 1 次车体、第 2 次人脸、第 3 次 SU7 标题，之后加入自然探索与复看。
- `game-scanpath-demo.png`：第 1 次右侧提示、第 2 次技能栏、第 3 次前方通路，之后围绕准星、通路、掩体与 HUD 搜索。

仿真参数采用约 60 Hz 采样、单次注视约 200–420 ms、每次扫视约 50–80 ms，并在注视内部加入相关漂移和少量测量噪声。每个场景约 14 次注视、总时长约 5 秒；生成器为 `demo/DemoOverlayExporter.cs`。

真实采集完成后，应直接用每个任务对应的原始 CSV 重新生成结果，并删除图上的 `DEMO · 假设…` 标记。

## 历史候选素材

- Kostiantyn Zavhorodnii / Pexels 数字座舱：<https://www.pexels.com/photo/modern-car-interior-with-advanced-dashboard-displays-34453314/>
- Kanat Kairatov / Pexels 数字座舱：<https://www.pexels.com/photo/modern-car-dashboard-with-infotainment-system-28581679/>
