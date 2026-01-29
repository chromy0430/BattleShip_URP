using MapAndRadarSystem;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RadarSystem : MonoBehaviour
{
    public static RadarSystem Instance { get; set; }

    [Header("Settings")]
    public float RadarDistance = 50f; // 탐지 범위
    public int initPoolSize = 500; // 초기화 풀링 생성

    [Header("UI Components")]
    public RectTransform RadarPointPrefab; // 적 아이콘 프리팹
    public Transform Root; // 아이콘 생성될 부모 오브젝트
    public Image Background; // 레이더맵 배경 (나침반 역할)
    public Transform PlayerView; // 플레이어 방향표 회전

    // 활성화된 적들과 매칭되는 UI 아이콘 관리
    private Dictionary<GameObject, RectTransform> _activeTargets = new Dictionary<GameObject, RectTransform>();

    // 재사용을 위한 풀
    private Queue<RectTransform> _iconPool = new Queue<RectTransform>();

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        InitializePool();
    }

    private void LateUpdate()
    {
        if (RadarManager.Instance.Actor == null) return;

        RadarDraw();
    }

    /// <summary>
    /// 적이 활성화될 때 호출하여 레이더 등록
    /// </summary>
    public void AddTarget(GameObject enemyObj, Sprite iconSprite)
    {
        if (enemyObj == null) return;
        if (_activeTargets.ContainsKey(enemyObj)) return; // 이미 등록됨

        // 풀에서 아이콘 가져오기 (없으면 생성)
        RectTransform iconRect = GetIconFromPool();

        // 아이콘 이미지 설정
        Image img = iconRect.GetComponent<Image>();
        if (img != null) img.sprite = iconSprite;

        // 관리 리스트에 등록
        _activeTargets.Add(enemyObj, iconRect);
    }

    /// <summary>
    /// 적이 죽거나 비활성화될 때 레이더에서 제거
    /// </summary>
    public void RemoveTarget(GameObject enemyObj)
    {
        if (enemyObj == null) return;
        if (_activeTargets.TryGetValue(enemyObj, out RectTransform iconRect))
        {
            // 아이콘을 풀로 반환 (비활성화)
            ReturnIconToPool(iconRect);

            // 관리 리스트에서 제거
            _activeTargets.Remove(enemyObj);
        }
    }

    private void RadarDraw()
    {
        Transform actor = RadarManager.Instance.Actor;

        if (actor == null) return;

        if (PlayerView != null)
            PlayerView.localRotation = Quaternion.identity;

        if(Background != null)
        {
            Background.rectTransform.localRotation = Quaternion.Euler(0, 0, actor.eulerAngles.y);
        }

        float mapRadius = Background.rectTransform.sizeDelta.x * 0.5f;
        Vector3 actorPos = actor.position;
        float actorYRot = actor.eulerAngles.y;

        foreach (var pair in _activeTargets)
        {
            GameObject target = pair.Key;
            RectTransform icon = pair.Value;

            if (target == null || !target.activeInHierarchy)
            {
                icon.gameObject.SetActive(false);
                continue;
            }

            Vector3 targetPos = target.transform.position;
            float dist = Vector3.Distance(actorPos, targetPos);

            // 레이더 범위 안에 있을 때만 표시
            if (dist <= RadarDistance)
            {
                Vector3 offset = targetPos - actorPos;
                Vector3 localPos = Quaternion.Euler(0, -actorYRot, 0) * offset;

                float scale = mapRadius / RadarDistance;
                icon.anchoredPosition = new Vector2(localPos.x * scale, localPos.z * scale);

                icon.localRotation = Quaternion.identity;                

                if (!icon.gameObject.activeSelf) icon.gameObject.SetActive(true);
            }
            else
            {
                if (icon.gameObject.activeSelf) icon.gameObject.SetActive(false);
            }
        }
    }

    private void InitializePool()
    {
        if (RadarPointPrefab == null || Root == null)
        {
            Debug.LogError("[RadarSystem] Prefab 또는 Root가 할당되지 않았습니다!");
            return;
        }

        for (int i = 0; i < initPoolSize; i++)
        {
            RectTransform icon = Instantiate(RadarPointPrefab, Root);
            icon.gameObject.SetActive(false); // 비활성화 상태로 생성
            icon.localScale = Vector3.one;
            _iconPool.Enqueue(icon); // 큐에 저장
        }
    }

    // 풀링 시스템
    private RectTransform GetIconFromPool()
    {
        RectTransform icon;
        if (_iconPool.Count > 0)
        {
            icon = _iconPool.Dequeue();
            icon.gameObject.SetActive(true);
        }
        else
        {
            icon = Instantiate(RadarPointPrefab, Root);
            icon.localScale = Vector3.one;
        }
        return icon;
    }

    private void ReturnIconToPool(RectTransform icon)
    {
        icon.gameObject.SetActive(false);
        _iconPool.Enqueue(icon);
    }
}

