using TMPro;
using UnityEngine;
using System.Collections;

public class DamageTMP : MonoBehaviour
{
    private TextMeshProUGUI m_Text;
    private RectTransform rectTransform;

    private float lifetime = 1f; // 폰트 지속시간
    private Vector2 velocity; // 초기속도 (포물선 운동)
    private float gravity = -1000f; // 중력효과 ui 이동이므로 값 조절 필요
    private Color textColor;

    private void Awake()
    {
        m_Text = GetComponent<TextMeshProUGUI>();
        rectTransform = GetComponent<RectTransform>();
    }

    public void Initialize(Transform parent, Vector3 pos, string temp)
    {
        transform.SetParent(parent);

        m_Text.text = temp;

        Vector3 screenPosition = Camera.main.WorldToScreenPoint(pos);
        rectTransform.position = screenPosition;

        velocity = new Vector2(Random.Range(-50.0f, 50.0f), Random.Range(150.0f, 250.0f));
        textColor = m_Text.color;

        StartCoroutine(MoveAndFade());
    }

    IEnumerator MoveAndFade()
    {
        float elapsedTime = 0.0f;

        while (elapsedTime < lifetime)
        {
            velocity.y += gravity * Time.deltaTime;
            rectTransform.anchoredPosition += velocity * Time.deltaTime;

            textColor.a = Mathf.Lerp(1.0f, 0.0f, elapsedTime / lifetime);
            m_Text.color = textColor;

            elapsedTime += Time.deltaTime;
            yield return null;
        }
        Destroy(this);
    }
}
