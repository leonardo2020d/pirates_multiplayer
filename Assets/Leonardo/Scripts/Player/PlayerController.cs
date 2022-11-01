using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using FishNet.Object;
using FishNet.Object.Prediction;
using FishNet.Object.Synchronizing;
using FishNet.Transporting;

public class PlayerController : NetworkBehaviour
{
  private Input_System m_Input;
  private Rigidbody2D rb;
  public float moveSpeed;
  private Vector2 direction;
  public float jumpForce;
  private bool jump;
  private bool isground;

  [SerializeField] private TrailRenderer tr;
  private bool canDash;
  private bool isDash;
  private bool dash;
  private float dashingPower = 1.2f;
  private float dashingTime = 0.2f;
  private float dashingCooldown = 1f;

  [SyncVar(Channel = Channel.Unreliable, OnChange = nameof(Flip))]
  private bool _facingRight;
  public Transform detectsGround;
  public LayerMask ground;
  public Animator animator;
  private Vector2 m_Velocity = Vector3.zero;

  public override void OnStartServer()
  {
    base.OnStartServer();
    _facingRight = true;
    rb = GetComponent<Rigidbody2D>();
    canDash = true;
  }

  public override void OnStartClient()
  {
    base.OnStartClient();
    rb = GetComponent<Rigidbody2D>();
    if (base.IsClientOnly)
      rb.isKinematic = true;
    if (base.IsOwner) {
      m_Input = new Input_System();
    }
      
  }

  public override void OnStartNetwork()
  {
    base.OnStartNetwork();
    if (base.IsServer || base.IsClient) {
      base.TimeManager.OnTick += TimeManager_OnTick;
      base.TimeManager.OnPostTick += TimeManager_OnPostTick;
    }
  }

  public override void OnStopNetwork()
  {
    base.OnStopNetwork();
    if (base.TimeManager != null) {
      base.TimeManager.OnTick -= TimeManager_OnTick;
      base.TimeManager.OnPostTick += TimeManager_OnPostTick;
    }
  }

  private void TimeManager_OnTick()
  {
    if (base.IsOwner)
    {
      Reconciliation(default, false);
      PrepareMoveData(out MoveData md);
      Move(md, false);
    }
    if (base.IsServer)
    {
      Move(default, true);
    }
  }

  private void TimeManager_OnPostTick()
  {
    if (base.IsServer)
    {
      ReconcileData rd = new ReconcileData(transform.position, rb.velocity, rb.angularVelocity, _facingRight, rb.gravityScale);
      Reconciliation(rd, true);
    }
  }

  public void PrepareMoveData(out MoveData md)
  {
    md = default;
    
    if (!jump && direction.x == 0f && direction.y == 0f)
      return;
    
    md = new MoveData(jump, direction, dash);
        jump = false;
        dash = false;
  }
    private IEnumerator Dash()
    {
        canDash = false;
        isDash = true;
        float originalGravity = rb.gravityScale;
        rb.gravityScale = 0f;
        rb.velocity = m_Velocity * dashingPower;
        tr.emitting = true;
        yield return new  WaitForSeconds(dashingTime);
        tr.emitting = false;
        rb.gravityScale = originalGravity;
        isDash = false;
        yield return new WaitForSeconds(dashingCooldown);
        canDash = true;

    }

  [Replicate]
  private void Move(MoveData md, bool asServer, bool replaying  = false)
  {
   if (isDash)
   {
     return;
   }
    Vector2 targetVelocity = new Vector2(md.direction.x * moveSpeed * (float)base.TimeManager.TickDelta * 10f, rb.velocity.y);
    rb.velocity = Vector2.SmoothDamp(rb.velocity, targetVelocity, ref m_Velocity, 0.05f);
    isground = Physics2D.OverlapCircle(detectsGround.position, 0.2f, ground);
    if (md.jump && isground)
    {
      rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
      if ((asServer || base.IsServer) && !replaying)
        animator.SetBool("isJump", true);
    }
    if (md.dash && canDash)
    {
      StartCoroutine(Dash());
    }

    if (md.direction.x > 0 && !_facingRight)
    {
      _facingRight = !_facingRight;
    }
    if (md.direction.x < 0 && _facingRight)
    {
      _facingRight = !_facingRight;
    }
    if ((asServer || base.IsServer) && !replaying)
    { 
      if (isground && rb.velocity.y == 0)
      {
        animator.SetBool("isJump", false);
      }
      if (isground && md.direction.x != 0)
      {
        animator.SetBool("isRun", true);
      }
      else if (isground && md.direction.x == 0)
      {
        animator.SetBool("isRun", false);
      }    
    }
    
  }

  private void Flip(bool oldVal, bool newVal, bool asServer)
  {
    Vector3 localScale = transform.localScale;
    if (_facingRight)
    {
      localScale.x = 1;
    } else {
      localScale.x = -1;
    }
    transform.localScale = localScale;
  }

  [Reconcile]
  private void Reconciliation(ReconcileData rd, bool asServer)
  {
    transform.position = rd.position;
    rb.velocity = rd.velocity;
    rb.angularVelocity = rd.angularVelocity;
    _facingRight = rd.facingRight;
    rb.gravityScale = rd.gravity;
  }
  private void OnEnable()
  {
    m_Input?.Enable();
  }
  private void OnDisable()
  {
    m_Input?.Disable();
  }
  public void Jump(InputAction.CallbackContext context)
  {
    if (base.IsOwner)
      jump = context.performed;
  }
  public void SetMovement(InputAction.CallbackContext context)
  {
    if (base.IsOwner)
      direction = context.ReadValue<Vector2>();
  }
    public void Dash(InputAction.CallbackContext ctx)
    {
        if (base.IsOwner)
        {
            dash = ctx.performed;
        }
            
    }
  public void OnDrawGizmos()
  {
    Gizmos.color = Color.red;
    Gizmos.DrawWireSphere(detectsGround.position, 0.2f);
  }
}


public struct  MoveData
{
  public bool jump;
  public Vector2 direction;
  public bool dash;
  public MoveData (bool jump, Vector2 direction, bool dash)
  {
    this.jump = jump;
    this.direction = direction;
    this.dash = dash;
  }
}

public struct ReconcileData
{
  public Vector3 position;
  public bool facingRight;
  public Vector2 velocity;
  public float angularVelocity;
    public float gravity;
  public ReconcileData (Vector3 pos, Vector2 vel, float aVel, bool fRight, float grav)
  {
    position = pos;
    velocity = vel;
    angularVelocity = aVel;
    facingRight = fRight;
    gravity = grav;

  }
}