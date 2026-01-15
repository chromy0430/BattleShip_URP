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
    }
}
