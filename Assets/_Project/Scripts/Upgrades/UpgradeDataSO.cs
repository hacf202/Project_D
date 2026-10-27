using UnityEngine;
using Project_D.Player;

namespace Project_D.Upgrades
{
    [CreateAssetMenu(fileName = "NewUpgrade", menuName = "Project_D/Upgrades/Upgrade Data")]
    public class UpgradeDataSO : ScriptableObject
    {
        [Header("General Info")]
        [SerializeField] private string _upgradeId = "upgrade_id";
        [SerializeField] private string _title = "New Upgrade";
        [TextArea(2, 4)]
        [SerializeField] private string _description = "+10% Sức mạnh";
        [SerializeField] private Sprite _icon;

        [Header("Upgrade Effect")]
        [SerializeField] private UpgradeType _type;
        [SerializeField] private float _value = 1f; // Giá trị buff (1 viên, 0.2 tốc bắn, 5 damage, 15% tốc chạy...)

        public string UpgradeId => _upgradeId;
        public string Title => _title;
        public string Description => _description;
        public Sprite Icon => _icon;
        public UpgradeType Type => _type;
        public float Value => _value;

        // Áp dụng buff trực tiếp vào PlayerStats theo nguyên tắc Open-Closed
        public void ApplyUpgrade()
        {
            if (PlayerStats.Instance == null) return;

            switch (_type)
            {
                case UpgradeType.Projectiles:
                    PlayerStats.Instance.AddProjectilesBonus(Mathf.RoundToInt(_value));
                    break;
                case UpgradeType.FireRate:
                    PlayerStats.Instance.AddFireRateBonus(_value);
                    break;
                case UpgradeType.Damage:
                    PlayerStats.Instance.AddDamageBonus(Mathf.RoundToInt(_value));
                    break;
                case UpgradeType.MoveSpeed:
                    PlayerStats.Instance.AddMoveSpeedBonus(_value);
                    break;
                case UpgradeType.MaxHealth:
                    PlayerStats.Instance.AddMaxHealthBonus(Mathf.RoundToInt(_value));
                    break;
                case UpgradeType.PickupRadius:
                    PlayerStats.Instance.AddPickupRadiusBonus(_value);
                    break;
            }
        }
    }
}
