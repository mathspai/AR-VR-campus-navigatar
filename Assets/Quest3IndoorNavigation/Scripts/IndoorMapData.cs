using System;
using System.Collections.Generic;
using UnityEngine;

namespace Quest3IndoorNavigation
{
    public enum NavigationPointPersistenceMode
    {
        SessionWorldJson,
        SpatialAnchor
    }

    [Serializable]
    public sealed class IndoorFloorData
    {
        [SerializeField] private int floorId;
        [SerializeField] private string displayName;

        public int FloorId => floorId;
        public string DisplayName => displayName;
    }

    [Serializable]
    public sealed class IndoorMapTargetData
    {
        [SerializeField] private string targetId;
        [SerializeField] private string displayName;
        [SerializeField] private int floorId;
        [SerializeField] private NavigationTargetType targetType;
        [SerializeField] private Vector3 position;
        [SerializeField] private Quaternion rotation = Quaternion.identity;
        [SerializeField] private NavigationPointPersistenceMode persistenceMode;
        [SerializeField] private string spatialAnchorId;

        public string TargetId => targetId;
        public string DisplayName => displayName;
        public int FloorId => floorId;
        public NavigationTargetType TargetType => targetType;
        public Vector3 Position => position;
        public Quaternion Rotation => rotation;
        public NavigationPointPersistenceMode PersistenceMode => persistenceMode;
        public string SpatialAnchorId => spatialAnchorId;
    }

    [Serializable]
    public sealed class IndoorMapData
    {
        [SerializeField] private List<IndoorFloorData> floors = new();
        [SerializeField] private List<IndoorMapTargetData> targets = new();
        [SerializeField] private List<FloorConnection> floorConnections = new();

        public IReadOnlyList<IndoorFloorData> Floors => floors;
        public IReadOnlyList<IndoorMapTargetData> Targets => targets;
        public IReadOnlyList<FloorConnection> FloorConnections => floorConnections;

        public string ToJson(bool prettyPrint = true)
        {
            return JsonUtility.ToJson(this, prettyPrint);
        }

        public static IndoorMapData FromJson(string json)
        {
            return string.IsNullOrWhiteSpace(json) ? new IndoorMapData() : JsonUtility.FromJson<IndoorMapData>(json);
        }
    }
}
