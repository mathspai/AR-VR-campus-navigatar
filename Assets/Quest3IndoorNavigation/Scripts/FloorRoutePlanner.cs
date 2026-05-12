using System;
using System.Collections.Generic;
using UnityEngine;

namespace Quest3IndoorNavigation
{
    public sealed class FloorRoutePlanner : MonoBehaviour
    {
        [SerializeField] private IndoorNavigationController navigationController;
        [SerializeField] private NavigationTargetRegistry targetRegistry;
        [SerializeField] private int currentFloorId = 1;
        [SerializeField] private List<FloorConnection> floorConnections = new();

        private NavigationTarget pendingDestination;
        private FloorConnection activeFloorConnection;
        private bool waitingForFloorChangeConfirmation;

        public int CurrentFloorId => currentFloorId;
        public int PendingDestinationFloorId => pendingDestination != null ? pendingDestination.FloorId : currentFloorId;
        public FloorConnection ActiveFloorConnection => activeFloorConnection;
        public bool WaitingForFloorChangeConfirmation => waitingForFloorChangeConfirmation;
        public string CurrentInstruction { get; private set; } = string.Empty;

        public event Action<string> InstructionChanged;

        private void Awake()
        {
            ResolveNavigationController();
            ResolveTargetRegistry();
        }

        public void SetDestination(NavigationTarget target)
        {
            ResolveNavigationController();
            pendingDestination = null;
            activeFloorConnection = null;
            waitingForFloorChangeConfirmation = false;

            if (target == null)
            {
                ClearRoute();
                return;
            }

            if (target.FloorId == currentFloorId)
            {
                navigationController?.SetDestination(target);
                SetInstruction($"Navigating on floor {currentFloorId}.");
                return;
            }

            var connection = FindConnection(currentFloorId, target.FloorId);
            if (connection == null)
            {
                navigationController?.ClearDestination();
                SetInstruction($"No floor connection configured from floor {currentFloorId} to floor {target.FloorId}.");
                return;
            }

            var currentFloorConnectionPoint = ResolveConnectionPoint(connection, currentFloorId);
            if (currentFloorConnectionPoint == null)
            {
                navigationController?.ClearDestination();
                SetInstruction($"Floor connection {connection.ConnectionId} has no point on floor {currentFloorId}.");
                return;
            }

            pendingDestination = target;
            activeFloorConnection = connection;
            waitingForFloorChangeConfirmation = true;
            navigationController?.SetDestination(currentFloorConnectionPoint);

            SetInstruction(
                $"Navigate to {connection.ConnectionId} ({connection.ConnectionType}) on floor {currentFloorId}, " +
                $"then go to floor {target.FloorId} and confirm arrival.");
        }

        public void SetDestination(Transform target)
        {
            ResolveNavigationController();
            pendingDestination = null;
            activeFloorConnection = null;
            waitingForFloorChangeConfirmation = false;
            navigationController?.SetDestination(target);
            SetInstruction(target != null ? $"Navigating on floor {currentFloorId}." : string.Empty);
        }

        public void SetCurrentFloorId(int floorId)
        {
            currentFloorId = floorId;
            SetInstruction($"Current floor set to {currentFloorId}.");
        }

        public void ConfirmArrivedAtFloor(int floorId)
        {
            currentFloorId = floorId;

            if (!waitingForFloorChangeConfirmation || pendingDestination == null)
            {
                SetInstruction($"Current floor set to {currentFloorId}.");
                return;
            }

            var target = pendingDestination;
            pendingDestination = null;
            activeFloorConnection = null;
            waitingForFloorChangeConfirmation = false;
            navigationController?.SetDestination(target);
            SetInstruction($"Floor {currentFloorId} confirmed. Navigating to {target.name}.");
        }

        public void ConfirmArrivedAtPendingDestinationFloor()
        {
            if (pendingDestination != null)
            {
                ConfirmArrivedAtFloor(pendingDestination.FloorId);
            }
        }

        public void ClearRoute()
        {
            pendingDestination = null;
            activeFloorConnection = null;
            waitingForFloorChangeConfirmation = false;
            navigationController?.ClearDestination();
            SetInstruction(string.Empty);
        }

        private FloorConnection FindConnection(int fromFloorId, int toFloorId)
        {
            foreach (var connection in floorConnections)
            {
                if (connection != null && connection.Connects(fromFloorId, toFloorId))
                {
                    return connection;
                }
            }

            return null;
        }

        private Transform ResolveConnectionPoint(FloorConnection connection, int floorId)
        {
            var point = connection.GetPointForFloor(floorId);
            if (point != null)
            {
                return point;
            }

            ResolveTargetRegistry();

            var targetId = connection.GetTargetIdForFloor(floorId);
            if (targetRegistry == null || string.IsNullOrWhiteSpace(targetId))
            {
                return null;
            }

            foreach (var target in targetRegistry.Targets)
            {
                if (target != null && target.TargetId == targetId)
                {
                    return target.transform;
                }
            }

            return null;
        }

        private void ResolveNavigationController()
        {
            if (navigationController == null)
            {
                navigationController = FindFirstObjectByType<IndoorNavigationController>();
            }
        }

        private void ResolveTargetRegistry()
        {
            if (targetRegistry == null)
            {
                targetRegistry = FindFirstObjectByType<NavigationTargetRegistry>();
            }
        }

        private void SetInstruction(string instruction)
        {
            if (CurrentInstruction == instruction)
            {
                return;
            }

            CurrentInstruction = instruction;
            InstructionChanged?.Invoke(CurrentInstruction);

            if (!string.IsNullOrEmpty(CurrentInstruction))
            {
                Debug.Log(CurrentInstruction, this);
            }
        }
    }
}
