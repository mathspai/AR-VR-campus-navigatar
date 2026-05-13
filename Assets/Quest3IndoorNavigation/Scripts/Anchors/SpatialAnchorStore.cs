using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Quest3IndoorNavigation.Anchors
{
    [DisallowMultipleComponent]
    public sealed class SpatialAnchorStore : MonoBehaviour
    {
        [SerializeField] private string fileName = "quest3_spatial_anchor_points.json";

        public string FilePath => Path.Combine(Application.persistentDataPath, fileName);

        public AnchorPointDatabase Load()
        {
            try
            {
                if (!File.Exists(FilePath))
                {
                    return new AnchorPointDatabase();
                }

                var json = File.ReadAllText(FilePath);
                return string.IsNullOrWhiteSpace(json)
                    ? new AnchorPointDatabase()
                    : JsonUtility.FromJson<AnchorPointDatabase>(json) ?? new AnchorPointDatabase();
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[SpatialAnchorStore] Load failed: {exception.Message}");
                return new AnchorPointDatabase();
            }
        }

        public IReadOnlyList<AnchorPointRecord> LoadPoints()
        {
            return Load().points;
        }

        public void Upsert(AnchorPointRecord record)
        {
            if (record == null || string.IsNullOrWhiteSpace(record.uuid))
            {
                return;
            }

            var database = Load();
            var index = database.points.FindIndex(point => point.uuid == record.uuid);
            if (index >= 0)
            {
                database.points[index] = record;
            }
            else
            {
                database.points.Add(record);
            }

            Save(database);
        }

        public void Clear()
        {
            Save(new AnchorPointDatabase());
        }

        public void Remove(string uuid)
        {
            var db = Load();
            db.points.RemoveAll(p => p.uuid == uuid);
            Save(db);
        }

        public void Save(AnchorPointDatabase database)
        {
            try
            {
                var directory = Path.GetDirectoryName(FilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(FilePath, JsonUtility.ToJson(database ?? new AnchorPointDatabase(), true));
            }
            catch (Exception exception)
            {
                Debug.LogError($"[SpatialAnchorStore] Save failed: {exception.Message}");
            }
        }
    }

    [Serializable]
    public sealed class AnchorPointDatabase
    {
        public List<AnchorPointRecord> points = new();
    }

    [Serializable]
    public sealed class AnchorPointRecord
    {
        public string uuid;
        public string displayName;
        public AnchorPointKind pointKind;
        public List<string> routeWaypointIds = new();

        public AnchorPointRecord()
        {
        }

        public AnchorPointRecord(string uuid, string displayName, AnchorPointKind pointKind)
        {
            this.uuid = uuid;
            this.displayName = displayName;
            this.pointKind = pointKind;
        }
    }
}
