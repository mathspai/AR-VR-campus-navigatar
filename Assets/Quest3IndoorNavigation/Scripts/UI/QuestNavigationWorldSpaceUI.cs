using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Quest3IndoorNavigation.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Canvas))]
    [RequireComponent(typeof(GraphicRaycaster))]
    public sealed class QuestNavigationWorldSpaceUI : MonoBehaviour
    {
        private const string NoTargetText = "\u672a\u9009\u62e9\u76ee\u6807";
        private const string NavigatingText = "\u6b63\u5728\u5bfc\u822a";
        private const string NoPathText = "\u627e\u4e0d\u5230\u8def\u5f84";
        private const string SelectorPlaceholder = "\u9009\u62e9\u76ee\u7684\u5730";
        private const string NoTargetPlaceholder = "\u6682\u65e0\u76ee\u6807\u70b9";
        private const string AddPointText = "\u6dfb\u52a0\u5f53\u524d\u4f4d\u7f6e\u4e3a\u5bfc\u822a\u70b9";
        private const string RefreshText = "\u5237\u65b0\u5217\u8868";
        private const string DeleteText = "\u5220\u9664\u9009\u4e2d\u70b9";
        private const string ClearRouteText = "\u6e05\u9664\u8def\u7ebf";
        private const string TitleText = "\u5ba4\u5185\u5bfc\u822a";
        private const string FallbackPointName = "\u76ee\u6807";
        private const string AddedPointFeedback = "\u5df2\u6dfb\u52a0\u5bfc\u822a\u70b9";
        private const string ConnectFloorsText = "\u8fde\u63a5\u697c\u5c42\u70b9";
        private const string ConnectFloorsPlaceholder = "\u697c\u5c42\u8fde\u63a5\u529f\u80fd\u5df2\u9884\u7559\uff0c\u7b2c\u4e8c\u9636\u6bb5\u63a5\u5165 FloorRoutePlanner";
        private const string FloorButtonPrefix = "\u5f53\u524d\u697c\u5c42";
        private const string TypeButtonPrefix = "\u70b9\u7c7b\u578b";

        [Header("Scene References")]
        [SerializeField] private NavigationTargetRegistry targetRegistry;
        [SerializeField] private IndoorNavigationController navigationController;

        [Header("Layout")]
        [SerializeField] private Font uiFont;
        [SerializeField] private Vector2 canvasSize = new(1040f, 940f);
        [SerializeField] private float canvasScale = 0.0015f;
        [SerializeField] private bool followMainCamera = false;
        [SerializeField] private float followDistance = 1.5f;
        [SerializeField] private float followHeightOffset = -0.22f;
        [SerializeField] private float followLerpSpeed = 25f;
        [SerializeField] private float snapDistanceThreshold = 1.2f;
        [SerializeField] private float buttonHeight = 82f;
        [SerializeField] private float buttonSpacing = 18f;
        [SerializeField] private float panelPadding = 42f;

        [Header("Runtime Points")]
        [SerializeField] private string generatedPointPrefix = "Point";
        [SerializeField] private Transform navigationPointsRoot;
        [SerializeField] private string[] floorIds = { "F1", "F2", "F3" };

        [Header("Colors")]
        [SerializeField] private Color panelColor = new(0.035f, 0.04f, 0.045f, 0.92f);
        [SerializeField] private Color buttonColor = new(0.12f, 0.28f, 0.32f, 0.96f);
        [SerializeField] private Color selectedButtonColor = new(0.14f, 0.46f, 0.40f, 1f);
        [SerializeField] private Color actionButtonColor = new(0.18f, 0.22f, 0.38f, 0.96f);
        [SerializeField] private Color deleteButtonColor = new(0.42f, 0.16f, 0.13f, 0.96f);
        [SerializeField] private Color statusReadyColor = new(0.88f, 0.9f, 0.86f, 1f);
        [SerializeField] private Color statusNavigatingColor = new(0.56f, 0.94f, 0.78f, 1f);
        [SerializeField] private Color statusNoPathColor = new(1f, 0.64f, 0.48f, 1f);

        private readonly List<Button> targetButtons = new();
        private readonly List<Text> targetButtonLabels = new();
        private readonly List<int> targetButtonRegistryIndices = new();
        private readonly List<Button> selectorButtons = new();
        private readonly List<Text> selectorButtonLabels = new();
        private readonly List<int> selectorButtonRegistryIndices = new();
        private Canvas canvas;
        private LineRenderer navigationLine;
        private RectTransform buttonListRoot;
        private RectTransform selectorListRoot;
        private Button selectorToggleButton;
        private Text selectorToggleLabel;
        private Text statusLabel;
        private Text feedbackLabel;
        private Text floorButtonLabel;
        private Text typeButtonLabel;
        private bool selectorOpen;
        private int selectedRegistryIndex = -1;
        private int generatedPointCount;
        private int currentFloorIndex;
        private NavigationPointType currentPointType = NavigationPointType.Normal;

        private string CurrentFloorId
        {
            get
            {
                if (floorIds == null || floorIds.Length == 0)
                {
                    return "F1";
                }

                currentFloorIndex = Mathf.Clamp(currentFloorIndex, 0, floorIds.Length - 1);
                return string.IsNullOrWhiteSpace(floorIds[currentFloorIndex]) ? $"F{currentFloorIndex + 1}" : floorIds[currentFloorIndex];
            }
        }

        private string FloorLabelText => $"{FloorButtonPrefix}: {CurrentFloorId}";
        private string TypeLabelText => $"{TypeButtonPrefix}: {GetPointTypeLabel(currentPointType)}";

        private void Awake()
        {
            followMainCamera = false;
            ResolveReferences();
            Build();
        }

        private void Start()
        {
            followMainCamera = false;
            DetachFromMovingParents();
            SnapToCamera();
        }

        private void OnEnable()
        {
            RefreshTargets();
            RefreshStatus();
        }

        private void Update()
        {
            RefreshStatus();
        }

        private void LateUpdate()
        {
            followMainCamera = false;
        }

        public void RefreshTargets()
        {
            if (buttonListRoot == null)
            {
                return;
            }

            ResolveReferences();
            ClearTargetButtons();
            RefreshSelectorOptions();

            var targets = targetRegistry != null ? targetRegistry.Targets : null;
            if (targets == null || GetValidTargetCount(targets) == 0)
            {
                var emptyLabel = CreateText("EmptyState", buttonListRoot, NoTargetPlaceholder, 34, FontStyle.Normal, TextAnchor.MiddleCenter);
                emptyLabel.color = new Color(0.78f, 0.82f, 0.78f, 1f);
                var emptyRect = emptyLabel.rectTransform;
                emptyRect.anchorMin = new Vector2(0f, 1f);
                emptyRect.anchorMax = new Vector2(1f, 1f);
                emptyRect.pivot = new Vector2(0.5f, 1f);
                emptyRect.offsetMin = new Vector2(0f, -buttonHeight);
                emptyRect.offsetMax = Vector2.zero;
                return;
            }

            var rowIndex = 0;
            string lastFloorId = null;
            var sortedIndices = GetSortedTargetIndices(targets);
            for (var sortedIndex = 0; sortedIndex < sortedIndices.Count; sortedIndex++)
            {
                var registryIndex = sortedIndices[sortedIndex];
                var target = targets[registryIndex];
                if (target == null)
                {
                    continue;
                }

                var floorId = GetFloorId(target);
                if (floorId != lastFloorId)
                {
                    var header = CreateGroupHeader($"TargetFloorHeader_{floorId}", buttonListRoot, floorId);
                    var headerRect = header.rectTransform;
                    var headerTop = -rowIndex * (buttonHeight + buttonSpacing);
                    headerRect.offsetMin = new Vector2(0f, headerTop - 46f);
                    headerRect.offsetMax = new Vector2(0f, headerTop);
                    rowIndex++;
                    lastFloorId = floorId;
                }

                var label = GetTargetDisplayName(registryIndex);
                var button = CreateButton($"TargetButton_{registryIndex}", buttonListRoot, label, buttonColor, buttonHeight, 34);
                var rect = button.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(0.5f, 1f);

                var top = -rowIndex * (buttonHeight + buttonSpacing);
                rect.offsetMin = new Vector2(0f, top - buttonHeight);
                rect.offsetMax = new Vector2(0f, top);

                var capturedRegistryIndex = registryIndex;
                button.onClick.AddListener(() => SelectTarget(capturedRegistryIndex));
                targetButtons.Add(button);
                targetButtonLabels.Add(button.GetComponentInChildren<Text>());
                targetButtonRegistryIndices.Add(registryIndex);
                rowIndex++;
            }

            RefreshSelectedControls();
        }

        public void Rebuild()
        {
            ResolveReferences();
            Build();
            RefreshStatus();
        }

        public void AddCurrentCameraPositionAsPoint()
        {
            ResolveReferences();

            var mainCamera = GetUserCamera();
            if (mainCamera == null || targetRegistry == null)
            {
                return;
            }

            generatedPointCount = Mathf.Max(generatedPointCount + 1, GetValidTargetCount(targetRegistry.Targets) + 1);
            var pointName = $"{generatedPointPrefix} {generatedPointCount}";
            var worldPosition = mainCamera.transform.position;
            var worldRotation = Quaternion.identity;
            var stableRoot = GetNavigationPointsRoot();

            var pointObject = new GameObject(pointName);
            pointObject.transform.SetParent(stableRoot, true);
            pointObject.transform.position = worldPosition;
            pointObject.transform.rotation = worldRotation;

            var target = pointObject.AddComponent<NavigationTarget>();
            var metadata = pointObject.AddComponent<NavigationPointMetadata>();
            metadata.SetMetadata(CurrentFloorId, currentPointType);
            var marker = pointObject.AddComponent<NavigationPointMarker>();
            marker.SetWorldLockPosition(worldPosition);
            marker.SetLabel($"{pointName}\n{CurrentFloorId} / {GetPointTypeLabel(currentPointType)}");

            targetRegistry.Register(target);
            selectedRegistryIndex = FindRegistryIndex(target);
            targetRegistry.SelectByIndex(selectedRegistryIndex);
            SetFeedback($"{AddedPointFeedback}: {pointName}");

            RefreshTargets();
            RefreshStatus();
        }

        public void ClearDestination()
        {
            ResolveReferences();
            selectedRegistryIndex = -1;

            if (navigationController != null)
            {
                navigationController.ClearDestination();
            }

            RefreshSelectedControls();
            RefreshStatus();
        }

        public void DeleteSelectedPoint()
        {
            ResolveReferences();

            var target = GetTargetByRegistryIndex(selectedRegistryIndex);
            if (target == null)
            {
                return;
            }

            if (navigationController != null && navigationController.Destination == target.transform)
            {
                navigationController.ClearDestination();
            }

            if (targetRegistry != null)
            {
                targetRegistry.Unregister(target);
            }

            DestroyChild(target.gameObject);
            selectedRegistryIndex = -1;
            RefreshTargets();
            RefreshStatus();
        }

        private void SelectTarget(int registryIndex)
        {
            ResolveReferences();
            selectedRegistryIndex = registryIndex;

            if (targetRegistry != null)
            {
                targetRegistry.SelectByIndex(registryIndex);
            }

            selectorOpen = false;
            SetFeedback(GetTargetDisplayName(registryIndex));
            RefreshSelectedControls();
            RefreshStatus();
        }

        private void Build()
        {
            canvas = GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = GetUserCamera();

            var canvasRect = canvas.GetComponent<RectTransform>();
            canvasRect.sizeDelta = canvasSize;
            transform.localScale = Vector3.one * canvasScale;

            EnsureRaycasters();

            DestroyGeneratedChildren();

            var panel = CreateImage("Panel", transform, panelColor);
            var panelRect = panel.rectTransform;
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            var title = CreateText("Title", panelRect, TitleText, 46, FontStyle.Bold, TextAnchor.MiddleLeft);
            title.color = Color.white;
            SetAnchoredRect(title.rectTransform, new Vector2(panelPadding, -28f), new Vector2(canvasSize.x - panelPadding * 2f, 62f));

            statusLabel = CreateText("Status", panelRect, NoTargetText, 34, FontStyle.Bold, TextAnchor.MiddleRight);
            statusLabel.color = statusReadyColor;
            SetAnchoredRect(statusLabel.rectTransform, new Vector2(panelPadding, -34f), new Vector2(canvasSize.x - panelPadding * 2f, 58f));

            var addButton = CreateButton("AddCurrentPointButton", panelRect, AddPointText, actionButtonColor, 76f, 30);
            addButton.onClick.AddListener(AddCurrentCameraPositionAsPoint);
            SetStretchTop(addButton.GetComponent<RectTransform>(), panelPadding, -108f, panelPadding, 76f);

            var floorButton = CreateButton("FloorSelectorButton", panelRect, FloorLabelText, actionButtonColor, 68f, 28);
            floorButton.onClick.AddListener(CycleFloor);
            floorButtonLabel = floorButton.GetComponentInChildren<Text>();
            SetStretchTop(floorButton.GetComponent<RectTransform>(), panelPadding, -194f, canvasSize.x * 0.52f, 68f);

            var typeButton = CreateButton("PointTypeSelectorButton", panelRect, TypeLabelText, actionButtonColor, 68f, 28);
            typeButton.onClick.AddListener(CyclePointType);
            typeButtonLabel = typeButton.GetComponentInChildren<Text>();
            SetStretchTop(typeButton.GetComponent<RectTransform>(), canvasSize.x * 0.52f, -194f, panelPadding, 68f);

            selectorToggleButton = CreateButton("DestinationSelectorButton", panelRect, SelectorPlaceholder, actionButtonColor, 76f, 30);
            selectorToggleButton.onClick.AddListener(ToggleSelector);
            selectorToggleLabel = selectorToggleButton.GetComponentInChildren<Text>();
            SetStretchTop(selectorToggleButton.GetComponent<RectTransform>(), panelPadding, -278f, panelPadding, 76f);

            var selectorList = new GameObject("DestinationSelectorOptions", typeof(RectTransform));
            selectorList.transform.SetParent(panelRect, false);
            selectorListRoot = selectorList.GetComponent<RectTransform>();
            selectorListRoot.anchorMin = new Vector2(0f, 1f);
            selectorListRoot.anchorMax = new Vector2(1f, 1f);
            selectorListRoot.pivot = new Vector2(0.5f, 1f);
            selectorListRoot.offsetMin = new Vector2(panelPadding, -756f);
            selectorListRoot.offsetMax = new Vector2(-panelPadding, -366f);
            selectorListRoot.gameObject.SetActive(false);

            var refreshButton = CreateButton("RefreshListButton", panelRect, RefreshText, actionButtonColor, 68f, 28);
            refreshButton.onClick.AddListener(RefreshTargets);
            SetStretchTop(refreshButton.GetComponent<RectTransform>(), panelPadding, -368f, canvasSize.x * 0.52f, 68f);

            var deleteButton = CreateButton("DeleteSelectedPointButton", panelRect, DeleteText, deleteButtonColor, 68f, 28);
            deleteButton.onClick.AddListener(DeleteSelectedPoint);
            SetStretchTop(deleteButton.GetComponent<RectTransform>(), canvasSize.x * 0.52f, -368f, panelPadding, 68f);

            var connectButton = CreateButton("ConnectFloorPointsButton", panelRect, ConnectFloorsText, actionButtonColor, 68f, 28);
            connectButton.onClick.AddListener(ShowConnectFloorsPlaceholder);
            SetStretchTop(connectButton.GetComponent<RectTransform>(), panelPadding, -446f, panelPadding, 68f);

            var list = new GameObject("TargetList", typeof(RectTransform));
            list.transform.SetParent(panelRect, false);
            buttonListRoot = list.GetComponent<RectTransform>();
            buttonListRoot.anchorMin = new Vector2(0f, 1f);
            buttonListRoot.anchorMax = new Vector2(1f, 1f);
            buttonListRoot.pivot = new Vector2(0.5f, 1f);
            buttonListRoot.offsetMin = new Vector2(panelPadding, -792f);
            buttonListRoot.offsetMax = new Vector2(-panelPadding, -530f);

            feedbackLabel = CreateText("Feedback", panelRect, string.Empty, 28, FontStyle.Bold, TextAnchor.MiddleLeft);
            feedbackLabel.color = statusNavigatingColor;
            SetStretchTop(feedbackLabel.rectTransform, panelPadding, -812f, panelPadding, 48f);

            var clearButton = CreateButton("ClearRouteButton", panelRect, ClearRouteText, deleteButtonColor, 78f, 32);
            clearButton.onClick.AddListener(ClearDestination);
            var clearRect = clearButton.GetComponent<RectTransform>();
            clearRect.anchorMin = new Vector2(0f, 0f);
            clearRect.anchorMax = new Vector2(1f, 0f);
            clearRect.pivot = new Vector2(0.5f, 0f);
            clearRect.offsetMin = new Vector2(panelPadding, panelPadding);
            clearRect.offsetMax = new Vector2(-panelPadding, panelPadding + 78f);

            RefreshCaptureModeLabels();
            RefreshTargets();
        }

        private void RefreshSelectorOptions()
        {
            if (selectorListRoot == null)
            {
                return;
            }

            ClearSelectorButtons();

            var targets = targetRegistry != null ? targetRegistry.Targets : null;
            if (targets == null || GetValidTargetCount(targets) == 0)
            {
                selectorListRoot.gameObject.SetActive(false);
                selectorOpen = false;
                return;
            }

            var rowIndex = 0;
            string lastFloorId = null;
            var sortedIndices = GetSortedTargetIndices(targets);
            for (var sortedIndex = 0; sortedIndex < sortedIndices.Count; sortedIndex++)
            {
                var registryIndex = sortedIndices[sortedIndex];
                var target = targets[registryIndex];
                if (target == null)
                {
                    continue;
                }

                var floorId = GetFloorId(target);
                if (floorId != lastFloorId)
                {
                    var header = CreateGroupHeader($"SelectorFloorHeader_{floorId}", selectorListRoot, floorId);
                    var headerRect = header.rectTransform;
                    var headerTop = -rowIndex * (buttonHeight + buttonSpacing);
                    headerRect.offsetMin = new Vector2(0f, headerTop - 46f);
                    headerRect.offsetMax = new Vector2(0f, headerTop);
                    rowIndex++;
                    lastFloorId = floorId;
                }

                var button = CreateButton($"SelectorOption_{registryIndex}", selectorListRoot, GetTargetDisplayName(registryIndex), buttonColor, buttonHeight, 32);
                var rect = button.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(0.5f, 1f);

                var top = -rowIndex * (buttonHeight + buttonSpacing);
                rect.offsetMin = new Vector2(0f, top - buttonHeight);
                rect.offsetMax = new Vector2(0f, top);

                var capturedRegistryIndex = registryIndex;
                button.onClick.AddListener(() => SelectTarget(capturedRegistryIndex));
                selectorButtons.Add(button);
                selectorButtonLabels.Add(button.GetComponentInChildren<Text>());
                selectorButtonRegistryIndices.Add(registryIndex);
                rowIndex++;
            }

            selectorListRoot.gameObject.SetActive(selectorOpen);
        }

        private void RefreshStatus()
        {
            if (statusLabel == null)
            {
                return;
            }

            ResolveReferences();

            if (navigationController == null || navigationController.Destination == null)
            {
                statusLabel.text = NoTargetText;
                statusLabel.color = statusReadyColor;
                selectedRegistryIndex = -1;
                RefreshSelectedControls();
                return;
            }

            var destinationIndex = FindRegistryIndex(navigationController.Destination.GetComponent<NavigationTarget>());
            if (destinationIndex >= 0)
            {
                selectedRegistryIndex = destinationIndex;
            }

            var hasPath = navigationLine != null && navigationLine.positionCount > 0;
            var targetName = GetTargetDisplayName(selectedRegistryIndex);
            statusLabel.text = hasPath ? $"{NavigatingText}: {targetName}" : $"{NoPathText}: {targetName}";
            statusLabel.color = hasPath ? statusNavigatingColor : statusNoPathColor;
            RefreshSelectedControls();
        }

        private void RefreshSelectedControls()
        {
            for (var i = 0; i < targetButtons.Count; i++)
            {
                if (targetButtons[i] == null)
                {
                    continue;
                }

                var isSelected = i < targetButtonRegistryIndices.Count && targetButtonRegistryIndices[i] == selectedRegistryIndex;
                var image = targetButtons[i].GetComponent<Image>();
                if (image != null)
                {
                    image.color = isSelected ? selectedButtonColor : buttonColor;
                }

                if (i < targetButtonLabels.Count && targetButtonLabels[i] != null)
                {
                    targetButtonLabels[i].fontStyle = isSelected ? FontStyle.Bold : FontStyle.Normal;
                }
            }

            for (var i = 0; i < selectorButtons.Count; i++)
            {
                if (selectorButtons[i] == null)
                {
                    continue;
                }

                var isSelected = i < selectorButtonRegistryIndices.Count && selectorButtonRegistryIndices[i] == selectedRegistryIndex;
                var image = selectorButtons[i].GetComponent<Image>();
                if (image != null)
                {
                    image.color = isSelected ? selectedButtonColor : buttonColor;
                }

                if (i < selectorButtonLabels.Count && selectorButtonLabels[i] != null)
                {
                    selectorButtonLabels[i].fontStyle = isSelected ? FontStyle.Bold : FontStyle.Normal;
                }
            }

            if (selectorToggleLabel != null)
            {
                selectorToggleLabel.text = selectedRegistryIndex >= 0 ? GetTargetDisplayName(selectedRegistryIndex) : SelectorPlaceholder;
            }

            if (selectorListRoot != null)
            {
                selectorListRoot.gameObject.SetActive(selectorOpen && selectorButtons.Count > 0);
                if (selectorOpen)
                {
                    selectorListRoot.SetAsLastSibling();
                }
            }
        }

        private void ToggleSelector()
        {
            selectorOpen = !selectorOpen;
            RefreshSelectorOptions();
            RefreshSelectedControls();
        }

        private void CycleFloor()
        {
            if (floorIds == null || floorIds.Length == 0)
            {
                currentFloorIndex = 0;
            }
            else
            {
                currentFloorIndex = (currentFloorIndex + 1) % floorIds.Length;
            }

            RefreshCaptureModeLabels();
            RefreshTargets();
            SetFeedback(FloorLabelText);
        }

        private void CyclePointType()
        {
            var values = (NavigationPointType[])Enum.GetValues(typeof(NavigationPointType));
            var nextIndex = (Array.IndexOf(values, currentPointType) + 1) % values.Length;
            currentPointType = values[nextIndex];
            RefreshCaptureModeLabels();
            SetFeedback(TypeLabelText);
        }

        private void ShowConnectFloorsPlaceholder()
        {
            SetFeedback(ConnectFloorsPlaceholder);
        }

        private void RefreshCaptureModeLabels()
        {
            if (floorButtonLabel != null)
            {
                floorButtonLabel.text = FloorLabelText;
            }

            if (typeButtonLabel != null)
            {
                typeButtonLabel.text = TypeLabelText;
            }
        }

        private void SetFeedback(string text)
        {
            if (feedbackLabel != null)
            {
                feedbackLabel.text = text;
            }
        }

        private void ResolveReferences()
        {
            if (targetRegistry == null)
            {
                targetRegistry = FindFirstObjectByType<NavigationTargetRegistry>();
            }

            if (navigationController == null)
            {
                navigationController = FindFirstObjectByType<IndoorNavigationController>();
            }

            navigationLine = navigationController != null ? navigationController.GetComponent<LineRenderer>() : null;
        }

        private Transform GetNavigationPointsRoot()
        {
            if (navigationPointsRoot != null)
            {
                EnsureSceneRoot(navigationPointsRoot);
                return navigationPointsRoot;
            }

            var existingRoot = GameObject.Find("NavigationPointsRoot");
            if (existingRoot == null)
            {
                existingRoot = new GameObject("NavigationPointsRoot");
            }

            navigationPointsRoot = existingRoot.transform;
            EnsureSceneRoot(navigationPointsRoot);
            return navigationPointsRoot;
        }

        private static void EnsureSceneRoot(Transform root)
        {
            if (root != null && root.parent != null)
            {
                root.SetParent(null, true);
            }
        }

        private void EnsureRaycasters()
        {
            if (GetComponent<GraphicRaycaster>() == null)
            {
                gameObject.AddComponent<GraphicRaycaster>();
            }

            var trackedRaycasterType = Type.GetType("UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster, Unity.XR.Interaction.Toolkit");
            if (trackedRaycasterType != null && GetComponent(trackedRaycasterType) == null)
            {
                gameObject.AddComponent(trackedRaycasterType);
            }
        }

        private NavigationTarget GetTargetByRegistryIndex(int registryIndex)
        {
            var targets = targetRegistry != null ? targetRegistry.Targets : null;
            if (targets == null || registryIndex < 0 || registryIndex >= targets.Count)
            {
                return null;
            }

            return targets[registryIndex];
        }

        private string GetTargetDisplayName(int registryIndex)
        {
            var target = GetTargetByRegistryIndex(registryIndex);
            if (target == null || string.IsNullOrWhiteSpace(target.name))
            {
                return SelectorPlaceholder;
            }

            return $"{target.name}  [{GetFloorId(target)} / {GetPointTypeLabel(GetPointType(target))}]";
        }

        private string GetFloorId(NavigationTarget target)
        {
            var metadata = target != null ? target.GetComponent<NavigationPointMetadata>() : null;
            return metadata != null ? metadata.FloorId : CurrentFloorId;
        }

        private NavigationPointType GetPointType(NavigationTarget target)
        {
            var metadata = target != null ? target.GetComponent<NavigationPointMetadata>() : null;
            return metadata != null ? metadata.TargetType : NavigationPointType.Normal;
        }

        private string GetPointTypeLabel(NavigationPointType pointType)
        {
            return pointType switch
            {
                NavigationPointType.Room => "\u623f\u95f4",
                NavigationPointType.Stairs => "\u697c\u68af",
                NavigationPointType.Elevator => "\u7535\u68af",
                NavigationPointType.Entrance => "\u51fa\u5165\u53e3",
                _ => "\u666e\u901a\u70b9"
            };
        }

        private int FindRegistryIndex(NavigationTarget target)
        {
            var targets = targetRegistry != null ? targetRegistry.Targets : null;
            if (targets == null || target == null)
            {
                return -1;
            }

            for (var i = 0; i < targets.Count; i++)
            {
                if (targets[i] == target)
                {
                    return i;
                }
            }

            return -1;
        }

        private int GetValidTargetCount(IReadOnlyList<NavigationTarget> targets)
        {
            if (targets == null)
            {
                return 0;
            }

            var count = 0;
            for (var i = 0; i < targets.Count; i++)
            {
                if (targets[i] != null)
                {
                    count++;
                }
            }

            return count;
        }

        private List<int> GetSortedTargetIndices(IReadOnlyList<NavigationTarget> targets)
        {
            var indices = new List<int>();
            if (targets == null)
            {
                return indices;
            }

            for (var i = 0; i < targets.Count; i++)
            {
                if (targets[i] != null)
                {
                    indices.Add(i);
                }
            }

            indices.Sort((left, right) =>
            {
                var floorCompare = string.Compare(GetFloorId(targets[left]), GetFloorId(targets[right]), StringComparison.Ordinal);
                return floorCompare != 0 ? floorCompare : left.CompareTo(right);
            });

            return indices;
        }

        private void ClearSelectorButtons()
        {
            selectorButtons.Clear();
            selectorButtonLabels.Clear();
            selectorButtonRegistryIndices.Clear();

            for (var i = selectorListRoot.childCount - 1; i >= 0; i--)
            {
                DestroyChild(selectorListRoot.GetChild(i).gameObject);
            }
        }

        private void ClearTargetButtons()
        {
            targetButtons.Clear();
            targetButtonLabels.Clear();
            targetButtonRegistryIndices.Clear();

            for (var i = buttonListRoot.childCount - 1; i >= 0; i--)
            {
                DestroyChild(buttonListRoot.GetChild(i).gameObject);
            }
        }

        private void DestroyGeneratedChildren()
        {
            for (var i = transform.childCount - 1; i >= 0; i--)
            {
                DestroyChild(transform.GetChild(i).gameObject);
            }
        }

        public void ToggleVisibility()
        {
            gameObject.SetActive(!gameObject.activeSelf);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public bool IsVisible => gameObject.activeSelf;

        public void RepositionToCamera()
        {
            SnapToCamera();
            DetachFromMovingParents();
        }

        private void SnapToCamera()
        {
            var mainCamera = GetUserCamera();
            if (mainCamera == null) return;
            var cameraTransform = mainCamera.transform;
            var flatForward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up);
            if (flatForward.sqrMagnitude < 0.001f) flatForward = cameraTransform.forward;
            flatForward.Normalize();
            transform.position = cameraTransform.position + flatForward * followDistance + Vector3.up * followHeightOffset;
            transform.rotation = Quaternion.LookRotation(-flatForward, Vector3.up);
        }

        private void FollowCamera()
        {
            var mainCamera = GetUserCamera();
            if (mainCamera == null)
            {
                return;
            }

            var cameraTransform = mainCamera.transform;
            var flatForward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up);
            if (flatForward.sqrMagnitude < 0.001f)
            {
                flatForward = cameraTransform.forward;
            }

            flatForward.Normalize();
            var targetPosition = cameraTransform.position + flatForward * followDistance + Vector3.up * followHeightOffset;
            var targetRotation = Quaternion.LookRotation(-flatForward, Vector3.up);

            // Snap immediately when panel is too far off (e.g. first frame or teleport)
            if (Vector3.Distance(transform.position, targetPosition) > snapDistanceThreshold)
            {
                transform.position = targetPosition;
                transform.rotation = targetRotation;
                return;
            }

            var lerp = 1f - Mathf.Exp(-followLerpSpeed * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, targetPosition, lerp);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, lerp);
        }

        private void DetachFromMovingParents()
        {
            if (transform.parent != null)
            {
                transform.SetParent(null, true);
            }
        }

        private static void DestroyChild(GameObject child)
        {
            if (Application.isPlaying)
            {
                Destroy(child);
            }
            else
            {
                DestroyImmediate(child);
            }
        }

        private static Camera GetUserCamera()
        {
            var cameras = FindObjectsByType<Camera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var candidate in cameras)
            {
                if (candidate != null && candidate.enabled && candidate.name == "CenterEyeAnchor")
                {
                    return candidate;
                }
            }

            return Camera.main;
        }

        private Image CreateImage(string name, Transform parent, Color color)
        {
            var imageObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            imageObject.transform.SetParent(parent, false);
            var image = imageObject.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private Button CreateButton(string name, Transform parent, string label, Color color, float height, int fontSize)
        {
            var image = CreateImage(name, parent, color);
            var rect = image.rectTransform;
            rect.sizeDelta = new Vector2(0f, height);

            var button = image.gameObject.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1f);
            colors.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
            colors.selectedColor = Color.white;
            button.colors = colors;

            var text = CreateText("Label", rect, label, fontSize, FontStyle.Normal, TextAnchor.MiddleCenter);
            text.color = Color.white;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 20;
            text.resizeTextMaxSize = fontSize;
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = new Vector2(24f, 10f);
            text.rectTransform.offsetMax = new Vector2(-24f, -10f);

            return button;
        }

        private Text CreateGroupHeader(string name, Transform parent, string label)
        {
            var header = CreateText(name, parent, label, 28, FontStyle.Bold, TextAnchor.MiddleLeft);
            header.color = statusReadyColor;
            header.rectTransform.anchorMin = new Vector2(0f, 1f);
            header.rectTransform.anchorMax = new Vector2(1f, 1f);
            header.rectTransform.pivot = new Vector2(0.5f, 1f);
            return header;
        }

        private Text CreateText(string name, Transform parent, string text, int fontSize, FontStyle fontStyle, TextAnchor alignment)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.transform.SetParent(parent, false);

            var label = textObject.GetComponent<Text>();
            label.text = text;
            label.font = uiFont != null ? uiFont : Resources.GetBuiltinResource<Font>("Arial.ttf");
            label.fontSize = fontSize;
            label.fontStyle = fontStyle;
            label.alignment = alignment;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            return label;
        }

        private static void SetAnchoredRect(RectTransform rect, Vector2 anchoredPosition, Vector2 size)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
        }

        private static void SetStretchTop(RectTransform rect, float left, float top, float right, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(left, top - height);
            rect.offsetMax = new Vector2(-right, top);
        }
    }
}
