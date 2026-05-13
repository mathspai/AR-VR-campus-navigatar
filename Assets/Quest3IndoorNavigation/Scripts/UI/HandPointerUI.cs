using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Quest3IndoorNavigation.UI
{
    /// <summary>
    /// Quest 3 hand interaction for WorldSpace UI.
    /// Strategy: hand pointer ray + index-pinch click.
    /// Both hands pinch simultaneously for 0.8 s to toggle UI panel.
    /// </summary>
    public class HandPointerUI : MonoBehaviour
    {
        [Header("Ray")]
        [SerializeField] private float maxDistance = 8f;
        [SerializeField] private float pinchThreshold = 0.7f;

        [Header("Toggle")]
        [SerializeField] private QuestNavigationWorldSpaceUI navigationUI;
        [SerializeField] private float bothPinchDuration = 0.8f;
        [SerializeField] private float repositionPinchMinDuration = 0.25f;
        [SerializeField] private float repositionCooldown = 1.0f;
        [SerializeField] private OVRHand leftHand;
        [SerializeField] private OVRHand rightHand;

        private LineRenderer gazeRay;
        private GameObject hovered;
        private Canvas debugCanvas;
        private Text debugText;
        private float bothPinchTimer;
        private bool toggleFired;
        private bool wasBothPinching;
        private bool wasLeftPinching;
        private bool wasRightPinching;
        private float nextHandStatusLogTime;
        private float lastClickFiredTime = -10f;
        private float lastLeftCloseTime = -10f;
        private float lastRepositionTime = -10f;
        private bool lastRayHitUI;
        private string lastRayHitName = "none";
        private string lastRaySource = "none";

        private readonly List<RaycastResult> hits = new();
        private GraphicRaycaster[] raycasters;

        private void Awake()
        {
            Debug.Log("[HandPointerUI] Awake");
        }

        private void Start()
        {
            if (navigationUI == null)
                navigationUI = FindFirstObjectByType<QuestNavigationWorldSpaceUI>();

            ResolveHands();
            raycasters = FindObjectsOfType<GraphicRaycaster>();

            gazeRay = CreateLine("GazeRay", new Color(0.4f, 0.9f, 1f, 0.55f));
            CreateDebugPanel();
            Debug.Log($"[HandPointerUI] Start navigationUI={navigationUI != null} raycasters={raycasters.Length} leftHand={leftHand != null} rightHand={rightHand != null}");
        }

        private void Update()
        {
            var cam = GetUserCamera();
            if (cam == null) return;

            ResolveHands();

            var leftPinch = IsIndexPinching(leftHand);
            var rightPinch = IsIndexPinching(rightHand);
            var leftPinchDown = leftPinch && !wasLeftPinching;
            var rightPinchDown = rightPinch && !wasRightPinching;
            wasLeftPinching = leftPinch;
            wasRightPinching = rightPinch;

            var leftCloseFired = leftPinchDown && !rightPinch && navigationUI != null && navigationUI.IsVisible;
            if (leftCloseFired)
            {
                navigationUI.Hide();
                lastLeftCloseTime = Time.time;
            }

            // --- Ray from active hand pointer; camera gaze is only a fallback diagnostic path. ---
            GetPointerRay(cam, leftPinch, rightPinch, out var origin, out var direction);

            var hitObj = CastRay(origin, direction, out var hitPoint);
            lastRayHitUI = hitObj != null;
            lastRayHitName = hitObj != null ? hitObj.name : "none";

            gazeRay.enabled = true;
            gazeRay.SetPosition(0, origin + direction * 0.02f);
            gazeRay.SetPosition(1, hitObj != null ? hitPoint : origin + direction * maxDistance);
            gazeRay.startColor = hitObj != null ? new Color(0.2f, 1f, 0.5f, 0.8f) : new Color(0.4f, 0.9f, 1f, 0.55f);
            gazeRay.endColor   = new Color(gazeRay.startColor.r, gazeRay.startColor.g, gazeRay.startColor.b, 0f);

            // --- Hover ---
            var eventTarget = hitObj != null ? ExecuteEvents.GetEventHandler<IPointerClickHandler>(hitObj) : null;

            if (eventTarget != hovered)
            {
                var evtData = new PointerEventData(EventSystem.current);
                if (hovered != null) ExecuteEvents.Execute(hovered, evtData, ExecuteEvents.pointerExitHandler);
                if (eventTarget  != null) ExecuteEvents.Execute(eventTarget,  evtData, ExecuteEvents.pointerEnterHandler);
                hovered = eventTarget;
            }

            // --- Click on pinch start ---
            if (rightPinchDown && !leftCloseFired && eventTarget != null)
            {
                var evtData = new PointerEventData(EventSystem.current)
                {
                    pointerPress = eventTarget,
                    pointerEnter = eventTarget,
                    eligibleForClick = true,
                    button = PointerEventData.InputButton.Left,
                    clickCount = 1,
                    clickTime = Time.unscaledTime
                };
                ExecuteEvents.Execute(eventTarget, evtData, ExecuteEvents.pointerDownHandler);
                ExecuteEvents.Execute(eventTarget, evtData, ExecuteEvents.pointerUpHandler);
                ExecuteEvents.Execute(eventTarget, evtData, ExecuteEvents.pointerClickHandler);
                lastClickFiredTime = Time.time;
            }

            // --- Both-hands pinch: short (<0.8s) = pull menu to view, long (>=0.8s) = toggle ---
            if (leftPinch && rightPinch)
            {
                bothPinchTimer += Time.deltaTime;
                if (bothPinchTimer >= bothPinchDuration && !toggleFired)
                {
                    navigationUI?.ToggleVisibility();
                    toggleFired = true;
                }
                wasBothPinching = true;
            }
            else
            {
                if (ShouldRepositionOnBothPinchRelease())
                {
                    navigationUI?.RepositionToCamera();
                    lastRepositionTime = Time.time;
                }

                bothPinchTimer  = 0f;
                toggleFired     = false;
                wasBothPinching = false;
            }

            UpdateDebugPanel(leftPinch, rightPinch);
            LogHandStatus(leftPinch, rightPinch);
        }

        private bool ShouldRepositionOnBothPinchRelease()
        {
            return wasBothPinching &&
                   !toggleFired &&
                   bothPinchTimer >= repositionPinchMinDuration &&
                   Time.time - lastRepositionTime >= repositionCooldown;
        }

        private GameObject CastRay(Vector3 origin, Vector3 dir, out Vector3 hitPoint)
        {
            hitPoint = origin + dir * maxDistance;

            if (raycasters == null || raycasters.Length == 0)
            {
                raycasters = FindObjectsOfType<GraphicRaycaster>();
            }

            foreach (var rc in raycasters)
            {
                if (rc == null || !rc.gameObject.activeInHierarchy) continue;

                var canvas = rc.GetComponent<Canvas>();
                if (canvas == null) continue;

                var plane = new Plane(-canvas.transform.forward, canvas.transform.position);
                if (!plane.Raycast(new Ray(origin, dir), out var dist) || dist > maxDistance) continue;

                var worldHit = origin + dir * dist;
                hitPoint     = worldHit;

                var camera = GetUserCamera();
                if (camera == null) continue;

                var screenPt = camera.WorldToScreenPoint(worldHit);
                if (screenPt.z <= 0f) continue;

                var evtData = new PointerEventData(EventSystem.current) { position = screenPt };
                hits.Clear();
                rc.Raycast(evtData, hits);
                if (hits.Count > 0) return hits[0].gameObject;
            }

            return null;
        }

        private void GetPointerRay(Camera cam, bool leftPinch, bool rightPinch, out Vector3 origin, out Vector3 direction)
        {
            var hand = GetActivePointerHand(leftPinch, rightPinch);
            if (hand != null && hand.IsPointerPoseValid)
            {
                var pointer = hand.PointerPose;
                origin = pointer.position;
                direction = pointer.forward;
                lastRaySource = hand.GetHand() == OVRPlugin.Hand.HandLeft ? "left hand" : "right hand";
                return;
            }

            origin = cam.transform.position;
            direction = cam.transform.forward;
            lastRaySource = "camera fallback";
        }

        private OVRHand GetActivePointerHand(bool leftPinch, bool rightPinch)
        {
            if (rightPinch && IsUsablePointer(rightHand)) return rightHand;
            if (leftPinch && IsUsablePointer(leftHand)) return leftHand;
            if (IsUsablePointer(rightHand)) return rightHand;
            if (IsUsablePointer(leftHand)) return leftHand;
            return null;
        }

        private static bool IsUsablePointer(OVRHand hand)
        {
            return hand != null && hand.IsTracked && hand.IsDataValid && hand.IsPointerPoseValid;
        }

        private LineRenderer CreateLine(string name, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.positionCount  = 2;
            lr.startWidth     = 0.004f;
            lr.endWidth       = 0.001f;
            lr.useWorldSpace  = true;
            lr.material       = new Material(Shader.Find("Sprites/Default"));
            lr.startColor     = color;
            lr.endColor       = new Color(color.r, color.g, color.b, 0f);
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            return lr;
        }

        private void CreateDebugPanel()
        {
            var cam = GetUserCamera();

            var go = new GameObject("HandTrackingDebugPanel", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(null, false);
            PositionDebugPanel(cam, go.transform);
            go.transform.localScale = Vector3.one * 0.0014f;

            debugCanvas = go.GetComponent<Canvas>();
            debugCanvas.renderMode = RenderMode.WorldSpace;
            debugCanvas.worldCamera = cam;

            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(620f, 280f);

            var bg = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            bg.transform.SetParent(go.transform, false);
            var bgRect = bg.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            bg.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.72f);

            var textGo = new GameObject("StatusText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textGo.transform.SetParent(go.transform, false);
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(24f, 18f);
            textRect.offsetMax = new Vector2(-24f, -18f);

            debugText = textGo.GetComponent<Text>();
            debugText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            debugText.fontSize = 30;
            debugText.alignment = TextAnchor.UpperLeft;
            debugText.color = Color.white;
            debugText.horizontalOverflow = HorizontalWrapMode.Wrap;
            debugText.verticalOverflow = VerticalWrapMode.Truncate;
        }

        private static void PositionDebugPanel(Camera cam, Transform panel)
        {
            if (cam == null || panel == null)
            {
                return;
            }

            var cameraTransform = cam.transform;
            var flatForward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up);
            if (flatForward.sqrMagnitude < 0.001f)
            {
                flatForward = cameraTransform.forward;
            }

            flatForward.Normalize();
            panel.position = cameraTransform.position + flatForward * 1.15f + Vector3.up * -0.34f - cameraTransform.right * 0.38f;
            panel.rotation = Quaternion.LookRotation(flatForward, Vector3.up);
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

        private void UpdateDebugPanel(bool leftPinch, bool rightPinch)
        {
            if (debugCanvas == null || debugText == null)
            {
                CreateDebugPanel();
            }

            var clickRecent = Time.time - lastClickFiredTime < 0.7f;
            debugText.text =
                "HAND DEBUG\n" +
                $"Left tracked: {leftHand != null && leftHand.IsTracked}\n" +
                $"Right tracked: {rightHand != null && rightHand.IsTracked}\n" +
                $"Left index pinch: {leftPinch}\n" +
                $"Right index pinch: {rightPinch}\n" +
                $"Ray source: {lastRaySource}\n" +
                $"Ray hit UI: {lastRayHitUI} ({lastRayHitName})\n" +
                $"Click fired: {clickRecent}\n" +
                $"Left close fired: {Time.time - lastLeftCloseTime < 0.7f}\n" +
                $"OVR focus: vr={OVRManager.hasVrFocus} input={OVRManager.hasInputFocus}";
        }

        private void ResolveHands()
        {
            if (leftHand != null && rightHand != null)
            {
                return;
            }

            var hands = FindObjectsOfType<OVRHand>();
            foreach (var hand in hands)
            {
                if (hand == null)
                {
                    continue;
                }

                if (hand.GetHand() == OVRPlugin.Hand.HandLeft)
                {
                    leftHand = hand;
                }
                else if (hand.GetHand() == OVRPlugin.Hand.HandRight)
                {
                    rightHand = hand;
                }
            }
        }

        private bool IsIndexPinching(OVRHand hand)
        {
            return hand != null &&
                hand.IsDataValid &&
                hand.GetFingerIsPinching(OVRHand.HandFinger.Index);
        }

        private void LogHandStatus(bool leftPinch, bool rightPinch)
        {
            if (Time.time < nextHandStatusLogTime)
            {
                return;
            }

            nextHandStatusLogTime = Time.time + 1f;
            Debug.Log(
                $"[HandPointerUI] leftExists={leftHand != null} leftTracked={leftHand != null && leftHand.IsTracked} " +
                $"leftDataValid={leftHand != null && leftHand.IsDataValid} leftPinch={leftPinch} " +
                $"rightExists={rightHand != null} rightTracked={rightHand != null && rightHand.IsTracked} " +
                $"rightDataValid={rightHand != null && rightHand.IsDataValid} rightPinch={rightPinch} " +
                $"raySource={lastRaySource} rayHitUI={lastRayHitUI} rayHitName={lastRayHitName} clickFired={Time.time - lastClickFiredTime < 0.7f} leftCloseFired={Time.time - lastLeftCloseTime < 0.7f} " +
                $"vrFocus={OVRManager.hasVrFocus} inputFocus={OVRManager.hasInputFocus}");
        }
    }
}
