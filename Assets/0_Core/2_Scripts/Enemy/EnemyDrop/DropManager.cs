using UnityEngine;

public class DropManager : MonoBehaviour
{
    public static DropManager Instance { get; set; }
    [SerializeField] private GameObject gemPrefab;
    [SerializeField] private LayerMask gemLayer;
    [SerializeField] private int maxGemCount = 300;

    private Collider[] _mergeBuffer = new Collider[5];

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this.gameObject);
        }
    }

    public void SpawnExperience(Vector3 position, int amount)
    {
        int currentActive = ObjectPoolManager.Instance.GetActiveCount(gemPrefab);

        if (currentActive > maxGemCount)
        {
            int count = Physics.OverlapSphereNonAlloc(position, 3f, _mergeBuffer, gemLayer);
            if (count > 0)
            {
                // 가장 가까운 보석 하나에 몰아주기
                if (_mergeBuffer[0].TryGetComponent<EnemyDrop>(out var existingGem))
                {
                    existingGem.AddExp(amount);
                    return; // 생성하지 않고 종료! (프레임 드랍 방지 핵심)
                }
            }
        }

        // 병합 대상이 없거나 여유가 있으면 새로 생성
        var newGemObj = ObjectPoolManager.Instance.Get(gemPrefab);
        newGemObj.transform.position = position;

        if (newGemObj.TryGetComponent<EnemyDrop>(out var dropItem))
        {
            dropItem.Initialize(amount); // 크기 리셋 및 초기값 주입
        }
    }
}
