using System.Collections.Generic;
using Meta.XR.MRUtilityKit;
using UnityEngine;
using UnityEngine.AI;

namespace Quest3IndoorNavigation.Anchors
{
    [DisallowMultipleComponent]
    public sealed class MRUKDoorNavigationService : MonoBehaviour, ISceneRouteProvider
    {
        private readonly NavMeshPath _path = new NavMeshPath();

        public bool TryGetRoute(Vector3 start, Vector3 destination, List<Vector3> routePoints)
        {
            if (MRUK.Instance == null) return false;
            var room = MRUK.Instance.GetCurrentRoom();
            if (room == null) return false;

            // Destination inside current room — let NavMesh handle it directly
            if (room.IsPositionInRoom(destination, false)) return false;

            // Destination is outside — route through the nearest door frame
            var doorPos = FindNearestDoorOnFloor(room, start);
            if (doorPos == null) return false;

            // NavMesh path to the door, fallback to straight line.
            // Route is intentionally truncated at the door — the line must not pass through the wall.
            if (NavMesh.SamplePosition(start, out var startHit, 1.5f, NavMesh.AllAreas) &&
                NavMesh.SamplePosition(doorPos.Value, out var doorHit, 1.5f, NavMesh.AllAreas) &&
                NavMesh.CalculatePath(startHit.position, doorHit.position, NavMesh.AllAreas, _path) &&
                _path.corners.Length >= 2)
            {
                foreach (var corner in _path.corners)
                    routePoints.Add(corner);
            }
            else
            {
                routePoints.Add(start);
                routePoints.Add(doorPos.Value);
            }

            routePoints.Add(destination);
            return routePoints.Count >= 2;
        }

        private static Vector3? FindNearestDoorOnFloor(MRUKRoom room, Vector3 from)
        {
            Vector3? nearest = null;
            float nearestDist = float.MaxValue;
            float floorY = room.FloorAnchors?.Count > 0
                ? room.FloorAnchors[0].transform.position.y + 0.05f
                : 0.05f;

            var roomCenter = room.GetRoomBounds().center;

            foreach (var anchor in room.Anchors)
            {
                if (!anchor.HasAnyLabel(MRUKAnchor.SceneLabels.DOOR_FRAME)) continue;
                var pos = new Vector3(anchor.transform.position.x, floorY, anchor.transform.position.z);
                // Push waypoint 0.35m toward room center so it lands on the NavMesh
                var toCenter = new Vector3(roomCenter.x - pos.x, 0f, roomCenter.z - pos.z);
                if (toCenter.sqrMagnitude > 0.001f)
                    pos += toCenter.normalized * 0.35f;
                float dist = Vector3.Distance(from, pos);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = pos;
                }
            }

            return nearest;
        }
    }
}
