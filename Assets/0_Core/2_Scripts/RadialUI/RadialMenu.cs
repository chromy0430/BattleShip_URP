using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[ExecuteAlways]
public class RadialMenu : MonoBehaviour
{
    [Header("Layout")]
    public float radius = 150f;
    public float startAngle = 90f;
    public List<RadialMenuItem> items = new();

    [Header("Button Design")]
    public Sprite buttonBackground;
    public Color defaultColor = Color.white;
    public Color hoverColor = Color.cyan;
    public float hoverScale = 1.15f;

    [Header("Icon Settings")]
    public Vector2 iconSize = new Vector2(32, 32);

    [Header("Open & Close")]
    public bool startOpen = true;
    public float openCloseDuration = 0.2f;

    [Header("Animation Curve")]
    public AnimationCurve openCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public float spiralStrength = 360f;   


    CanvasGroup canvasGroup;
    Coroutine openCloseRoutine;
    public GameObject RadialMenuObj;
    public bool IsOpen { get; private set; }

    [Header("Center Icon")]
    [SerializeField] Image centerIcon;
    [SerializeField] Vector2 centerIconSize = new Vector2(48, 48);

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (!canvasGroup) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        if (startOpen) Open(true);
        else Close(false);
    }

    private void Update()
    {
        if (openCloseRoutine == null)
        {
            UpdateLayout(radius, 0f);
        }
        //UpdateLayout();

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Toggle();
        }
    }

    void UpdateLayout(float currentRadius, float offsetAngle)
    {
        int count = items.Count;
        if (count == 0) return;

        float step = 360f / count;

        for (int i = 0; i < count; i++)
        {
            if (!items[i]) continue;

            RectTransform rect = items[i].GetComponent<RectTransform>();

            float angle = (startAngle - step * i) + offsetAngle;
            //float angle = startAngle - step * i;
            float rad = angle * Mathf.Deg2Rad;

            rect.anchoredPosition = new Vector2
            (
                Mathf.Cos(rad) * currentRadius, //radius,
                Mathf.Sin(rad) * currentRadius //radius
            );
        }
    }

    public void ApplyDefaultsToItem(RadialMenuItem item)
    {
        if (!item) return;

        if (item.Backgroundimage)
        {
            item.Backgroundimage.sprite = buttonBackground;
            item.Backgroundimage.color = defaultColor;
        }

        if (item.IconRect)
        {
            item.IconRect.sizeDelta = iconSize;
        }
    }
    public void ApplyDefaultsToAll()
    {
        foreach (var item in items)
        {
            if (!item) continue;
            ApplyDefaultsToItem(item);
        }
    }

    private void OnEnable()
    {
        //UpdateLayout ();
        ApplyDefaultsToAll();

        if (startOpen)
        {
            Open(true);
        }
        else
        {
            Close(true);
        }

        if (centerIcon) centerIcon.rectTransform.sizeDelta = centerIconSize;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (!Application.isPlaying) return;
        UpdateLayout(radius, 0f);
        //UpdateLayout();
        ApplyDefaultsToAll();
    }
#endif

    public void SetCenterIcon(Sprite sprite)
    {
        if (!centerIcon) return;

        centerIcon.sprite = sprite;
        centerIcon.enabled = sprite != null;
    }

    void SetInteractable(bool value)
    {
        canvasGroup.interactable = value;
        canvasGroup.blocksRaycasts = value;
    }

    void SetVisualState(float v)
    {
        canvasGroup.alpha = v;
        if (RadialMenuObj != null) RadialMenuObj.transform.localScale = Vector3.one * v;
    }

    IEnumerator OpenCloseRoutine(float from, float to)
    {
        float t = 0f;

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / openCloseDuration;
            float curveValue = openCurve.Evaluate(t);
            float v = Mathf.Lerp(from, to, curveValue);

            SetVisualState(v);

            float curRadius = Mathf.Lerp(0f, radius, v);
            float curOffset = Mathf.Lerp(spiralStrength, 0f, v);

            UpdateLayout(curRadius, curOffset);

            yield return null;
        }

        /*while (t < 1f)
        {
            t += Time.unscaledDeltaTime / openCloseDuration;
            float v = Mathf.Lerp(from, to, openCurve.Evaluate(t));
            SetVisualState(v);
            yield return null;
        }*/

        SetVisualState(to);

        if (to >= 0.99f) // 열린 상태면 정위치
            UpdateLayout(radius, 0f);
        else // 닫힌 상태면 중심으로 모임
            UpdateLayout(0f, spiralStrength);

        openCloseRoutine = null;
    }

    void StartOpenClose(bool instant, float from, float to)
    {
        if (openCloseRoutine != null) StopCoroutine(openCloseRoutine);


        if (instant)
        {
            SetVisualState(to);
            // 즉시 이동 시 위치도 바로 반영
            if (to >= 0.99f) UpdateLayout(radius, 0f);
            else UpdateLayout(0f, spiralStrength);
            return;
        }

        /*if (instant)
        {
            SetVisualState(to);
            return;
        }*/

        openCloseRoutine = StartCoroutine(OpenCloseRoutine(from, to));
    }

    public void Open(bool instant = false)
    {
        if (IsOpen) return;

        IsOpen = true;
        SetInteractable(true);

        StartOpenClose(instant, 0f, 1f);
    }

    public void Close(bool instant = false)
    {
        if (!IsOpen) return;

        IsOpen = false;
        SetInteractable(false);

        StartOpenClose(instant, 1f, 0f);
    }

    public void Toggle()
    {
        if (IsOpen) Close();
        else Open();
    }
}
