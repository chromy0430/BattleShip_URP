using UnityEngine;

public delegate void OnExpChanged(float exp);

public class PlayerExpManager : MonoBehaviour
{
    public static PlayerExpManager Instance { get; private set; }
    public event OnExpChanged onExpChanged;

    private int[] PlayerRequireExp =
    {
        10, 12, 15, 30, 35, 45, 60, 80, 90, 100
    };
    public int currentLevel = 0;
    [SerializeField] private int currentExp = 0;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this);
        }
    }

    public void GetExp(int amount)
    {
        currentExp += amount;

        if (currentExp >= PlayerRequireExp[currentLevel])
        {
            currentExp = 0;
            currentLevel++;
        }
        onExpChanged?.Invoke(currentExp);
    }
}
