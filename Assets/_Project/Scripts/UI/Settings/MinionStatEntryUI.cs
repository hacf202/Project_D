using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Project_D.Minions;

namespace Project_D.UI.Settings
{
    public class MinionStatEntryUI : MonoBehaviour
    {
        [Header("Identity Elements")]
        [SerializeField] private Image _iconImage;
        [SerializeField] private TMP_Text _iconFallbackText;
        [SerializeField] private Image _tierBadgeImage;
        [SerializeField] private TMP_Text _tierBadgeText;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _classText;
        [SerializeField] private TMP_Text _countText;

        [Header("Combat Stats Elements")]
        [SerializeField] private TMP_Text _damageText;
        [SerializeField] private TMP_Text _attackSpeedText;
        [SerializeField] private TMP_Text _rangeText;
        [SerializeField] private TMP_Text _methodText;
        [SerializeField] private TMP_Text _moveSpeedText;
        [SerializeField] private TMP_Text _maxHealthText;
        [SerializeField] private TMP_Text _respawnText;

        public void AutoFindReferences()
        {
            if (_iconImage == null)
            {
                var iconT = transform.Find("Icon");
                if (iconT != null) _iconImage = iconT.GetComponent<Image>();
            }

            if (_iconFallbackText == null && _iconImage != null)
            {
                _iconFallbackText = _iconImage.GetComponentInChildren<TMP_Text>(true);
            }

            if (_tierBadgeImage == null)
            {
                var tb = transform.Find("TierBadge");
                if (tb != null)
                {
                    _tierBadgeImage = tb.GetComponent<Image>();
                    _tierBadgeText = tb.GetComponentInChildren<TMP_Text>(true);
                }
            }

            if (_nameText == null)
            {
                var n = transform.Find("NameText");
                if (n != null) _nameText = n.GetComponent<TMP_Text>();
            }

            if (_classText == null)
            {
                var c = transform.Find("ClassText");
                if (c != null) _classText = c.GetComponent<TMP_Text>();
            }

            if (_countText == null)
            {
                var cnt = transform.Find("CountText");
                if (cnt != null) _countText = cnt.GetComponent<TMP_Text>();
            }

            // Stats row
            if (_damageText == null) _damageText = FindStatText("DamageText");
            if (_attackSpeedText == null) _attackSpeedText = FindStatText("AttackSpeedText");
            if (_rangeText == null) _rangeText = FindStatText("RangeText");
            if (_methodText == null) _methodText = FindStatText("MethodText");
            if (_moveSpeedText == null) _moveSpeedText = FindStatText("MoveSpeedText");
            if (_maxHealthText == null) _maxHealthText = FindStatText("MaxHealthText");
            if (_respawnText == null) _respawnText = FindStatText("RespawnText");
        }

        private TMP_Text FindStatText(string childName)
        {
            var t = transform.Find(childName);
            if (t != null) return t.GetComponent<TMP_Text>();
            var all = GetComponentsInChildren<TMP_Text>(true);
            foreach (var txt in all)
            {
                if (txt.gameObject.name.Equals(childName, System.StringComparison.OrdinalIgnoreCase))
                {
                    return txt;
                }
            }
            return null;
        }

        public void Setup(MinionDataSO data, int aliveCount, int totalCount)
        {
            AutoFindReferences();

            if (data == null) return;

            // 1. Icon & Tên
            if (_nameText != null)
            {
                _nameText.text = $"<b>{data.MinionName}</b>";
            }

            if (_classText != null)
            {
                _classText.text = $"{GetClassName(data.Class)}";
                _classText.color = GetClassColor(data.Class);
            }

            if (_tierBadgeImage != null)
            {
                _tierBadgeImage.color = GetTierColor(data.Tier);
            }

            if (_tierBadgeText != null)
            {
                _tierBadgeText.text = GetTierName(data.Tier);
            }

            if (_iconImage != null)
            {
                Sprite s = data.Icon;
                if (s == null && data.Prefab != null)
                {
                    var sr = data.Prefab.GetComponentInChildren<SpriteRenderer>();
                    if (sr != null) s = sr.sprite;
                }

                if (s != null)
                {
                    _iconImage.sprite = s;
                    _iconImage.preserveAspect = true;
                    _iconImage.color = (aliveCount > 0) ? Color.white : new Color(0.4f, 0.4f, 0.4f, 0.6f);
                    if (_iconFallbackText != null) _iconFallbackText.gameObject.SetActive(false);
                }
                else
                {
                    _iconImage.sprite = null;
                    _iconImage.color = GetClassColor(data.Class);
                    if (_iconFallbackText != null)
                    {
                        _iconFallbackText.gameObject.SetActive(true);
                        _iconFallbackText.text = !string.IsNullOrEmpty(data.MinionName) ? data.MinionName.Substring(0, 1) : "M";
                    }
                }
            }

            // 2. Số lượng sống / tổng
            if (_countText != null)
            {
                string countColor = (aliveCount > 0) ? "#4CAF50" : "#E53935";
                _countText.text = $"Số lượng: <color={countColor}><b>{aliveCount}/{totalCount}</b></color>";
            }

            // 3. Thông số chiến đấu
            if (_damageText != null)
            {
                _damageText.text = $"Sát thương: <color=#FFD54F><b>{data.AttackDamage}</b></color>";
            }

            if (_attackSpeedText != null)
            {
                _attackSpeedText.text = $"Tốc độ đánh: <color=#81D4FA><b>{data.AttackSpeed:0.1f} đòn/s</b></color>";
            }

            if (_rangeText != null)
            {
                _rangeText.text = $"Tầm đánh: <color=#A5D6A7><b>{data.AttackRange:0.1f}m</b></color>";
            }

            if (_methodText != null)
            {
                _methodText.text = $"Cách đánh: <color=#CE93D8><b>{GetAttackMethodName(data.AttackMethod)}</b></color>";
            }

            if (_moveSpeedText != null)
            {
                _moveSpeedText.text = $"Tốc độ chạy: <b>{data.MoveSpeed:0.1f}</b>";
            }

            if (_maxHealthText != null)
            {
                _maxHealthText.text = $"Máu tối đa: <color=#81C784><b>{data.MaxHealth}</b></color>";
            }

            if (_respawnText != null)
            {
                _respawnText.text = $"Hồi sinh sau: <color=#FFB74D><b>{data.RespawnDuration:0}s</b></color>";
            }
        }

        private string GetClassName(MinionClass mClass)
        {
            switch (mClass)
            {
                case MinionClass.Warrior: return "Chiến Binh";
                case MinionClass.Archer: return "Cung Thủ";
                case MinionClass.Mage: return "Pháp Sư";
                case MinionClass.Tank: return "Đỡ Đòn";
                case MinionClass.Support: return "Hỗ Trợ";
                default: return "Lính";
            }
        }

        private Color GetClassColor(MinionClass mClass)
        {
            switch (mClass)
            {
                case MinionClass.Warrior: return new Color(0.3f, 0.75f, 1f);
                case MinionClass.Archer: return new Color(0.35f, 0.95f, 0.45f);
                case MinionClass.Mage: return new Color(0.8f, 0.45f, 1f);
                case MinionClass.Tank: return new Color(1f, 0.65f, 0.25f);
                case MinionClass.Support: return new Color(1f, 0.9f, 0.25f);
                default: return Color.white;
            }
        }

        private string GetTierName(MinionTier tier)
        {
            switch (tier)
            {
                case MinionTier.Tier1: return "TIER 1";
                case MinionTier.Tier2: return "TIER 2";
                case MinionTier.Tier3: return "TIER 3";
                case MinionTier.Tier4_Special: return "TIER 4";
                default: return "TIER 1";
            }
        }

        private Color GetTierColor(MinionTier tier)
        {
            switch (tier)
            {
                case MinionTier.Tier1: return new Color(0.8f, 0.85f, 0.9f);
                case MinionTier.Tier2: return new Color(0.3f, 0.85f, 0.45f);
                case MinionTier.Tier3: return new Color(0.85f, 0.45f, 1f);
                case MinionTier.Tier4_Special: return new Color(1f, 0.65f, 0.15f);
                default: return Color.white;
            }
        }

        private string GetAttackMethodName(AttackMethod method)
        {
            switch (method)
            {
                case AttackMethod.Melee: return "Cận Chiến";
                case AttackMethod.RangedProjectile: return "Bắn Tỉa Tầm Xa";
                case AttackMethod.AoE: return "Sát Thương Lan (AoE)";
                default: return "Tự Động";
            }
        }
    }
}
