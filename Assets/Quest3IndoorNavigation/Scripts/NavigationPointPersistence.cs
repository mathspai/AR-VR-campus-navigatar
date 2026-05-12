namespace Quest3IndoorNavigation
{
    public readonly struct NavigationPointStoreResult
    {
        public NavigationPointStoreResult(bool success, string message)
        {
            Success = success;
            Message = message;
        }

        public bool Success { get; }
        public string Message { get; }

        public static NavigationPointStoreResult Ok(string message = "") => new(true, message);
        public static NavigationPointStoreResult Fail(string message) => new(false, message);
    }

    public interface INavigationPointStore
    {
        string StoreName { get; }
        bool SupportsLongTermWorldLock { get; }
        NavigationPointPersistenceMode PersistenceMode { get; }
        NavigationPointStoreResult Save(IndoorMapData mapData);
        NavigationPointStoreResult TryLoad(out IndoorMapData mapData);
    }

    public interface ISpatialAnchorNavigationPointStore : INavigationPointStore
    {
        bool IsSpatialAnchorRuntimeAvailable { get; }
        string RequiredSetupSummary { get; }
        NavigationPointStoreResult TryCreateAnchorForTarget(NavigationTarget target, out string spatialAnchorId);
        NavigationPointStoreResult TryResolveAnchor(string spatialAnchorId, out UnityEngine.Pose pose);
    }
}
