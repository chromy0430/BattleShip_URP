using StylizedWater3;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMove : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private PlayerDataLoad playerData;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 30f;
    [SerializeField] private float dashSpeed = 50f;
    [SerializeField] private float turnSpeed = 5f;
    [SerializeField] private bool dashOn;
    [SerializeField] private float currentSpeed;

    private Rigidbody rb;
    private AlignToWater atw;
    private float moveInput;
    private float turnInput;

    public InputActionReference moveAction;
    public InputActionReference dashAction;

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
        atw = GetComponent<AlignToWater>();       
    }

    private void Start()
    {
        if (playerData == null) Debug.LogError("데이터 없음");

        dashSpeed += playerData.shipData.BaseMaxSpeed * .1f;
        moveSpeed += playerData.shipData.BaseSpeed * .1f;
        turnSpeed += playerData.shipData.BaseTurnSpeed * .1f;
    }

    private void Update()
    {
        turnInput = moveAction.action.ReadValue<Vector2>().x; // A, D 키, 회전
        moveInput = moveAction.action.ReadValue<Vector2>().y; // W, S 키, 직진                                                              
    }

    private void FixedUpdate()
    {
        Move();
        Dash();
        Turn();
    }

    private void Move()
    {
        // 배의 정면을 향해서 직진하도록만 설정
        rb.linearVelocity = (moveInput * transform.forward) * currentSpeed;
    }

    private void Dash()
    {
        currentSpeed = dashOn ? dashSpeed : moveSpeed;        
    }

    private void Turn()
    {
        // Align Transform To Water 컴포넌트의 Rotation 기능을 통해 회전
        atw.rotation += turnInput * turnSpeed;
    }
}
