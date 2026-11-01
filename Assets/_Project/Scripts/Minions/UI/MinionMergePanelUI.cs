using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Project_D.Minions.UI
{
    public class MinionMergePanelUI : MonoBehaviour
    {
        [Header("Panel Structure")]
        [SerializeField] private GameObject _panelRoot;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private Transform _entriesContainer;
        [SerializeField] private GameObject _entryPrefab;
        [SerializeField] private Button _closeButton;
        [SerializeField] private TMP_Text _emptyMessageText;

        [Header("Data Reference")]
        [SerializeField] private MinionDatabaseSO _database;

        [Header("Settings")]
        [SerializeField] private bool _pauseTimeWhenOpen = true;

        private readonly List<MinionMergeEntryUI> _spawnedEntries = new List<MinionMergeEntryUI>();
        private bool _isOpen = false;

        public bool IsOpen => _isOpen;

        private void Awake()
        {
            if (_panelRoot == null) _panelRoot = gameObject;

            if (_canvasGroup == null)
            {
                _canvasGroup = _panelRoot.GetComponent<CanvasGroup>();
                if (_canvasGroup == null)
                {
                    _canvasGroup = _panelRoot.AddComponent<CanvasGroup>();
                }
            }

            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveListener(Close);
                _closeButton.onClick.AddListener(Close);
            }

            // Mặc định ẩn bằng CanvasGroup thay vì SetActive(false)
            // Giúp GameObject luôn Active để tránh bẫy vòng đời Awake/Start và hiện mượt mà ngay lần bấm đầu
            HidePanel();
        }

        public void SetupReferences(GameObject panelRoot, Transform entriesContainer, Button closeButton, CanvasGroup canvasGroup = null)
        {
            _panelRoot = panelRoot;
            _entriesContainer = entriesContainer;
            _closeButton = closeButton;
            _canvasGroup = canvasGroup != null ? canvasGroup : panelRoot.GetComponent<CanvasGroup>();

            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveAllListeners();
                _closeButton.onClick.AddListener(Close);
            }
        }

        public void Open()
        {
            if (_isOpen) return;

            ShowPanel();

            if (_pauseTimeWhenOpen && Project_D.Core.PanelQueueManager.Instance != null)
            {
                Project_D.Core.PanelQueueManager.Instance.RequestPause(this, "MinionMergePanelModal");
            }

            RefreshList();
        }

        public void Close()
        {
            if (!_isOpen) return;

            HidePanel();

            if (_pauseTimeWhenOpen && Project_D.Core.PanelQueueManager.Instance != null)
            {
                Project_D.Core.PanelQueueManager.Instance.ReleasePause(this);
            }
        }

        private void ShowPanel()
        {
            _isOpen = true;

            // Đảm bảo panel root luôn active
            if (_panelRoot != null && !_panelRoot.activeSelf)
            {
                _panelRoot.SetActive(true);
            }

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
                _canvasGroup.interactable = true;
                _canvasGroup.blocksRaycasts = true;
            }
        }

        private void HidePanel()
        {
            _isOpen = false;

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.interactable = false;
                _canvasGroup.blocksRaycasts = false;
            }
            else if (_panelRoot != null)
            {
                _panelRoot.SetActive(false);
            }
        }

        private void OnDisable()
        {
            if (_isOpen && _pauseTimeWhenOpen && Project_D.Core.PanelQueueManager.HasInstance)
            {
                Project_D.Core.PanelQueueManager.Instance.ReleasePause(this);
                _isOpen = false;
            }
        }

        public void RefreshList()
        {
            if (_entriesContainer == null) return;

            // Đảm bảo có Database để tra cứu dữ liệu công thức
            if (_database == null && MinionManager.Instance != null)
            {
                _database = MinionManager.Instance.Database;
            }

            List<MergeAvailableInfo> availableMerges = null;
            if (MinionManager.Instance != null)
            {
                availableMerges = MinionManager.Instance.GetAllRecipesStatus();
            }
            else if (_database != null && _database.AllRecipes != null)
            {
                // Chế độ xem trước (Preview trong Editor hoặc khi chưa vào game)
                availableMerges = new List<MergeAvailableInfo>();
                foreach (var r in _database.AllRecipes)
                {
                    if (r != null)
                    {
                        availableMerges.Add(new MergeAvailableInfo
                        {
                            Recipe = r,
                            CurrentCount = 0,
                            RequiredCount = r.RequiredCount,
                            CanMerge = false,
                            IsLockedBySpecialCondition = r.RequiresSpecialUnlock
                        });
                    }
                }
            }

            if (availableMerges == null || availableMerges.Count == 0)
            {
                if (_emptyMessageText != null)
                {
                    _emptyMessageText.gameObject.SetActive(true);
                    _emptyMessageText.text = "Chưa có lính nào đủ điều kiện hợp nhất.\nHãy thu thập thêm lính cùng loại từ Thẻ Nâng Cấp!";
                }
                return;
            }

            if (_emptyMessageText != null)
            {
                _emptyMessageText.gameObject.SetActive(false);
            }

            // Đồng bộ danh sách entry có sẵn trong container nếu _spawnedEntries chưa cache
            if (_spawnedEntries.Count == 0 && _entriesContainer.childCount > 0)
            {
                var existingEntries = _entriesContainer.GetComponentsInChildren<MinionMergeEntryUI>(true);
                foreach (var e in existingEntries)
                {
                    if (e != null) _spawnedEntries.Add(e);
                }
            }

            // Ẩn tất cả entry trước khi cập nhật
            for (int i = 0; i < _spawnedEntries.Count; i++)
            {
                if (_spawnedEntries[i] != null) _spawnedEntries[i].gameObject.SetActive(false);
            }

            // Cập nhật hoặc sinh mới từng công thức
            for (int i = 0; i < availableMerges.Count; i++)
            {
                MinionMergeEntryUI entryUI;
                if (i < _spawnedEntries.Count && _spawnedEntries[i] != null && _spawnedEntries[i].IsInitialized)
                {
                    entryUI = _spawnedEntries[i];
                    entryUI.gameObject.SetActive(true);
                }
                else
                {
                    if (i < _spawnedEntries.Count && _spawnedEntries[i] != null)
                    {
                        if (Application.isPlaying)
                            Destroy(_spawnedEntries[i].gameObject);
                        else
                            DestroyImmediate(_spawnedEntries[i].gameObject);
                    }

                    if (_entryPrefab == null)
                    {
                        entryUI = MinionMergeEntryUI.CreateDynamicEntry(_entriesContainer);
                    }
                    else
                    {
                        GameObject itemObj = Instantiate(_entryPrefab, _entriesContainer);
                        entryUI = itemObj.GetComponent<MinionMergeEntryUI>();
                    }

                    if (i < _spawnedEntries.Count)
                    {
                        _spawnedEntries[i] = entryUI;
                    }
                    else
                    {
                        _spawnedEntries.Add(entryUI);
                    }
                }

                entryUI.Setup(availableMerges[i], OnExecuteMergeClicked);
            }
        }

        private void OnExecuteMergeClicked(MergeRecipeSO recipe)
        {
            if (MinionManager.Instance != null && recipe != null)
            {
                bool success = MinionManager.Instance.ExecuteMerge(recipe);
                if (success)
                {
                    RefreshList();
                }
            }
        }
    }
}
