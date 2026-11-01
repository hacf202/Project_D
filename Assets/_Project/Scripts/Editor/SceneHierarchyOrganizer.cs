#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Project_D.EditorTools
{
    public static class SceneHierarchyOrganizer
    {
        [MenuItem("Project_D/Organize Hierarchy", false, 100)]
        public static void Organize()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (!activeScene.isLoaded)
            {
                Debug.LogWarning("[HierarchyOrganizer] Không tìm thấy Scene đang mở!");
                return;
            }

            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Organize Scene Hierarchy");

            Debug.Log("<color=cyan>[HierarchyOrganizer]</color> Bắt đầu sắp xếp lại Hierarchy...");

            // 1. SẮP XẾP TRONG CANVAS
            GameObject canvasObj = GameObject.Find("Canvas");
            if (canvasObj != null)
            {
                // Unpack prefab instance if connected, so children can be reparented
                if (PrefabUtility.IsPartOfPrefabInstance(canvasObj))
                {
                    PrefabUtility.UnpackPrefabInstance(canvasObj, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                }

                RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
                if (canvasRect != null)
                {
                    // Group 1: [HUD & Controls]
                    RectTransform hudGroup = GetOrCreateUIGroup(canvasRect, "[HUD & Controls]", 0);
                    MoveUIToGroup(canvasRect, hudGroup, new[]
                    {
                        "JoystickTouchZone",
                        "JoystickBackground",
                        "HUDPanel",
                        "RunHUDPanel",
                        "MinionQuickMergeBar",
                        "MinionRosterBar"
                    });

                    // Group 2: [Popups & Screens]
                    RectTransform popupGroup = GetOrCreateUIGroup(canvasRect, "[Popups & Screens]", 1);
                    MoveUIToGroup(canvasRect, popupGroup, new[]
                    {
                        "LevelUpPanel",
                        "GameOverPanel",
                        "RestSitePanel",
                        "ShopPanel",
                        "VictoryPanel",
                        "SettingsPanel"
                    });

                    // Group 3: [Modals & Detail]
                    RectTransform modalGroup = GetOrCreateUIGroup(canvasRect, "[Modals & Detail]", 2);
                    MoveUIToGroup(canvasRect, modalGroup, new[]
                    {
                        "MinionMergePanelModal",
                        "MinionDetailStatsPanelModal"
                    });

                    hudGroup.SetSiblingIndex(0);
                    popupGroup.SetSiblingIndex(1);
                    modalGroup.SetSiblingIndex(2);
                }
            }

            // 2. SẮP XẾP Ở SCENE ROOT
            // Nhóm --- SETUP --- (index 0)
            Transform setupGroup = GetOrCreateRootGroup("--- SETUP ---");
            MoveRootObjectToGroup(setupGroup, "Main Camera");
            MoveRootObjectToGroup(setupGroup, "Global Light 2D");
            MoveRootObjectToGroup(setupGroup, "EventSystem");

            // Nhóm --- MANAGERS --- (index 1)
            Transform managerGroup = GetOrCreateRootGroup("--- MANAGERS ---");
            MoveRootObjectToGroup(managerGroup, "PoolManager");
            MoveRootObjectToGroup(managerGroup, "FlowFieldManager");
            MoveRootObjectToGroup(managerGroup, "DamageTextManager");
            MoveRootObjectToGroup(managerGroup, "UpgradeManager");
            MoveRootObjectToGroup(managerGroup, "MinionManager");
            MoveRootObjectToGroup(managerGroup, "DungeonManager");

            // Thứ tự các đối tượng ở Scene Root:
            // 0: --- SETUP ---
            // 1: --- MANAGERS ---
            // 2: Player
            // 3: Canvas
            // 4: PanelQueueManager
            setupGroup.SetSiblingIndex(0);
            managerGroup.SetSiblingIndex(1);

            GameObject playerObj = GameObject.Find("Player");
            if (playerObj != null && playerObj.transform.parent == null)
            {
                playerObj.transform.SetSiblingIndex(2);
            }

            if (canvasObj != null && canvasObj.transform.parent == null)
            {
                canvasObj.transform.SetSiblingIndex(3);
            }

            GameObject panelQueueObj = GameObject.Find("PanelQueueManager");
            if (panelQueueObj != null && panelQueueObj.transform.parent == null)
            {
                panelQueueObj.transform.SetSiblingIndex(4);
            }

            // 3. LƯU SCENE
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);
            Undo.CollapseUndoOperations(undoGroup);

            Debug.Log("<color=green>[HierarchyOrganizer] Sắp xếp Hierarchy thành công và đã lưu Scene!</color>");
        }

        private static RectTransform GetOrCreateUIGroup(RectTransform canvasRect, string groupName, int siblingIndex)
        {
            Transform existing = canvasRect.Find(groupName);
            if (existing != null && existing is RectTransform existingRect)
            {
                existingRect.SetSiblingIndex(siblingIndex);
                return existingRect;
            }

            GameObject groupObj = new GameObject(groupName, typeof(RectTransform));
            groupObj.layer = canvasRect.gameObject.layer;
            Undo.RegisterCreatedObjectUndo(groupObj, $"Create {groupName}");

            RectTransform rect = groupObj.GetComponent<RectTransform>();
            rect.SetParent(canvasRect, false);

            // Stretch full-screen
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.localPosition = Vector3.zero;
            rect.localScale = Vector3.one;

            rect.SetSiblingIndex(siblingIndex);
            return rect;
        }

        private static void MoveUIToGroup(RectTransform canvasRect, RectTransform groupRect, string[] names)
        {
            foreach (string name in names)
            {
                Transform target = canvasRect.Find(name) ?? groupRect.Find(name);
                if (target != null && target is RectTransform rt)
                {
                    Vector2 anchorMin = rt.anchorMin;
                    Vector2 anchorMax = rt.anchorMax;
                    Vector2 anchoredPos = rt.anchoredPosition;
                    Vector2 sizeDelta = rt.sizeDelta;
                    Vector2 pivot = rt.pivot;
                    Vector3 localScale = rt.localScale;
                    Quaternion localRotation = rt.localRotation;

                    Undo.SetTransformParent(rt, groupRect, $"Move {name} to {groupRect.name}");
                    rt.SetParent(groupRect, false);

                    rt.anchorMin = anchorMin;
                    rt.anchorMax = anchorMax;
                    rt.anchoredPosition = anchoredPos;
                    rt.sizeDelta = sizeDelta;
                    rt.pivot = pivot;
                    rt.localScale = localScale;
                    rt.localRotation = localRotation;
                }
            }
        }

        private static Transform GetOrCreateRootGroup(string groupName)
        {
            Scene activeScene = SceneManager.GetActiveScene();
            foreach (var go in activeScene.GetRootGameObjects())
            {
                if (go.name == groupName) return go.transform;
            }

            GameObject newGroup = new GameObject(groupName);
            newGroup.transform.position = Vector3.zero;
            newGroup.transform.rotation = Quaternion.identity;
            newGroup.transform.localScale = Vector3.one;

            Undo.RegisterCreatedObjectUndo(newGroup, $"Create {groupName}");
            return newGroup.transform;
        }

        private static void MoveRootObjectToGroup(Transform parentGroup, string objectName)
        {
            Scene activeScene = SceneManager.GetActiveScene();
            GameObject target = null;
            foreach (var go in activeScene.GetRootGameObjects())
            {
                if (go.name == objectName)
                {
                    target = go;
                    break;
                }
            }
            if (target == null)
            {
                // In case it's already a child of parentGroup or another group
                target = parentGroup.Find(objectName)?.gameObject;
            }

            if (target != null && target.transform.parent != parentGroup)
            {
                Undo.SetTransformParent(target.transform, parentGroup, $"Move {objectName} to {parentGroup.name}");
                target.transform.SetParent(parentGroup, true);
            }
        }
    }
}
#endif
