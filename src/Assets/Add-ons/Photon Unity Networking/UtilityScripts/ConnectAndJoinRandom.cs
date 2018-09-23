using System;
using UnityEngine;
using System.Collections;

/// <summary>
/// This script automatically connects to Photon (using the settings file),
/// tries to join a random room and creates one if none was found (which is ok).
/// </summary>
public class ConnectAndJoinRandom : Photon.MonoBehaviour
{
    /// <summary>Connect automatically? If false you can set this to true later on or call ConnectUsingSettings in your own scripts.</summary>
    public bool AutoConnect = true;

    public byte Version = 1;

    /// <summary>if we don't want to connect in Start(), we have to "remember" if we called ConnectUsingSettings()</summary>
    private bool ConnectInUpdate = true;


    public virtual void Start()
    {

        PhotonNetwork.sendRate = 100;
        PhotonNetwork.autoJoinLobby = false;    // we join randomly. always. no need to join a lobby to get the list of rooms.
        if (PhotonNetwork.connected)
        {
            PhotonNetwork.isMessageQueueRunning = true;

            if (PhotonNetwork.inRoom) PhotonNetwork.LeaveRoom();
            if (PhotonNetwork.insideLobby) PhotonNetwork.LeaveLobby();
        }
        else
        {
            Debug.Log("NOT PhotonNetwork.connected");
        }
    }

    public virtual void Update()
    {
        if (ConnectInUpdate && AutoConnect && !PhotonNetwork.connected)
        {
            ConnectInUpdate = false;
            PhotonNetwork.ConnectUsingSettings(Version + "." + SceneManagerHelper.ActiveSceneBuildIndex);
        }
    }


    // below, we implement some callbacks of PUN
    // you can find PUN's callbacks in the class PunBehaviour or in enum PhotonNetworkingMessage


    public virtual void OnConnectedToMaster()
    {
        PhotonNetwork.JoinRandomRoom();
    }
    
    public virtual void OnJoinedLobby()
    {
        PhotonNetwork.JoinRandomRoom();
    }

    public virtual void OnPhotonRandomJoinFailed()
    {
        PhotonNetwork.CreateRoom(null, new RoomOptions() { MaxPlayers = 10 }, null);
    }

    // the following methods are implemented to give you some context. re-implement them as needed.

    public virtual void OnFailedToConnectToPhoton(DisconnectCause cause)
    {
        Debug.LogError("Cause: " + cause);
    }

    public void OnJoinedRoom()
    {
    }
}
