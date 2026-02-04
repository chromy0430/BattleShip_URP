using UnityEngine;
using StylizedWater3; // WaterBatchManager가 사용하는 네임스페이스
using Battleship_Enum;

namespace BattleShip
{
    [RequireComponent(typeof(BoxCollider))]
    public class FieldItem : MonoBehaviour, IWaterFloatTarget
    {
        [Header("Item Data")]
        [SerializeField] private BaseItemData _itemData;

        [Header("Visual")]
        [Tooltip("아이템의 아이콘을 표시할 스프라이트 렌더러 (상자 위에 띄울 용도)")]
        [SerializeField] private SpriteRenderer _iconRenderer;

        [Header("Floating Settings (부력 설정)")]
        [Tooltip("수면 기준 높이 오프셋")]
        [SerializeField] private float _heightOffset = 0.0f;

        [Tooltip("부력 계산에 사용될 표면 크기")]
        [SerializeField] private Vector2 _surfaceSize = new Vector2(0.5f, 0.5f);

        // --- IWaterFloatTarget 인터페이스 구현 ---
        public Transform Transform => transform;
        public float HeightOffset => _heightOffset;
        public Vector2 SurfaceSize => _surfaceSize;
        public bool IsActive => gameObject.activeInHierarchy;

        private HeightQuerySystem.Interface _heightInterface;

        private bool _isCollected = false;

        private void Awake()
        {
            GetComponent<BoxCollider>().isTrigger = true;
        }

        private void OnEnable()
        {
            if (WaterBatchManager.Instance != null)
            {
                WaterBatchManager.Instance.Register(this);
            }
        }

        private void OnDisable()
        {
            if (WaterBatchManager.Instance != null)
            {
                WaterBatchManager.Instance.Unregister(this);
            }
        }

        public void SetWaterObject(HeightQuerySystem.Interface heightInterface)
        {
            // 이미 같은 WaterObject면 갱신하지 않음 
            if (_heightInterface == heightInterface)
                return;

            _heightInterface = heightInterface;
        }

        // 아이템 초기화 (DropManager가 호출)
        public void Initialize(BaseItemData data)
        {
            _itemData = data;

            // 아이콘 설정
            if (_iconRenderer != null && data != null && data.Icon != null)
            {
                _iconRenderer.sprite = data.Icon;

                // 3D 뷰에서 아이콘이 잘 보이도록 카메라를 향하게 하거나 눕히는 설정
                // (일단 x축 90도로 눕혀서 수면에 떠있는 것처럼 보이게 설정 예시)
                _iconRenderer.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_isCollected) return;

            if (other.CompareTag("Player"))
            {
                CollectItem();
            }
        }

        private void CollectItem()
        {
            _isCollected = true;

            if (_itemData != null)
            {
                if (PlayerItemManager.Instance != null)
                {
                    PlayerItemManager.Instance.ObtainItem(_itemData);
                }
            }

            Destroy(gameObject);
        }
    }
}