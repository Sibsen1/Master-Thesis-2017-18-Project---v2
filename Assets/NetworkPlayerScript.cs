using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class NetworkPlayerScript : NetworkBehaviour
{
    [SyncVar(hook = "OnNewID")]
    public int ID;
    [SyncVar]
    public bool isActivePlayer; // Is set to false if this player should not be part of the gameplay

    public override void OnStartLocalPlayer()
    {
        var gameManager = GameManagerScript.instance;

        base.OnStartLocalPlayer();

        print("Starting local player (isactive: "+gameManager.isActivePlayer+")");

        gameManager.localPlayer = this;

        CmdSetIsActive(gameManager.isActivePlayer);

        //print(GameManagerScript.instance.playerCount);

        //GameManagerScript.instance.playerCount = GameManagerScript.instance.networkManager.numPlayers;
        //GameManagerScript.instance.localPlayer = GameManagerScript.instance.networkManager.numPlayers - 1;

        //print(GameManagerScript.instance.networkManager.numPlayers);
        //print(GameManagerScript.instance.networkManager.client.connection.playerControllers.Count);
    }

    public void nextTurn()
    {
        print("Player: Next turn");
        CmdNextTurn();
    }

    [Command]
    public void CmdNextTurn()
    {
        PrintLogger.printLog("NetworkPlayer: Going to next turn/player");

        GameManagerScript.instance.nextTurn();

        // Moved to gameManager - now this command is just to make sure the gameManager runs its method on the server

        /*gameManager.turnsPlayed += 1;
        gameManager.currentPlayer = gameManager.currentPlayer >= gameManager.playerCount - 1 ? 0 : gameManager.currentPlayer + 1;
        // Go through the players round-robin style

        if (gameManager.turnsPlayed >= 5)
        {
            gameManager.RpcEndGame();
            return;
        }*/
    }

    [Command]
    public void CmdSetIsActive(bool isActive)
    {
        print("Player: Set active: " + isActive);
        isActivePlayer = isActive;
        GameManagerScript.instance.registerPlayers();
    }

    [Command]
    public void CmdRegisterPlayer()
    {
        GameManagerScript.instance.registerPlayers();
    }

    [Command]
    public void CmdSetPerson(int personID)
    {
        print("CmdSetPerson");
        GameManagerScript.instance.setPerson(personID);
    }

    [Command]
    public void CmdAddStoryTag(int sTagID, StoryLinkStruct sLinkStruct)
    {
        print("CmdAddStoryTag");
        //gameManager.addStoryTag(sTagID, sLinkStruct);

        GameManagerScript.instance.RpcAddStoryTag(sTagID, sLinkStruct);
    }

    [Command]
    public void CmdEndGame()
    {
        print("CmdEndGame");
        GameManagerScript.instance.RpcEndGame();
    }

    public void OnNewID(int newID)
    {
        PrintLogger.printLog("NetworkPlayer: Registered Player ID: " + newID);
        ID = newID;
    }
}