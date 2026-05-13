using UnityEngine;
using UnityEngine.UI;

namespace Quest3IndoorNavigation.Anchors
{
    [DisallowMultipleComponent]
    public sealed class LeftHandMenuRecall : MonoBehaviour
    {
        [SerializeField] private OVRHand leftHand;
        [SerializeField] private Transform menuRoot;
        [SerializeField] private float distance = 1.15f;
        [SerializeField] private bool showOnStart;
        [SerializeField] private Text statusText;

        private bool wasPinching;

        private void Start()
        {
            if (menuRoot != null)
            {
                menuRoot.gameObject.SetActive(showOnStart);
            }

            if (showOnStart)
            {
                SpawnMenuInFrontOfCamera();
            }
            else
            {
                SetStatus("Pinch left hand to open menu");
            }
        }

        private void Update()
        {
            if (leftHand == null)
            {
                leftHand = FindLeftHand();
            }

            if (leftHand == null || !leftHand.IsTracked)
            {
                wasPinching = false;
                return;
            }

            var pinching = leftHand.GetFingerIsPinching(OVRHand.HandFinger.Index);
            if (pinching && !wasPinching)
            {
                ToggleMenuInFrontOfCamera();
            }

            wasPinching = pinching;
        }

        public void RepositionMenuToCamera()
        {
            SpawnMenuInFrontOfCamera();
        }

        public void ToggleMenuInFrontOfCamera()
        {
            if (menuRoot == null)
            {
                return;
            }

            if (menuRoot.gameObject.activeSelf)
            {
                menuRoot.gameObject.SetActive(false);
                SetStatus("Menu hidden");
                return;
            }

            SpawnMenuInFrontOfCamera();
        }

        public void SpawnMenuInFrontOfCamera()
        {
            if (menuRoot == null || Camera.main == null)
            {
                return;
            }

            var cameraTransform = Camera.main.transform;
            var flatForward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up);
            if (flatForward.sqrMagnitude < 0.001f)
            {
                flatForward = cameraTransform.forward;
            }

            flatForward.Normalize();
            menuRoot.position = cameraTransform.position + flatForward * distance;
            menuRoot.rotation = Quaternion.LookRotation(flatForward, Vector3.up);
            menuRoot.gameObject.SetActive(true);
        }

        private void SetStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }
        }

        private static OVRHand FindLeftHand()
        {
            var hands = FindObjectsByType<OVRHand>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var hand in hands)
            {
                if (hand != null && hand.GetHand() == OVRPlugin.Hand.HandLeft)
                {
                    return hand;
                }
            }

            return null;
        }
    }
}
