using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class CTRL_WarriorAvatar : Photon.MonoBehaviour, I2DContactsCollector
{
    public float RunSpeed = 5f;
    public float RunAccel = 5f;
    public float RunDecel = 0.1f;
    public float AnimSpeed = 1f;
    public float JumpForce;

    Animator m_Animator;
    public GameObject mainSprite;
    Rigidbody2D m_Body;
    PhotonView m_PhotonView;
    Text m_txt_Username;

    public BoxCollider2D m_c2d_atckTrigger;

    public bool m_IsGrounded;

    float facing = 1;
    Vector3 facingL;
    Vector3 facingR;

    Camera cam;

    void Awake()
    {
        //Debug.Log(Environment.Version);
        m_Animator = GetComponentInChildren<Animator>();
        m_Body = GetComponent<Rigidbody2D>();
        m_PhotonView = GetComponent<PhotonView>();
        m_txt_Username = GetComponentInChildren<Text>();
        ContactsTriggered = new List<Collider2D>();

        float s = mainSprite.transform.localScale.x;
        facingL = new Vector3(-s, s, 1);
        facingR = new Vector3(s, s, 1);

        if (!m_PhotonView.isMine || m_PhotonView.isSceneView) return;
        cam = Camera.main;
    }

    private void Start()
    {
        if (m_PhotonView.isMine) m_txt_Username.text = WebCredential.playerCredential.UserName;
        if (m_PhotonView.isSceneView) m_txt_Username.text = "Bot";
        m_Animator.speed = AnimSpeed;
        m_c2d_atckTrigger.gameObject.SetActive(false);

        if (!m_PhotonView.isMine && !m_PhotonView.isSceneView)
        {
            m_Body.gravityScale = 0f;
            m_Body.isKinematic = true;
        }
    }

    void Update()
    {
        Vector3 pos = transform.position;
        if (pos.y < -10f)
        {
            transform.position = Vector3.zero;
            m_Body.velocity = Vector3.zero;
        }

        //█
        if (!m_PhotonView.isMine || m_PhotonView.isSceneView) return; 
        pos.z = cam.transform.position.z;
        pos.y += 2f;
        cam.transform.position = Vector3.Lerp(cam.transform.position, pos, Time.deltaTime * 2.5f);


        if (!isAnimating)
        {
            if (Input.GetButtonDown("Btn0"))
            {
                m_Animator.SetInteger("stateVal", 4);
                StartCoroutine(resetIdleState());
                StartCoroutine(attack(0));
            }
            if (Input.GetButtonDown("Btn1"))
            {
                m_Animator.SetInteger("stateVal", 5);
                StartCoroutine(resetIdleState());
                StartCoroutine(attack(1));
            }
            if (Input.GetButtonDown("Btn2"))
            {
                m_Animator.SetInteger("stateVal", 6);
                StartCoroutine(resetIdleState());
                StartCoroutine(attack(2));
            }
            if (Input.GetButtonDown("Btn4"))
            {
                m_Animator.SetInteger("stateVal", 3);
                StartCoroutine(resetIdleState());
                StartCoroutine(attack(3));
            }
            if (Input.GetButton("Btn3") && m_IsGrounded)
            {
                m_Animator.SetInteger("stateVal", 2);
                StartCoroutine(resetIdleState());
                m_Body.AddForce(Vector2.up * JumpForce);
            }
        }
    }

    void FixedUpdate()
    {

        UpdateIsGrounded();
        UpdateIsRunning();

        if (m_PhotonView.isSceneView) m_Body.velocity = new Vector2(m_Body.velocity.x * RunDecel, m_Body.velocity.y);
        if (!m_PhotonView.isMine || m_PhotonView.isSceneView) return;

        UpdateMovement();
    }

    bool isAnimating = false;
    IEnumerator resetIdleState()
    {
        isAnimating = true;
        yield return new WaitForSeconds(0.28f);
        m_Animator.SetInteger("stateVal", 0);
        isAnimating = false;
    }

    IEnumerator attack(int attackType)
    {
        switch (attackType)
        {
            case 0:
                m_Body.AddForce(new Vector2(5f * facing, 5f), ForceMode2D.Impulse);
                break;
            case 2:
                m_Body.AddForce(new Vector2(12f * facing, 2f), ForceMode2D.Impulse);
                break;
            case 3:
                m_Body.AddForce(new Vector2(20f * facing, 3f), ForceMode2D.Impulse);
                break;
        }


        m_c2d_atckTrigger.gameObject.SetActive(true);
        yield return new WaitForSeconds(0.14f);
        m_c2d_atckTrigger.gameObject.SetActive(false);
        if (attackType == 1)
            m_PhotonView.RPC("InvokePbox", PhotonTargets.MasterClient);

        // ON TOUCH █

        foreach (Collider2D item in ContactsTriggered.Where(w => w.gameObject != null && w.gameObject.tag == "Collidable"))
        {
            Rigidbody2D body = item.GetComponentInParent<Rigidbody2D>() ?? item.GetComponent<Rigidbody2D>();
            if (body.tag == "Player") body.GetComponent<CTRL_WarriorAvatar>().callBloodInstance();
            switch (attackType)
            {
                case 0:
                    body.AddForce(new Vector2(2f * facing, 5f), ForceMode2D.Impulse);
                    break;
                case 2:
                    body.AddForce(new Vector2(12f * facing, 1f), ForceMode2D.Impulse);
                    break;
            }
        }
        ContactsTriggered.Clear();

    }

    void UpdateMovement()
    {
        m_Body.velocity = new Vector2(m_Body.velocity.x * RunDecel, m_Body.velocity.y);
        if (Input.GetAxisRaw("Horizontal") < -0.1f) { mainSprite.transform.localScale = facingL; facing = -1; }
        else if (Input.GetAxisRaw("Horizontal") > 0.1f) { mainSprite.transform.localScale = facingR; facing = 1; }
        if (Mathf.Abs(m_Body.velocity.x) < RunSpeed)
            m_Body.AddForce(new Vector2(RunAccel * Input.GetAxisRaw("Horizontal") * Mathf.Abs(Input.GetAxisRaw("Horizontal")), .05f), ForceMode2D.Impulse);
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
    }
    void UpdateIsGrounded()
    {
        Vector2 position = new Vector2(transform.position.x, transform.position.y - 0.05f);
        RaycastHit2D hit = Physics2D.Raycast(position, -Vector2.up, 0.2f);
        m_IsGrounded = hit.collider != null;
        if (m_IsGrounded && m_Animator.GetInteger("stateVal") == 2)
        {
            m_Animator.SetInteger("stateVal", 0); // A TRADUIRE EN ENUMEREE
        }
    }


    public GameObject gop_bloodparticles;
    public void callBloodInstance() { m_PhotonView.RPC("SpawnBlood", PhotonTargets.All); }

    [PunRPC]
    void InvokePbox()
    {
        PhotonNetwork.InstantiateSceneObject("Physics Box", transform.position + new Vector3(facing * 1f, 1f, 0), Quaternion.identity, 0, null).GetComponent<Rigidbody2D>().AddForce(new Vector3(facing * 2f, 1f, 0), ForceMode2D.Impulse);
    }

    [PunRPC]
    void SpawnBlood()
    {
        GameObject.Instantiate(gop_bloodparticles, transform.position + new Vector3(0, 1.35f, 0), Quaternion.Euler(0, 0, Random.value * 180f - 90f));
    }
    List<Collider2D> ContactsTriggered;
    public void AddCollision2D(Collider2D coll)
    {
        ContactsTriggered.Add(coll);
    }
}
