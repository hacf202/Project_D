using System;
using System.Collections.Generic;
using UnityEngine;
using Project_D.Dungeon.Run;

namespace Project_D.Dungeon.Room
{
    [Serializable]
    public class NodeVisualEntry
    {
        public RunNodeType NodeType;
        public string Title;
        public string Symbol; // Ký tự / Emoji đại diện (Fallback khi chưa gán Sprite)
        public Color ThemeColor = Color.white;
        public Sprite CustomIcon;
    }

    [CreateAssetMenu(fileName = "PortalVisualConfig", menuName = "Project_D/Dungeon/Portal Visual Config")]
    public class PortalVisualConfigSO : ScriptableObject
    {
        [SerializeField] private List<NodeVisualEntry> _entries = new List<NodeVisualEntry>();

        public NodeVisualEntry GetVisualForType(RunNodeType type)
        {
            var entry = _entries.Find(e => e.NodeType == type);
            if (entry != null)
            {
                // Đảm bảo Symbol an toàn tuyệt đối với Font TextMeshPro (tránh ký tự emoji gây thiếu glyph)
                if (IsEmojiOrNonAscii(entry.Symbol))
                {
                    entry.Symbol = GetDefaultBadgeForType(type);
                }
                return entry;
            }

            // Fallback mặc định nếu chưa cấu hình
            return new NodeVisualEntry
            {
                NodeType = type,
                Title = type.ToString(),
                Symbol = GetDefaultBadgeForType(type),
                ThemeColor = Color.white
            };
        }

        public static string GetDefaultBadgeForType(RunNodeType type)
        {
            switch (type)
            {
                case RunNodeType.NormalBattle: return "VS";
                case RunNodeType.StarterReward: return "GIFT";
                case RunNodeType.EliteBattle: return "ELITE";
                case RunNodeType.MiniBoss: return "M-BOSS";
                case RunNodeType.BigReward: return "LOOT";
                case RunNodeType.RestSite: return "REST";
                case RunNodeType.Shop: return "SHOP";
                case RunNodeType.BuffReward: return "BUFF";
                case RunNodeType.BossBattle: return "BOSS";
                default: return "GATE";
            }
        }

        public static bool IsEmojiOrNonAscii(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            foreach (char c in text)
            {
                // Ký tự ngoài bảng mã ASCII chuẩn (0-127) hoặc ký tự surrogate / emoji
                if (c > 127 || char.IsSurrogate(c))
                {
                    return true;
                }
            }
            return false;
        }

        public void SanitizeSymbols()
        {
            if (_entries == null) return;
            foreach (var entry in _entries)
            {
                if (IsEmojiOrNonAscii(entry.Symbol))
                {
                    entry.Symbol = GetDefaultBadgeForType(entry.NodeType);
                }
            }
        }

        [ContextMenu("Populate Default Distinct Icons")]
        public void PopulateDefaultDistinctIcons()
        {
            _entries.Clear();

            _entries.Add(new NodeVisualEntry
            {
                NodeType = RunNodeType.NormalBattle,
                Title = "Quái Thường",
                Symbol = GetDefaultBadgeForType(RunNodeType.NormalBattle),
                ThemeColor = new Color(0.9f, 0.3f, 0.3f) // Đỏ kiếm
            });

            _entries.Add(new NodeVisualEntry
            {
                NodeType = RunNodeType.StarterReward,
                Title = "Thưởng Khởi Đầu",
                Symbol = GetDefaultBadgeForType(RunNodeType.StarterReward),
                ThemeColor = new Color(0.3f, 0.8f, 1f) // Xanh lam ngọc
            });

            _entries.Add(new NodeVisualEntry
            {
                NodeType = RunNodeType.EliteBattle,
                Title = "Tinh Anh ELITE",
                Symbol = GetDefaultBadgeForType(RunNodeType.EliteBattle),
                ThemeColor = new Color(0.95f, 0.15f, 0.55f) // Hồng tím đậm
            });

            _entries.Add(new NodeVisualEntry
            {
                NodeType = RunNodeType.MiniBoss,
                Title = "MINI BOSS",
                Symbol = GetDefaultBadgeForType(RunNodeType.MiniBoss),
                ThemeColor = new Color(0.85f, 0.2f, 0.9f) // Tím quỷ
            });

            _entries.Add(new NodeVisualEntry
            {
                NodeType = RunNodeType.BigReward,
                Title = "Thưởng Lớn Mini Boss",
                Symbol = GetDefaultBadgeForType(RunNodeType.BigReward),
                ThemeColor = new Color(1f, 0.8f, 0.1f) // Vàng sao
            });

            _entries.Add(new NodeVisualEntry
            {
                NodeType = RunNodeType.RestSite,
                Title = "Lửa Trại Hồi Máu",
                Symbol = GetDefaultBadgeForType(RunNodeType.RestSite),
                ThemeColor = new Color(0.2f, 0.9f, 0.35f) // Xanh lá
            });

            _entries.Add(new NodeVisualEntry
            {
                NodeType = RunNodeType.Shop,
                Title = "Cửa Hàng Thương Nhân",
                Symbol = GetDefaultBadgeForType(RunNodeType.Shop),
                ThemeColor = new Color(1f, 0.85f, 0.2f) // Vàng kim
            });

            _entries.Add(new NodeVisualEntry
            {
                NodeType = RunNodeType.BuffReward,
                Title = "Lựa Chọn Buff",
                Symbol = GetDefaultBadgeForType(RunNodeType.BuffReward),
                ThemeColor = new Color(0.4f, 0.7f, 1f) // Xanh dương
            });

            _entries.Add(new NodeVisualEntry
            {
                NodeType = RunNodeType.BossBattle,
                Title = "TRÙM CUỐI BOSS",
                Symbol = GetDefaultBadgeForType(RunNodeType.BossBattle),
                ThemeColor = new Color(1f, 0.15f, 0.1f) // Đỏ rực
            });
        }

        private void OnEnable()
        {
            SanitizeSymbols();
        }

        private void OnValidate()
        {
            if (_entries == null || _entries.Count == 0)
            {
                PopulateDefaultDistinctIcons();
            }
            else
            {
                SanitizeSymbols();
            }
        }
    }
}
