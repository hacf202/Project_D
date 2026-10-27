using UnityEngine;
using Project_D.Dungeon.Run;

namespace Project_D.Dungeon.Room
{
    [CreateAssetMenu(fileName = "RoomVariation", menuName = "Project_D/Dungeon/Room Variation")]
    public class RoomVariationSO : ScriptableObject
    {
        [Header("Room Identity")]
        [SerializeField] private string _roomName = "New Room Variation";
        [SerializeField] private RunNodeType _supportedType = RunNodeType.NormalBattle;
        [SerializeField] private GameObject _roomPrefab;

        [Header("Selection & Mechanics")]
        [Range(1, 100)]
        [SerializeField] private int _selectionWeight = 10;
        [TextArea(2, 4)]
        [SerializeField] private string _description = "Mô tả bố cục và chiến thuật của phòng";

        public string RoomName => _roomName;
        public RunNodeType SupportedType => _supportedType;
        public GameObject RoomPrefab => _roomPrefab;
        public int SelectionWeight => _selectionWeight;
        public string Description => _description;

        public void Initialize(string roomName, RunNodeType type, GameObject prefab, int weight = 10, string desc = "")
        {
            _roomName = roomName;
            _supportedType = type;
            _roomPrefab = prefab;
            _selectionWeight = weight;
            _description = desc;
        }

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(_roomName) && _roomPrefab != null)
            {
                _roomName = _roomPrefab.name;
            }
        }
    }
}
