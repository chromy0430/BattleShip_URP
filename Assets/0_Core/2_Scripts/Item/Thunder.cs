using UnityEngine;
using Battleship_Enum;

namespace BattleShip
{
    public class Thunder : MonoBehaviour
    {
        [Header("피드백 관련")]
        [SerializeField] private GameObject _lightningEffectPrefab; // 번개 이펙트 프리팹
        [SerializeField] private AudioClip _thunderSfx; // 천둥 소리

        private bool _isActivated = false;

        private void OnTriggerEnter(Collider other)
        {
            if (_isActivated) return;

            if (other.CompareTag("Player"))
            {
                ActivateThunder();
            }
        }

        private void ActivateThunder()
        {
            _isActivated = true;

            // 시각/청각 효과
            if (_lightningEffectPrefab != null)
            {
                // 화면 전체에 번개가 치는 느낌을 위해 카메라 위치나 0,0,0 등에 생성
                // 테스트 후 변경 예정
                Instantiate(_lightningEffectPrefab, transform.position, Quaternion.identity);
            }

            // 적 몰살 로직
            if (EnemyManager.Instance != null)
            {
                Debug.Log("[Thunder] 적 전체 처치 실행!");
                EnemyManager.Instance.KillAllEnemies();
            }

            // 아이템 제거
            Destroy(gameObject);
        }
    }
}