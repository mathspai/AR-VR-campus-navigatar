using UnityEngine;
using UnityEngine.AI;

namespace Quest3IndoorNavigation
{
    [RequireComponent(typeof(LineRenderer))]
    public sealed class IndoorNavigationController : MonoBehaviour
    {
        [SerializeField] private Transform user;
        [SerializeField] private Transform destination;
        [SerializeField] private float sampleRadius = 1.5f;
        [SerializeField] private float pathHeightOffset = 0.04f;
        [SerializeField] private float refreshInterval = 0.25f;

        private NavMeshPath path;
        private LineRenderer lineRenderer;
        private float nextRefreshTime;

        public Transform Destination => destination;

        private void Awake()
        {
            path = new NavMeshPath();
            lineRenderer = GetComponent<LineRenderer>();
            lineRenderer.useWorldSpace = true;
            ResolveUserCamera();
        }

        private void OnEnable()
        {
            RefreshPath();
        }

        private void Update()
        {
            if (Time.time < nextRefreshTime)
            {
                return;
            }

            nextRefreshTime = Time.time + refreshInterval;
            RefreshPath();
        }

        public void SetDestination(Transform target)
        {
            destination = target;
            RefreshPath();
        }

        public void SetDestination(NavigationTarget target)
        {
            SetDestination(target != null ? target.transform : null);
        }

        public void ClearDestination()
        {
            destination = null;
            if (lineRenderer != null)
            {
                lineRenderer.positionCount = 0;
            }
        }

        private void RefreshPath()
        {
            path ??= new NavMeshPath();
            ResolveUserCamera();

            if (user == null ||
                destination == null ||
                !NavMesh.SamplePosition(user.position, out var startHit, sampleRadius, NavMesh.AllAreas) ||
                !NavMesh.SamplePosition(destination.position, out var endHit, sampleRadius, NavMesh.AllAreas) ||
                !NavMesh.CalculatePath(startHit.position, endHit.position, NavMesh.AllAreas, path) ||
                path.status == NavMeshPathStatus.PathInvalid ||
                path.corners.Length == 0)
            {
                lineRenderer.positionCount = 0;
                return;
            }

            lineRenderer.positionCount = path.corners.Length;

            for (var i = 0; i < path.corners.Length; i++)
            {
                lineRenderer.SetPosition(i, path.corners[i] + Vector3.up * pathHeightOffset);
            }
        }

        private void ResolveUserCamera()
        {
            if (user != null)
            {
                return;
            }

            var mainCamera = Camera.main;
            if (mainCamera != null)
            {
                user = mainCamera.transform;
            }
        }
    }
}
