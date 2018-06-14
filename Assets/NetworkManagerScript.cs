using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.Match;

public class NetworkManagerScript : NetworkManager {

    [Tooltip("The channel that this will use for finding and making rooms. It will ignore rooms on other channels.")]
    public int channel = 1;

    NetworkMatch networkMatcher;
    public bool isConnected = false;
    public int sessionID; // 'Unique' random identifier for the session/group
    private bool isHost;

    private string currentMatch; // Name of current match
    private List<string> blackListedMatches;

    public bool restartMode = false;

    public void Awake()
    {
        print("NetworkManager awake");

        blackListedMatches = new List<string>();
        networkMatcher = gameObject.AddComponent<NetworkMatch>();
    }

    public void Start()
    {
        print("Networkmanager starting");
        startConnection();
    }

    public void startConnection()
    {
        networkMatcher.ListMatches(0, 1, "", true, 0, 0, ConnectToOrCreateMatch);
        /*if (!restartMode)
        {
            networkMatcher.ListMatches(0, 1, "", true, 0, 0, ConnectToOrCreateMatch);
        }
        else
        {
            restartMode = false;
            createNewMatch();
        }*/
    }

    public void ConnectToOrCreateMatch(bool success, string extendedInfo, List<MatchInfoSnapshot> matches)
    {
        print("NetworkManager: ConnectToOrCreateMatch");
        if (success && matches != null)
        {

            var filteredMatches = new List<MatchInfoSnapshot>(matches); // Filter out matches based on channel and blacklist
            filteredMatches.RemoveAll(
                (MatchInfoSnapshot snapshot) => blackListedMatches.Contains(snapshot.name));
            filteredMatches.RemoveAll(
                (MatchInfoSnapshot snapshot) => !snapshot.name.Contains("ch"+channel));

            if (filteredMatches.Count > 0 && filteredMatches[0].currentSize > 0)
            {
                currentMatch = filteredMatches[0].name;
                print("NetworkManager: Joining match '"+ currentMatch  
                    +"' with " + filteredMatches[0].currentSize + " players");

                networkMatcher.JoinMatch(filteredMatches[0].networkId, "", "", "", 0, 0, OnMatchJoined);
            } else
            {
                createNewMatch();
            }
        }
        else
        {
            print("Failed listing matches: " + extendedInfo);
        }
    }

    private void createNewMatch()
    {
        sessionID = UnityEngine.Random.Range(1000, 10000); // Assumed to be short-term unique
        currentMatch = "Session " + sessionID + " (ch" + channel + ")";
        print("NetworkManager: Creating match named '" + currentMatch
            + "' (no available matches found)");

        networkMatcher.CreateMatch(currentMatch, 20, true, "", "mm.unet.unity3d.com", "", 0, 0, OnMatchCreate);
    }

    public override void OnStartHost()
    {
        base.OnStartHost();
        print("Networkmanager: Starting Host");
        isHost = true;
    }

    public override void OnServerAddPlayer(NetworkConnection conn, short playerControllerId)
    {
        base.OnServerAddPlayer(conn, playerControllerId);
        GameManagerScript.instance.playerCount = numPlayers;

        print("NM: Added player ID " + conn.connectionId + 
            "(addr: "+ conn.address +", total players: "+ numPlayers+")");
    }

    public override void OnClientConnect(NetworkConnection conn)
    {
        base.OnClientConnect(conn);
        PrintLogger.printLog("Client Connection Established");

        isConnected = true;

        if (currentMatch != null)
        {
            var reg = Regex.Match(currentMatch, "^Session ([0-9]+) \\(ch[0-9]+\\)$");
            if (reg.Success)
            {
                sessionID = int.Parse(reg.Groups[1].Value); 
                // Group 1 is the first set of parantheses in the regex, which is the session number
                return;
            }
        }

        print("NetworkManager: Invalid Match name '" + currentMatch + "'");
    }

    public override void OnClientDisconnect(NetworkConnection conn)
    {
        if (conn != null)
            base.OnClientDisconnect(conn);
        PrintLogger.printLog("Client disconnected, retrying...");

        if (!restartMode)
        {
            isConnected = false;
            isHost = false;
            currentMatch = null;
            sessionID = 0;
            GameManagerScript.instance.LoadScene(SceneConstants.OFFLINE);

            startConnection();
        }
    }

    public override void OnServerRemovePlayer(NetworkConnection conn, PlayerController player)
    {
        base.OnServerRemovePlayer(conn, player);
    }

    void OnApplicationQuit()
    {
        if (isHost && matchInfo != null)
        {
            // If the match is not destroyed, the room will stay visible for 20-30 seconds, confusing new clients

            // This method is not reliable, as the application quits before DestroyMatch is able to finish
            networkMatcher.DestroyMatch(matchInfo.networkId, 0, OnDestroyMatch);
            PrintLogger.printLog("NetworkManager: Application quit with client as host; destroying room");
        }
        networkMatcher.DropConnection(matchInfo.networkId, matchInfo.nodeId, 0, OnDropConnection);
        
        GameManagerScript.instance.isActivePlayer = false;
        GameManagerScript.instance.registerPlayers();
    }

    public override void OnDestroyMatch(bool success, string extendedInfo)
    {
        base.OnDestroyMatch(success, extendedInfo);
        print("Destroyed Match: " + extendedInfo);
    }

    public void findNewMatch()
    {

        if (currentMatch != null)
        {
            print("NetworkManager: Relisting and finding new match. Blacklisting match '" + currentMatch +"'");
            blackListedMatches.Add(currentMatch);

        } else
        {
            print("NetworkManager: Relisting and finding match. No match currently joined");
        }

        Shutdown();
        restartMode = true; // Will create new match when in restartmode
        Invoke("startConnection", 2); // Give the NetworkManager 2 seconds to wrap up before restarting
    }
}
