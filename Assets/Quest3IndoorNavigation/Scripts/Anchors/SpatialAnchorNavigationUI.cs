using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Meta.XR.MRUtilityKit;
using UnityEngine;
using UnityEngine.UI;

namespace Quest3IndoorNavigation.Anchors
{
    [DisallowMultipleComponent]
    public sealed class SpatialAnchorNavigationUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SpatialAnchorStore anchorStore;
        [SerializeField] private SimpleAnchorNavigator navigator;
        [SerializeField] private Transform anchorRoot;
        [SerializeField] private Button placeAnchorButton;
        [SerializeField] private Button selectAnchorButton;
        [SerializeField] private Button startNavigationButton;
        [SerializeField] private Button clearAnchorsButton;
        [SerializeField] private Button openNamePanelButton;
        [SerializeField] private Text statusText;
        [SerializeField] private Text selectedAnchorLabel;
        [SerializeField] private RectTransform rightPanelRoot;
        [SerializeField] private Text rightPanelHeader;

        [Header("Placement")]
        [SerializeField] private float anchorHeightAboveFloor = 0.05f;
        [SerializeField] private AnchorPointKind newAnchorKind = AnchorPointKind.Destination;
        [SerializeField] private Material markerMaterial;
        [SerializeField] private string[] anchorNamePresets = new[]
            { "Entrance", "Exit", "Master Bedroom", "Bedroom", "Master Bath", "Bathroom",
              "Balcony", "Living Room", "Kitchen", "Hallway", "Lobby", "Restroom",
              "Stairs", "Elevator", "Meeting Room", "Office", "Storage", "Activity" };

        private enum RightPanelMode { None, Names, Anchors }

        private readonly List<Button> rightPanelButtons = new();
        private List<AnchorPointRecord> records = new();
        private AnchorPointRecord selectedAnchor;
        private bool busy;
        private int selectedNameIndex = -1;
        private RightPanelMode panelMode = RightPanelMode.None;
        private RectTransform rightPanelContentRoot;
        private ScrollRect rightPanelScrollRect;

        public Text StatusText => statusText;

        private void Awake()
        {
            ResolveReferences();
            EnsureRightPanelScrollContent();
            if (rightPanelRoot != null)
                rightPanelRoot.gameObject.SetActive(false);
        }

        private void Start()
        {
            SuppressBoundary();
            _ = AutoLoadAnchorsAsync();
        }

        private void SuppressBoundary()
        {
            if (OVRManager.instance != null)
                OVRManager.instance.shouldBoundaryVisibilityBeSuppressed = true;
        }

        private async Task AutoLoadAnchorsAsync()
        {
            await Task.Delay(2500);
            ResolveReferences();
            var saved = anchorStore.LoadPoints();
            foreach (var record in saved)
            {
                await navigator.GetOrLoadAnchorAsync(record);
            }
            RefreshRecords();
            SetStatus(saved.Count > 0 ? $"Loaded {saved.Count} anchors" : "Ready");
        }

        private static float GetFloorY()
        {
            if (MRUK.Instance != null)
            {
                var room = MRUK.Instance.GetCurrentRoom();
                if (room?.FloorAnchors?.Count > 0)
                    return room.FloorAnchors[0].transform.position.y;
            }
            return 0f;
        }

        private void OnEnable()
        {
            placeAnchorButton?.onClick.AddListener(PlaceAnchor);
            selectAnchorButton?.onClick.AddListener(OpenAnchorPanel);
            startNavigationButton?.onClick.AddListener(StartNavigation);
            clearAnchorsButton?.onClick.AddListener(ClearAnchors);
            openNamePanelButton?.onClick.AddListener(OpenNamePanel);
            RefreshRecords();
            SetStatus("Ready");
        }

        private void OnDisable()
        {
            placeAnchorButton?.onClick.RemoveListener(PlaceAnchor);
            selectAnchorButton?.onClick.RemoveListener(OpenAnchorPanel);
            startNavigationButton?.onClick.RemoveListener(StartNavigation);
            clearAnchorsButton?.onClick.RemoveListener(ClearAnchors);
            openNamePanelButton?.onClick.RemoveListener(OpenNamePanel);
        }

        public void PlaceAnchor()
        {
            if (!busy) _ = PlaceAnchorAsync();
        }

        public void StartNavigation()
        {
            if (!busy) _ = StartNavigationAsync();
        }

        private void CancelNavigation()
        {
            if (navigator == null) return;
            navigator.OnArrivedAtDestination -= OnNavigationArrived;
            navigator.StopNavigation();
            SetSelectedLabel("");
        }

        private void OnNavigationArrived()
        {
            navigator.OnArrivedAtDestination -= OnNavigationArrived;
            SetStatus("Arrived at destination");
        }

        public void ClearAnchors()
        {
            if (busy) return;
            CloseRightPanel();
            CancelNavigation();
            anchorStore?.Clear();
            selectedAnchor = null;
            SetSelectedLabel("");
            var root = GetAnchorRoot();
            for (var i = root.childCount - 1; i >= 0; i--)
                Destroy(root.GetChild(i).gameObject);
            RefreshRecords();
            SetStatus("All anchors cleared");
        }

        public void OpenNamePanel()
        {
            ShowRightPanel("Select Name", RightPanelMode.Names);
        }

        public void OpenAnchorPanel()
        {
            RefreshRecords();
            ShowRightPanel("Select Anchor", RightPanelMode.Anchors);
        }

        private void ShowRightPanel(string header, RightPanelMode mode)
        {
            if (rightPanelRoot == null) return;
            EnsureRightPanelScrollContent();
            panelMode = mode;
            if (rightPanelHeader != null) rightPanelHeader.text = header;
            rightPanelRoot.gameObject.SetActive(true);
            if (rightPanelScrollRect != null) rightPanelScrollRect.verticalNormalizedPosition = 1f;
            RebuildRightPanel();
        }

        private void CloseRightPanel()
        {
            if (rightPanelRoot != null) rightPanelRoot.gameObject.SetActive(false);
            panelMode = RightPanelMode.None;
        }

        private void RebuildRightPanel()
        {
            EnsureRightPanelScrollContent();
            var contentRoot = rightPanelContentRoot != null ? rightPanelContentRoot : rightPanelRoot;
            for (var i = contentRoot.childCount - 1; i >= 0; i--)
                Destroy(contentRoot.GetChild(i).gameObject);
            rightPanelButtons.Clear();

            if (panelMode == RightPanelMode.Names)
            {
                // Auto-number option at row 0
                var autoColor = selectedNameIndex < 0
                    ? new Color(0.1f, 0.45f, 0.36f, 0.95f)
                    : new Color(0.12f, 0.18f, 0.22f, 0.95f);
                var autoBtn = CreateRightButton(0, "Auto", autoColor);
                autoBtn.onClick.AddListener(() => SelectName(-1));
                rightPanelButtons.Add(autoBtn);

                for (var i = 0; i < anchorNamePresets.Length; i++)
                {
                    var idx = i;
                    var col = selectedNameIndex == i
                        ? new Color(0.1f, 0.45f, 0.36f, 0.95f)
                        : new Color(0.12f, 0.18f, 0.22f, 0.95f);
                    var btn = CreateRightButton(i + 1, anchorNamePresets[i], col);
                    btn.onClick.AddListener(() => SelectName(idx));
                    rightPanelButtons.Add(btn);
                }
            }
            else if (panelMode == RightPanelMode.Anchors)
            {
                for (var i = 0; i < records.Count; i++)
                {
                    var record = records[i];
                    var isSelected = selectedAnchor != null && record.uuid == selectedAnchor.uuid;
                    var col = isSelected
                        ? new Color(0.1f, 0.45f, 0.36f, 0.95f)
                        : new Color(0.12f, 0.18f, 0.22f, 0.95f);
                    var row = CreateAnchorRow(i, record, col);
                    rightPanelButtons.Add(row);
                }
            }

            UpdateRightPanelContentHeight();
        }

        private void SelectName(int index)
        {
            selectedNameIndex = index;
            var label = index < 0 ? "Auto" : anchorNamePresets[index];
            RebuildRightPanel();
            SetStatus($"Name: {label}");
        }

        private void SelectAnchor(AnchorPointRecord record)
        {
            selectedAnchor = record;
            CloseRightPanel();
            SetSelectedLabel(record.displayName);
            SetStatus($"Selected: {record.displayName}");
        }

        private void DeleteAnchor(AnchorPointRecord record)
        {
            if (selectedAnchor?.uuid == record.uuid)
            {
                selectedAnchor = null;
                CancelNavigation();
            }

            anchorStore?.Remove(record.uuid);

            var all = FindObjectsByType<SpatialAnchorPoint>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var sp in all)
            {
                if (sp.Uuid == record.uuid)
                {
                    Destroy(sp.gameObject);
                    break;
                }
            }

            RefreshRecords();
            if (panelMode == RightPanelMode.Anchors)
                RebuildRightPanel();
            SetStatus($"Deleted: {record.displayName}");
        }

        private string GetNextAnchorName()
        {
            if (selectedNameIndex >= 0 && anchorNamePresets != null && selectedNameIndex < anchorNamePresets.Length)
                return anchorNamePresets[selectedNameIndex];
            return $"Anchor {records.Count + 1}";
        }

        private async Task PlaceAnchorAsync()
        {
            busy = true;
            SetButtonsInteractable(false);
            SetStatus("Creating...");

            try
            {
                var pose = GetPlacementPose();
                var pointObject = new GameObject("Spatial Anchor Point");
                pointObject.transform.SetParent(GetAnchorRoot(), true);
                pointObject.transform.SetPositionAndRotation(pose.position, pose.rotation);

                var spatialAnchor = pointObject.AddComponent<OVRSpatialAnchor>();
                var created = await spatialAnchor.WhenCreatedAsync();
                if (!created)
                {
                    Destroy(pointObject);
                    SetStatus("Create failed");
                    return;
                }

                var saveResult = await spatialAnchor.SaveAnchorAsync();
                if (!saveResult.Success)
                {
                    Destroy(pointObject);
                    SetStatus($"Save failed: {saveResult.Status}");
                    return;
                }

                var uuid = spatialAnchor.Uuid.ToString();
                var displayName = GetNextAnchorName();
                pointObject.name = displayName;

                var point = pointObject.AddComponent<SpatialAnchorPoint>();
                point.Initialize(uuid, displayName, newAnchorKind);
                CreateMarker(pointObject.transform);

                var record = new AnchorPointRecord(uuid, displayName, newAnchorKind);
                anchorStore.Upsert(record);
                navigator.RegisterSceneAnchor(point);
                selectedAnchor = record;
                RefreshRecords();
                CloseRightPanel();
                SetStatus($"Saved: {displayName}");
            }
            catch (Exception e)
            {
                SetStatus($"Create failed: {e.Message}");
                Debug.LogException(e);
            }
            finally
            {
                busy = false;
                SetButtonsInteractable(true);
            }
        }

        private async Task StartNavigationAsync()
        {
            if (selectedAnchor == null)
            {
                SetStatus("Select an anchor first");
                return;
            }

            busy = true;
            SetButtonsInteractable(false);
            SetStatus("Loading...");

            try
            {
                var point = await navigator.GetOrLoadAnchorAsync(selectedAnchor);
                if (point == null)
                {
                    SetStatus("Load failed");
                    return;
                }

                navigator.OnArrivedAtDestination -= OnNavigationArrived;
                navigator.OnArrivedAtDestination += OnNavigationArrived;
                navigator.StartNavigation(point.transform);
                SetStatus($"Navigating: {point.DisplayName}");
            }
            catch (Exception e)
            {
                SetStatus($"Load failed: {e.Message}");
                Debug.LogException(e);
            }
            finally
            {
                busy = false;
                SetButtonsInteractable(true);
            }
        }

        private void RefreshRecords()
        {
            ResolveReferences();
            records = new List<AnchorPointRecord>(anchorStore.LoadPoints());
        }

        // Creates a row button for the right panel (names or plain items)
        private Button CreateRightButton(int row, string labelText, Color bgColor)
        {
            var obj = new GameObject($"RightItem_{row}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            obj.transform.SetParent(rightPanelContentRoot != null ? rightPanelContentRoot : rightPanelRoot, false);
            LayoutRightRow(obj.GetComponent<RectTransform>(), row);
            obj.GetComponent<Image>().color = bgColor;
            AddLabel(obj.transform, labelText, 0f, 1f);
            return obj.GetComponent<Button>();
        }

        // Creates a composite row with select area (left) + delete button (right) for anchors
        private Button CreateAnchorRow(int row, AnchorPointRecord record, Color bgColor)
        {
            var rowObj = new GameObject($"AnchorRow_{row}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            rowObj.transform.SetParent(rightPanelContentRoot != null ? rightPanelContentRoot : rightPanelRoot, false);
            LayoutRightRow(rowObj.GetComponent<RectTransform>(), row);
            rowObj.GetComponent<Image>().color = bgColor;

            // Select area (left 78%)
            var selObj = new GameObject("SelectArea", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            selObj.transform.SetParent(rowObj.transform, false);
            var selRect = selObj.GetComponent<RectTransform>();
            selRect.anchorMin = Vector2.zero;
            selRect.anchorMax = new Vector2(0.78f, 1f);
            selRect.offsetMin = Vector2.zero;
            selRect.offsetMax = Vector2.zero;
            selObj.GetComponent<Image>().color = Color.clear;
            AddLabel(selObj.transform, $"{record.displayName}", 0f, 0f);
            var selBtn = selObj.GetComponent<Button>();
            var captured = record;
            selBtn.onClick.AddListener(() => SelectAnchor(captured));

            // Delete button (right 20%)
            var delObj = new GameObject("DeleteBtn", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            delObj.transform.SetParent(rowObj.transform, false);
            var delRect = delObj.GetComponent<RectTransform>();
            delRect.anchorMin = new Vector2(0.80f, 0.1f);
            delRect.anchorMax = new Vector2(1f, 0.9f);
            delRect.offsetMin = new Vector2(4f, 0f);
            delRect.offsetMax = new Vector2(-8f, 0f);
            delObj.GetComponent<Image>().color = new Color(0.6f, 0.1f, 0.1f, 0.95f);
            AddLabel(delObj.transform, "X", 0f, 0f);
            var delBtn = delObj.GetComponent<Button>();
            delBtn.onClick.AddListener(() => DeleteAnchor(captured));

            return selBtn;
        }

        private void EnsureRightPanelScrollContent()
        {
            if (rightPanelRoot == null)
            {
                return;
            }

            if (rightPanelContentRoot == null)
            {
                var content = rightPanelRoot.Find("RightPanelContent");
                if (content == null)
                {
                    var contentObj = new GameObject("RightPanelContent", typeof(RectTransform));
                    contentObj.transform.SetParent(rightPanelRoot, false);
                    content = contentObj.transform;
                }

                rightPanelContentRoot = content.GetComponent<RectTransform>();
                rightPanelContentRoot.SetAsFirstSibling();
            }

            if (rightPanelScrollRect == null)
            {
                rightPanelScrollRect = rightPanelRoot.GetComponent<ScrollRect>();
                if (rightPanelScrollRect == null)
                {
                    rightPanelScrollRect = rightPanelRoot.gameObject.AddComponent<ScrollRect>();
                }
            }

            rightPanelContentRoot.anchorMin = new Vector2(0f, 1f);
            rightPanelContentRoot.anchorMax = new Vector2(1f, 1f);
            rightPanelContentRoot.pivot = new Vector2(0.5f, 1f);
            rightPanelContentRoot.offsetMax = Vector2.zero;

            rightPanelScrollRect.viewport = rightPanelRoot;
            rightPanelScrollRect.content = rightPanelContentRoot;
            rightPanelScrollRect.horizontal = false;
            rightPanelScrollRect.vertical = true;
            rightPanelScrollRect.movementType = ScrollRect.MovementType.Clamped;
            rightPanelScrollRect.inertia = true;
            rightPanelScrollRect.scrollSensitivity = 80f;
        }

        private void UpdateRightPanelContentHeight()
        {
            if (rightPanelContentRoot == null)
            {
                return;
            }

            const float headerOffset = 58f;
            const float rowH = 44f;
            const float gap = 4f;
            var contentHeight = headerOffset + (rowH + gap) * rightPanelButtons.Count + gap;
            var panelHeight = rightPanelRoot != null ? rightPanelRoot.rect.height : 0f;
            contentHeight = Mathf.Max(contentHeight, panelHeight);
            rightPanelContentRoot.sizeDelta = new Vector2(0f, contentHeight);
            rightPanelContentRoot.anchoredPosition = Vector2.zero;
        }

        private static void LayoutRightRow(RectTransform rect, int row)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            const float headerOffset = 58f; // header height (54) + gap
            const float rowH = 44f;
            const float gap = 4f;
            float topY = -(headerOffset + (rowH + gap) * row + gap);
            rect.offsetMin = new Vector2(4f, topY - rowH);
            rect.offsetMax = new Vector2(-4f, topY);
        }

        private static void AddLabel(Transform parent, string text, float leftPad, float rightPad)
        {
            var labelObj = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            labelObj.transform.SetParent(parent, false);
            var r = labelObj.GetComponent<RectTransform>();
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = new Vector2(leftPad + 14f, 6f);
            r.offsetMax = new Vector2(-rightPad - 8f, -6f);
            var lbl = labelObj.GetComponent<Text>();
            lbl.text = text;
            lbl.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            lbl.fontSize = 28;
            lbl.alignment = TextAnchor.MiddleLeft;
            lbl.color = Color.white;
            lbl.resizeTextForBestFit = true;
            lbl.resizeTextMinSize = 16;
            lbl.resizeTextMaxSize = 28;
        }

        private Pose GetPlacementPose()
        {
            var uiPosition = transform.position;
            if (transform.Find("Panel") is RectTransform panelRect)
            {
                uiPosition = panelRect.TransformPoint(panelRect.rect.center);
            }

            var pos = new Vector3(uiPosition.x, GetFloorY() + anchorHeightAboveFloor, uiPosition.z);
            return new Pose(pos, Quaternion.identity);
        }

        private void CreateMarker(Transform parent)
        {
            var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.name = "Marker";
            marker.transform.SetParent(parent, false);
            marker.transform.localPosition = Vector3.zero;
            marker.transform.localScale = Vector3.one * 0.18f;
            if (markerMaterial != null)
                marker.GetComponent<MeshRenderer>().sharedMaterial = markerMaterial;
        }

        private Transform GetAnchorRoot()
        {
            if (anchorRoot != null) return anchorRoot;
            var root = GameObject.Find("SpatialAnchorPoints") ?? new GameObject("SpatialAnchorPoints");
            anchorRoot = root.transform;
            return anchorRoot;
        }

        private void ResolveReferences()
        {
            anchorStore ??= FindFirstObjectByType<SpatialAnchorStore>();
            navigator ??= FindFirstObjectByType<SimpleAnchorNavigator>();
        }

        private void SetButtonsInteractable(bool interactable)
        {
            if (placeAnchorButton != null) placeAnchorButton.interactable = interactable;
            if (selectAnchorButton != null) selectAnchorButton.interactable = interactable;
            if (startNavigationButton != null) startNavigationButton.interactable = interactable;
        }

        private void SetStatus(string message)
        {
            if (statusText != null) statusText.text = message;
        }

        private void SetSelectedLabel(string text)
        {
            if (selectedAnchorLabel != null) selectedAnchorLabel.text = text;
        }
    }
}
