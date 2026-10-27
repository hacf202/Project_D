using UnityEngine;
using TMPro;
using Project_D.Dungeon.Run;
using Project_D.Dungeon.Room;

namespace Project_D.Dungeon.UI
{
    public class RunHUD : MonoBehaviour
    {
        public static RunHUD Instance { get; private set; }

        [Header("Floor & Room Info")]
        [SerializeField] private TMP_Text _floorText;
        [SerializeField] private TMP_Text _roomTypeText;

        [Header("Souls Currency")]
        [SerializeField] private TMP_Text _soulsText;

        [Header("Combat Wave Info")]
        [SerializeField] private GameObject _waveInfoPanel;
        [SerializeField] private TMP_Text _waveProgressText;
        [SerializeField] private TMP_Text _remainingEnemiesText;

        [Header("Room Cleared Banner")]
        [SerializeField] private GameObject _roomClearedBanner;
        [SerializeField] private float _bannerDuration = 2.5f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (_roomClearedBanner != null) _roomClearedBanner.SetActive(false);
        }

        private void OnEnable()
        {
            RunManager.OnFloorChanged += HandleFloorChanged;
            RunManager.OnSoulsChanged += HandleSoulsChanged;
            RoomController.OnWaveProgressChanged += HandleWaveProgressChanged;
            RoomController.OnRoomCombatStarted += HandleCombatStarted;
            RoomController.OnRoomCleared += HandleRoomCleared;
        }

        private void OnDisable()
        {
            RunManager.OnFloorChanged -= HandleFloorChanged;
            RunManager.OnSoulsChanged -= HandleSoulsChanged;
            RoomController.OnWaveProgressChanged -= HandleWaveProgressChanged;
            RoomController.OnRoomCombatStarted -= HandleCombatStarted;
            RoomController.OnRoomCleared -= HandleRoomCleared;
        }

        private void Start()
        {
            if (RunManager.Instance != null)
            {
                HandleFloorChanged(RunManager.Instance.CurrentFloor, RunManager.Instance.CurrentNode);
                HandleSoulsChanged(RunManager.Instance.CurrentSouls);
            }
        }

        private void HandleFloorChanged(int floor, RunNodeData nodeData)
        {
            if (_floorText != null)
            {
                _floorText.text = $"TẦNG {floor} / 13";
            }

            if (_roomTypeText != null && nodeData != null)
            {
                _roomTypeText.text = nodeData.DisplayName;
                _roomTypeText.color = nodeData.NodeColor;
            }

            // Tắt banner chiến thắng khi bước sang tầng mới
            if (_roomClearedBanner != null) _roomClearedBanner.SetActive(false);
        }

        private void HandleSoulsChanged(int souls)
        {
            if (_soulsText != null)
            {
                _soulsText.text = $"Linh Hồn: {souls} Souls";
            }
        }

        private void HandleCombatStarted()
        {
            if (_waveInfoPanel != null) _waveInfoPanel.SetActive(true);
        }

        private void HandleWaveProgressChanged(int currentWave, int totalWaves, int remainingEnemies, int totalInWave)
        {
            if (_waveInfoPanel != null && !_waveInfoPanel.activeSelf)
            {
                _waveInfoPanel.SetActive(true);
            }

            if (_waveProgressText != null)
            {
                _waveProgressText.text = $"ĐỢT: {currentWave} / {totalWaves}";
            }

            if (_remainingEnemiesText != null)
            {
                _remainingEnemiesText.text = totalInWave > 0 ? $"Quái: {remainingEnemies} / {totalInWave}" : $"Quái: {remainingEnemies}";
            }
        }

        private void HandleRoomCleared()
        {
            if (_waveInfoPanel != null) _waveInfoPanel.SetActive(false);

            if (_roomClearedBanner != null)
            {
                _roomClearedBanner.SetActive(true);
                CancelInvoke(nameof(HideClearedBanner));
                Invoke(nameof(HideClearedBanner), _bannerDuration);
            }
        }

        private void HideClearedBanner()
        {
            if (_roomClearedBanner != null) _roomClearedBanner.SetActive(false);
        }
    }
}
