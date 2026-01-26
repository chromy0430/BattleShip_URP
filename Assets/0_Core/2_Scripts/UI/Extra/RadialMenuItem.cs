using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class RadialMenuItem : MonoBehaviour
{
    [Header("References")]
    [SerializeField] Image backgroundImage;
    [SerializeField] Image iconImage;
    [SerializeField] Sprite iconSprite;
    public Sprite IconSprite => iconSprite;
    public UnityEvent onClick;

    public RectTransform IconRect => iconImage ? iconImage.rectTransform : null;
    public Image Backgroundimage => backgroundImage;


    public void EnsureReferences()
    {
        if (!backgroundImage) backgroundImage = GetComponent<Image>();
        if (!iconImage) iconImage = transform.Find("Icon")?.GetComponent<Image>();
    }

    void ApplyIcon()
    {
        if(iconImage) iconImage.sprite = iconSprite;
    }

    public void Invoke()
    {
        onClick?.Invoke();
    }

    private void OnEnable()
    {
        EnsureReferences();
        ApplyIcon();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        EnsureReferences() ;
        ApplyIcon();
    }
#endif
}
