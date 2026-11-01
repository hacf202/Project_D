#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Project_D.Player;

namespace Project_D.EditorTools
{
    [CustomEditor(typeof(PlayerHealth))]
    public class PlayerHealthEditor : Editor
    {
        private PlayerHealth _target;
        private SerializedProperty _playerStatsProp;
        private SerializedProperty _fallbackMaxHealthProp;
        private SerializedProperty _iFrameDurationProp;
        private SerializedProperty _flashIntervalProp;
        private SerializedProperty _spriteRendererProp;

        private void OnEnable()
        {
            _target = (PlayerHealth)target;
            _playerStatsProp = serializedObject.FindProperty("_playerStats");
            _fallbackMaxHealthProp = serializedObject.FindProperty("_fallbackMaxHealth");
            _iFrameDurationProp = serializedObject.FindProperty("_iFrameDuration");
            _flashIntervalProp = serializedObject.FindProperty("_flashInterval");
            _spriteRendererProp = serializedObject.FindProperty("_spriteRenderer");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // 1. HEADER & STATUS BANNER
            EditorGUILayout.Space(4);
            bool hasStats = _playerStatsProp.objectReferenceValue != null || _target.LinkedStats != null;
            int maxHp = _target.MaxHealth;
            int curHp = Application.isPlaying ? _target.CurrentHealth : maxHp;

            Color bannerColor = hasStats ? new Color(0.1f, 0.35f, 0.2f, 0.5f) : new Color(0.35f, 0.25f, 0.1f, 0.5f);
            GUI.backgroundColor = bannerColor;
            EditorGUILayout.BeginVertical("box");
            GUI.backgroundColor = Color.white;

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("❤️ SINH LỰC NHÂN VẬT (PLAYER HEALTH)", EditorStyles.boldLabel);
            if (hasStats)
            {
                GUI.color = Color.green;
                GUILayout.Label("[PlayerStats: OK]", EditorStyles.miniLabel, GUILayout.ExpandWidth(false));
                GUI.color = Color.white;
            }
            else
            {
                GUI.color = Color.yellow;
                GUILayout.Label("[Dự Phòng / Fallback]", EditorStyles.miniLabel, GUILayout.ExpandWidth(false));
                GUI.color = Color.white;
            }
            EditorGUILayout.EndHorizontal();

            // Thanh Máu Trực Quan
            float hpRatio = maxHp > 0 ? Mathf.Clamp01((float)curHp / maxHp) : 0f;
            Color barColor = hpRatio > 0.5f ? new Color(0.2f, 0.9f, 0.3f) : (hpRatio > 0.25f ? new Color(1f, 0.8f, 0.2f) : new Color(1f, 0.3f, 0.3f));
            
            Rect rect = GUILayoutUtility.GetRect(18, 20, "TextField");
            EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.15f));
            Rect fillRect = new Rect(rect.x, rect.y, rect.width * hpRatio, rect.height);
            EditorGUI.DrawRect(fillRect, barColor);
            EditorGUI.LabelField(rect, $"  Máu: {curHp} / {maxHp} HP ({hpRatio * 100:F0}%)", EditorStyles.boldLabel);

            if (hasStats && _target.LinkedStats != null && _target.LinkedStats.PlayerData != null)
            {
                EditorGUILayout.HelpBox($"Nguồn chỉ số: ScriptableObject '{_target.LinkedStats.PlayerData.CharacterName}' ({_target.LinkedStats.PlayerData.name}) | Máu gốc: {_target.LinkedStats.BaseMaxHealth} HP", MessageType.None);
            }

            EditorGUILayout.EndVertical();

            // 2. DEPENDENCIES SECTION
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("1. Nguồn Dữ Liệu & Ràng Buộc", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_playerStatsProp, new GUIContent("Player Stats", "Script quản lý chỉ số toàn diện (Tự động lấy GetComponent nếu để trống)"));
            
            if (_playerStatsProp.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("Chưa gán PlayerStats! PlayerHealth sẽ sử dụng giá trị Fallback bên dưới.", MessageType.Warning);
                EditorGUILayout.PropertyField(_fallbackMaxHealthProp, new GUIContent("Fallback Max Health", "Máu tối đa dùng khi không có PlayerStats"));
            }

            // 3. I-FRAMES & VISUALS
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("2. Bất Tử Tạm Thời & Hiệu Ứng (I-Frames)", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_iFrameDurationProp, new GUIContent("I-Frame Duration (s)", "Thời gian bất tử sau khi trúng đòn"));
            EditorGUILayout.PropertyField(_flashIntervalProp, new GUIContent("Flash Interval (s)", "Tần suất nhấp nháy"));
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(_spriteRendererProp, new GUIContent("Sprite Renderer", "Renderer hiển thị nhân vật"));
            if (_spriteRendererProp.objectReferenceValue == null)
            {
                if (GUILayout.Button("Tự Động Gán", GUILayout.Width(100)))
                {
                    var sr = _target.GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        _spriteRendererProp.objectReferenceValue = sr;
                    }
                }
            }
            EditorGUILayout.EndHorizontal();

            if (_spriteRendererProp.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("SpriteRenderer chưa được gán! Hiệu ứng nhấp nháy khi bị đánh sẽ không hoạt động.", MessageType.Warning);
            }

            // 4. PLAY MODE TEST CONTROLS
            if (Application.isPlaying)
            {
                EditorGUILayout.Space(8);
                EditorGUILayout.LabelField("3. Công Cụ Test Khi Chạy Game (Play Mode)", EditorStyles.boldLabel);
                EditorGUILayout.BeginHorizontal();
                
                GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
                if (GUILayout.Button("-50 HP (Sát thương)"))
                {
                    _target.TakeDamage(50);
                }
                if (GUILayout.Button("-100 HP"))
                {
                    _target.TakeDamage(100);
                }
                
                GUI.backgroundColor = new Color(0.4f, 1f, 0.5f);
                if (GUILayout.Button("+50 HP (Hồi)"))
                {
                    _target.Heal(50);
                }
                if (GUILayout.Button("Hồi Đầy Máu"))
                {
                    _target.HealFull();
                }
                GUI.backgroundColor = Color.white;
                
                EditorGUILayout.EndHorizontal();
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
