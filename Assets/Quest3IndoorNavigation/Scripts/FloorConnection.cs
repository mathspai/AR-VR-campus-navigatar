using System;
using UnityEngine;

namespace Quest3IndoorNavigation
{
    public enum FloorConnectionType
    {
        Stairs,
        Elevator,
        Escalator
    }

    [Serializable]
    public sealed class FloorConnection
    {
        [SerializeField] private string connectionId;
        [SerializeField] private int fromFloorId;
        [SerializeField] private int toFloorId;
        [SerializeField] private string fromTargetId;
        [SerializeField] private string toTargetId;
        [SerializeField] private FloorConnectionType connectionType;
        [SerializeField] private Transform fromPoint;
        [SerializeField] private Transform toPoint;

        public string ConnectionId => string.IsNullOrWhiteSpace(connectionId) ? "Floor Connection" : connectionId;
        public int FromFloorId => fromFloorId;
        public int ToFloorId => toFloorId;
        public string FromTargetId => fromTargetId;
        public string ToTargetId => toTargetId;
        public FloorConnectionType ConnectionType => connectionType;
        public Transform FromPoint => fromPoint;
        public Transform ToPoint => toPoint;

        public bool Connects(int currentFloorId, int targetFloorId)
        {
            return (fromFloorId == currentFloorId && toFloorId == targetFloorId) ||
                (toFloorId == currentFloorId && fromFloorId == targetFloorId);
        }

        public Transform GetPointForFloor(int floorId)
        {
            if (floorId == fromFloorId)
            {
                return fromPoint;
            }

            return floorId == toFloorId ? toPoint : null;
        }

        public int GetOtherFloor(int floorId)
        {
            if (floorId == fromFloorId)
            {
                return toFloorId;
            }

            return floorId == toFloorId ? fromFloorId : floorId;
        }

        public string GetTargetIdForFloor(int floorId)
        {
            if (floorId == fromFloorId)
            {
                return fromTargetId;
            }

            return floorId == toFloorId ? toTargetId : string.Empty;
        }
    }
}
