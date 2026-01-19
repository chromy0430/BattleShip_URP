using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
public class StageRestart : MonoBehaviour
{    
    void Update()
    {
        if (Keyboard.current.f1Key.wasPressedThisFrame)
        {
            SceneManager.LoadScene(0);
        }
        
        if (Keyboard.current.f2Key.wasPressedThisFrame)
        {
            Time.timeScale = 10f;
        }

        if (Keyboard.current.f3Key.wasPressedThisFrame)
        {
            Time.timeScale = 1f;
        }
    }
}
