using Mono.Cecil.Cil;
using StylizedWater3;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMove : MonoBehaviour
{
    private PlayerStat _stat => PlayerDataLoad.playerData.CurrentPlayerStat;
    private Rigidbody rb;
    public InputActionReference moveAction;
    public InputActionReference dashAction;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 30f;
    [SerializeField] private float dashSpeed = 50f;
    [SerializeField] private float turnSpeed = 5f;
    [SerializeField] private bool dashOn;
    public Vector3 shipOffset;
    public float currentSpeed { get; set; }

    //[SerializeField] private AlignToWater atw;        
    private float moveInput;
    private float turnInput;

    
    private void OnEnable()
    {
        dashOn = false;
        moveAction.action.Enable();

        dashAction.action.started += context => dashOn = true;
        dashAction.action.performed += context => dashOn = true;
        dashAction.action.canceled += context => dashOn = false;

        if (PlayerDataLoad.playerData != null) PlayerDataLoad.playerData.OnStatUpdated += UpdateMoveStat;    
    }

    private void OnDisable()
    {
        moveAction.action.Disable();
        dashAction.action.Disable();

        if (PlayerDataLoad.playerData != null) PlayerDataLoad.playerData.OnStatUpdated -= UpdateMoveStat;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        //atw = GetComponent<AlignToWater>();       
    }
    private void Start()
    {
        if (PlayerDataLoad.playerData == null)
        {
            Debug.LogError("PlayerDataLoad가 없습니다!");
            return;
        }
        
        moveSpeed = _stat.GetFinalMoveSpeed(_stat.shipMoveSpeed); 
        dashSpeed = moveSpeed * 1.8f;
        turnSpeed = _stat.GetFinalTurnSpeed(_stat.shipTurnSpeed);
    }

    private void Update()
    {
        turnInput = moveAction.action.ReadValue<Vector2>().x; // A, D 키, 회전
        moveInput = moveAction.action.ReadValue<Vector2>().y; // W, S 키, 직진

#if UNITY_EDITOR
        if (Keyboard.current.kKey.wasPressedThisFrame)
        {
            if (EnemyManager.Instance != null)
            {
                EnemyManager.Instance.KillAllEnemies();
            }
        }
#endif
    }

    #region 물리기반 움직임
    private void FixedUpdate()
    {
        Move();
        Dash();
        Turn();
    }

    private void Move()
    {
        // 적에게 밀리면 플레이어 떨림 현상 해결
        rb.angularVelocity = Vector3.zero;

        // 배의 정면을 향해서 직진하도록만 설정
        if (Mathf.Abs(moveInput) > 0.01f)
        {            
            rb.linearVelocity = (moveInput * transform.forward) * currentSpeed;
        }
        else
        {
            rb.linearVelocity = Vector3.zero;
        }
    }

    private void Dash()
    {
        currentSpeed = dashOn ? dashSpeed : moveSpeed;        
    }

    private void Turn()
    {
        // Align Transform To Water 컴포넌트의 Rotation 기능을 통해 회전
        /*if (atw != null)
        {
            atw.rotation += turnInput * turnSpeed;
        }*/
        Quaternion deltaRotation = Quaternion.Euler(new Vector3(0, turnInput * turnSpeed * 10f * Time.fixedDeltaTime, 0));
        rb.MoveRotation(rb.rotation * deltaRotation);
        rb.angularVelocity = Vector3.zero; // 없으면 회전하고 나서 혼자 밀림        
    }

    #endregion

    #region 바다에 닿아있는가 체크 (아직 안 씀)
    public LayerMask waterlayer;
    private bool CheckWater()
    {
        return Physics.CheckBox(transform.position + shipOffset, new Vector3(.5f, .5f, .5f), Quaternion.identity, waterlayer);
    }
    #endregion

    // 갱신 요소
    private void UpdateMoveStat()
    {
        moveSpeed = _stat.GetFinalMoveSpeed(_stat.shipMoveSpeed);
        dashSpeed = moveSpeed * 1.8f;
        turnSpeed = _stat.GetFinalTurnSpeed(_stat.shipTurnSpeed);
    }
}
