using UnityEngine;
using UnityEngine.SceneManagement;
public class StageRestart : MonoBehaviour
{    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            SceneManager.LoadScene(0);
        }
    }
}
