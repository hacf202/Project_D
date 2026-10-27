using UnityEngine;

namespace Project_D.Combat
{
    // Tạo menu chuột phải trong Unity: Create -> Project_D -> Combat -> Weapon Data
    [CreateAssetMenu(fileName = "NewWeaponData", menuName = "Project_D/Combat/Weapon Data")]
    public class WeaponDataSO : ScriptableObject
    {
        [Header("General Info")]
        [SerializeField] private string _weaponName = "New Weapon";
        [SerializeField] private Sprite _icon;

        [Header("Combat Stats")]
        [SerializeField] private int _damage = 10;                 // Sát thương cơ bản của mỗi phát đạn
        [SerializeField] private float _knockbackForce = 0f;       // Lực giật lùi quái (Tùy chọn: 0 = không giật lùi)
        [SerializeField] private GameObject _projectilePrefab;     // Khuôn đạn của vũ khí này
        [SerializeField] private float _fireRate = 1.5f;           // Số phát bắn mỗi giây
        [SerializeField] private float _range = 8f;                // Tầm bắn xa tối đa (mét)
        [SerializeField] private int _projectilesPerShot = 1;      // Số viên đạn mỗi phát bắn (Pistol = 1, Shotgun = 3-5)
        [SerializeField] private float _spreadAngle = 0f;          // Độ xòe góc bắn (dành cho súng chùm)

        // Các thuộc tính chỉ đọc (Read-only properties) để WeaponController lấy thông số
        public string WeaponName => _weaponName;
        public Sprite Icon => _icon;
        public int Damage => _damage;
        public float KnockbackForce => _knockbackForce;
        public GameObject ProjectilePrefab => _projectilePrefab;
        public float FireRate => _fireRate;
        public float Range => _range;
        public int ProjectilesPerShot => _projectilesPerShot;
        public float SpreadAngle => _spreadAngle;
    }
}
