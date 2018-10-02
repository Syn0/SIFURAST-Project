using UnityEngine;
using System.Collections;
using System;
using UnityEngine.UI;

public delegate void JumpDelegate ();

public class ThirdPersonControllerNET : Photon.MonoBehaviour
{
    float timeCantMove;
    float timeCantPunch;
    float timeCantClimbGrab;
    public bool _climbing = false;
    public bool _grounded = false;

    public float durationCantMove = 5f;
    public float durationCantPunch = 1f;
    public float durationCantClimbGrab = 0.1f;

    bool isCapturingThief = false;

    public Rigidbody target;
	public float speed = 1.0f,
        walkSpeedDownscale = 2.0f,
        jumpforce = 1.0f;
	public LayerMask groundLayers = -1;
    public bool showGizmos = true;
	public JumpDelegate onJump = null;
    public float 
        inputThreshold = 0.001f
        , groundDrag = 5.0f
        , directionalJumpFactor = 0.7f
        , airDrag = 0f
        , groundedDistance = 0.25f //0.6f
        , groundedCheckOffset = 1.05f //0.7f
        , airmove_mult= 0.05f
        , checkclimbforward = 1f
        , checkclimbtop = 1f
        ;
    AnimationController CTRL_Animation;

    private const int CapturePoint = 1;


    public bool grounded {
        get {
            _grounded = Physics.CheckSphere(target.transform.position + target.transform.up * -groundedCheckOffset, groundedDistance, groundLayers);
            if (_grounded) _climbing = false; return _grounded;
        } }

    void Reset ()
	{
        Setup ();
	}
    void Setup ()
    {
        if (target == null) { target = GetComponent<Rigidbody>(); }
        if (CTRL_Animation == null) { CTRL_Animation = GetComponent<AnimationController>(); }
    }


    void Start ()
	{
        Setup (); // Retry setup if references were cleared post-add
		if (target == null)
		{
			Debug.LogError ("No target assigned. Please correct and restart.");
			enabled = false;
			return;
		}
		//target.freezeRotation = true;
	}

    /// <summary>
    /// Si le joueur est frapp� par une balles : on l'immobilise
    /// </summary>
    /// <param name="collision"></param>
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag == "Ball" && photonView.owner != null)
        {
            GetComponent<PhotonView>().RPC("rpc_immobilize", PhotonTargets.All);
            ChatVik.SendRoomMessage(collision.gameObject.GetComponent<PhotonView>().owner.NickName + " knocked out " + photonView.owner.NickName);
        }
    }

    /// <summary>
    /// Ici on check les touches press�s si le joeuur n'est pas immobilis�
    /// </summary>
    void Update ()
	// Handle rotation here to ensure smooth application.
	{
        if (!photonView.isMine) return;


        if (!PhotonNetwork.player.GetAttribute(PlayerAttributes.ISIMMOBILIZED, false)) // immobilization gestion 
        {


            if (Input.GetButtonDown("Btn1")
                && Time.timeSinceLevelLoad - timeCantPunch > durationCantPunch
                && !_climbing) // you can only give a slap when you're a thief
            {
                timeCantPunch = Time.timeSinceLevelLoad;
                CTRL_Animation.call_anim_trigger("Punch", layer: 1);

                RaycastHit hitInfo = new RaycastHit();
                bool hit = Physics.Raycast(transform.forward * 0.3f + transform.position, transform.forward, out hitInfo, 1.2f, LayerMask.GetMask("NetEntity"));
                if (hit && hitInfo.transform.gameObject.tag == "Player")
                {
                    hitInfo.transform.GetComponent<PhotonView>().RPC("rpc_immobilize", PhotonTargets.All);
                    ChatVik.SendRoomMessage(photonView.owner.NickName + " kick the ass of " + hitInfo.transform.GetComponent<PhotonView>());
                }
            }

            if (Input.GetButtonDown("Btn0") 
                && (grounded || (_climbing)))
            // Handle jumping
            {
                target.AddForce( jumpforce * target.transform.up + target.velocity.normalized * directionalJumpFactor, ForceMode.VelocityChange );
                onJump();
                if (_climbing)
                {
                    CTRL_Animation.call_anim_trigger("ClimbUp", "ClimbUp", layer: 2);
                }
                else CTRL_Animation.call_anim_trigger("Jump");
                timeCantClimbGrab = Time.timeSinceLevelLoad;
                _climbing = false;
            }
            else if (Input.GetKeyDown(KeyCode.LeftControl)
                && _climbing)
            // Handle releasing
            {
                CTRL_Animation.call_anim_trigger("Jump");
                _climbing = false;
                timeCantClimbGrab = Time.timeSinceLevelLoad;
                target.AddForce( target.transform.up*-0.1f , ForceMode.VelocityChange );
            }
            else if (Input.GetKey(KeyCode.LeftControl)
                && !_climbing 
                && Time.timeSinceLevelLoad - timeCantClimbGrab > durationCantClimbGrab)
            // Handle climbing
            {
                bool canclimb = Physics.CheckSphere(target.transform.position + target.transform.up * checkclimbtop + target.transform.forward * checkclimbforward, 0.2f, groundLayers);

                if (canclimb)
                {
                    CTRL_Animation.call_anim_trigger("Climb",layer:2);
                    _climbing = true;
                }
            }
        }
    }

    /// <summary>
    /// M�thode appel� en fin d'instanciation d'un objet sur le r�seau
    /// , on se sert de cette m�thode pour r�cup�rer des param�tres.
    /// </summary>
    /// <param name="info"></param>
    void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        //object[] objs = photonView.instantiationData; //The instantiate data..
        //bool[] mybools = (bool[])objs[0];   //Our bools!
    }

    /// <summary>
    /// Ici on g�re la physique du personnage en fonction de son �tat
    /// </summary>
    void FixedUpdate()
    // Handle movement here since physics will only be calculated in fixed frames anyway
    {
        if (!photonView.isMine) return;

        if (Time.timeSinceLevelLoad > timeCantMove + durationCantMove && !PhotonNetwork.room.GetAttribute(RoomAttributes.IMMOBILIZEALL, false))
            PhotonNetwork.player.SetAttribute(PlayerAttributes.ISIMMOBILIZED, false);

        // On applique le drag appropri�
        if (_climbing && !PhotonNetwork.player.GetAttribute(PlayerAttributes.ISIMMOBILIZED, false)) target.drag = 999f;
        else if (grounded) target.drag = groundDrag;
        else target.drag = airDrag;

        // MOVE
        if (!PhotonNetwork.player.GetAttribute(PlayerAttributes.ISIMMOBILIZED, false)) move();
    }
	
    /// <summary>
    /// Mouvement du joueur.
    /// </summary>
    void move()
    {
        //horizontal
        float sidestep = -(Input.GetKey(KeyCode.Q) ? 1 : 0) + (Input.GetKey(KeyCode.D) ? 1 : 0);
        float horizontal = Input.GetAxis("Horizontal");
        float SidestepAxisInput = Mathf.Abs(sidestep) > Mathf.Abs(horizontal) ? sidestep : horizontal;

        Vector3 movement = Input.GetAxis("Vertical") * target.transform.forward +
            SidestepAxisInput * target.transform.right;

        // + INAIR
        float appliedSpeed = speed
            * (_grounded ? 1f : airmove_mult)
            / (Input.GetAxis("Vertical") < 0.0f ? walkSpeedDownscale : 1);

        if (movement.magnitude > inputThreshold)
        // Only apply movement if we have sufficient input
        {
            
            target.AddForce(movement.normalized * appliedSpeed, ForceMode.VelocityChange);
        }
    }






}
