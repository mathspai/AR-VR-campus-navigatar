using UnityEngine;

namespace Quest3IndoorNavigation
{
    public enum NavigationTargetType
    {
        Normal,
        Room,
        Stairs,
        Elevator,
        Entrance
    }

    public sealed class NavigationTarget : MonoBehaviour
    {
        [SerializeField] private IndoorNavigationController navigationController;
        [SerializeField] private FloorRoutePlanner floorRoutePlanner;
        [SerializeField] private string targetId;
        [SerializeField] private string displayName;
        [SerializeField] private int floorId;
        [SerializeField] private NavigationTargetType targetType;

        public string TargetId => string.IsNullOrWhiteSpace(targetId) ? name : targetId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
        public int FloorId => floorId;
        public NavigationTargetType TargetType => targetType;

        public void Select()
        {
            ResolvePlanner();

            if (floorRoutePlanner != null)
            {
                floorRoutePlanner.SetDestination(this);
                return;
            }

            ResolveController();

            if (navigationController != null)
            {
                navigationController.SetDestination(this);
            }
        }

        private void ResolvePlanner()
        {
            if (floorRoutePlanner != null)
            {
                return;
            }

            floorRoutePlanner = FindFirstObjectByType<FloorRoutePlanner>();
        }

        private void ResolveController()
        {
            if (navigationController != null)
            {
                return;
            }

            navigationController = FindFirstObjectByType<IndoorNavigationController>();
        }
    }
}
