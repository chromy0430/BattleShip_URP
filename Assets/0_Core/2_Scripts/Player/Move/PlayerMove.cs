using StylizedWater3;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMove : MonoBehaviour
{
    //[Header("Data")]
    //[SerializeField] private PlayerDataLoad playerData;
    private BaseShipData shipData;

    private ShipStat _runtimeShipStat;
    public ShipStat RuntimeShipStat => _runtimeShipStat;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 30f;
    [SerializeField] private float dashSpeed = 50f;
    [SerializeField] private float turnSpeed = 5f;
    [SerializeField] private bool dashOn;
    public float currentSpeed { get; set; }

    private Rigidbody rb;
    [SerializeField] private AlignToWater atw;
    private float moveInput;
    private float turnInput;

    public InputActionReference moveAction;
    public InputActionReference dashAction;

    public Vector3 shipOffset;

    private void OnEnable()
    {
        dashOn = false;
        moveAction.action.Enable();

        dashAction.action.started += context => dashOn = true;
        dashAction.action.performed += context => dashOn = true;
        dashAction.action.canceled += context => dashOn = false;
    }

    private void OnDisable()
    {
        moveAction.action.Disable();
        dashAction.action.Disable();
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        //atw = GetComponent<AlignToWater>();       
    }
    private void Start()
    {
        if (shipData == null) shipData = PlayerDataLoad.playerData.shipData;

        _runtimeShipStat = new ShipStat(shipData);

        dashSpeed += _runtimeShipStat.shipMoveSpeed * .2f;
        moveSpeed += _runtimeShipStat.shipMoveSpeed * .1f;
        turnSpeed += _runtimeShipStat.shipTurnSpeed * .1f;
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

    private void FixedUpdate()
    {
        Move();
        Dash();
        Turn();

        Debug.Log($"현재 속도 = {currentSpeed}");
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
        if (atw != null)
        {
            atw.rotation += turnInput * turnSpeed;
        }
        else
        {
            Quaternion deltaRotation = Quaternion.Euler(new Vector3(0, turnInput * turnSpeed * 10f * Time.fixedDeltaTime, 0));
            rb.MoveRotation(rb.rotation * deltaRotation);

            //rb.angularVelocity = Vector3.zero;
        }
    }

    public LayerMask waterlayer;
    private bool CheckWater()
    {
        return Physics.CheckBox(transform.position + shipOffset, new Vector3(.5f, .5f, .5f), Quaternion.identity, waterlayer);
    }

   
}
