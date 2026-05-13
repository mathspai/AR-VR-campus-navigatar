using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Quest3IndoorNavigation.UI
{
    [DisallowMultipleComponent]
    public sealed class SimpleHandTouchUI : MonoBehaviour
    {
        [SerializeField] private OVRSkeleton leftSkeleton;
        [SerializeField] private OVRSkeleton rightSkeleton;
        [SerializeField] private Camera eventCamera;
        [SerializeField] private Text statusText;
        [SerializeField] private float touchDepth = 0.045f;
        [SerializeField] private float touchCooldown = 0.25f;

        private readonly List<RaycastResult> raycastResults = new();
        private readonly Dictionary<OVRSkeleton, TouchState> states = new();
        private GraphicRaycaster[] raycasters;

        private sealed class TouchState
        {
            public GameObject Hovered;
            public bool WasTouching;
            public float LastClickTime = -10f;
        }

        private void Awake()
        {
            raycasters = GetComponentsInChildren<GraphicRaycaster>(true);
        }

        private void Update()
        {
            ResolveReferences();
            ProcessHand(leftSkeleton);
            ProcessHand(rightSkeleton);
        }

        private void ResolveReferences()
        {
            if (eventCamera == null)
            {
                eventCamera = Camera.main;
            }

            if (raycasters == null || raycasters.Length == 0)
            {
                raycasters = GetComponentsInChildren<GraphicRaycaster>(true);
            }

            if (leftSkeleton != null && rightSkeleton != null)
            {
                return;
            }

            var skeletons = FindObjectsByType<OVRSkeleton>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var skeleton in skeletons)
            {
                if (skeleton == null)
                {
                    continue;
                }

                var type = skeleton.GetSkeletonType();
                if (type == OVRSkeleton.SkeletonType.HandLeft || type == OVRSkeleton.SkeletonType.XRHandLeft)
                {
                    leftSkeleton = skeleton;
                }
                else if (type == OVRSkeleton.SkeletonType.HandRight || type == OVRSkeleton.SkeletonType.XRHandRight)
                {
                    rightSkeleton = skeleton;
                }
            }
        }

        private void ProcessHand(OVRSkeleton skeleton)
        {
            if (skeleton == null || !skeleton.IsDataValid || !TryGetIndexTip(skeleton, out var tipPosition))
            {
                ClearHover(skeleton);
                return;
            }

            var touchedObject = RaycastTouchedUi(tipPosition);
            var target = touchedObject != null ? ExecuteEvents.GetEventHandler<IPointerClickHandler>(touchedObject) : null;
            var state = GetState(skeleton);

            if (target != state.Hovered)
            {
                var eventData = NewPointerData(tipPosition, target);
                if (state.Hovered != null)
                {
                    ExecuteEvents.Execute(state.Hovered, eventData, ExecuteEvents.pointerExitHandler);
                }

                if (target != null)
                {
                    ExecuteEvents.Execute(target, eventData, ExecuteEvents.pointerEnterHandler);
                }

                state.Hovered = target;
            }

            var touching = target != null;
            if (touching && !state.WasTouching && Time.unscaledTime - state.LastClickTime >= touchCooldown)
            {
                Click(target, tipPosition);
                state.LastClickTime = Time.unscaledTime;
                SetStatus("Button touched");
            }

            state.WasTouching = touching;
        }

        private GameObject RaycastTouchedUi(Vector3 tipPosition)
        {
            if (eventCamera == null || raycasters == null)
            {
                return null;
            }

            foreach (var raycaster in raycasters)
            {
                if (raycaster == null || !raycaster.isActiveAndEnabled)
                {
                    continue;
                }

                var canvas = raycaster.GetComponent<Canvas>();
                var rect = raycaster.GetComponent<RectTransform>();
                if (canvas == null || rect == null || !canvas.gameObject.activeInHierarchy)
                {
                    continue;
                }

                var planeDistance = Mathf.Abs(Vector3.Dot(tipPosition - rect.position, rect.forward));
                if (planeDistance > touchDepth)
                {
                    continue;
                }

                var local = rect.InverseTransformPoint(tipPosition);
                if (!rect.rect.Contains(new Vector2(local.x, local.y)))
                {
                    continue;
                }

                var screenPoint = eventCamera.WorldToScreenPoint(tipPosition);
                if (screenPoint.z <= 0f)
                {
                    continue;
                }

                var eventData = new PointerEventData(EventSystem.current) { position = screenPoint };
                raycastResults.Clear();
                raycaster.Raycast(eventData, raycastResults);
                if (raycastResults.Count > 0)
                {
                    return raycastResults[0].gameObject;
                }
            }

            return null;
        }

        private static bool TryGetIndexTip(OVRSkeleton skeleton, out Vector3 position)
        {
            foreach (var bone in skeleton.Bones)
            {
                if (bone.Id == OVRSkeleton.BoneId.Hand_IndexTip || bone.Id == OVRSkeleton.BoneId.XRHand_IndexTip)
                {
                    position = bone.Transform.position;
                    return true;
                }
            }

            position = default;
            return false;
        }

        private void Click(GameObject target, Vector3 tipPosition)
        {
            var eventData = NewPointerData(tipPosition, target);
            ExecuteEvents.Execute(target, eventData, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(target, eventData, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(target, eventData, ExecuteEvents.pointerClickHandler);
        }

        private PointerEventData NewPointerData(Vector3 tipPosition, GameObject target)
        {
            var screenPoint = eventCamera != null ? eventCamera.WorldToScreenPoint(tipPosition) : Vector3.zero;
            return new PointerEventData(EventSystem.current)
            {
                position = screenPoint,
                pointerEnter = target,
                pointerPress = target,
                eligibleForClick = true,
                button = PointerEventData.InputButton.Left,
                clickCount = 1,
                clickTime = Time.unscaledTime
            };
        }

        private TouchState GetState(OVRSkeleton skeleton)
        {
            if (!states.TryGetValue(skeleton, out var state))
            {
                state = new TouchState();
                states[skeleton] = state;
            }

            return state;
        }

        private void ClearHover(OVRSkeleton skeleton)
        {
            if (skeleton == null || !states.TryGetValue(skeleton, out var state))
            {
                return;
            }

            if (state.Hovered != null)
            {
                ExecuteEvents.Execute(state.Hovered, new PointerEventData(EventSystem.current), ExecuteEvents.pointerExitHandler);
                state.Hovered = null;
            }

            state.WasTouching = false;
        }

        private void SetStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }
        }
    }
}
