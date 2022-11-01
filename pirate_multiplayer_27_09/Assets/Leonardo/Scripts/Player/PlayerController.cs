using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    private Input_System m_Input;
    private Rigidbody2D rb;
    public float moveSpeed;
    private Vector2 direction;
    public float jumpForce;
    private bool jump;
    private bool isground;
    private Vector3 facingRigth;
    private Vector3 facingLeft;
    public Transform detectsGround;
    public LayerMask ground;
    public Animator animator;

    private void Awake()
    {
        facingLeft = transform.localScale; 
        facingRigth = transform.localScale;
        facingLeft.x = facingLeft.x * -1;

        m_Input = new Input_System();
    }
    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        m_Input.Enable();
    }
    private void OnDisable()
    {
        m_Input.Disable();
    }
    public void Jump(InputAction.CallbackContext context)
    {
        jump = context.performed;
    }
    public void SetMoviment(InputAction.CallbackContext context)
    {
       direction = context.ReadValue<Vector2>();
    }
    private void FixedUpdate()
    {
        rb.velocity= new Vector2(direction.x * moveSpeed, rb.velocity.y);
        if(direction.x>0)
        {
            transform.localScale = facingRigth;
        }
        if(direction.x < 0)
        {
            transform.localScale = facingLeft;
        }
        isground = Physics2D.OverlapCircle(detectsGround.position, 0.2f, ground);
        if (jump == true && isground==true)
        {
            rb.velocity = Vector2.zero;
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            animator.SetBool("isJump", true);
           
        }
        if(isground && rb.velocity.y == 0)
        {
            animator.SetBool("isJump", false);
        }
        if (direction.x != 0)
        {
            animator.SetBool("isRun", true);
        }
        else
        {
            animator.SetBool("isRun", false);
        }
        


    }


}
