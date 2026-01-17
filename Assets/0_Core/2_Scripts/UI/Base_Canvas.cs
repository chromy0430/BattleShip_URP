using UnityEngine;

public class Base_Canvas : MonoBehaviour
{
    public static Base_Canvas Instance {  get; private set; }
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }
}
