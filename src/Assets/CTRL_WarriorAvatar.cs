using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CTRL_WarriorAvatar : Photon.MonoBehaviour
{


    public float Speed;
    public float AnimSpeed = 1f;
    public float JumpForce;

    Animator m_Animator;
    Rigidbody2D m_Body;
    PhotonView m_PhotonView;
    Text m_txt_Username;

    public bool m_IsGrounded;

    Vector3 facingL;
    Vector3 facingR;

    void Awake()
    {
        m_Animator = GetComponent<Animator>();
        m_Body = GetComponent<Rigidbody2D>();
        m_PhotonView = GetComponent<PhotonView>();
        m_txt_Username = GetComponentInChildren<Text>();
        float s = transform.localScale.x;
        facingL = new Vector3(-s, s, 1);
        facingR = new Vector3(s, s, 1);
    }

    private void Start()
    {
        m_txt_Username.text = WebCredential.playerCredential.UserName;
        m_Animator.speed = AnimSpeed;
    }

    void Update()
    {
        UpdateIsGrounded();
        UpdateIsRunning();
        UpdateFacingDirection();

        if (Input.GetKeyDown(KeyCode.F)) m_Animator.SetInteger("stateVal", 4);
        if (Input.GetKeyDown(KeyCode.G)) m_Animator.SetInteger("stateVal", 5);
        if (Input.GetKeyDown(KeyCode.H)) m_Animator.SetInteger("stateVal", 6);
    }

    void FixedUpdate()
    {
        if (m_PhotonView.isMine == false)
        {
            return;
        }

        UpdateMovement();
        UpdateJumping();
    }

    void UpdateFacingDirection()
    {
        if (m_Body.velocity.x > 0.2f)
        {
            transform.localScale = facingR;
        }
        else if (m_Body.velocity.x < -0.2f)
        {
            transform.localScale = facingL;
        }
    }

    void UpdateJumping()
    {
        if (Input.GetButton("Jump") && m_IsGrounded)
        {
            m_Animator.SetInteger("stateVal", 2);
            m_Body.AddForce(Vector2.up * JumpForce);
            m_PhotonView.RPC("DoJump", PhotonTargets.Others);
        }
    }

    [PunRPC]
    void DoJump()
    {
        m_Animator.SetInteger("stateVal", 2);
    }

    void UpdateMovement()
    {
        Vector2 movementVelocity = m_Body.velocity;
        if (Input.GetAxisRaw("Horizontal") > 0.5f)
            movementVelocity.x = Speed;
        else if (Input.GetAxisRaw("Horizontal") < -0.5f)
            movementVelocity.x = -Speed;
        else
            movementVelocity.x = 0;
        m_Body.velocity = movementVelocity;
    }

    void UpdateIsRunning()
    {
        if (Mathf.Abs(m_Body.velocity.x) > 0.1f && m_Animator.GetInteger("stateVal") != 2)
        {
            m_Animator.SetInteger("stateVal", 1);
             m_Animator.SetFloat("runSpeed", Mathf.Abs(m_Body.velocity.x)/6f);
        }
            m_Animator.SetInteger("stateVal", 1);
        if (Mathf.Abs(m_Body.velocity.x) <= 0.1f && m_Animator.GetInteger("stateVal") == 1)
            m_Animator.SetInteger("stateVal", 0);
        //m_Animator.SetBool("IsRunning", Mathf.Abs(m_Body.velocity.x) > 0.1f);
    }

    void UpdateIsGrounded()
    {
        Vector2 position = new Vector2(transform.position.x, transform.position.y - 0.05f);

        //RaycastHit2D hit = Physics2D.Raycast( position, -Vector2.up, 0.1f, 1 << LayerMask.NameToLayer( "Ground" ) );
        RaycastHit2D hit = Physics2D.Raycast(position, -Vector2.up, 0.05f);

        m_IsGrounded = hit.collider != null;
        if (m_IsGrounded && m_Animator.GetInteger("stateVal") == 2)
        {
            m_Animator.SetInteger("stateVal", 0); // A TRADUIRE EN ENUMEREE
        }
    }






}
