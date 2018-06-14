using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StartScreenSystemScript : EventSystemScript {

    public int playerCount = -1;
    public int ID = -1;
    public int sessionID = -1;
    public bool isConnected = false;

    public Text playerCountElement; // Defined in editor
    public Text IDElement; // Defined in editor
    public Text SessionElement; // Defined in editor

    void Start()
    {
        InvokeRepeating("slowUpdate", 0, 0.5f);
    }

    public void findNewNetworkMatch() // Called by pressing the "Session" element in the scene
    {
        GameManagerScript.instance.findNewNetworkMatch();
    }

    void slowUpdate() // Updates every half second
    {
        if (GameManagerScript.instance == null)
            return;

        if (GameManagerScript.instance.activePlayerCount != playerCount)
        {

            playerCount = GameManagerScript.instance.activePlayerCount;
        }
        if (GameManagerScript.instance.localPlayer != null &&
            GameManagerScript.instance.localPlayer.ID != ID)
        {

            ID = GameManagerScript.instance.localPlayer.ID;
        }
        if (GameManagerScript.instance.networkManager != null &&
            GameManagerScript.instance.networkManager.sessionID != sessionID)
        {

            sessionID = GameManagerScript.instance.networkManager.sessionID;
        }
        if (GameManagerScript.instance.networkManager.isConnected != isConnected)
        {
            print("StartScreen: isConnected Changed");
            isConnected = GameManagerScript.instance.networkManager.isConnected;
        }

        playerCountElement.text = isConnected ? playerCount.ToString() : "-";
        IDElement.text = isConnected ? ID.ToString() : "-Not Connected-";
        SessionElement.text = sessionID.ToString();
    }
}
