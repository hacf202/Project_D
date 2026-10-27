using UnityEngine;
using TMPro;
using Project_D.Player;
using Project_D.Dungeon.Run;

namespace Project_D.Dungeon.Interactions
{
    public enum InteractionType
    {
        RestSite,       // Lửa trại hồi máu (Miễn phí 30% hoặc Souls 100%)
        Shop,           // Cửa hàng mua vật phẩm bằng Souls
        BuffReward      // Bệ thờ nhận buff nâng cấp
    }

    [RequireComponent(typeof(Collider2D))]
    public class InteractionPoint : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private InteractionType _type = InteractionType.RestSite;
        [SerializeField] private string _promptText = "Chạm để tương tác";
        [SerializeField] private bool _oneTimeUse = true;

        [Header("Visual Prompt")]
        [SerializeField] private GameObject _promptBubble;
        [SerializeField] private TMP_Text _promptLabel;

        private bool _isUsed = false;

        public InteractionType Type => _type;

        private void Awake()
        {
            var col = GetComponent<Collider2D>();
            col.isTrigger = true;

            if (_promptLabel != null)
            {
                _promptLabel.text = _promptText;
            }

            if (_promptBubble != null)
            {
                _promptBubble.SetActive(false);
            }
        }

        private void OnEnable()
        {
            // Reset khi vào phòng mới
            _isUsed = false;
            if (_promptBubble != null) _promptBubble.SetActive(false);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_isUsed) return;

            if (other.CompareTag("Player") || other.GetComponent<PlayerController>() != null)
            {
                if (_promptBubble != null) _promptBubble.SetActive(true);

                // Tự động mở bảng tương tác khi người chơi chạm vào điểm tương tác
                Interact();
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.CompareTag("Player") || other.GetComponent<PlayerController>() != null)
            {
                if (_promptBubble != null) _promptBubble.SetActive(false);
            }
        }

        public void Interact()
        {
            if (_isUsed) return;

            Debug.Log($"<color=cyan>[INTERACTION]</color> Tương tác tại điểm: {_type}");

            switch (_type)
            {
                case InteractionType.RestSite:
                    if (UI.RestSiteUI.Instance != null)
                    {
                        UI.RestSiteUI.Instance.OpenRestSite(OnInteractionCompleted);
                    }
                    break;

                case InteractionType.Shop:
                    if (UI.ShopUI.Instance != null)
                    {
                        UI.ShopUI.Instance.OpenShop(OnInteractionCompleted);
                    }
                    break;

                case InteractionType.BuffReward:
                    if (Upgrades.UpgradeManager.Instance != null)
                    {
                        // Thưởng 1 lượt chọn thẻ nâng cấp
                        PlayerStats.Instance?.AddExp(0); // hoặc kích hoạt lượt chọn
                        OnInteractionCompleted();
                    }
                    break;
            }
        }

        private void OnInteractionCompleted()
        {
            if (_oneTimeUse)
            {
                _isUsed = true;
                if (_promptBubble != null) _promptBubble.SetActive(false);
            }
        }
    }
}
