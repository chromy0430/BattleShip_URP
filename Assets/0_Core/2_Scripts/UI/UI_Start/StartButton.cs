using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StartButton : MonoBehaviour
{
    [SerializeField] private Button startBtn;
    [SerializeField] private RectTransform originalRect;
    [SerializeField] private float downValue;
    [SerializeField] private Ease downEase;

    private void Start()
    {
        startBtn.onClick.AddListener(startGame);
        
        if(originalRect == null)  originalRect = startBtn.GetComponent<RectTransform>();        
    }

    private void startGame()
    {
        originalRect.DOAnchorPosY(downValue, .2f).SetEase(downEase).OnComplete(LoadScene);
    }

    private void LoadScene()
    {
        SceneManager.LoadScene(1);
    }
}
