using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project_D.UI.Settings
{
    /// <summary>
    /// Component quản lý Card đại diện (Hero hoặc Lính) trong thanh tỷ lệ sát thương của Settings Panel.
    /// </summary>
    public class DamageEntryCard : MonoBehaviour
    {
        private Image _bg;
        private Outline _outline;
        private Image _icon;
        private TextMeshProUGUI _countBadge;
        private TextMeshProUGUI _percentText;
        private string _tooltipContent;
        private Action<string> _onCardClicked;

        public void Init(Image bg, Outline outline, Image icon, TextMeshProUGUI count, TextMeshProUGUI pct)
        {
            _bg = bg;
            _outline = outline;
            _icon = icon;
            _countBadge = count;
            _percentText = pct;

            var btn = GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveListener(OnClick);
                btn.onClick.AddListener(OnClick);
            }
        }

        public void Setup(Sprite sprite, string pctString, bool isHero, int count, Color outlineColor, string tooltip, Action<string> onCardClicked)
        {
            _tooltipContent = tooltip;
            _onCardClicked = onCardClicked;

            if (_icon != null)
            {
                _icon.sprite = sprite;
                _icon.enabled = (sprite != null);
            }

            if (_percentText != null)
            {
                _percentText.text = pctString;
            }

            if (_countBadge != null)
            {
                _countBadge.gameObject.SetActive(!isHero && count > 1);
                _countBadge.text = $"x{count}";
            }

            if (_outline != null)
            {
                _outline.effectColor = outlineColor;
                _outline.effectDistance = isHero ? new Vector2(3f, -3f) : new Vector2(2f, -2f);
            }
        }

        private void OnClick()
        {
            _onCardClicked?.Invoke(_tooltipContent);
        }
    }
}
