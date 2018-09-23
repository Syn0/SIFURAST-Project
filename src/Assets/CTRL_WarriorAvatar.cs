using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CTRL_WarriorAvatar : Photon.MonoBehaviour
{


    public float RunSpeed = 5f;
    public float RunAccel = 5f;
    public float RunDecel = 0.1f;
    public float AnimSpeed = 1f;
    public float JumpForce;

    Animator m_Animator;
    Rigidbody2D m_Body;
    PhotonView m_PhotonView;
    Text m_txt_Username;

    public bool m_IsGrounded;

    Vector3 facingL;
    Vector3 facingR;

    Camera cam;

    void Awake()
    {
        //Debug.Log(Environment.Version);
        m_Animator = GetComponent<Animator>();
        m_Body = GetComponent<Rigidbody2D>();
        m_PhotonView = GetComponent<PhotonView>();
        m_txt_Username = GetComponentInChildren<Text>();
        float s = transform.localScale.x;
        facingL = new Vector3(-s, s, 1);
        facingR = new Vector3(s, s, 1);


        if (!m_PhotonView.isMine || m_PhotonView.isSceneView) return;
        cam = Camera.main;
    }

    private void Start()
    {
        m_txt_Username.text = WebCredential.playerCredential.UserName;
        m_Animator.speed = AnimSpeed;
    }

    void Update()
    {
        if (!m_PhotonView.isMine || m_PhotonView.isSceneView) return;

        Vector3 pos = transform.position;
        pos.z = cam.transform.position.z;
        pos.y += 2f;
        cam.transform.position = Vector3.Lerp(cam.transform.position, pos, Time.deltaTime*2.5f);


        if (Input.GetButtonDown("Btn0") && !isAnimating)
        {
            m_Animator.SetInteger("stateVal", 4);
            StartCoroutine(resetIdleState());
        }
        if (Input.GetButtonDown("Btn1") && !isAnimating)
        {
            m_Animator.SetInteger("stateVal", 5);
            StartCoroutine(resetIdleState());
        }
        if (Input.GetButtonDown("Btn2") && !isAnimating)
        {
            m_Animator.SetInteger("stateVal", 6);
            StartCoroutine(resetIdleState());
        }
        if (Input.GetButtonDown("Btn4") && !isAnimating && Mathf.Abs(m_Body.velocity.x) > 0.1f)
        {
            m_Animator.SetInteger("stateVal", 3);
            StartCoroutine(resetIdleState());
        }
    }

    void FixedUpdate()
    {

        UpdateIsGrounded();
        UpdateIsRunning();
        UpdateFacingDirection();

        if (m_PhotonView.isSceneView) m_Body.velocity = new Vector2(m_Body.velocity.x * RunDecel, m_Body.velocity.y);
        if (!m_PhotonView.isMine || m_PhotonView.isSceneView) return;

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
        if (Input.GetButtonDown("Btn3") && m_IsGrounded)
        {
            m_Animator.SetInteger("stateVal", 2);
            m_Body.AddForce(Vector2.up * JumpForce);
            m_PhotonView.RPC("DoJump", PhotonTargets.Others);
        }
    }

    bool isAnimating = false;
    IEnumerator resetIdleState()
    {
        isAnimating = true;
        yield return new WaitForSeconds(0.28f);
        m_Animator.SetInteger("stateVal", 0);
        isAnimating = false;
    }

    [PunRPC]
    void DoJump()
    {
        m_Animator.SetInteger("stateVal", 2);
    }

    void UpdateMovement()
    {

        if (Input.GetAxisRaw("Horizontal") > 0.5f && m_Body.velocity.x < RunSpeed)
            m_Body.AddForce(new Vector2(RunAccel, 0f), ForceMode2D.Impulse);
        else if (Input.GetAxisRaw("Horizontal") < -0.5f && m_Body.velocity.x > -RunSpeed)
            m_Body.AddForce(new Vector2(-RunAccel, 0f), ForceMode2D.Impulse);
        else
            m_Body.velocity = new Vector2(m_Body.velocity.x * RunDecel, m_Body.velocity.y);


    }

    void UpdateIsRunning()
    {
        if (!m_IsGrounded || m_Animator.GetInteger("stateVal") > 2) return;
        if (Mathf.Abs(m_Body.velocity.x) > 0.1f && m_Animator.GetInteger("stateVal") != 2)
        {
            m_Animator.SetInteger("stateVal", 1);
            m_Animator.SetFloat("runSpeed", Mathf.Abs(m_Body.velocity.x) / 6f);
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
        RaycastHit2D hit = Physics2D.Raycast(position, -Vector2.up, 0.1f);

        m_IsGrounded = hit.collider != null;
        if (m_IsGrounded && m_Animator.GetInteger("stateVal") == 2)
        {
            m_Animator.SetInteger("stateVal", 0); // A TRADUIRE EN ENUMEREE
        }
    }






}
