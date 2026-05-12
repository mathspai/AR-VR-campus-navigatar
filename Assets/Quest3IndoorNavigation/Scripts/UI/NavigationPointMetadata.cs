using UnityEngine;

namespace Quest3IndoorNavigation.UI
{
    public enum NavigationPointType
    {
        Normal,
        Room,
        Stairs,
        Elevator,
        Entrance
    }

    [DisallowMultipleComponent]
    public sealed class NavigationPointMetadata : MonoBehaviour
    {
        [SerializeField] private string floorId = "F1";
        [SerializeField] private NavigationPointType targetType = NavigationPointType.Normal;

        public string FloorId => floorId;
        public NavigationPointType TargetType => targetType;

        public void SetMetadata(string newFloorId, NavigationPointType newTargetType)
        {
            floorId = string.IsNullOrWhiteSpace(newFloorId) ? "F1" : newFloorId;
            targetType = newTargetType;
        }
    }
}
