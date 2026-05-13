using Meta.XR.MRUtilityKit;
using UnityEngine;

namespace Quest3IndoorNavigation.Anchors
{
    /// <summary>
    /// Shows anchor markers only for the room the user is currently in.
    /// Anchors in other rooms are hidden until the user walks into that room.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MRUKRoomAnchorVisibility : MonoBehaviour
    {
        [SerializeField] private float checkInterval = 0.4f;

        private float _nextCheck;
        private MRUKRoom _lastRoom;

        private void Update()
        {
            if (Time.time < _nextCheck) return;
            _nextCheck = Time.time + checkInterval;
            Refresh();
        }

        private void Refresh()
        {
            if (MRUK.Instance == null || MRUK.Instance.Rooms == null || MRUK.Instance.Rooms.Count == 0)
            {
                ShowAll();
                return;
            }

            var cam = Camera.main;
            if (cam == null) return;

            var userRoom = GetRoomContaining(cam.transform.position);

            // Nothing changed
            if (userRoom == _lastRoom && userRoom != null) return;
            _lastRoom = userRoom;

            if (userRoom == null)
            {
                ShowAll();
                return;
            }

            foreach (var anchor in FindObjectsByType<SpatialAnchorPoint>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var inRoom = userRoom.IsPositionInRoom(anchor.transform.position, false);
                SetMarkerVisible(anchor, inRoom);
            }
        }

        private static void ShowAll()
        {
            foreach (var anchor in FindObjectsByType<SpatialAnchorPoint>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                SetMarkerVisible(anchor, true);
            }
        }

        private static void SetMarkerVisible(SpatialAnchorPoint anchor, bool visible)
        {
            var marker = anchor.transform.Find("Marker");
            if (marker != null) marker.gameObject.SetActive(visible);
        }

        private static MRUKRoom GetRoomContaining(Vector3 position)
        {
            foreach (var room in MRUK.Instance.Rooms)
            {
                if (room != null && room.IsPositionInRoom(position, false))
                    return room;
            }
            return null;
        }
    }
}
