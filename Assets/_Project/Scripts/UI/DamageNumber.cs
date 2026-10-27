using UnityEngine;
using TMPro;
using Project_D.Core;

namespace Project_D.UI
{
    public class DamageNumber : MonoBehaviour
    {
        [Header("Movement & Lifetime")]
        [SerializeField] private float _lifeTime = 0.6f;
        [SerializeField] private float _upwardSpeed = 2f;

        [Header("Visual Colors")]
        [SerializeField] private Color _normalColor = new Color(1f, 0.9f, 0.3f, 1f); // Vàng sáng
        [SerializeField] private Color _critColor = new Color(1f, 0.25f, 0.25f, 1f);   // Đỏ chí mạng

        private TMP_Text _textMesh;
        private GameObject _prefabOrigin;
        private float _elapsedTime;
        private Vector3 _driftDirection;
        private Color _baseColor;

        private void Awake()
        {
            _textMesh = GetComponent<TMP_Text>();
            // Nếu dùng TextMeshPro 3D (World Space), đưa sorting order lên cao để không bị che khuất
            if (TryGetComponent<Renderer>(out var rend))
            {
                rend.sortingOrder = 100;
            }
            if (_textMesh != null)
            {
                _textMesh.alignment = TextAlignmentOptions.Center;
            }
        }

        // Khởi tạo hiển thị sát thương và cấu hình tái sử dụng từ Pool
        public void Initialize(int damage, GameObject prefabOrigin, bool isCrit = false)
        {
            if (_textMesh == null) _textMesh = GetComponent<TMP_Text>();

            _prefabOrigin = prefabOrigin;
            _textMesh.text = damage.ToString();

            _baseColor = isCrit ? _critColor : _normalColor;
            _textMesh.color = _baseColor;

            // Kích thước chữ (Chí mạng to hơn)
            transform.localScale = isCrit ? Vector3.one * 1.3f : Vector3.one;

            // Độ trôi ngẫu nhiên nhẹ sang trái/phải để các số không bị đè khít lên nhau
            float randomX = Random.Range(-0.6f, 0.6f);
            _driftDirection = new Vector3(randomX, _upwardSpeed, 0f);

            _elapsedTime = 0f;
        }

        private void Update()
        {
            _elapsedTime += Time.deltaTime;

            // 1. Bay bổng lên trên và trôi nhẹ
            transform.position += _driftDirection * Time.deltaTime;

            // 2. Hiệu ứng phóng nảy lúc đầu (Punch pop)
            float progress = _elapsedTime / _lifeTime;
            if (progress < 0.2f)
            {
                // Phóng to nhẹ trong 20% đầu thời gian
                float popScale = Mathf.Lerp(0.8f, 1.2f, progress / 0.2f);
                transform.localScale = Vector3.one * popScale;
            }
            else
            {
                // Trở về kích thước chuẩn
                transform.localScale = Vector3.MoveTowards(transform.localScale, Vector3.one, Time.deltaTime * 3f);
            }

            // 3. Mờ dần (Fade out alpha) ở nửa sau vòng đời
            if (progress > 0.5f)
            {
                float fadeProgress = (progress - 0.5f) / 0.5f;
                Color c = _baseColor;
                c.a = Mathf.Lerp(1f, 0f, fadeProgress);
                _textMesh.color = c;
            }

            // 4. Trả về PoolManager khi hết thời gian
            if (_elapsedTime >= _lifeTime)
            {
                Despawn();
            }
        }

        private void Despawn()
        {
            if (PoolManager.Instance != null && _prefabOrigin != null)
            {
                PoolManager.Instance.ReturnToPool(_prefabOrigin, gameObject);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }
}
