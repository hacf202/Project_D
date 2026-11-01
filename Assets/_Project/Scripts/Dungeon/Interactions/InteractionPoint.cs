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
        private bool _isPlayerInRange = false;

        public InteractionType Type => _type;

        private void Awake()
        {
            var col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;

            var sr = GetComponent<SpriteRenderer>();
            if (sr != null) sr.enabled = true;

            EnsurePromptBubble();

            if (_promptBubble != null)
            {
                _promptBubble.SetActive(false);
            }
        }

        private void EnsurePromptBubble()
        {
            if (_promptBubble != null)
            {
                if (_promptLabel != null) _promptLabel.text = _promptText;
                return;
            }

            // Tự động tạo bong bóng nút bấm World Space nổi trên đầu vật thể nếu chưa được gán
            var bubbleGo = new GameObject("PromptBubble");
            bubbleGo.transform.SetParent(transform, false);
            bubbleGo.transform.localPosition = new Vector3(0, 1.4f, 0);

            var tmp = bubbleGo.AddComponent<TextMeshPro>();
            tmp.text = $"<color=#FFD700><b>[E]</b></color> {_promptText}";
            tmp.fontSize = 4f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.sortingOrder = 100;
            tmp.rectTransform.sizeDelta = new Vector2(4f, 1f);

            // Gắn BoxCollider2D để người chơi có thể click chuột / chạm tay trực tiếp trên mobile
            var boxCol = bubbleGo.AddComponent<BoxCollider2D>();
            boxCol.isTrigger = true;
            boxCol.size = new Vector2(3f, 0.8f);

            _promptBubble = bubbleGo;
            _promptLabel = tmp;
        }

        private void Update()
        {
            if (!_isPlayerInRange || _isUsed) return;

            // Nhấn phím E hoặc Space trên bàn phím
            bool keyPressed = false;
            if (UnityEngine.InputSystem.Keyboard.current != null)
            {
                if (UnityEngine.InputSystem.Keyboard.current.eKey.wasPressedThisFrame ||
                    UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame)
                {
                    keyPressed = true;
                }
            }

            // Chạm hoặc click chuột vào vùng tương tác
            bool mouseTapped = false;
            if (UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
            {
                Vector2 mouseWorld = Camera.main != null ? (Vector2)Camera.main.ScreenToWorldPoint(UnityEngine.InputSystem.Mouse.current.position.ReadValue()) : Vector2.zero;
                var hit = Physics2D.OverlapPoint(mouseWorld);
                if (hit != null && (hit.transform == transform || hit.transform.IsChildOf(transform)))
                {
                    mouseTapped = true;
                }
            }

            if (keyPressed || mouseTapped)
            {
                Interact();
            }
        }

        private void OnEnable()
        {
            // Reset khi vào phòng mới
            _isUsed = false;
            _isPlayerInRange = false;
            if (_promptBubble != null) _promptBubble.SetActive(false);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_isUsed) return;

            if (other.CompareTag("Player") || other.GetComponent<PlayerController>() != null)
            {
                _isPlayerInRange = true;
                if (_promptBubble != null) _promptBubble.SetActive(true);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.CompareTag("Player") || other.GetComponent<PlayerController>() != null)
            {
                _isPlayerInRange = false;
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
                        // Thưởng 1 lượt chọn thẻ nâng cấp miễn phí từ Bệ Thờ Ban Phước
                        Upgrades.UpgradeManager.Instance.GrantBonusUpgrade(OnInteractionCompleted);
                    }
                    else
                    {
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
