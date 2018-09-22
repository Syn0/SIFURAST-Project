﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using UnityEngine.SceneManagement;

public class MNG_GameManager : Photon.MonoBehaviour
{

    static MNG_GameManager instance;
    public PlayerGBInfo[] playersGBInfoList;

    // GAME_ROUND
    public int durationRound = 180000;
    public int minPlayers = 2;
    public int durationRoundPreparation = 10000;
    public int durationRoundFinalize = 5000;

    //DISPLAY
    public Text text_btn_Ready;
    public GameObject go_checkmark_Ready;
    MNG_MainMenu mng_main;

    //GAMEBOARD
    public GameObject content_Gameboard;
    public GameObject gop_Gameboard_playerinfo;

    public static PlayerInitializer PlayerAvatar;
    public GameObject MainMenuCameraRoot;

    //SCORING
    public int PointGenereParPrisonnier = 5;

    private void Awake()
    {
        instance = this;
        mng_main = FindObjectOfType<MNG_MainMenu>();
    }


    void Update ()
    {
        if (!PhotonNetwork.inRoom) return;

        //MISE A JOUR AFFICHAGE DU STATUS DE CONNECTION A LA ROOM  

        if ( PhotonNetwork.room.GetAttribute(RoomAttributes.PLAYERSCANSPAWN,false)
            && PhotonNetwork.player.getTeamID()!=0
            && !PhotonNetwork.player.GetAttribute(PlayerAttributes.HASSPAWNED, false))
        {
            SpawnMyAvatar();
        }

        // ON CHECK LA PRESENCE D'UN MASTER PLAYER // UNIQUEMENT SI CE N'EST PAS EN COURS // REVIENS A : PhotonNetwork.isNonMasterClientInRoom
        if (!PhotonNetwork.playerList.AsEnumerable().Any(a=>a.isGameOwner()) && !PhotonNetwork.room.GetAttribute(RoomAttributes.CHANGEINGMASTER, false))
        {
            // ON LOCK LA METHODE POUR EVITER QUE TOUT LES JOUEURS CHANGENT L'HOST !
            PhotonNetwork.room.SetAttribute(RoomAttributes.CHANGEINGMASTER, true);
            //On le designe automatiquement par le premier joueur de la liste
            PhotonNetwork.SetMasterClient(PhotonNetwork.playerList[0]);
            // ON DELOCK
            PhotonNetwork.room.SetAttribute(RoomAttributes.CHANGEINGMASTER, false);
        }

        // CONTROLLER DE STATUT DE JEU = GEREE PAR LE PLAYERMASTER
        if ( PhotonNetwork.player.isGameOwner())
        {
            if (PhotonNetwork.room.GetRoomState() == GameState.isWaitingNewGame)
            {
                ChatVik.SendRoomMessage("Warmup : Waiting " + minPlayers + " players minimum to start game");
                StartCoroutine(WarmUp());
            }
        }

        //FONCTION DE DEBUG POUR FAIRE RESPAWN LE JOUEUR
        if (Input.GetKeyDown(KeyCode.F10)){
            photonView.RPC("rpc_UnspawnPlayerAvatar", PhotonTargets.All, new object[] { PhotonNetwork.player});
        }
    }




    /// <summary>
    /// Une fois la room créé, on la rend visible et accessible
    /// </summary>
    public void OnCreatedRoom()
    {
        if(PhotonNetwork.inRoom && !PhotonNetwork.isNonMasterClientInRoom)
        {
            PhotonNetwork.room.IsVisible = true;
            PhotonNetwork.room.IsOpen = true;
            PhotonNetwork.room.SetRoomState( GameState.isWaitingNewGame);
            InitRoomAttributes(true);
            InvokeRepeating("RefreshGameBoard", 0f, 1f);
        }
    }

    /// <summary>
    /// Mets a jour du bouton Ready
    /// </summary>
    public void UpdateReadyBtnState()
    {
        bool value = PhotonNetwork.player.GetAttribute<bool>(PlayerAttributes.ISREADY, false);
        text_btn_Ready.text = value ? "Ready" : "Not Ready";
        
        go_checkmark_Ready.SetActive(value);
    }

    /// <summary>
    /// Mettre un joueur en status "prêt"
    /// </summary>
    public void switchReadyState()
    {
        if (!PhotonNetwork.inRoom
            && !PhotonNetwork.player.GetAttribute(PlayerAttributes.HASSPAWNED, false)
            && PhotonNetwork.player.GetPlayerState()!=PlayerState.inGame)
            return;
        bool newVal = !PhotonNetwork.player.GetAttribute<bool>(PlayerAttributes.ISREADY, false);
        PhotonNetwork.player.SetAttribute(PlayerAttributes.ISREADY, newVal);
        ChatVik.SendRoomMessage(PhotonNetwork.player.NickName + " is now " + (newVal ? "ready" : "not ready"));
        UpdateReadyBtnState();
    }
    void OnJoinedRoom()
    {
        ChatVik.SendRoomMessage(PhotonNetwork.player.NickName + " enter the game");
        InitPlayerAttributes(PhotonNetwork.player,false);
        
        PhotonNetwork.player.SetAttribute(PlayerAttributes.TEAM, 0);
        InvokeRepeating("RefreshGameBoard", 0f, 1f);

        SpawnMyAvatar();
    }
    void OnLeftRoom()
    {
        ChatVik.SendRoomMessage(PhotonNetwork.player.NickName + " leave the game");
        PhotonNetwork.player.SetAttribute(PlayerAttributes.TEAM, 0);
        InitPlayerAttributes(PhotonNetwork.player,false);
    }
    void ReloadRoomScene()
    {
        return; // pour l'instant
        PhotonNetwork.DestroyAll();
        PhotonNetwork.LoadLevel("0-MainMenu");
    }

    /// <summary>
    /// Ici on raffraichis, le gameboard
    /// </summary>
    void RefreshGameBoard()
    {
        PhotonPlayer[] plist = PhotonNetwork.playerList; // BUGFIX CAR LA LISTE PEUT CHANGER EN TRAITEMENT

        // RAFRAICHISSEMENT DU GAMEBOARD DES JOUEURS
        List<PlayerGBInfo> newList = playersGBInfoList.ToList();
        foreach (PlayerGBInfo PGBI in playersGBInfoList.ToList())
        {
            if (!plist.Contains(PGBI.player))
            { // remove 
                Destroy(PGBI);
                newList.Remove(PGBI);
            }
            else//refresh
            {
                PGBI.txt_state.text = getPlayerStrState(PGBI.player);
                PGBI.txt_latence.text = "-";
                PGBI.txt_score.text = PGBI.player.GetAttribute(PlayerAttributes.SCORE, 0).ToString();
                PGBI.txt_capture.text = PGBI.player.GetAttribute(PlayerAttributes.CAPTURESCORE, 0).ToString();
                PGBI.gameObject.SetActive(false);
            }
        }
        playersGBInfoList = newList.ToArray();
        //
        foreach (PhotonPlayer player in plist)
        {
            if (!playersGBInfoList.ToList().Any(w => w.player == player)) // add
            {
                PlayerGBInfo newone = Instantiate(gop_Gameboard_playerinfo, content_Gameboard.transform).GetComponent<PlayerGBInfo>();
                newone.txt_nickname.text = player.NickName;
                newone.player = player;
                newList.Add(newone);

                newone.txt_state.text = getPlayerStrState(newone.player);
                newone.txt_latence.text = "-";
                newone.txt_score.text = newone.player.GetAttribute(PlayerAttributes.SCORE, 0).ToString();
                newone.txt_capture.text = newone.player.GetAttribute(PlayerAttributes.CAPTURESCORE, 0).ToString();
                newone.gameObject.SetActive(false);
            }
        }
        playersGBInfoList = newList.ToArray();
        // ORDERING
        int j = 0;
        foreach (PlayerGBInfo item in playersGBInfoList.Where(w => w.player.getTeamID() == 1).OrderByDescending(o => o.player.GetAttribute(PlayerAttributes.SCORE, 0)))
        {
            item.gameObject.SetActive(true);
            var rect = item.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector3(135f, -12 + (-22f * j), 0f);
            j++;
        }
        j = 0;
        foreach (PlayerGBInfo item in playersGBInfoList.Where(w => w.player.getTeamID() == 2).OrderByDescending(o => o.player.GetAttribute(PlayerAttributes.SCORE, 0)))
        {
            item.gameObject.SetActive(true);
            var rect = item.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector3(135f + 276.5f, -12 + (-22f * j), 0f);
            j++;
        }

    }
    string getPlayerStrState(PhotonPlayer player)
    {
        return (player.GetAttribute(PlayerAttributes.ISCAPTURED, false) ? "Captured" : player.GetPlayerState() == PlayerState.inGame ? "ig" : player.GetAttribute(PlayerAttributes.ISREADY, false) ? "Ready" : "-");
    }

    //============================================================================================//

    /// <summary>
    /// Cette méthode vérifie l'état de jeu dans une session en cours.
    /// </summary>
    public void UpdateGameInfos()
    {
        // UPDATE DE LA PARTIE UNIQUEMENT POUR LE MASTERPLAYER
        if (!(PhotonNetwork.player.isGameOwner() && PhotonNetwork.room.GetRoomState() == GameState.RoundRunning)) return;
        // ON CHECK SI LE TIMER EST ATTEInt OU TOUT LES THIEFS SONT CAPTUREE
        if (PhotonNetwork.playerList.Where(s => s.getTeamID() == 1 && s.GetAttribute(PlayerAttributes.HASSPAWNED, false)).All(s => s.GetAttribute(PlayerAttributes.ISCAPTURED, false)))
        {
            PhotonNetwork.room.SetAttribute(RoomAttributes.ALLTHIEFCATCHED, true);
            ChatVik.SendRoomMessage("COPS CATCH ALL THIEVES AND WIN THE ROUND");
            PhotonNetwork.room.SetTeamAttribute(2, TeamAttributes.ROUNDSWON, PhotonNetwork.room.GetTeamAttribute(2, TeamAttributes.ROUNDSWON, 0) + 1);
            foreach (PhotonPlayer p in PhotonNetwork.playerList.Where(s => s.getTeamID() == 1 || s.getTeamID() == 2))
                p.SetAttribute(PlayerAttributes.ISIMMOBILIZED, true);

        }

    }

    /// <summary>
    /// Coroutine de jeu : Echauffement
    /// </summary>
    /// <returns></returns>
    IEnumerator WarmUp()
    {
        //INIT DES VARIABLES DE WARMUP
        PhotonNetwork.room.SetRoomState(GameState.WarmUp); // au cas ou
        InitRoomAttributes(true);
        SetImmobilizeAll(false);
        PhotonNetwork.room.SetAttribute(RoomAttributes.PLAYERSCANSPAWN, true);

        //ECOUTE DU NOMBRE DE JOUEUR READY
        while (true) yield return new WaitForSeconds(1f);

        // CREATION DU PREMIER ROUND
        PhotonNetwork.room.SetRoomState(GameState.BeginningRound);
        ReloadRoomScene();
    }



    //============================================================================================//
    //============================================================================================//

    /// <summary>
    /// Ca peut servir....
    /// </summary>
    /// <returns></returns>
    private IEnumerator MoveToGameScene()
    {
        // Temporary disable processing of futher network messages
        PhotonNetwork.isMessageQueueRunning = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name); // custom method to load the new scene by name
        while (true/*newSceneDidNotFinishLoading*/)
        {
            yield return null;
        }
        PhotonNetwork.isMessageQueueRunning = true;
    }



    /*============================================================================================*/

    /// <summary>
    ///  GESTION SPAWNING DES JOUEURS
    /// </summary>
    public string playerprefabname_overlaw = "Overlaw_Player";
    public string spectatorPrefabName = "Spectator";
    /// <summary>
    /// Faire apparaitre le joueur au point de spawn d'équipe choisit.
    /// </summary>
    public void SpawnMyAvatar()
    {
        if (!PhotonNetwork.inRoom || PhotonNetwork.player.GetAttribute<bool>(PlayerAttributes.HASSPAWNED, false)) return;

        //bugfix
        ResetCameraTransform();
        //PhotonNetwork.DestroyPlayerObjects(PhotonNetwork.player);
        DestroyPlayerCharacters(PhotonNetwork.player);

        // CREATE AVATAR
        GameObject playerChar;
        Vector2 randpos = UnityEngine.Random.insideUnitCircle * 5f;
        playerChar = PhotonNetwork.Instantiate(this.playerprefabname_overlaw, new Vector3(randpos.x, 0f, randpos.y), Quaternion.identity, 0);
        PhotonNetwork.player.SetAttribute(PlayerAttributes.HASSPAWNED, true);
        PhotonNetwork.player.SetAttribute(PlayerAttributes.TEAM, 0);
        PhotonNetwork.player.SetPlayerState(PlayerState.inGame);

        //SETUP CAMERA
        playerChar.GetComponent<MNG_CameraController>().camera = Camera.main;
        Camera.main.transform.parent = playerChar.transform;
        Camera.main.transform.localPosition = new Vector3(0, 0, 0);
        Camera.main.transform.localEulerAngles = new Vector3(0, 0, 0);
        //LOCKING MOUSE
        MNG_MainMenu.captureMouse = true;
    }

    public void call_UnspawnPlayerAvatar(PhotonPlayer p)
    {
        photonView.RPC("rpc_UnspawnPlayerAvatar", PhotonTargets.All, new object[] { p });
    }

    /// <summary>
    /// Appelle la destruction d'un avatar d'un joueur
    /// </summary>
    /// <param name="player"></param>
    [PunRPC]
    public void rpc_UnspawnPlayerAvatar(PhotonPlayer player)
    {
        if (!PhotonNetwork.inRoom || !player.GetAttribute<bool>(PlayerAttributes.HASSPAWNED, false)) return;
        
        //RESET DE LAA CAMERA UNIQUEMENT CHEZ LE CLIENT CONCERNEE
        if (PhotonNetwork.player == player)
        {
            ResetCameraTransform();
            //DESTROY AVATAR UNIQUEMENT PAR LE MASTERCLIENT
            //PhotonNetwork.DestroyPlayerObjects(player);
            DestroyPlayerCharacters(player);
            PlayerAvatar = null;
            player.SetAttribute(PlayerAttributes.HASSPAWNED, false);
        }

        

    }

    /// <summary>
    /// Methode permettant de retrouver l'avatar du joueur
    /// </summary>
    /// <param name="player"></param>
    void DestroyPlayerCharacters(PhotonPlayer player)
    {
        foreach(PhotonView pv in FindObjectsOfType<PhotonView>().Where(x=> x.ownerId == player.ID && x.gameObject.tag == "Player"))
        {
            PhotonNetwork.Destroy(pv);
        }
    }

    //============================================================================================//

    void ResetCameraTransform()
    {
        //SETUP CAMERA
        Camera.main.transform.parent = MainMenuCameraRoot.transform;
        Camera.main.transform.localPosition = new Vector3(0, 0, -48);
        Camera.main.transform.localEulerAngles = new Vector3(40, 0, 0);
    }
    void InitRoundAttributes()
    {

    }

    /// <summary>
    /// Réinitialise tous les attributs par défaut  d'une room
    /// </summary>
    /// <param name="reinitgameattr"></param>
    void InitRoomAttributes(bool reinitgameattr)
    {
        PhotonNetwork.room.SetAttribute(RoomAttributes.PRISONOPEN, false);
        PhotonNetwork.room.SetAttribute(RoomAttributes.PLAYERSCANSPAWN, false);
        PhotonNetwork.room.SetAttribute(RoomAttributes.ALLTHIEFCATCHED, false);
        PhotonNetwork.room.SetAttribute(RoomAttributes.IMMOBILIZEALL, false);
        //PhotonNetwork.room.SetAttribute(RoomAttributes.TIMEROUNDSTARTED, PhotonNetwork.ServerTimestamp);
        
        if (reinitgameattr)
        {
            PhotonNetwork.room.SetAttribute(RoomAttributes.ROUNDNUMBER, 0);
        }
    }

    /// <summary>
    /// Réinitialise tous les attributs par défauts d'un joueur
    /// </summary>
    /// <param name="Player"></param>
    /// <param name="reinitgameattr"></param>
    public void InitPlayerAttributes(PhotonPlayer Player,bool reinitgameattr)
    {
        if (!PhotonNetwork.inRoom) return;
        Player.SetAttribute(PlayerAttributes.ISIDLE, false);
        Player.SetAttribute(PlayerAttributes.ISREADY, false);
        Player.SetAttribute(PlayerAttributes.ISLAGGY, false);
        Player.SetAttribute(PlayerAttributes.ISROOMADMIN, false);
        Player.SetAttribute(PlayerAttributes.ISIMMOBILIZED, PhotonNetwork.room.GetAttribute(RoomAttributes.IMMOBILIZEALL, false));
        Player.SetPlayerState(PlayerState.isReadyToPlay);
        if (reinitgameattr)
        {
            Player.SetAttribute(PlayerAttributes.SCORE, 0);
            Player.SetAttribute(PlayerAttributes.CAPTURESCORE, 0);
            
            Player.SetAttribute(PlayerAttributes.ISCAPTURED, false);
            Player.SetAttribute(PlayerAttributes.INPRISONZONE, false);
            Player.SetAttribute(PlayerAttributes.testKey, "INITIED");

        }
        UpdateReadyBtnState();
    }
    public void SetImmobilizeAll(bool value)
    {
        if (!PhotonNetwork.inRoom) return;
        PhotonNetwork.room.SetAttribute(RoomAttributes.IMMOBILIZEALL, value);
        foreach (PhotonPlayer p in PhotonNetwork.playerList) p.SetAttribute(PlayerAttributes.ISIMMOBILIZED, value);
    }


}

[System.Serializable]
public struct Team
{
    public string TeamName;
    public Vector3 TeamSpawnLocation;
    public GameObject BtnCheckmark;
    public Button Btn_join;
    public Text txt_roundswon;
    public Text txt_tscore;

    public GameObject panel_winround;
    public GameObject panel_wingame;


}

