using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
//using Unity.Android.Gradle;
using UnityEngine;
using UnityEngine.InputSystem;


public class PlayerCharacter : MonoBehaviour
{
    [SerializeField] float SlamForce;
    [SerializeField] float circleRadius;
    [SerializeField] float MaxMoveSpeed;
    [SerializeField] public float jumpForce;
    [SerializeField] public float moveSpeed;
    [SerializeField] public float DefaultGravity;
    [SerializeField] public float ApexGravity;
    [SerializeField] public float FallGravity;
    [SerializeField] public float JumpGravity;
    [SerializeField] private float CoyoteTimer;
    [SerializeField] private float DashForce;
    [SerializeField] private float DashTime;
    [SerializeField] private float DashCooldown;
    [SerializeField] public LayerMask m_LayerMask;
    [SerializeField] Transform castPosition;

    public float m_faxis { get; set; }
    public bool m_b_FacingRight = true;
    public bool isJumping;
    public bool isSlaming;
    public bool isDashing;
    public bool isPlayerJumping;
    public Vector2 FireDirection;

    bool gravityApexStatus;
    bool jumpBufferStatus;
    bool coyoteTime;

    Coroutine c_RJumpBuffer;
    Coroutine c_RCoyoteTime;
    Coroutine c_RDash;

    Rigidbody2D rb;
    InputHandler InputHandler;
    GroundedComp GroundedComp;
    StaminaComponent StaminaComponentScr;
    Fire FireScr;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        GroundedComp = GetComponent<GroundedComp>();
        InputHandler = GetComponent<InputHandler>();
        StaminaComponentScr = GetComponent<StaminaComponent>();
        FireScr = GetComponent<Fire>();
    }

    private void OnEnable()
    {
        GroundedComp.OnGroundedChanged += Handle_GroundedChanged;
    }

    private void OnDisable()
    {
        GroundedComp.OnGroundedChanged -= Handle_GroundedChanged;
    }

    public void Handle_GroundedChanged(bool grounded)
    {
        if (grounded)
        {
            Debug.Log("Player is grounded!");
            isPlayerJumping = false;
            isJumping = false;
            isSlaming = false;

            rb.gravityScale = DefaultGravity;

            if (jumpBufferStatus)
            {
                StopCoroutine(c_RJumpBuffer);
                jumpBufferStatus = false;
                Jump();
            }

            if(InputHandler.c_RGravityApex != null)
            {
                StopCoroutine(InputHandler.c_RGravityApex);
                InputHandler.c_RGravityApex = null;
                gravityApexStatus = false;
            }
        }
        else if (!jumpBufferStatus)
        {
            c_RCoyoteTime = StartCoroutine(C_CoyoteTime());
        }
    }

    public void PlayerJump()
    {
        if (GroundedComp.IsGrounded)
        {
            Jump();
        }
        else if (coyoteTime)
        {
            Jump();
            StopCoroutine(c_RCoyoteTime);
            coyoteTime = false;
        }
        else if (!GroundedComp.IsGrounded)
        {
            c_RJumpBuffer = StartCoroutine(C_JumpBuffer());
        }
    }

    public void PlayerSlam()
    {
        rb.AddForce(Vector2.down * SlamForce, ForceMode2D.Impulse);
        StaminaComponentScr.StaminaDrain(15f);
        isJumping = false;
        isSlaming = true;
    }

    public void Move()
    {
        rb.AddForce(transform.right * m_faxis * moveSpeed * 1);
        rb.velocity = new Vector2(m_faxis * moveSpeed, rb.velocity.y);
        StaminaComponentScr.StaminaDrain(0.2f);
        //Debug.Log($"Axis: {m_faxis} ");

        if(m_faxis > 0 && !m_b_FacingRight)
        {
            Flip();
            FireDirection = transform.right;
        }
        else if(m_faxis < 0 && m_b_FacingRight)
        {
            Flip();
            FireDirection = -transform.right;
        }
    }

    public void Jump()
    {
        StartCoroutine(C_JumpBlindness());
        rb.velocity = new Vector2(rb.velocity.x, 0);
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        StaminaComponentScr.StaminaDrain(5f);

        if (!GroundedComp.IsGrounded)
        {
            isJumping = true;
        }
    }

    public void Dash()
    {
        if(c_RDash == null && isDashing == false)
        {
            c_RDash = StartCoroutine(C_Dash());
        }
    }

    public void Flip()
    {
        Vector2 currentScale = gameObject.transform.localScale;
        currentScale.x *= -1;
        gameObject.transform.localScale= currentScale;
        m_b_FacingRight = !m_b_FacingRight;
    }

    public void SetTeleporterLocation(Transform teleporterLocation)
    {
        transform.position = teleporterLocation.position;
    }

    public IEnumerator C_Dash()
    {
        while (InputHandler.m_b_InDashActive)
        {
            isDashing = true;
            rb.gravityScale = 0f;
            //rb.velocity = new Vector2(transform.localScale.x * DashForce, 0f);
            //rb.AddForce(Vector2.right * DashForce, ForceMode2D.Impulse);
            rb.AddRelativeForce(rb.velocity * DashForce, ForceMode2D.Impulse);
            Debug.Log("DASHED!!");
            StaminaComponentScr.StaminaDrain(10f);
            yield return new WaitForSeconds(DashTime);
            rb.gravityScale = DefaultGravity;
            isDashing = false;
            yield return new WaitForSeconds(DashCooldown);
            Debug.Log("DASH COOLDOWN DONE!!");
            c_RDash = null;
        }
    }

    public IEnumerator C_JumpBuffer()
    {
        jumpBufferStatus = true;
        yield return new WaitForSeconds(0.35f);
        jumpBufferStatus = false;
    }

    public IEnumerator C_CoyoteTime()
    {
        coyoteTime = true;
        yield return new WaitForSeconds(CoyoteTimer);
        coyoteTime = false;
    }

    public IEnumerator C_GravityApex()
    {
        rb.gravityScale = JumpGravity;
        while (InputHandler.m_b_InJumpActive && !gravityApexStatus)
        {
            if(rb.velocity.y <= 1)
            {
                rb.gravityScale = ApexGravity;
                gravityApexStatus = true;
            }
            yield return null;
        }

        yield return new WaitForSeconds(.2f);
        rb.gravityScale = FallGravity;
    }

    IEnumerator C_JumpBlindness()
    {
        jumpBufferStatus = true;
        yield return new WaitForSeconds(0.2f);
        jumpBufferStatus = false;
    }
}
