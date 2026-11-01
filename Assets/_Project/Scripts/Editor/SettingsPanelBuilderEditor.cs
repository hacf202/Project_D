using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

namespace Project_D.UI.Settings
{
    public static class SettingsPanelBuilderEditor
    {
        private const string ENLARGED_VERSION_KEY = "SettingsPanel_Enlarged_100Pct_Applied_v6";

        [MenuItem("Project_D/UI/Tạo Lại Prefab SettingsPanel (100% Kích Thước)", false, 49)]
        public static void RebuildSettingsPanelPrefabDirectly()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[SettingsPanelBuilder] Không thể tạo Prefab trong Play Mode. Vui lòng dừng game trước!");
                return;
            }

            string prefabPath = "Assets/_Project/Prefabs/UI/SettingsPanel.prefab";
            string dir = System.IO.Path.GetDirectoryName(prefabPath);
            if (!System.IO.Directory.Exists(dir))
            {
                System.IO.Directory.CreateDirectory(dir);
            }

            // Tạo root tạm để dựng hierarchy
            var tempRoot = new GameObject("TempSettingsPanelBuilder");
            try
            {
                var panelObj = CreateSettingsPanelHierarchy(tempRoot.transform);
                PrefabUtility.SaveAsPrefabAsset(panelObj, prefabPath);
                Debug.Log("<color=#00FF66><b>[SettingsPanelBuilder]</b></color> ĐÃ TẠO VÀ LƯU PREFAB MỚI (PHÓNG TO 100%): " + prefabPath);
            }
            finally
            {
                Object.DestroyImmediate(tempRoot);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Nếu Scene đang mở có Canvas thì cập nhật các liên kết
            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas != null)
            {
                var sp = canvas.GetComponentInChildren<SettingsPanelUI>(true);
                if (sp != null)
                {
                    EnsureSettingsButtonOnHUD(canvas.transform, sp);
                    sp.AutoFindReferences();
                    EditorUtility.SetDirty(sp.gameObject);
                }
            }
        }

        [MenuItem("Project_D/UI/Tạo Settings Panel Tự Động (In Scene)", false, 50)]
        public static void GenerateSettingsPanelInScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[SettingsPanelBuilder] Không thể tạo GameObject trong Scene khi đang ở Play Mode!");
                return;
            }

            RebuildSettingsPanelPrefabDirectly();

            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                var canvasObj = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = canvasObj.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = canvasObj.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080, 1920);
                scaler.matchWidthOrHeight = 0.5f;
            }

            // Xóa bản cũ nếu có
            var existing = canvas.transform.Find("SettingsPanel");
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            string prefabPath = "Assets/_Project/Prefabs/UI/SettingsPanel.prefab";
            var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            GameObject panelObj;
            if (prefabAsset != null)
            {
                panelObj = (GameObject)PrefabUtility.InstantiatePrefab(prefabAsset, canvas.transform);
                panelObj.name = "SettingsPanel";
            }
            else
            {
                panelObj = CreateSettingsPanelHierarchy(canvas.transform);
            }

            EnsureSettingsButtonOnHUD(canvas.transform, panelObj.GetComponent<SettingsPanelUI>());
            Selection.activeGameObject = panelObj;
            EditorUtility.SetDirty(panelObj);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(panelObj.scene);

            SyncSettingsPanelHierarchyAndPrefab();

            Debug.Log("<color=#00E5FF><b>[SettingsPanelBuilder]</b></color> Đã tạo SettingsPanel mới theo chuẩn bố cục ảnh mẫu!");
        }

        [MenuItem("Project_D/UI/Đồng Bộ SettingsPanel (Hierarchy <-> Prefab)", false, 51)]
        public static void SyncSettingsPanelHierarchyAndPrefab()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[SettingsPanelSync] Không thể đồng bộ Prefab khi đang ở Play Mode!");
                return;
            }

            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("[SettingsPanelSync] Không tìm thấy Canvas trong Scene!");
                return;
            }

            var spTransform = canvas.transform.Find("SettingsPanel");
            if (spTransform == null)
            {
                Debug.LogError("[SettingsPanelSync] Không tìm thấy SettingsPanel trong Canvas!");
                return;
            }

            var sp = spTransform.gameObject;
            string prefabPath = "Assets/_Project/Prefabs/UI/SettingsPanel.prefab";

            // Đảm bảo thư mục tồn tại
            string dir = System.IO.Path.GetDirectoryName(prefabPath);
            if (!System.IO.Directory.Exists(dir))
            {
                System.IO.Directory.CreateDirectory(dir);
            }

            // Nếu đã là Prefab Instance thì Apply overrides
            if (PrefabUtility.IsPartOfPrefabInstance(sp))
            {
                PrefabUtility.ApplyPrefabInstance(sp, InteractionMode.AutomatedAction);
                Debug.Log("<color=#00FF66><b>[SettingsPanelSync]</b></color> Đã Apply toàn bộ Overrides từ Hierarchy vào Prefab: " + prefabPath);
            }
            else
            {
                // Nếu chưa là Prefab Instance thì Save và Connect
                PrefabUtility.SaveAsPrefabAssetAndConnect(sp, prefabPath, InteractionMode.AutomatedAction);
                Debug.Log("<color=#00FF66><b>[SettingsPanelSync]</b></color> Đã Lưu và Kết nối (Save & Connect) Hierarchy vào Prefab: " + prefabPath);
            }

            // Đồng bộ luôn GameCanvas.prefab
            string canvasPath = "Assets/_Project/Prefabs/UI/GameCanvas.prefab";
            if (PrefabUtility.IsPartOfPrefabInstance(canvas.gameObject))
            {
                PrefabUtility.ApplyPrefabInstance(canvas.gameObject, InteractionMode.AutomatedAction);
            }
            else if (System.IO.File.Exists(canvasPath))
            {
                PrefabUtility.SaveAsPrefabAssetAndConnect(canvas.gameObject, canvasPath, InteractionMode.AutomatedAction);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(sp.scene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();

            Debug.Log("<color=#00FF66><b>[SettingsPanelSync]</b></color> ĐÃ HOÀN TẤT ĐỒNG BỘ 100%: SettingsPanel (Hierarchy) == SettingsPanel.prefab");
        }

        [MenuItem("Project_D/UI/Cập Nhật SettingsPanel Trong Scene Từ Prefab (Prefab -> Hierarchy)", false, 52)]
        public static void SyncPrefabToSceneHierarchy()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[SettingsPanelSync] Không thể đồng bộ Prefab khi đang ở Play Mode!");
                return;
            }

            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("[SettingsPanelSync] Không tìm thấy Canvas trong Scene!");
                return;
            }

            string prefabPath = "Assets/_Project/Prefabs/UI/SettingsPanel.prefab";
            var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefabAsset == null)
            {
                Debug.LogError("[SettingsPanelSync] Không tìm thấy Prefab tại: " + prefabPath);
                return;
            }

            var existing = canvas.transform.Find("SettingsPanel");
            int siblingIndex = existing != null ? existing.GetSiblingIndex() : -1;
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            var panelObj = (GameObject)PrefabUtility.InstantiatePrefab(prefabAsset, canvas.transform);
            panelObj.name = "SettingsPanel";
            if (siblingIndex >= 0)
            {
                panelObj.transform.SetSiblingIndex(siblingIndex);
            }

            var spUI = panelObj.GetComponent<SettingsPanelUI>();
            if (spUI != null)
            {
                EnsureSettingsButtonOnHUD(canvas.transform, spUI);
                spUI.AutoFindReferences();
                EditorUtility.SetDirty(panelObj);
            }

            // Đồng bộ lại GameCanvas.prefab
            string canvasPath = "Assets/_Project/Prefabs/UI/GameCanvas.prefab";
            if (PrefabUtility.IsPartOfPrefabInstance(canvas.gameObject))
            {
                PrefabUtility.ApplyPrefabInstance(canvas.gameObject, InteractionMode.AutomatedAction);
            }
            else if (System.IO.File.Exists(canvasPath))
            {
                PrefabUtility.SaveAsPrefabAssetAndConnect(canvas.gameObject, canvasPath, InteractionMode.AutomatedAction);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(panelObj.scene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();

            Debug.Log("<color=#00FF66><b>[SettingsPanelSync]</b></color> ĐÃ CẬP NHẬT SETTINGSPANEL TRONG SCENE HOÀN TOÀN TỪ PREFAB!");
        }

        [MenuItem("Project_D/UI/Đồng Bộ Trực Tiếp SettingsPanel Vào GameCanvas.prefab", false, 52)]
        public static void SyncDirectlyToGameCanvasPrefab()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[SettingsPanelSync] Không thể đồng bộ Prefab khi đang ở Play Mode!");
                return;
            }

            string canvasPrefabPath = "Assets/_Project/Prefabs/UI/GameCanvas.prefab";
            string settingsPrefabPath = "Assets/_Project/Prefabs/UI/SettingsPanel.prefab";
            if (!System.IO.File.Exists(canvasPrefabPath) || !System.IO.File.Exists(settingsPrefabPath)) return;

            var canvasRoot = PrefabUtility.LoadPrefabContents(canvasPrefabPath);
            try
            {
                var settingsAsset = AssetDatabase.LoadAssetAtPath<GameObject>(settingsPrefabPath);
                if (settingsAsset == null) return;

                var oldSettings = canvasRoot.transform.Find("SettingsPanel");
                int siblingIndex = oldSettings != null ? oldSettings.GetSiblingIndex() : -1;
                if (oldSettings != null)
                {
                    Object.DestroyImmediate(oldSettings.gameObject);
                }

                var newSettings = (GameObject)PrefabUtility.InstantiatePrefab(settingsAsset, canvasRoot.transform);
                newSettings.name = "SettingsPanel";
                if (siblingIndex >= 0) newSettings.transform.SetSiblingIndex(siblingIndex);

                var spUI = newSettings.GetComponent<SettingsPanelUI>();
                if (spUI != null)
                {
                    EnsureSettingsButtonOnHUD(canvasRoot.transform, spUI);
                    spUI.AutoFindReferences();
                }

                PrefabUtility.SaveAsPrefabAsset(canvasRoot, canvasPrefabPath);
                Debug.Log("<color=#00FF66><b>[SettingsPanelSync]</b></color> ĐÃ ĐỒNG BỘ TRỰC TIẾP VÀO GameCanvas.prefab!");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(canvasRoot);
            }
        }

        [InitializeOnLoadMethod]
        private static void AutoSyncOnCompile()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;

                if (!EditorPrefs.GetBool(ENLARGED_VERSION_KEY, false))
                {
                    RebuildSettingsPanelPrefabDirectly();
                    SyncDirectlyToGameCanvasPrefab();
                    SyncPrefabToSceneHierarchy();
                    EditorPrefs.SetBool(ENLARGED_VERSION_KEY, true);
                    Debug.Log("<color=#00FF66><b>[SettingsPanelBuilder]</b></color> Đã tự động cập nhật SettingsPanel với kích thước tăng 100% và đồng bộ vào Scene & GameCanvas.prefab!");
                }
            };
        }

        public static void EnsureSettingsButtonOnHUD(Transform canvasTransform, SettingsPanelUI panelUI)
        {
            var runHud = canvasTransform.Find("RunHUDPanel") ?? canvasTransform.Find("HUDPanel");
            if (runHud == null) return;

            var souls = runHud.Find("SoulsText");
            if (souls != null)
            {
                var sRt = souls.GetComponent<RectTransform>();
                sRt.anchoredPosition = new Vector2(-90f, -40f);
                sRt.sizeDelta = new Vector2(270f, 50f);
            }

            var btnTransform = runHud.Find("Btn_Settings");
            GameObject btnObj;
            if (btnTransform == null)
            {
                btnObj = new GameObject("Btn_Settings", typeof(RectTransform), typeof(Image), typeof(Button));
                btnObj.transform.SetParent(runHud, false);
                var bRt = btnObj.GetComponent<RectTransform>();
                bRt.anchorMin = new Vector2(1f, 1f);
                bRt.anchorMax = new Vector2(1f, 1f);
                bRt.pivot = new Vector2(1f, 1f);
                bRt.anchoredPosition = new Vector2(-20f, -35f);
                bRt.sizeDelta = new Vector2(64f, 64f);

                var img = btnObj.GetComponent<Image>();
                img.color = new Color(0.16f, 0.20f, 0.28f, 0.95f);

                var outline = btnObj.AddComponent<Outline>();
                outline.effectColor = new Color(0.35f, 0.42f, 0.55f, 0.8f);
                outline.effectDistance = new Vector2(2f, -2f);

                for (int i = 0; i < 3; i++)
                {
                    var bar = new GameObject($"Bar_{i}", typeof(RectTransform), typeof(Image));
                    bar.transform.SetParent(btnObj.transform, false);
                    var barRt = bar.GetComponent<RectTransform>();
                    barRt.sizeDelta = new Vector2(32f, 4f);
                    float y = (i == 0) ? 10f : ((i == 1) ? 0f : -10f);
                    barRt.anchoredPosition = new Vector2(0f, y);
                    bar.GetComponent<Image>().color = new Color(0.9f, 0.92f, 0.96f, 1f);
                    bar.GetComponent<Image>().raycastTarget = false;
                }
            }
            else
            {
                btnObj = btnTransform.gameObject;
            }

            if (panelUI != null)
            {
                var btn = btnObj.GetComponent<Button>();
                UnityEditor.Events.UnityEventTools.RemovePersistentListener(btn.onClick, panelUI.Open);
                UnityEditor.Events.UnityEventTools.AddPersistentListener(btn.onClick, panelUI.Open);
                panelUI.AutoFindReferences();
            }
        }

        public static GameObject CreateSettingsPanelHierarchy(Transform canvasTransform)
        {
            // 1. Root Panel
            var root = new GameObject("SettingsPanel", typeof(RectTransform), typeof(CanvasGroup), typeof(SettingsPanelUI));
            root.transform.SetParent(canvasTransform, false);
            var rootRt = root.GetComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = Vector2.zero;
            rootRt.offsetMax = Vector2.zero;

            var cg = root.GetComponent<CanvasGroup>();
            cg.alpha = 1f;
            cg.interactable = true;
            cg.blocksRaycasts = true;

            // 2. Backdrop (Nền đen mờ 75%)
            var backdrop = new GameObject("Backdrop", typeof(RectTransform), typeof(Image), typeof(Button));
            backdrop.transform.SetParent(root.transform, false);
            var bdRt = backdrop.GetComponent<RectTransform>();
            bdRt.anchorMin = Vector2.zero;
            bdRt.anchorMax = Vector2.one;
            bdRt.sizeDelta = Vector2.zero;
            var bdImg = backdrop.GetComponent<Image>();
            bdImg.color = new Color(0f, 0f, 0f, 0.75f);
            var bdBtn = backdrop.GetComponent<Button>();
            bdBtn.transition = Selectable.Transition.None;

            // 3. PanelRoot
            var panelRoot = new GameObject("PanelRoot", typeof(RectTransform));
            panelRoot.transform.SetParent(root.transform, false);
            var prRt = panelRoot.GetComponent<RectTransform>();
            prRt.anchorMin = Vector2.zero;
            prRt.anchorMax = Vector2.one;
            prRt.sizeDelta = Vector2.zero;

            // =========================================================================
            // PHẦN 1: MINI MAP GRAPH (NỬA TRÊN MÀN HÌNH - PHÓNG TO 100%)
            // =========================================================================
            var mapSection = new GameObject("MapSection", typeof(RectTransform), typeof(Image));
            mapSection.transform.SetParent(panelRoot.transform, false);
            var msRt = mapSection.GetComponent<RectTransform>();
            msRt.anchorMin = new Vector2(0.5f, 1f);
            msRt.anchorMax = new Vector2(0.5f, 1f);
            msRt.pivot = new Vector2(0.5f, 1f);
            msRt.anchoredPosition = new Vector2(0f, -70f);
            msRt.sizeDelta = new Vector2(980f, 580f);

            var msImg = mapSection.GetComponent<Image>();
            msImg.color = new Color(0.08f, 0.10f, 0.14f, 0.85f);
            var msOutline = mapSection.AddComponent<Outline>();
            msOutline.effectColor = new Color(0.25f, 0.30f, 0.40f, 0.6f);
            msOutline.effectDistance = new Vector2(2f, -2f);

            // Badge Tầng ở góc trên
            var floorBadge = CreateText(mapSection.transform, "FloorBadgeText", "TIẾN TRÌNH HẦM NGỤC (TẦNG 1 / 13)", 24, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, new Color(0.35f, 0.85f, 1f));
            var fbRt = floorBadge.GetComponent<RectTransform>();
            fbRt.anchorMin = new Vector2(0f, 1f);
            fbRt.anchorMax = new Vector2(0f, 1f);
            fbRt.pivot = new Vector2(0f, 1f);
            fbRt.anchoredPosition = new Vector2(25f, -14f);
            fbRt.sizeDelta = new Vector2(500f, 38f);

            // ScrollView chứa cây Dungeon Graph
            var mapScrollView = new GameObject("MapScrollView", typeof(RectTransform), typeof(ScrollRect));
            mapScrollView.transform.SetParent(mapSection.transform, false);
            var msvRt = mapScrollView.GetComponent<RectTransform>();
            msvRt.anchorMin = Vector2.zero;
            msvRt.anchorMax = Vector2.one;
            msvRt.offsetMin = new Vector2(15f, 15f);
            msvRt.offsetMax = new Vector2(-15f, -56f);

            var mScroll = mapScrollView.GetComponent<ScrollRect>();
            mScroll.horizontal = false;
            mScroll.vertical = true;
            mScroll.scrollSensitivity = 35f;

            // Viewport sử dụng RectMask2D chuẩn, không cần Image Stencil
            var mapViewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            mapViewport.transform.SetParent(mapScrollView.transform, false);
            var mvRt = mapViewport.GetComponent<RectTransform>();
            mvRt.anchorMin = Vector2.zero;
            mvRt.anchorMax = Vector2.one;
            mvRt.sizeDelta = Vector2.zero;
            mScroll.viewport = mvRt;

            var mapContent = new GameObject("MapGraphContent", typeof(RectTransform));
            mapContent.transform.SetParent(mapViewport.transform, false);
            var mcRt = mapContent.GetComponent<RectTransform>();
            mcRt.anchorMin = new Vector2(0.5f, 1f);
            mcRt.anchorMax = new Vector2(0.5f, 1f);
            mcRt.pivot = new Vector2(0.5f, 1f);
            mcRt.anchoredPosition = Vector2.zero;
            mcRt.sizeDelta = new Vector2(950f, 1200f);
            mScroll.content = mcRt;

            // Player Pin (Token chỉ điểm người chơi)
            var playerPin = new GameObject("PlayerPin", typeof(RectTransform), typeof(Image));
            playerPin.transform.SetParent(mapContent.transform, false);
            var pinRt = playerPin.GetComponent<RectTransform>();
            pinRt.anchorMin = new Vector2(0.5f, 1f);
            pinRt.anchorMax = new Vector2(0.5f, 1f);
            pinRt.pivot = new Vector2(0.5f, 0.5f);
            pinRt.sizeDelta = new Vector2(56f, 56f);
            var pinImg = playerPin.GetComponent<Image>();
            pinImg.color = new Color(1f, 0.85f, 0.25f, 1f);
            var pinOutline = playerPin.AddComponent<Outline>();
            pinOutline.effectColor = new Color(0f, 0f, 0f, 0.95f);
            pinOutline.effectDistance = new Vector2(2.5f, -2.5f);

            var pinTxt = CreateText(playerPin.transform, "PinIcon", "P", 28, FontStyles.Bold, TextAlignmentOptions.Center, Color.black);
            var ptRt = pinTxt.GetComponent<RectTransform>();
            ptRt.anchorMin = Vector2.zero;
            ptRt.anchorMax = Vector2.one;
            ptRt.sizeDelta = Vector2.zero;

            // =========================================================================
            // PHẦN 2: OPTION MODAL (Ở GIỮA THEO STYLE ẢNH MẪU - PHÓNG TO 100%)
            // =========================================================================
            var optionModal = new GameObject("OptionModal", typeof(RectTransform), typeof(Image));
            optionModal.transform.SetParent(panelRoot.transform, false);
            var omRt = optionModal.GetComponent<RectTransform>();
            omRt.anchorMin = new Vector2(0.5f, 0.5f);
            omRt.anchorMax = new Vector2(0.5f, 0.5f);
            omRt.pivot = new Vector2(0.5f, 0.5f);
            omRt.anchoredPosition = new Vector2(0f, -20f);
            omRt.sizeDelta = new Vector2(980f, 620f);

            var omImg = optionModal.GetComponent<Image>();
            omImg.color = new Color(0.12f, 0.14f, 0.18f, 0.98f);

            var omOutline = optionModal.AddComponent<Outline>();
            omOutline.effectColor = new Color(0.40f, 0.45f, 0.55f, 0.8f);
            omOutline.effectDistance = new Vector2(3f, -3f);

            // Nút Close "X" ở góc trên phải
            var closeBtn = CreateButton(optionModal.transform, "CloseButton", "X", new Vector2(56f, 56f), new Color(0.85f, 0.25f, 0.25f, 0.9f), 32);
            var cbRt = closeBtn.GetComponent<RectTransform>();
            cbRt.anchorMin = new Vector2(1f, 1f);
            cbRt.anchorMax = new Vector2(1f, 1f);
            cbRt.pivot = new Vector2(1f, 1f);
            cbRt.anchoredPosition = new Vector2(-14f, -14f);

            // Header "Option" + Badge "Tùy chọn"
            var optTitle = CreateText(optionModal.transform, "OptionTitle", "Option", 38, FontStyles.Bold, TextAlignmentOptions.Center, new Color(0.95f, 0.95f, 0.98f));
            var otRt = optTitle.GetComponent<RectTransform>();
            otRt.anchorMin = new Vector2(0.5f, 1f);
            otRt.anchorMax = new Vector2(0.5f, 1f);
            otRt.pivot = new Vector2(0.5f, 1f);
            otRt.anchoredPosition = new Vector2(0f, -16f);
            otRt.sizeDelta = new Vector2(300f, 48f);

            var subBadge = new GameObject("SubBadge", typeof(RectTransform), typeof(Image));
            subBadge.transform.SetParent(optionModal.transform, false);
            var sbRt = subBadge.GetComponent<RectTransform>();
            sbRt.anchorMin = new Vector2(0.5f, 1f);
            sbRt.anchorMax = new Vector2(0.5f, 1f);
            sbRt.pivot = new Vector2(0.5f, 1f);
            sbRt.anchoredPosition = new Vector2(0f, -66f);
            sbRt.sizeDelta = new Vector2(180f, 34f);
            subBadge.GetComponent<Image>().color = new Color(0.20f, 0.24f, 0.32f, 1f);

            var sbTxt = CreateText(subBadge.transform, "Text", "Tùy chọn", 18, FontStyles.Normal, TextAlignmentOptions.Center, new Color(0.85f, 0.88f, 0.95f));
            var sbtRt = sbTxt.GetComponent<RectTransform>();
            sbtRt.anchorMin = Vector2.zero;
            sbtRt.anchorMax = Vector2.one;
            sbtRt.sizeDelta = Vector2.zero;

            // Row 1: BGM Slider (Left) & SFX Slider (Right)
            CreateHalfSlider(optionModal.transform, "BGMVolumeSlider", "BGMVolumeText", "BGM", -125f, true, 0.8f);
            CreateHalfSlider(optionModal.transform, "SFXVolumeSlider", "SFXVolumeText", "Hiệu ứng âm thanh", -125f, false, 0.9f);

            // Row 2: Font Sát Thương (Left) & Đồ Họa Cao (Right)
            CreateHalfToggle(optionModal.transform, "DamageNumbersToggle", "Font Sát Thương", -205f, true, true);
            CreateHalfToggle(optionModal.transform, "HighQualityToggle", "Đồ Họa Cao", -205f, false, true);

            // Row 3: Rung Màn Hình (Center/Left)
            CreateHalfToggle(optionModal.transform, "ScreenShakeToggle", "Rung Màn Hình", -275f, true, true);

            // Row 4: 2 Action Buttons ("Rút Lui" & "Tiếp Tục Chiến Đấu")
            var retreatBtn = CreateButton(optionModal.transform, "RetreatButton", "Rút Lui", new Vector2(360f, 76f), new Color(0.25f, 0.16f, 0.18f, 1f), 26);
            var rbRt = retreatBtn.GetComponent<RectTransform>();
            rbRt.anchorMin = new Vector2(0.5f, 0f);
            rbRt.anchorMax = new Vector2(0.5f, 0f);
            rbRt.pivot = new Vector2(1f, 0f);
            rbRt.anchoredPosition = new Vector2(-20f, 28f);
            var rbOutline = retreatBtn.AddComponent<Outline>();
            rbOutline.effectColor = new Color(0.55f, 0.25f, 0.28f, 0.9f);
            rbOutline.effectDistance = new Vector2(2.5f, -2.5f);

            var resumeBtn = CreateButton(optionModal.transform, "ResumeButton", "Tiếp Tục Chiến Đấu", new Vector2(480f, 76f), new Color(0.18f, 0.42f, 0.28f, 1f), 26);
            var resRt = resumeBtn.GetComponent<RectTransform>();
            resRt.anchorMin = new Vector2(0.5f, 0f);
            resRt.anchorMax = new Vector2(0.5f, 0f);
            resRt.pivot = new Vector2(0f, 0f);
            resRt.anchoredPosition = new Vector2(20f, 28f);
            var resOutline = resumeBtn.AddComponent<Outline>();
            resOutline.effectColor = new Color(0.35f, 0.85f, 0.45f, 0.9f);
            resOutline.effectDistance = new Vector2(2.5f, -2.5f);

            // =========================================================================
            // PHẦN 3: SÁT THƯƠNG GÂY CHO QUÁI VẬT (NẰM Ở ĐÁY MÀN HÌNH - PHÓNG TO 100%)
            // =========================================================================
            var dmgSection = new GameObject("DamageSection", typeof(RectTransform));
            dmgSection.transform.SetParent(panelRoot.transform, false);
            var dsRt = dmgSection.GetComponent<RectTransform>();
            dsRt.anchorMin = new Vector2(0.5f, 0f);
            dsRt.anchorMax = new Vector2(0.5f, 0f);
            dsRt.pivot = new Vector2(0.5f, 0f);
            dsRt.anchoredPosition = new Vector2(0f, 35f);
            dsRt.sizeDelta = new Vector2(980f, 260f);

            var dmgTitle = CreateText(dmgSection.transform, "DamageBarTitleText", "Sát thương gây cho quái vật", 22, FontStyles.Bold, TextAlignmentOptions.Center, new Color(0.85f, 0.88f, 0.92f));
            var dtRt = dmgTitle.GetComponent<RectTransform>();
            dtRt.anchorMin = new Vector2(0.5f, 1f);
            dtRt.anchorMax = new Vector2(0.5f, 1f);
            dtRt.pivot = new Vector2(0.5f, 1f);
            dtRt.anchoredPosition = new Vector2(0f, 0f);
            dtRt.sizeDelta = new Vector2(500f, 34f);

            var dmgBarBg = new GameObject("DamageBarBg", typeof(RectTransform), typeof(Image));
            dmgBarBg.transform.SetParent(dmgSection.transform, false);
            var dbRt = dmgBarBg.GetComponent<RectTransform>();
            dbRt.anchorMin = new Vector2(0f, 0f);
            dbRt.anchorMax = new Vector2(1f, 0f);
            dbRt.pivot = new Vector2(0.5f, 0f);
            dbRt.anchoredPosition = new Vector2(0f, 0f);
            dbRt.sizeDelta = new Vector2(0f, 210f);
            dmgBarBg.GetComponent<Image>().color = new Color(0.12f, 0.14f, 0.18f, 0.95f);
            var dbOutline = dmgBarBg.AddComponent<Outline>();
            dbOutline.effectColor = new Color(0.28f, 0.32f, 0.42f, 0.8f);
            dbOutline.effectDistance = new Vector2(2.5f, -2.5f);

            // Container chứa các Card ô vuông lính + người chơi
            var entriesContainer = new GameObject("DamageEntriesContainer", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
            entriesContainer.transform.SetParent(dmgBarBg.transform, false);
            var ecRt = entriesContainer.GetComponent<RectTransform>();
            ecRt.anchorMin = new Vector2(0.5f, 0.5f);
            ecRt.anchorMax = new Vector2(0.5f, 0.5f);
            ecRt.pivot = new Vector2(0.5f, 0.5f);
            ecRt.anchoredPosition = Vector2.zero;
            ecRt.sizeDelta = new Vector2(0f, 180f);

            var hlg = entriesContainer.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 24f;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            var csf = entriesContainer.GetComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Tooltip Popup nhỏ khi bấm vào card để xem chi tiết
            var tooltipBox = new GameObject("StatTooltipBox", typeof(RectTransform), typeof(Image));
            tooltipBox.transform.SetParent(panelRoot.transform, false);
            var tipRt = tooltipBox.GetComponent<RectTransform>();
            tipRt.anchorMin = new Vector2(0.5f, 0f);
            tipRt.anchorMax = new Vector2(0.5f, 0f);
            tipRt.pivot = new Vector2(0.5f, 0f);
            tipRt.anchoredPosition = new Vector2(0f, 310f);
            tipRt.sizeDelta = new Vector2(750f, 180f);
            tooltipBox.GetComponent<Image>().color = new Color(0.08f, 0.10f, 0.14f, 0.98f);
            var tipOutline = tooltipBox.AddComponent<Outline>();
            tipOutline.effectColor = new Color(1f, 0.85f, 0.35f, 0.8f);
            tipOutline.effectDistance = new Vector2(2.5f, -2.5f);

            var tipTxt = CreateText(tooltipBox.transform, "TooltipText", "", 22, FontStyles.Normal, TextAlignmentOptions.Center, Color.white);
            var tptRt = tipTxt.GetComponent<RectTransform>();
            tptRt.anchorMin = Vector2.zero;
            tptRt.anchorMax = Vector2.one;
            tptRt.sizeDelta = new Vector2(-24f, -24f);

            tooltipBox.SetActive(false);

            // Nối dây tự động
            var panelUI = root.GetComponent<SettingsPanelUI>();
            panelUI.AutoFindReferences();

            return root;
        }

        private static void CreateHalfSlider(Transform parent, string sliderName, string textName, string label, float yPos, bool isLeft, float defaultVal)
        {
            var rowObj = new GameObject(sliderName + "_Row", typeof(RectTransform));
            rowObj.transform.SetParent(parent, false);
            var rRt = rowObj.GetComponent<RectTransform>();
            rRt.anchorMin = new Vector2(isLeft ? 0f : 0.5f, 1f);
            rRt.anchorMax = new Vector2(isLeft ? 0.5f : 1f, 1f);
            rRt.pivot = new Vector2(0.5f, 1f);
            rRt.anchoredPosition = new Vector2(isLeft ? 24f : -24f, yPos);
            rRt.sizeDelta = new Vector2(-48f, 55f);

            var lbl = CreateText(rowObj.transform, "Label", label, 22, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, new Color(0.85f, 0.88f, 0.92f));
            var lRt = lbl.GetComponent<RectTransform>();
            lRt.anchorMin = new Vector2(0f, 1f);
            lRt.anchorMax = new Vector2(1f, 1f);
            lRt.pivot = new Vector2(0f, 1f);
            lRt.anchoredPosition = Vector2.zero;
            lRt.sizeDelta = new Vector2(0f, 26f);

            var sliderObj = new GameObject(sliderName, typeof(RectTransform), typeof(Slider));
            sliderObj.transform.SetParent(rowObj.transform, false);
            var sRt = sliderObj.GetComponent<RectTransform>();
            sRt.anchorMin = new Vector2(0f, 0f);
            sRt.anchorMax = new Vector2(1f, 0f);
            sRt.pivot = new Vector2(0f, 0f);
            sRt.anchoredPosition = new Vector2(0f, 2f);
            sRt.sizeDelta = new Vector2(-75f, 26f);

            var slider = sliderObj.GetComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = defaultVal;

            var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(sliderObj.transform, false);
            var bgRt = bg.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.sizeDelta = Vector2.zero;
            bg.GetComponent<Image>().color = new Color(0.22f, 0.25f, 0.32f, 1f);

            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(sliderObj.transform, false);
            var faRt = fillArea.GetComponent<RectTransform>();
            faRt.anchorMin = Vector2.zero;
            faRt.anchorMax = Vector2.one;
            faRt.sizeDelta = Vector2.zero;

            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(fillArea.transform, false);
            var fRt = fill.GetComponent<RectTransform>();
            fRt.sizeDelta = Vector2.zero;
            var fillImg = fill.GetComponent<Image>();
            fillImg.color = new Color(0.25f, 0.8f, 1f, 1f);

            slider.fillRect = fRt;
            slider.targetGraphic = fillImg;

            var valTxt = CreateText(rowObj.transform, textName, $"{Mathf.RoundToInt(defaultVal * 100)}%", 22, FontStyles.Bold, TextAlignmentOptions.MidlineRight, Color.white);
            var vtRt = valTxt.GetComponent<RectTransform>();
            vtRt.anchorMin = new Vector2(1f, 0f);
            vtRt.anchorMax = new Vector2(1f, 0f);
            vtRt.pivot = new Vector2(1f, 0f);
            vtRt.anchoredPosition = new Vector2(0f, 2f);
            vtRt.sizeDelta = new Vector2(65f, 26f);
        }

        private static void CreateHalfToggle(Transform parent, string toggleName, string label, float yPos, bool isLeft, bool defaultVal)
        {
            var rowObj = new GameObject(toggleName + "_Row", typeof(RectTransform));
            rowObj.transform.SetParent(parent, false);
            var rRt = rowObj.GetComponent<RectTransform>();
            rRt.anchorMin = new Vector2(isLeft ? 0f : 0.5f, 1f);
            rRt.anchorMax = new Vector2(isLeft ? 0.5f : 1f, 1f);
            rRt.pivot = new Vector2(0.5f, 1f);
            rRt.anchoredPosition = new Vector2(isLeft ? 24f : -24f, yPos);
            rRt.sizeDelta = new Vector2(-48f, 50f);

            var lbl = CreateText(rowObj.transform, "Label", label, 22, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, new Color(0.85f, 0.88f, 0.92f));
            var lRt = lbl.GetComponent<RectTransform>();
            lRt.anchorMin = new Vector2(0f, 0.5f);
            lRt.anchorMax = new Vector2(1f, 0.5f);
            lRt.pivot = new Vector2(0f, 0.5f);
            lRt.anchoredPosition = Vector2.zero;
            lRt.sizeDelta = new Vector2(-60f, 36f);

            var toggleObj = new GameObject(toggleName, typeof(RectTransform), typeof(Toggle));
            toggleObj.transform.SetParent(rowObj.transform, false);
            var tRt = toggleObj.GetComponent<RectTransform>();
            tRt.anchorMin = new Vector2(1f, 0.5f);
            tRt.anchorMax = new Vector2(1f, 0.5f);
            tRt.pivot = new Vector2(1f, 0.5f);
            tRt.anchoredPosition = Vector2.zero;
            tRt.sizeDelta = new Vector2(46f, 46f);

            var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(toggleObj.transform, false);
            var bgRt = bg.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.sizeDelta = Vector2.zero;
            bg.GetComponent<Image>().color = new Color(0.2f, 0.23f, 0.30f, 1f);

            var check = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
            check.transform.SetParent(bg.transform, false);
            var cRt = check.GetComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0.15f, 0.15f);
            cRt.anchorMax = new Vector2(0.85f, 0.85f);
            cRt.sizeDelta = Vector2.zero;
            var cImg = check.GetComponent<Image>();
            cImg.color = new Color(0.25f, 0.85f, 0.45f, 1f);

            var toggle = toggleObj.GetComponent<Toggle>();
            toggle.targetGraphic = bg.GetComponent<Image>();
            toggle.graphic = cImg;
            toggle.isOn = defaultVal;
        }

        private static TextMeshProUGUI CreateText(Transform parent, string name, string content, float size, FontStyles style, TextAlignmentOptions align, Color color)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            obj.transform.SetParent(parent, false);
            var tmp = obj.GetComponent<TextMeshProUGUI>();
            tmp.text = content;
            tmp.fontSize = size;
            tmp.fontStyle = style;
            tmp.alignment = align;
            tmp.color = color;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static GameObject CreateButton(Transform parent, string name, string label, Vector2 size, Color bgColor, float fontSize = 26)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            obj.transform.SetParent(parent, false);
            var rt = obj.GetComponent<RectTransform>();
            rt.sizeDelta = size;

            var img = obj.GetComponent<Image>();
            img.color = bgColor;

            var btn = obj.GetComponent<Button>();
            btn.targetGraphic = img;

            var txt = CreateText(obj.transform, "Text", label, fontSize, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
            var tRt = txt.GetComponent<RectTransform>();
            tRt.anchorMin = Vector2.zero;
            tRt.anchorMax = Vector2.one;
            tRt.sizeDelta = Vector2.zero;

            return obj;
        }
    }
}
