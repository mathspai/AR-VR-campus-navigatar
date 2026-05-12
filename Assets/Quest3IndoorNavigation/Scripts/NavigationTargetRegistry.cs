using System.Collections.Generic;
using UnityEngine;

namespace Quest3IndoorNavigation
{
    public sealed class NavigationTargetRegistry : MonoBehaviour
    {
        [SerializeField] private IndoorNavigationController navigationController;
        [SerializeField] private FloorRoutePlanner floorRoutePlanner;
        [SerializeField] private List<NavigationTarget> targets = new();

        public IReadOnlyList<NavigationTarget> Targets => targets;

        private void Awake()
        {
            ResolvePlanner();
            ResolveController();
        }

        public void SelectByIndex(int index)
        {
            if (index < 0 || index >= targets.Count || targets[index] == null)
            {
                return;
            }

            ResolvePlanner();

            if (floorRoutePlanner != null)
            {
                floorRoutePlanner.SetDestination(targets[index]);
                return;
            }

            ResolveController();

            if (navigationController != null)
            {
                navigationController.SetDestination(targets[index]);
            }
            else
            {
                targets[index].Select();
            }
        }

        public void Register(NavigationTarget target)
        {
            if (target != null && !targets.Contains(target))
            {
                targets.Add(target);
            }
        }

        public void Unregister(NavigationTarget target)
        {
            targets.Remove(target);
        }

        private void ResolveController()
        {
            if (navigationController != null)
            {
                return;
            }

            navigationController = FindFirstObjectByType<IndoorNavigationController>();
        }

        private void ResolvePlanner()
        {
            if (floorRoutePlanner != null)
            {
                return;
            }

            floorRoutePlanner = FindFirstObjectByType<FloorRoutePlanner>();
        }
    }
}
