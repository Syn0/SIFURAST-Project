using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class JumpAndRunMovement : MonoBehaviour
{
    public Text txt_Username;
    public float Speed;
    public float JumpForce;

    Animator m_Animator;
    Rigidbody2D m_Body;
    PhotonView m_PhotonView;

    bool m_IsGrounded;

    void Awake()
    {
        m_Animator = GetComponent<Animator>();
        m_Body = GetComponent<Rigidbody2D>();
        m_PhotonView = GetComponent<PhotonView>();
    }

    private void Start()
    {
        txt_Username.text = WebCredential.playerCredential.UserName;
    }

    void Update()
    {
        UpdateIsGrounded();
        UpdateIsRunning();
        UpdateFacingDirection();
    }

    void FixedUpdate()
    {
        if( m_PhotonView.isMine == false )
        {
            return;
        }

        UpdateMovement();
        UpdateJumping();
    }

    void UpdateFacingDirection()
    {
        if( m_Body.velocity.x > 0.2f )
        {
            transform.localScale = new Vector3( 1, 1, 1 );
        }
        else if( m_Body.velocity.x < -0.2f )
        {
            transform.localScale = new Vector3( -1, 1, 1 );
        }
    }

    void UpdateJumping()
    {
        if (Input.GetButton("Jump") && m_IsGrounded)
        {
            m_Animator.SetTrigger("IsJumping");
            m_Body.AddForce(Vector2.up * JumpForce);
            m_PhotonView.RPC("DoJump", PhotonTargets.Others);
        }
    }

    [PunRPC]
    void DoJump()
    {
        m_Animator.SetTrigger( "IsJumping" );
    }

    void UpdateMovement()
    {
        Vector2 movementVelocity = m_Body.velocity;

        if( Input.GetAxisRaw( "Horizontal" ) > 0.5f )
        {
            movementVelocity.x = Speed;

        }
        else if( Input.GetAxisRaw( "Horizontal" ) < -0.5f )
        {
            movementVelocity.x = -Speed;
        }
        else
        {
            movementVelocity.x = 0;
        }

        m_Body.velocity = movementVelocity;
    }

    void UpdateIsRunning()
    {
        m_Animator.SetBool( "IsRunning", Mathf.Abs( m_Body.velocity.x ) > 0.1f );
    }

    void UpdateIsGrounded()
    {
        Vector2 position = new Vector2( transform.position.x, transform.position.y );

        //RaycastHit2D hit = Physics2D.Raycast( position, -Vector2.up, 0.1f, 1 << LayerMask.NameToLayer( "Ground" ) );
        RaycastHit2D hit = Physics2D.Raycast(position, -Vector2.up, 0.1f);

        m_IsGrounded = hit.collider != null;
        m_Animator.SetBool( "IsGrounded", m_IsGrounded );
    }


    public void run()
    {
        m_Animator.SetTrigger("run");
    }

    public void jump()
    {
        m_Animator.SetTrigger("jump");
    }

    public void attack1()
    {
        m_Animator.SetTrigger("attack1");
    }

    public void attack2()
    {
        m_Animator.SetTrigger("attack2");
    }

    public void attack3()
    {
        m_Animator.SetTrigger("attack3");
    }

    public void skill()
    {
        m_Animator.SetTrigger("skill");
    }


}
