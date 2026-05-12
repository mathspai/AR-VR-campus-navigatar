using System;
using Quest3IndoorNavigation.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Quest3IndoorNavigation.Editor
{
    public static class QuestNavigationWorldSpaceUICreator
    {
        [MenuItem("Quest3 Indoor Navigation/Create World Space Navigation UI")]
        public static void CreateWorldSpaceNavigationUI()
        {
            var uiObject = new GameObject(
                "Quest3 World Space Navigation UI",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(GraphicRaycaster),
                typeof(QuestNavigationWorldSpaceUI));

            uiObject.transform.position = new Vector3(0f, 1.45f, 1.6f);
            uiObject.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            uiObject.GetComponent<QuestNavigationWorldSpaceUI>().Rebuild();

            EnsureEventSystem();

            Selection.activeGameObject = uiObject;
            Undo.RegisterCreatedObjectUndo(uiObject, "Create Quest3 World Space Navigation UI");
        }

        private static void EnsureEventSystem()
        {
            if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() != null)
            {
                return;
            }

            var eventSystem = new GameObject("EventSystem", typeof(EventSystem));
            var xrInputModuleType = Type.GetType("UnityEngine.XR.Interaction.Toolkit.UI.XRUIInputModule, Unity.XR.Interaction.Toolkit");
            eventSystem.AddComponent(xrInputModuleType ?? typeof(StandaloneInputModule));
            Undo.RegisterCreatedObjectUndo(eventSystem, "Create EventSystem");
        }
    }
}
