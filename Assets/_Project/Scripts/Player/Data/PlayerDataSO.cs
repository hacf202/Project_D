using UnityEngine;

namespace Project_D.Player.Data
{
    [CreateAssetMenu(fileName = "New_PlayerData", menuName = "Project_D/Player/Player Data")]
    public class PlayerDataSO : ScriptableObject
    {
        [Header("Character Identity")]
        [SerializeField] private string _characterId = "hero_default";
        [SerializeField] private string _characterName = "Chiến Binh";
        [TextArea(2, 4)]
        [SerializeField] private string _description = "Chiến binh kiên cường với chỉ số cân bằng.";
        [SerializeField] private Sprite _portrait;

        [Header("Base Vitality & Mobility")]
        [Tooltip("Lượng máu tối đa cơ bản")]
        [SerializeField] private int _baseMaxHealth = 1000;

        [Tooltip("Tốc độ di chuyển cơ bản (m/s)")]
        [SerializeField] private float _baseMoveSpeed = 5f;

        [Tooltip("Bán kính hút ngọc kinh nghiệm và vật phẩm")]
        [SerializeField] private float _basePickupRadius = 3.5f;

        [Tooltip("Tốc độ hồi máu nội tại mỗi giây (HP/s)")]
        [SerializeField] private float _baseHealthRegen = 0f;

        [Header("Base Combat Modifiers")]
        [Tooltip("Sát thương cộng thêm cơ bản")]
        [SerializeField] private int _baseDamageBonus = 0;

        [Tooltip("Hệ số tốc độ bắn cơ bản (1.0 = 100%)")]
        [SerializeField] private float _baseFireRateModifier = 1f;

        [Tooltip("Số viên đạn bắn thêm mỗi loạt cơ bản")]
        [SerializeField] private int _baseProjectilesBonus = 0;

        [Header("Level & Experience Progression")]
        [Tooltip("Kinh nghiệm cần để đạt cấp 2")]
        [SerializeField] private int _baseExpToNextLevel = 20;

        [Tooltip("Kinh nghiệm tăng thêm mỗi cấp (Công thức lũy tiến)")]
        [SerializeField] private int _expIncreasePerLevel = 15;

        // Getters
        public string CharacterId => _characterId;
        public string CharacterName => _characterName;
        public string Description => _description;
        public Sprite Portrait => _portrait;

        public int BaseMaxHealth => _baseMaxHealth;
        public float BaseMoveSpeed => _baseMoveSpeed;
        public float BasePickupRadius => _basePickupRadius;
        public float BaseHealthRegen => _baseHealthRegen;

        public int BaseDamageBonus => _baseDamageBonus;
        public float BaseFireRateModifier => _baseFireRateModifier;
        public int BaseProjectilesBonus => _baseProjectilesBonus;

        public int BaseExpToNextLevel => _baseExpToNextLevel;
        public int ExpIncreasePerLevel => _expIncreasePerLevel;
    }
}
