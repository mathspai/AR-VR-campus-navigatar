using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Meta.XR.MRUtilityKit;
using UnityEngine;
using UnityEngine.AI;

namespace Quest3IndoorNavigation.Anchors
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(LineRenderer))]
    public sealed class SimpleAnchorNavigator : MonoBehaviour
    {
        [SerializeField] private Transform userCamera;
        [SerializeField] private Transform anchorRoot;
        [SerializeField] private float refreshInterval = 0.2f;
        [SerializeField] private float navMeshSampleRadius = 1.5f;
        [SerializeField] private float lineHeightOffset = 0.04f;
        [SerializeField] private float arrivalRadius = 0.5f;
        [SerializeField] private Material markerMaterial;
        [SerializeField] private MonoBehaviour sceneRouteProviderBehaviour;

        private readonly Dictionary<string, SpatialAnchorPoint> sceneAnchors = new();
        private readonly List<OVRSpatialAnchor.UnboundAnchor> unboundAnchors = new();
        private readonly List<Vector3> sceneRouteBuffer = new();
        private LineRenderer lineRenderer;
        private NavMeshPath navMeshPath;
        private ISceneRouteProvider sceneRouteProvider;
        private Transform destination;
        private float nextRefreshTime;

        public event Action OnArrivedAtDestination;
        public Transform Destination => destination;

        private void Awake()
        {
            lineRenderer = GetComponent<LineRenderer>();
            lineRenderer.useWorldSpace = true;
            navMeshPath = new NavMeshPath();
            sceneRouteProvider = sceneRouteProviderBehaviour as ISceneRouteProvider;
            ResolveCamera();
            EnsureAnchorRoot();
            RebuildSceneAnchorIndex();
        }

        private void Update()
        {
            if (Time.time < nextRefreshTime)
            {
                return;
            }

            nextRefreshTime = Time.time + refreshInterval;
            RefreshLine();
        }

        public void RegisterSceneAnchor(SpatialAnchorPoint point)
        {
            if (point != null && !string.IsNullOrWhiteSpace(point.Uuid))
            {
                sceneAnchors[point.Uuid] = point;
            }
        }

        public async Task<SpatialAnchorPoint> GetOrLoadAnchorAsync(AnchorPointRecord record)
        {
            if (record == null || !Guid.TryParse(record.uuid, out var uuid))
            {
                return null;
            }

            RebuildSceneAnchorIndex();
            if (sceneAnchors.TryGetValue(record.uuid, out var existing) && existing != null)
            {
                return existing;
            }

            unboundAnchors.Clear();
            var loadResult = await OVRSpatialAnchor.LoadUnboundAnchorsAsync(new[] { uuid }, unboundAnchors);
            if (!loadResult.Success || unboundAnchors.Count == 0)
            {
                Debug.LogWarning($"[SimpleAnchorNavigator] Load failed for {record.uuid}: {loadResult.Status}");
                return null;
            }

            var unboundAnchor = unboundAnchors[0];
            var localized = await unboundAnchor.LocalizeAsync();
            if (!localized || !unboundAnchor.TryGetPose(out var pose))
            {
                Debug.LogWarning($"[SimpleAnchorNavigator] Localize failed for {record.uuid}");
                return null;
            }

            var pointObject = new GameObject(record.displayName);
            pointObject.transform.SetParent(EnsureAnchorRoot(), true);
            pointObject.transform.SetPositionAndRotation(pose.position, pose.rotation);

            var spatialAnchor = pointObject.AddComponent<OVRSpatialAnchor>();
            unboundAnchor.BindTo(spatialAnchor);

            var point = pointObject.AddComponent<SpatialAnchorPoint>();
            point.Initialize(record.uuid, record.displayName, record.pointKind);
            point.SetRouteWaypointIds(record.routeWaypointIds);
            CreateMarker(pointObject.transform, point.DisplayName);
            RegisterSceneAnchor(point);
            return point;
        }

        public void StartNavigation(Transform target)
        {
            destination = target;
            RefreshLine();
        }

        public void StopNavigation()
        {
            destination = null;
            if (lineRenderer != null)
            {
                lineRenderer.positionCount = 0;
            }
        }

        private void RefreshLine()
        {
            ResolveCamera();
            if (lineRenderer == null || userCamera == null || destination == null)
            {
                if (lineRenderer != null)
                {
                    lineRenderer.positionCount = 0;
                }
                return;
            }

            if (CheckArrival()) return;

            if (TryDrawSceneRoute())
            {
                return;
            }

            if (TryDrawNavMeshPath())
            {
                return;
            }

            lineRenderer.positionCount = 2;
            var floorY = GetFloorY();
            lineRenderer.SetPosition(0, new Vector3(userCamera.position.x, floorY + lineHeightOffset, userCamera.position.z));
            lineRenderer.SetPosition(1, new Vector3(destination.position.x, floorY + lineHeightOffset, destination.position.z));
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

        private bool TryDrawSceneRoute()
        {
            if (sceneRouteProvider == null)
            {
                return false;
            }

            var floorY = GetFloorY();
            var floorStart = new Vector3(userCamera.position.x, floorY, userCamera.position.z);
            sceneRouteBuffer.Clear();
            if (!sceneRouteProvider.TryGetRoute(floorStart, destination.position, sceneRouteBuffer) ||
                sceneRouteBuffer.Count < 2)
            {
                return false;
            }

            // Render only the portion of the route that lies within the current room.
            // Points beyond the door (in the next room or through walls) are excluded,
            // so the line never visually passes through a wall.
            var renderCount = RoomVisiblePointCount();
            if (renderCount < 2) return false;

            lineRenderer.positionCount = renderCount;
            for (var i = 0; i < renderCount; i++)
            {
                lineRenderer.SetPosition(i, sceneRouteBuffer[i] + Vector3.up * lineHeightOffset);
            }

            return true;
        }

        // Returns how many leading route points can be rendered without crossing a room wall.
        // Falls back to the full count when MRUK room data is unavailable.
        private int RoomVisiblePointCount()
        {
            if (MRUK.Instance == null) return sceneRouteBuffer.Count;
            var room = MRUK.Instance.GetCurrentRoom();
            if (room == null) return sceneRouteBuffer.Count;

            // Walk from the end back to find the last point still inside the current room.
            for (var i = sceneRouteBuffer.Count - 1; i >= 1; i--)
            {
                if (room.IsPositionInRoom(sceneRouteBuffer[i], false))
                    return i + 1;
            }
            return sceneRouteBuffer.Count;
        }

        private bool TryDrawNavMeshPath()
        {
            if (!NavMesh.SamplePosition(userCamera.position, out var startHit, navMeshSampleRadius, NavMesh.AllAreas) ||
                !NavMesh.SamplePosition(destination.position, out var endHit, navMeshSampleRadius, NavMesh.AllAreas) ||
                !NavMesh.CalculatePath(startHit.position, endHit.position, NavMesh.AllAreas, navMeshPath) ||
                navMeshPath.status == NavMeshPathStatus.PathInvalid ||
                navMeshPath.corners == null ||
                navMeshPath.corners.Length < 2)
            {
                return false;
            }

            lineRenderer.positionCount = navMeshPath.corners.Length;
            for (var i = 0; i < navMeshPath.corners.Length; i++)
            {
                lineRenderer.SetPosition(i, navMeshPath.corners[i] + Vector3.up * lineHeightOffset);
            }

            return true;
        }

        private void RebuildSceneAnchorIndex()
        {
            sceneAnchors.Clear();
            foreach (var point in FindObjectsByType<SpatialAnchorPoint>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                RegisterSceneAnchor(point);
            }
        }

        private Transform EnsureAnchorRoot()
        {
            if (anchorRoot != null)
            {
                return anchorRoot;
            }

            var root = GameObject.Find("SpatialAnchorPoints");
            if (root == null)
            {
                root = new GameObject("SpatialAnchorPoints");
            }

            anchorRoot = root.transform;
            return anchorRoot;
        }

        private void ResolveCamera()
        {
            if (userCamera != null)
            {
                return;
            }

            userCamera = Camera.main != null ? Camera.main.transform : null;
        }

        private void CreateMarker(Transform parent, string label)
        {
            var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.name = "Marker";
            marker.transform.SetParent(parent, false);
            marker.transform.localPosition = Vector3.zero;
            marker.transform.localScale = Vector3.one * 0.18f;
            if (markerMaterial != null)
            {
                marker.GetComponent<MeshRenderer>().sharedMaterial = markerMaterial;
            }
        }

        // Stops navigation when user is within arrivalRadius of the destination anchor.
        private bool CheckArrival()
        {
            var camFlat = new Vector3(userCamera.position.x, destination.position.y, userCamera.position.z);
            if (Vector3.Distance(camFlat, destination.position) > arrivalRadius) return false;
            OnArrivedAtDestination?.Invoke();
            StopNavigation();
            return true;
        }
    }

    public interface ISceneRouteProvider
    {
        bool TryGetRoute(Vector3 start, Vector3 destination, List<Vector3> routePoints);
    }
}
