# PROJECT_SYNC

## Last Updated
- 时间：2026-05-12 22:35:00 +08:00
- 更新人/线路：主工程线（保守维护）
- 当前一句话状态：新增 NavigationTargetRegistry.Unregister API，修复删除导航点后 Registry 列表 null 空洞问题；UI 删除点时先调用 Unregister 再销毁 GameObject；Unity batchmode 验证编译通过，无 error CS。

## Global Status
- 项目路径：D:\VR_navigation
- Unity 版本：Unity 6.0 LTS 6000.0.67f1
- 当前最大阻塞：Quest 3 真机 Passthrough/Scene/Depth/空间稳定性仍待验证。
- 当前是否能编译：是，2026-05-12 22:31 主工程线（保守维护）运行 Unity 6000.0.67f1 batchmode，日志 D:\VR_navigation\Logs\registry_remove_conservative_verify.log 未发现 error CS，Tundra build success (3.32 seconds, 9 items updated, 1653 evaluated)。
- 当前是否能进场景：主工程线待确认
- 是否真机测试过：主工程线/MR 空间线待确认

## Main Engineering Line
- 状态：本次临时更新了导航核心的第二阶段 Floor System 预留；主工程线仍需最终确认。
- 已完成：NavigationTarget 保留 floorId；FloorConnection 作为楼梯/电梯/扶梯连接数据结构；新增 FloorRoutePlanner 作为跨楼层上层调度；IndoorNavigationController 已恢复为只负责单楼层 NavMesh 路径；NavigationTargetRegistry/NavigationTarget 优先调用 FloorRoutePlanner，找不到 planner 时回落到 IndoorNavigationController，保留当前单楼层流程。
- 正在做：等待主工程线在 Unity Editor Console 复核无红色 error；随后在 FloorRoutePlanner 上配置 floorConnections。
- 阻塞：未做 Quest 3 真机测试；跨楼层交互需要 UI/MR 线接入后再测。（原 QuestNavigationWorldSpaceUICreator.cs Object 命名歧义已由 UI 线修复，全项目 batchmode 编译已通过，见阻塞 4 和 2026-05-12 22:31 验证日志。）
- 修改过的完整路径：D:\VR_navigation\Assets\Quest3IndoorNavigation\Scripts\IndoorNavigationController.cs；D:\VR_navigation\Assets\Quest3IndoorNavigation\Scripts\FloorRoutePlanner.cs；D:\VR_navigation\Assets\Quest3IndoorNavigation\Scripts\FloorRoutePlanner.cs.meta；D:\VR_navigation\Assets\Quest3IndoorNavigation\Scripts\NavigationTarget.cs；D:\VR_navigation\Assets\Quest3IndoorNavigation\Scripts\NavigationTargetRegistry.cs；D:\VR_navigation\Assets\Quest3IndoorNavigation\Editor\Quest3SceneBootstrap.cs；D:\VR_navigation\Assets\Scenes\SampleScene.unity；D:\VR_navigation\PROJECT_SYNC.md
- 下一步：UI 线修复 Object 歧义后，主工程线重新跑 Unity 编译；随后在 FloorRoutePlanner 上配置 floorConnections，第一版通过按钮调用 ConfirmArrivedAtFloor(目标楼层) 或 ConfirmArrivedAtPendingDestinationFloor() 手动确认上下楼。

## UI Line
- 状态：UI Editor 编译错误已修复；基础 World Space Canvas、新增导航点管理功能、Quest 3 大按钮交互和地图采集模式入口均已预留，待场景交互和真机验证。
- 已完成：新增 World Space Canvas UI；默认跟随 Camera.main 前方约 1.5m；目标点大按钮列表；点击目标调用 NavigationTargetRegistry.SelectByIndex(index)；状态显示未选择目标/正在导航/找不到路径，并在导航中/找不到路径时显示当前目标名称；清除路线按钮调用 IndoorNavigationController.ClearDestination()；新增 Editor 菜单用于手动创建 UI 对象；新增“添加当前位置为导航点”按钮；读取 Camera.main.transform 创建 NavigationTarget；自动命名 Point 1/Point 2/Point 3；自动注册到 NavigationTargetRegistry；新增小球和文字标签标记；将目的地选择改为自定义大按钮展开列表，避免依赖桌面 Dropdown；新增刷新列表按钮；新增删除当前选中点按钮并刷新 UI 映射；添加点成功有明确反馈；如果项目存在 XR Interaction Toolkit，会自动挂 TrackedDeviceGraphicRaycaster 和 XRUIInputModule；预留地图采集模式入口，支持当前楼层 F1/F2/F3 或 Inspector 自定义 floorIds；添加点时记录 NavigationPointMetadata.floorId 和 targetType；targetType 预留普通点/房间/楼梯/电梯/出入口；目标列表和目的地展开列表按楼层分组显示；新增“连接楼层点”占位入口，后续给 FloorRoutePlanner 使用。
- 正在做：等待主工程线/真机环境确认 Quest/MR 点击输入和场景交互。
- 阻塞：NavigationTargetRegistry.Unregister API 已由主工程线（保守维护）新增，DeleteSelectedPoint 已同步调用；不再有 null 空洞残留。剩余阻塞：XR 点击输入和场景交互仍需真机验证。
- 修改过的完整路径：D:\VR_navigation\PROJECT_SYNC.md；D:\VR_navigation\Assets\Quest3IndoorNavigation\Scripts\UI\QuestNavigationWorldSpaceUI.cs；D:\VR_navigation\Assets\Quest3IndoorNavigation\Scripts\UI\NavigationPointMarker.cs；D:\VR_navigation\Assets\Quest3IndoorNavigation\Scripts\UI\NavigationPointMetadata.cs；D:\VR_navigation\Assets\Quest3IndoorNavigation\Scripts\UI\Editor\QuestNavigationWorldSpaceUICreator.cs；D:\VR_navigation\Assets\Quest3IndoorNavigation\Scripts\UI\Editor.meta；D:\VR_navigation\Assets\Quest3IndoorNavigation\Scripts\UI\QuestNavigationWorldSpaceUI.cs.meta；D:\VR_navigation\Assets\Quest3IndoorNavigation\Scripts\UI\NavigationPointMarker.cs.meta；D:\VR_navigation\Assets\Quest3IndoorNavigation\Scripts\UI\NavigationPointMetadata.cs.meta；D:\VR_navigation\Assets\Quest3IndoorNavigation\Scripts\UI\Editor\QuestNavigationWorldSpaceUICreator.cs.meta
- 需要主工程线配合：确认 EventSystem/Input Module 与 XR Ray Interactor/手势 pinch 输入兼容；如需真正从 Registry 移除点，请主工程线给 NavigationTargetRegistry 增加 Remove/Unregister API；上下楼作为第二阶段由 FloorRoutePlanner 上层调度，UI 线后续只负责显示“先去楼梯/电梯连接点、请切楼层、继续导航”等状态，不改 IndoorNavigationController 单楼层职责。
- 下一步：在 Unity 场景中验证 UI 生成、自定义目的地展开列表、按楼层分组显示、添加点元数据记录、SelectByIndex 调用、删除点刷新、路径状态显示和 XR 射线/pinch 点击。

## MR Spatial Line
- 状态：进行中，基础 MR 透视和头显空间定位配置已完成本地修改，Unity batchmode 脚本编译通过，待 Quest 3 真机验证。
- 已完成：检查 Packages 已存在 OpenXR、Meta OpenXR、Meta XR SDK Core、AR Foundation、UXR.QuestMeshing、AI Navigation；检查 AndroidManifest 已有 USE_SCENE、USE_ANCHOR_API、USE_PASSTHROUGH 和 quest3/quest3s supportedDevices；检查 OpenXR Android Features 中 ARCamera/Passthrough、AROcclusion/Depth、ARMesh、ARPlane、ARSession、ARRaycast、MetaXR、Touch Plus、Foveation、Subsampled Layout 已启用；修正 SampleScene 中 XROrigin.m_Camera 指向 CenterEyeAnchor Camera，CameraFloorOffset 指向 rig，Camera Y Offset 设为 0；修正 NavigationController.user 指向 CenterEyeAnchor transform；启用场景 OVRManager.isInsightPassthroughEnabled；只保留 CenterEyeAnchor 为 MainCamera，禁用默认 Main Camera 的 Camera/AudioListener；更新 MR bootstrap，后续重建场景会保持这些配置；将 ARAnchorFeature Android 加入 MR bootstrap 启用列表，供第二版 Spatial Anchors 使用；运行 Unity batchmode 打开项目并完成脚本编译，日志未发现 error CS。
- 正在做：MR 空间线本地配置检查已完成，等待真机验证反馈。
- 阻塞：Quest 3 Passthrough、Scene API、Depth API 和空间稳定性必须真机运行验证；Spatial Anchors 长期点位方案尚未实现，当前建议第一版使用 session 世界坐标。
- 修改过的完整路径：D:\VR_navigation\Assets\Quest3IndoorNavigation\Editor\Quest3SceneBootstrap.cs；D:\VR_navigation\Assets\Quest3IndoorNavigation\Editor\Quest3ProjectBootstrap.cs；D:\VR_navigation\Assets\Scenes\SampleScene.unity；D:\VR_navigation\PROJECT_SYNC.md
- 需要主工程线配合：确认 Unity Console 无 C# 编译错误；确认 OpenXR/Meta XR validation 无阻塞项；若主工程线统一管理 OpenXR 配置，请确认 ARAnchorFeature Android 是现在启用还是留到第二版 Spatial Anchors。
- 下一步：在 Unity Editor 打开 SampleScene，确认只有 CenterEyeAnchor 是 MainCamera 且 NavigationController.user 指向它；Quest 3 构建运行验证真实房间可见、不是黑底 VR、目标点和路径线叠加在透视画面上；第一版点位保存使用 session 世界坐标，第二版再接 Spatial Anchors 做跨重启/跨进入持久化。

## Shared File Ownership
- manifest.json：只允许主工程线改
- packages-lock.json：只允许主工程线改
- ProjectSettings：主工程线/MR 空间线需互相确认
- AndroidManifest.xml：主工程线/MR 空间线需互相确认
- UI 目录：只允许 UI 线改
- Navigation 核心脚本：主工程线负责，UI 线只调用，不随意改
- 上下楼调度：第二阶段新增 FloorRoutePlanner 作为 IndoorNavigationController 上层调度，不推翻当前单楼层 NavMesh 路径流程

## Current Blockers
- 阻塞 1：主工程线待确认 Package Manager/OpenXR/Meta XR/UXR.QuestMeshing 集成状态。
- 阻塞 2：已解决。Unity batchmode 脚本编译已通过（2026-05-12 22:31 最新验证），未发现 error CS；仍需主工程线在 Editor Console 复核。
- 阻塞 3：已解决。NavigationTargetRegistry 已新增 Unregister API；UI 线删除点时先调用 Unregister 再销毁 GameObject，Registry 列表不再有 null 空洞。MR 空间线尚未真机验证 Passthrough/Scene/Depth/空间稳定性，该部分仍为未解决阻塞。
- 阻塞 4：已解决。UI 线已将 QuestNavigationWorldSpaceUICreator.cs 中的 Object 简写改为 UnityEngine.Object，并通过 Unity batchmode 编译检查。

## Integration Notes
- UI 线交付物如何接入：在 Unity 菜单点击 Quest3 Indoor Navigation/Create World Space Navigation UI，或手动创建 World Space Canvas 并挂 QuestNavigationWorldSpaceUI；UI 默认 World Space 并跟随用户前方约 1.5m，必要时拖入 NavigationTargetRegistry、IndoorNavigationController 和中文字体；运行时点击“添加当前位置为导航点”会在 Camera.main.transform 位置创建 Point N、NavigationTarget、NavigationPointMarker 和 NavigationPointMetadata，并注册到 NavigationTargetRegistry；目的地选择使用自定义大按钮展开列表或大目标按钮，都会调用 SelectByIndex；地图采集模式第一版只记录 floorId/targetType 并显示分组，“连接楼层点”按钮目前只提示预留，第二阶段接 FloorRoutePlanner。
- MR 空间线交付物如何接入：以 SampleScene 中 XR Origin / Camera Rig、CenterEyeAnchor、OVRManager、OVRPassthroughLayer、OpenXR/Meta feature 配置为基础；用户空间坐标使用 CenterEyeAnchor transform；第一版导航点使用 session 世界坐标，第二版再接 Spatial Anchors。
- 主工程线最终合并步骤：确认 UI Editor 编译错误修复后，重新编译；确认 FloorRoutePlanner 挂在场景并配置 floorConnections；单楼层目标继续由 IndoorNavigationController 直接算路径，跨楼层目标由 FloorRoutePlanner 先导向连接点、等待手动确认楼层、再导向最终目标。

## Verification Checklist
- manifest.json 合法
- packages-lock.json 包含 OpenXR
- packages-lock.json 包含 Meta XR
- packages-lock.json 包含 UXR.QuestMeshing
- Unity Console 无红色编译错误
- 场景里有 XR Origin / Camera Rig
- Quest 3 可见 Passthrough
- 能添加导航点
- 能下拉选择目的地
- 能显示路径线
- 已真机测试

## Main Engineering Line Update - 2026-05-12 21:28 +08:00
- 状态：已为第二阶段多楼层导航预留 Floor System 数据层；未推翻当前单楼层 NavMesh 路径流程。
- 已完成：`IndoorNavigationController` 保持只负责单楼层 `NavMesh.CalculatePath` 和 `LineRenderer` 路线显示；`NavigationTarget` 增加 `targetId`、`displayName`、`floorId`、`targetType`；新增/调整 `FloorConnection`，预留 `connectionId`、`fromFloorId`、`toFloorId`、`fromTargetId`、`toTargetId`、`connectionType`；新增 `IndoorMapData`，预留 `floors`、`targets`、`floorConnections`，第一版支持 `JsonUtility` JSON 序列化；新增/保留 `FloorRoutePlanner` 作为上层跨楼层调度入口。
- 正在做：等待 UI 线后续把“我已到达 2F”等手动确认按钮接到 `FloorRoutePlanner.ConfirmArrivedAtFloor(int)` 或 `ConfirmArrivedAtPendingDestinationFloor()`。
- 阻塞：未做 Quest 3 真机测试；跨楼层交互需要 UI/MR 线接入后再测；当前全项目 Unity 编译阻塞来自 UI 线 `QuestNavigationWorldSpaceUICreator.cs(34,17)` 的 `Object` 命名歧义，本次未修改 UI 目录。
- 修改过的完整路径：`D:\VR_navigation\Assets\Quest3IndoorNavigation\Scripts\IndoorNavigationController.cs`；`D:\VR_navigation\Assets\Quest3IndoorNavigation\Scripts\NavigationTarget.cs`；`D:\VR_navigation\Assets\Quest3IndoorNavigation\Scripts\NavigationTargetRegistry.cs`；`D:\VR_navigation\Assets\Quest3IndoorNavigation\Scripts\FloorConnection.cs`；`D:\VR_navigation\Assets\Quest3IndoorNavigation\Scripts\FloorRoutePlanner.cs`；`D:\VR_navigation\Assets\Quest3IndoorNavigation\Scripts\FloorRoutePlanner.cs.meta`；`D:\VR_navigation\Assets\Quest3IndoorNavigation\Scripts\IndoorMapData.cs`；`D:\VR_navigation\Assets\Quest3IndoorNavigation\Scripts\IndoorMapData.cs.meta`；`D:\VR_navigation\PROJECT_SYNC.md`
- 编译验证：`D:\VR_navigation\Logs\FloorPlannerVerify2.log` 中 Floor System 相关脚本编译通过；最终全项目验证 `D:\VR_navigation\Logs\FloorPlannerFinalVerify.log` 当前失败于 UI 线 `QuestNavigationWorldSpaceUICreator.cs(34,17)`，不是 FloorRoutePlanner/IndoorNavigationController 错误。
- 下一步：如果要在场景中演示跨楼层第一版，需要放置楼梯/电梯目标点，并在 `FloorRoutePlanner.floorConnections` 中配置连接关系；这属于第二阶段扩展，不影响当前单楼层导航。

## Map Point Persistence Update - 2026-05-12 21:35 +08:00
- 状态：已完成接口和数据字段预留，未强行接完整 Spatial Anchors。
- 方案评估：第一版保存 NavigationTarget 的 targetId、displayName、floorId、targetType、position、rotation 到 IndoorMapData JSON；这适合当前 session 或短期演示，但重启/重新 Guardian/重新定位后世界原点可能变化。
- 长期方案：如果要求跨天、重启、重新进入后仍稳定，需要第二阶段接 Spatial Anchors，把每个长期点位绑定到 spatialAnchorId，启动后先 resolve anchor，再把导航点恢复到 anchor pose。
- 已完成：IndoorMapTargetData 增加 rotation、persistenceMode、spatialAnchorId；新增 NavigationPointPersistenceMode；新增 INavigationPointStore；新增 ISpatialAnchorNavigationPointStore，仅预留 Save/Load/CreateAnchor/ResolveAnchor 接口。
- 未做：未调用 OVRSpatialAnchor/Meta Anchor API；未请求额外运行时权限；未改 AndroidManifest/OpenXR 设置；未改变当前单楼层导航流程。
- 修改过的完整路径：D:\VR_navigation\Assets\Quest3IndoorNavigation\Scripts\IndoorMapData.cs；D:\VR_navigation\Assets\Quest3IndoorNavigation\Scripts\NavigationPointPersistence.cs；D:\VR_navigation\Assets\Quest3IndoorNavigation\Scripts\NavigationPointPersistence.cs.meta；D:\VR_navigation\PROJECT_SYNC.md
- 编译验证：D:\VR_navigation\Logs\PointPersistenceVerify.log 当前全项目仍失败于 UI 线 QuestNavigationWorldSpaceUICreator.cs(34,17) 的 Object 命名歧义；未出现 NavigationPointPersistence/IndoorMapData 相关 error CS。
- 下一步：第一版由主工程/UI 线在添加/删除点时把 IndoorMapData 写入 JSON；第二版再实现 ISpatialAnchorNavigationPointStore，并在真机上验证 anchor resolve 后的点位稳定性。

## Conservative Maintenance Update - 2026-05-12 22:35 +08:00

### 本次操作范围
保守维护，仅处理明确的最小问题：NavigationTargetRegistry 删除点后 null 空洞残留。本轮为 Editor / batchmode / 静态逻辑修复完成，不代表 Quest 3 真机运行通过；真机 Passthrough / Scene / Depth / 空间稳定性仍需后续单独验证。

### Main Engineering Line 备注

- **是否新增 Unregister API**：是。`NavigationTargetRegistry.cs` 新增 `public void Unregister(NavigationTarget target)` 方法，内部调用 `targets.Remove(target)`，安全移除指定 NavigationTarget，不改变整体 Registry 结构，不改变已有 SelectByIndex 行为。
- **问题确认**：静态分析确认，删除点后 `targets` 列表会保留 Unity fake-null 空洞。现有代码的 SelectByIndex、GetValidTargetCount、GetSortedTargetIndices 均有 null 守卫，不会 crash，但 null 空洞随操作累积，是 PROJECT_SYNC.md 阻塞 3 的已知问题。
- **IndoorNavigationController 职责**：未修改。仍只负责单楼层 NavMesh.CalculatePath 和 LineRenderer 路径显示。
- **FloorRoutePlanner 职责**：未修改。仍作为跨楼层上层调度，跨楼层逻辑本次未扩展。
- **SelectByIndex 行为**：未改变。Registry 调用 FloorRoutePlanner 或 IndoorNavigationController 的优先级逻辑未动。
- **编译结果**：2026-05-12 22:31 Unity 6000.0.67f1 batchmode，日志 D:\VR_navigation\Logs\registry_remove_conservative_verify.log，Tundra build success (3.32 seconds, 9 items updated, 1653 evaluated)，无 error CS，无 Exception，无 Bee/Tundra build failed。
- **修改过的完整路径**：`D:\VR_navigation\Assets\Quest3IndoorNavigation\Scripts\NavigationTargetRegistry.cs`；`D:\VR_navigation\Assets\Quest3IndoorNavigation\Scripts\UI\QuestNavigationWorldSpaceUI.cs`；`D:\VR_navigation\PROJECT_SYNC.md`
- **主工程线下一步**：在 Unity Editor Console 复核当前无红色 error；若需要演示跨楼层，在 FloorRoutePlanner.floorConnections 中配置楼梯/电梯连接点（第二阶段，不影响当前单楼层流程）。

### UI Line 备注

- **删除导航点是否已同步从 Registry 注销**：是。`DeleteSelectedPoint()` 现在先调用 `targetRegistry.Unregister(target)` 将目标从 Registry 内部列表移除，再调用 `DestroyChild(target.gameObject)` 销毁 GameObject，最后刷新 UI 列表。执行顺序：Unregister → Destroy → RefreshTargets → RefreshStatus。
- **UI 列表刷新是否保持正常**：是。`RefreshTargets()` 重建按钮列表，`GetSortedTargetIndices` 从 Registry 最新 Targets 读取，Unregister 后列表已移除该元素，刷新后无 null 空洞、无残留按钮。
- **添加当前位置为导航点、floorId、targetType 元数据**：未改动，功能完整保留。`AddCurrentCameraPositionAsPoint` 逻辑不变。
- **World Space UI / Quest 大按钮 / XR 点击输入**：架构未改动，仍需真机验证。
- **UI 线下一步**：在 Unity 场景中验证删除点流程（添加若干点→选中一个→点击删除→确认列表刷新、Registry 列表无空洞）；验证 XR 射线/pinch 点击大按钮；验证楼层分组显示。

### MR Spatial Line 备注

- **本次是否进行 Quest 3 真机测试**：否。本次未连接 Quest 3、未构建 APK、未进行任何真机运行。
- **本次是否修改 MR/OpenXR/Android/Depth/Passthrough 配置**：否。本次未修改 OpenXR 配置、Meta XR 配置、AndroidManifest.xml、ProjectSettings、Passthrough、Scene API、Depth API 相关任何文件。
- **Quest 3 Passthrough / Scene API / Depth API / 空间稳定性**：仍待后续真机验证，本次未完成任何真机验证，状态与上次 MR 空间线更新相同。
- **第一版点位方案**：仍使用 session 世界坐标，未变更。
- **Spatial Anchors**：仍为第二阶段预留，未实现，接口 ISpatialAnchorNavigationPointStore 已在 NavigationPointPersistence.cs 中预留。
- **MR 空间线下一步**：后续由 MR 空间线在 Quest 3 真机上构建运行，验证 Passthrough 透视可见、CenterEyeAnchor 为 MainCamera、导航目标点和路径线叠加在透视画面上。

## Conservative QA / Handoff Check - 2026-05-12 22:50 +08:00

### 本轮说明
主工程线暂时不在；本轮以质检员 + 书记员 + 交接准备身份工作，不做最终收尾，不做大改。

### 静态检查结果汇总

**本轮未修改代码（除文档一致性修正）。**
本轮未进行 Quest 3 真机测试。本轮未 Build APK。本轮未修改 MR/OpenXR/Android/Passthrough/Depth/Scene 任何配置。
本轮为 Editor / batchmode / 静态逻辑检查完成，不代表 Quest 3 真机运行通过。

**NavigationTargetRegistry.Unregister 检查结果：**
- `targets.Remove(target)` 正确：调用发生在 Destroy 之前，target 引用仍有效，List.Remove 使用引用相等，移除成功后列表紧缩（无 null 空洞）。安全。
- `SelectByIndex` 行为未变：方法本身未动，null 守卫完整。安全。
- `Register` 重复注册守卫 `!targets.Contains(target)` 未动。安全。
- **已知轻微隐患（不是 bug，仅记录）**：`Unregister(null)` 无显式 null 守卫；`List<T>.Remove(null)` 在 C# 是合法操作（查找并移除第一个 null 元素），但当前所有调用路径在传入前均已 null 守卫，风险极低；如需更防御性写法，主工程线复核时可加 `if (target == null) return;`，改动为 1 行。

**QuestNavigationWorldSpaceUI.DeleteSelectedPoint 流程检查结果：**
- 执行顺序：ClearDestination（仅当删除目标等于当前导航目标时）→ Unregister → DestroyChild → selectedRegistryIndex = -1 → RefreshTargets → RefreshStatus。顺序正确，无资源提前释放。
- null target 守卫：`GetTargetByRegistryIndex(selectedRegistryIndex)` 返回 null 时立即 return。安全。
- 无选中点时点击删除：selectedRegistryIndex = -1 → GetTargetByRegistryIndex(-1) → registryIndex < 0 → return null → 安全退出。
- 连续快速双击：第一次完成后 selectedRegistryIndex = -1，第二次进入时 GetTargetByRegistryIndex(-1) 返回 null → 安全退出。
- 删除 Point1 后 Point2 可选：Unregister 后 registry = [Point2(0)]，RefreshTargets 重建按钮，capturedRegistryIndex = 0，SelectByIndex(0) 找到 Point2，正常。
- 删除非导航目标点：若 selectedRegistryIndex 指向的 target ≠ navigationController.Destination（极端情况），ClearDestination 不执行，Destination 保留，RefreshStatus 会通过 FindRegistryIndex 找到仍在 registry 中的 Destination 并更新显示。安全。
- **已知轻微隐患（不是 bug，仅记录）**：RefreshStatus 中若 FindRegistryIndex 返回 -1（目标已从外部销毁但 Destination 仍指向它），statusLabel 会显示"正在导航: 选择目的地"——显示略奇怪但无 crash。这是删除点流程之外的边界场景，与本次修复无关。
- floorId / targetType / metadata 逻辑：`AddCurrentCameraPositionAsPoint` 未动，floorId/targetType 记录完整。
- World Space UI 架构、Canvas、XR Raycaster、FollowCamera：均未改动。

**PROJECT_SYNC.md 一致性检查结果：**
- 发现两处文档落后于代码现状（本次已修正为文档级修正）：
  - Main Engineering Line 头部"阻塞"字段仍写 UICreator 编译错误为当前阻塞，实际已解决（阻塞 4 已标记解决，最新 batchmode 编译通过）。→ 已更新。
  - UI Line 头部"阻塞"字段仍写 NavigationTargetRegistry 无 Remove API，实际已新增 Unregister。→ 已更新。
- Main Engineering Line Update 历史块（2026-05-12 21:28）仍保留原始描述，作为历史记录保持不动，但第 79-81 行"阻塞"描述已明确是历史状态。
- MR Spatial Line：正确记录了本轮未做真机测试、未改 MR 配置、Passthrough/Scene/Depth 待真机验证。一致。
- Spatial Anchors：正确标注为第二阶段预留、未实现。一致。
- 第一版点位 session 世界坐标：已记录。一致。

### 等待主工程线确认的事项

1. `NavigationTargetRegistry.Unregister(null)` 是否需要加显式 null 守卫（建议加，改动 1 行）。
2. 是否需要额外增加 `ClearNullTargets()` / `CompactTargets()` 方法（应对外部销毁导航点的边界场景）。
3. `IReadOnlyList<NavigationTarget> Targets` 暴露的是否足够（当前消费方已自行 null 过滤）。
4. `DeleteSelectedPoint` 删除顺序 `ClearDestination → Unregister → Destroy` 是否符合主工程线设计意图。
5. `generatedPointCount` 在删除点后不递减（防止名称重用）——这是有意设计，请主工程线确认是否同意。
6. FloorRoutePlanner.floorConnections 是否在场景中配置（第二阶段）。
7. 是否启动第一版 JSON 保存/加载（IndoorMapData），由 UI 线还是主工程线负责触发。
8. `RefreshStatus` 中 FindRegistryIndex 返回 -1 但 Destination 非 null 时的显示问题是否需要处理。

## Handoff Summary - 2026-05-12 23:00 +08:00

**Main Engineering Line：**
本轮静态检查确认 `NavigationTargetRegistry.Unregister` 逻辑正确，`targets.Remove` 在 Destroy 前调用，列表紧缩无 null 空洞，batchmode 编译通过（`registry_remove_conservative_verify.log`，无 error CS）；`Unregister(null)` 无显式 null 守卫属低风险隐患，尚未修复，留主工程线决定。下一步由主工程线在 Unity Editor Console 复核无红色 error，并按手动测试 Checklist 跑一遍 Play Mode 删除点流程，完成后将验证结果写入 PROJECT_SYNC.md。

**UI Line：**
本轮确认 `DeleteSelectedPoint` 执行顺序（ClearDestination → Unregister → Destroy → RefreshTargets → RefreshStatus）安全，无 null 崩溃、无重复删除、无 index 越界风险，floorId/targetType metadata 逻辑未被破坏；XR 射线/pinch 点击大按钮、World Space Canvas 跟随、Quest 3 场景交互本轮未做真机验证。下一步由 UI 线在 Editor Play Mode 手动验证删除流程，随后由 MR 空间线接入真机验证 XR 输入。

**MR Spatial Line：**
本轮未连接 Quest 3、未 Build APK、未修改 OpenXR/Meta XR/AndroidManifest/ProjectSettings/Passthrough/Depth/Scene 任何配置，所有 MR 配置停留在上次 MR 空间线本地静态检查状态；Passthrough 透视可见、Scene API/Depth API 可用、UXR.QuestMeshing 环境 mesh、NavMesh 路径叠加在透视画面上均未验证。下一步由 MR 空间线在主工程线 Editor 验证通过后，独立开阶段进行 Quest 3 真机构建与运行验证。
