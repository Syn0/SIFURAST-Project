using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class MNG_Multiplayer : MonoBehaviour {

    public static MNG_Multiplayer instance { get; private set; }

    //Multi 
    public string MS_adress = "127.0.0.1";
    public int MS_port = 80;
    public ExitGames.Client.Photon.ConnectionProtocol MS_protocol = ExitGames.Client.Photon.ConnectionProtocol.WebSocket;
    public string MS_appid = "6d12a3eb-889b-463f-998e-4dee86b25971";
    public string MS_ConnVersion = "v1.0";
    public bool MS_WebCloudConnection = true;
    public bool loadFirstPreset = false;

    public void SetMS_adress(string value) { MS_adress = value; }
    public void SetMS_port(string value) { try { MS_port = int.Parse(value); } catch { } }
    public void SetMS_protocol(int value) {
        try {
            switch (value)
            {
                case 0:
                    MS_protocol = ExitGames.Client.Photon.ConnectionProtocol.Udp;
                    break;
                case 1:
                    MS_protocol = ExitGames.Client.Photon.ConnectionProtocol.Tcp;
                    break;
                case 2:
                    MS_protocol = ExitGames.Client.Photon.ConnectionProtocol.WebSocket;
                    break;
                case 3:
                    MS_protocol = ExitGames.Client.Photon.ConnectionProtocol.WebSocketSecure;
                    break;
            }
        }
        catch { }
    }
    public void SetMS_ConnVersion(string value) { MS_ConnVersion = value; }  
    public void SetMS_WebCloudConnection(string value) { try { MS_WebCloudConnection = bool.Parse(value); } catch { } }  

    public Text text_ConnectionState;

    public MultiplayerProtocol[] ConnectionsPreset; // INIT = WebCloud, CustomWebCloud, CustomTCPCloud,CustomUDPCloud
    public InputField AdressInput;
    public InputField PortInput;
    public Dropdown ConnectionsPresetDropdown;
    public InputField ConnVersionInput;
    public Dropdown ProtocolDropdown;
    public Button Btn_Multiplayer;
    public InputField Input_Pseudo;

    /// <summary>
    /// On commence par appliquer une configuration réseau par défaut et on essaye de s'y connecter.
    /// </summary>
    void Start () {
        instance = this;
        //loadConnectionsPresets();
        if (!PhotonNetwork.connected)
        {
            if (loadFirstPreset) onNewConnectionPreset(0);
            Btn_Multiplayer.interactable = false;
            startPUNConnect();
        }
    }
	
    /// <summary>
    /// Ici on mets à jour la vision du statut de connexion à ¨Photon
    /// </summary>
	void Update () {
        string context = ""
            + (PhotonNetwork.inRoom ? " inRoom" : "")
            + (PhotonNetwork.insideLobby ? " insideLobby" : "")
            + (PhotonNetwork.player.IsInactive ? " IsInactive" : "")
            ;
        text_ConnectionState.text = (context!=""?"["+ context + " ] ":"") + PhotonNetwork.networkingPeer.State.ToString();
    }

    //████████████████████████████████████████████████

    /// <summary>
    /// Méthode de tentative de connexion au serveur de Photon.
    /// </summary>
    public void startPUNConnect()
    {
        StartCoroutine(TryLogToRegionMaster());
    }
    IEnumerator TryLogToMaster()
    {
        while (!PhotonNetwork.connected)
        {
            if (MS_WebCloudConnection)
            {
                print("<color=green>Trying ConnectToRegion EU : " + MS_ConnVersion + " </color>");
                PhotonNetwork.ConnectToRegion(CloudRegionCode.eu, MS_ConnVersion);
            }
            else
            {
                print("PhotonNetwork.gameVersion: " + PhotonNetwork.gameVersion);
                print("PhotonNetwork.versionPUN: " + PhotonNetwork.versionPUN);
                print("<color=orange>Trying ConnectToMaster : " + MS_adress + ":" + MS_port + " " + MS_ConnVersion + " " + MS_protocol.ToString() + " </color>");
                PhotonNetwork.SwitchToProtocol(MS_protocol);
                PhotonNetwork.ConnectToMaster(MS_adress, MS_port, MS_appid, MS_ConnVersion);
            }
            yield return new WaitForSecondsRealtime(3f);
        }
        print("<color=green>Connected to master  : " + MS_adress + ":" + MS_port + " " + MS_ConnVersion + " " + MS_protocol.ToString() + " </color>");
    }

    public static IEnumerator TryLogToRegionMaster()
    {
        while (!PhotonNetwork.connected)
        {
                print("<color=green>Trying ConnectToRegion EU : " + "v1.0" + " </color>");
                PhotonNetwork.ConnectToRegion(CloudRegionCode.eu, "v1.0");
            yield return new WaitForSecondsRealtime(3f);
        }
        PlayerPrefs.SetString("playerName", "Guest" + Random.Range(1, 9999));
        PhotonNetwork.playerName = PlayerPrefs.GetString("playerName");
        print("<color=green>Connected to master region as "+ PhotonNetwork.playerName + ".</color>");

    }

    /// <summary>
    /// Une fois la connection établie : On affecte un nom au joueur et on authorise le joueur à se connecter à un serveur.
    /// </summary>
    public void OnConnectedToMaster()
    {
        Btn_Multiplayer.interactable = true;
        Input_Pseudo.interactable = true;

        if (wwwManager.instance.ServerStatus == wwwManager.serverStatus.Connected)
        {
            onPseudoChange(WebCredential.playerCredential.UserName);
        }
        else
        {
            PhotonNetwork.player.SetPlayerState(PlayerState.inMenu);
            if (PlayerPrefs.GetString("playerName") == "")
                onPseudoChange("Guest" + Random.Range(1, 9999));
            else
                Input_Pseudo.text = PlayerPrefs.GetString("playerName");
        }
        //PhotonNetwork.JoinLobby();  // this joins the "default" lobby
    }
    /// <summary>
    /// Fonction de fermeture de l'application
    /// </summary>
    public void CloseGame()
    {
        Application.Quit();
    }
    public void DisconnectPUN()
    {
        if (PhotonNetwork.inRoom) PhotonNetwork.LeaveRoom();
        if (PhotonNetwork.insideLobby) PhotonNetwork.LeaveLobby();
        if (PhotonNetwork.Server == ServerConnection.MasterServer) PhotonNetwork.Disconnect();
    }

    //████████████████████████████████████████████████


    /// <summary>
    /// Aide visuel par F2 pour la room du joueur.
    /// </summary>
    void OnGUI()
    {
        if (Input.GetKey(KeyCode.F2) && PhotonNetwork.inRoom)
        {   
            Rect centeredRect = new Rect(5, 45, Screen.width-10, (PhotonNetwork.playerList.Length+4) * 14 + 20);

            GUILayout.BeginArea(centeredRect, GUI.skin.box);
            {
                string tmp = string.Empty;
                tmp += "[ROOM INFOS]\n\n";
                tmp += "ROOM > " + PhotonNetwork.room   .Name
                        + " GAMESTATE: " + PhotonNetwork.room.GetAttribute<GameState>(RoomAttributes.GAMESTATE, GameState.GameState_error)
                        + (PhotonNetwork.room.GetAttribute<bool>(RoomAttributes.PLAYERSREADY, false) ? " PLAYERSREADY" : "")
                        + (PhotonNetwork.room.GetAttribute<bool>(RoomAttributes.PLAYERSCANSPAWN, false) ? " PLAYERSCANSPAWN" : "")
                        + (PhotonNetwork.room.GetAttribute<bool>(RoomAttributes.PRISONOPEN, false) ? " PRISONOPEN" : "")
                        + (PhotonNetwork.room.GetAttribute<bool>(RoomAttributes.IMMOBILIZEALL, false) ? " IMMOBILIZEALL" : "")
                        + " TS="+ (int)PhotonNetwork.ServerTimestamp
                        + " Ping: " + PhotonNetwork.GetPing()+"ms"
                        + "\n";
                foreach (PhotonPlayer p in PhotonNetwork.playerList)
                {
                    tmp +=
                        "PLAYER > " + p.NickName //SCORE,TEAM,PLAYERSTATE,ISIDLE,ISLAGGY
                        + " STATE: " + (p.GetPlayerState()).ToString()
                        + " ISALIVE: " + p.GetAttribute<bool>(PlayerAttributes.HASSPAWNED, false)
                        + (p.IsMasterClient? " MASTERCLIENT" : "")
                        + (p.GetAttribute<bool>(PlayerAttributes.ISREADY, false)? " ISREADY" : "")
                        + (p.GetAttribute<bool>(PlayerAttributes.ISLAGGY, false)?" ISLAGGY":"")
                        + (p.GetAttribute<bool>(PlayerAttributes.ISROOMADMIN, false)? " ISROOMADMIN" : "")
                        + (p.GetAttribute<bool>(PlayerAttributes.ISIDLE, false)? " ISIDLE" : "")
                        + (p.GetAttribute<bool>(PlayerAttributes.INPRISONZONE, false)? " INPRISONZONE" : "")
                        + (p.GetAttribute<bool>(PlayerAttributes.ISCAPTURED, false) ? " ISCAPTURED" : "")
                        + (p.GetAttribute<bool>(PlayerAttributes.ISIMMOBILIZED, false) ? " ISIMMOBLIZED" : "")
                        + " "+ p.GetAttribute<string>(PlayerAttributes.INZONE, "?")
                        + " SCORE: " + p.GetAttribute<int>(PlayerAttributes.SCORE, 0)
                        + " " + p.CustomProperties[PlayerAttributes.testKey]
                        + "\n";
                }
                tmp += "\n";
                // AFICHER TEAM ICI
                GUILayout.Label(tmp);
            }
            GUILayout.EndArea();
        }
    }

    /// <summary>
    /// Méthode qui modifie le preset de connection. Avec IP Port Socket.
    /// </summary>
    /// <param name="value"></param>
    public void onNewConnectionPreset(int value)
    {
        MS_adress = ConnectionsPreset[value].protocolAdress;
        AdressInput.text = MS_adress;
        MS_port = ConnectionsPreset[value].protocolDefaultPort;
        PortInput.text = MS_port.ToString();
        MS_WebCloudConnection = ConnectionsPreset[value].useWebCloudConnection;
        MS_ConnVersion = ConnectionsPreset[value].connectionVersion;
        ConnVersionInput.text = MS_ConnVersion.ToString();
        MS_protocol = ConnectionsPreset[value].protocolConn;
        switch (MS_protocol)
        {
            case ExitGames.Client.Photon.ConnectionProtocol.Udp:
                ProtocolDropdown.value = 0;
                break;
            case ExitGames.Client.Photon.ConnectionProtocol.Tcp:
                ProtocolDropdown.value = 1;
                break;
            case ExitGames.Client.Photon.ConnectionProtocol.WebSocket:
                ProtocolDropdown.value = 2;
                break;
            case ExitGames.Client.Photon.ConnectionProtocol.WebSocketSecure:
                ProtocolDropdown.value = 3;
                break;
        }
        if (MS_WebCloudConnection)
        {
            print("<color=blue>Preset = ConnectToRegion : " + MS_ConnVersion + " </color>");
        }
        else
        {
            print("<color=blue>Preset = ConnectToMaster : " + MS_adress + ":" + MS_port + " " + MS_ConnVersion + " " + MS_protocol.ToString() + " </color>");
        }

    }

    /// <summary>
    /// Ici on créé la liste dans l'interface du jeu.
    /// </summary>
    public void loadConnectionsPresets()
    {
        List<Dropdown.OptionData> ops = new List<Dropdown.OptionData>();
        foreach (MultiplayerProtocol item in ConnectionsPreset)
            ops.Add(new Dropdown.OptionData {text = item.protocolName });
        ConnectionsPresetDropdown.AddOptions(ops);
    }

    void OnDisconnectedFromMasterServer(NetworkDisconnection info){
        Btn_Multiplayer.interactable = false;
        Input_Pseudo.interactable = false;

    }
    void OnServerInitialized(){}
    void OnDisconnectedFromServer(NetworkDisconnection info){}
    void OnFailedToConnect(NetworkConnectionError error){}
    void OnFailedToConnectToMasterServer(NetworkConnectionError error){
        Btn_Multiplayer.interactable = false;
        Input_Pseudo.interactable = false;
    }
    void OnPlayerConnected(NetworkPlayer player){}
    void OnPlayerDisconnected(NetworkPlayer player){ }
    void OnDisconnectedFromPhoton(){Debug.LogWarning("OnDisconnectedFromPhoton");}
    private void OnConnectedToServer() { print("OnConnectedToServer : " + PhotonNetwork.connectionStateDetailed); }

    IEnumerator OnLeftRoom()
    {
        print("OnLeftRoom");
        while (PhotonNetwork.room != null || PhotonNetwork.connected == false) yield return 0;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    public void OnJoinedRoom()
    {
        print("OnJoinedRoom");
        Network.sendRate = 100;
    }
    public void OnCreatedRoom()
    {
        print("OnCreatedRoom");
    }
    public void onPseudoChange(string value)
    {
        if (value.Length>14) { Input_Pseudo.text = PlayerPrefs.GetString("playerName"); return; }

        PlayerPrefs.SetString("playerName", value);
        PhotonNetwork.playerName = value;
        Input_Pseudo.text = value;
    }

    //████████████████████████████████████████████████

    string SelectedRoom = "";


    /// <summary>
    /// Creation de serveur
    /// </summary>
    public void CreateRoom() { PhotonNetwork.CreateRoom("Serveur de " + PhotonNetwork.playerName, new RoomOptions() { MaxPlayers = 16, IsOpen = false }, TypedLobby.Default); }
    /// <summary>
    /// Tentative de connection à une room
    /// </summary>
    public void JoinRoom()
    {
        if (PhotonNetwork.connected && SelectedRoom != null && SelectedRoom != "")
        {
            PhotonNetwork.JoinRoom(SelectedRoom);
            PhotonNetwork.player.SetPlayerState(PlayerState.joiningRoom);
        }
    }
    /// <summary>
    /// Quitter la room
    /// </summary>
    public void ExitRoom() { if (PhotonNetwork.inRoom) PhotonNetwork.LeaveRoom(false); }

}

[System.Serializable]
public struct MultiplayerProtocol
{
    public string protocolName;
    public string protocolAdress;
    public int protocolDefaultPort;
    public ExitGames.Client.Photon.ConnectionProtocol protocolConn;
    public string connectionVersion;
    public bool useWebCloudConnection;
}
