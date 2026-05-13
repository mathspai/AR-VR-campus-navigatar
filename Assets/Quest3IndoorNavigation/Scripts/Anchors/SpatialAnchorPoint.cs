using System;
using System.Collections.Generic;
using UnityEngine;

namespace Quest3IndoorNavigation.Anchors
{
    public enum AnchorPointKind
    {
        Normal,
        Doorway,
        Corner,
        Stairs,
        Elevator,
        Destination
    }

    [DisallowMultipleComponent]
    public sealed class SpatialAnchorPoint : MonoBehaviour
    {
        [SerializeField] private string uuid;
        [SerializeField] private string displayName;
        [SerializeField] private AnchorPointKind pointKind = AnchorPointKind.Normal;
        [SerializeField] private List<string> routeWaypointIds = new();

        public string Uuid => uuid;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
        public AnchorPointKind PointKind => pointKind;
        public IReadOnlyList<string> RouteWaypointIds => routeWaypointIds;

        public void Initialize(string anchorUuid, string anchorName, AnchorPointKind kind)
        {
            uuid = anchorUuid;
            displayName = string.IsNullOrWhiteSpace(anchorName) ? $"Anchor {ShortId(anchorUuid)}" : anchorName;
            pointKind = kind;
            name = displayName;
        }

        public void SetRouteWaypointIds(IEnumerable<string> waypointIds)
        {
            routeWaypointIds.Clear();
            if (waypointIds == null)
            {
                return;
            }

            foreach (var waypointId in waypointIds)
            {
                if (!string.IsNullOrWhiteSpace(waypointId) && !routeWaypointIds.Contains(waypointId))
                {
                    routeWaypointIds.Add(waypointId);
                }
            }
        }

        private static string ShortId(string value)
        {
            return Guid.TryParse(value, out var guid) ? guid.ToString("N")[..8] : "New";
        }
    }
}
