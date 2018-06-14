using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using System;
using System.Collections.Generic;
using UnityEngine.Networking.Match;

public class GameManagerScript : NetworkBehaviour {

    public static GameManagerScript instance = null;   // Static instance of GameManager which allows it to be accessed by any other script.

    public AssetManagerScript assetManager; // Should be given in editor
    public NetworkManagerScript networkManager; // Should be given in editor

    public Story story;

    private int currentScene;
    [SyncVar]
    public int playerCount; // Obsolete
    [SyncVar]
    public int activePlayerCount; // TODO: Less redundant way of communicating this between clients

    [SyncVar(hook = "OnNewTurn")]
    public int turnsPlayed; // Incremented every time a character or storytag is added by a player
    [SyncVar]
    public int currentRound; // One round means all players has gotten to add a StoryTag once
    [SyncVar]
    public int currentPlayer; // The player whose turn it is 
    public NetworkPlayerScript localPlayer; // Get connectionID from localPlayer.ID

    public StoryTag selectedStoryTag;
    public Camera mainCamera;
    AudioRecorder audioRecorder;
    private List<NetworkPlayerScript> playerList;
    
    public int storyTagNegativeIter;
    public int storyTagPositiveIter;

    public int maxRounds = 2;

    public bool isActivePlayer = true;
    public bool isStoryView = false;
    [SyncVar]
    public bool currentTurnIsPositive;
    private int lastStoryTagTurn = -1; // Used for ensuring only 1 Story Tag is added per turn

    void Awake () {

        print("GameManager Awake");

        if (instance != null && instance != this)
        {
            print("Destroying duplicate GameManager");
            Destroy(gameObject);
            return;
        }
        instance = this;

        turnsPlayed = -1;
        currentRound = 0;
        storyTagNegativeIter = 0;
        storyTagPositiveIter = 0;

        audioRecorder = new AudioRecorder();
        playerList = new List<NetworkPlayerScript>();

        story = new Story();

        if (isStoryView)
            isActivePlayer = false;

        LoadScene(SceneConstants.OFFLINE); // OFFLINE currently same as START
    }

    public override void OnStartServer()
    {
        print("Server Starting");
        base.OnStartServer();
    }

    public void registerPlayers(bool clientToServer = false)
    {
        if (!isServer && clientToServer)
        {
            if (clientToServer)
                localPlayer.CmdRegisterPlayer();
            return;
        }

        print("GameManager: Starting registering new players");

        var foundPlayers = new List<NetworkPlayerScript>(FindObjectsOfType<NetworkPlayerScript>());
        
        foreach (var player in playerList.ToArray())
        {
            if (!foundPlayers.Contains(player))
            {
                playerList.Remove(player);
                continue;
            }
        }

        activePlayerCount = 0;
        foreach (var player in foundPlayers)
        {
            int newPlayerID = -1;

            if (player.isActivePlayer)
            {
                activePlayerCount += 1;
                newPlayerID = getNewPLayerID();
            }

            if (!playerList.Contains(player))
            {
                player.ID = newPlayerID;
                playerList.Add(player);
                PrintLogger.printLog("GameManager (server): Registered player: " + newPlayerID);
            }
        }
    }

    private int getNewPLayerID()
    {
        var playerIDsInUse = new List<int>();
        foreach (var player in playerList)
        {
            playerIDsInUse.Add(player.ID);
        }

        int newPlayerID = 0;
        // Find the lowest playerID that isn't in use
        while (playerIDsInUse.Contains(newPlayerID))
        {
            newPlayerID += 1;
        }

        return newPlayerID;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        PrintLogger.printLog("GameManager: Client Starting");

        if (isActivePlayer)
            LoadScene(SceneConstants.START);
        else if (isStoryView)
            LoadScene(SceneConstants.STORY_SCREEN);
    }
    
    public void nextTurn()
    {
        if (!isServer)
        {
            PrintLogger.printLog("GameManager (client): Ended turn as player "+localPlayer.ID);
            localPlayer.CmdNextTurn();
            return;
        }

        // currentTurnIsPositive alternates every turn and again if even playerCount and round : P,N,|N,P
        currentTurnIsPositive = activePlayerCount % 2 == 0 && currentRound % 2 == 0 ?
            (turnsPlayed + 1) % 2 == 1 : turnsPlayed % 2 == 1;

        turnsPlayed += 1;
        currentPlayer = (turnsPlayed+1) % activePlayerCount; // Go through the players round-robin style:

        PrintLogger.printLog("GameManager (server): Next turn");
    }
    
    public void OnNewTurn(int newTurnsPlayed) // Updated on each client once turnsPlayed changes
    {turnsPlayed = newTurnsPlayed;

        if (turnsPlayed % activePlayerCount == 0)
            currentRound += 1;
        
        currentPlayer = (turnsPlayed + 1) % activePlayerCount; // Redundantly assign these on each client
        currentTurnIsPositive = activePlayerCount % 2 == 0 && currentRound % 2 == 0 ?
            (turnsPlayed + 1) % 2 == 1 : turnsPlayed % 2 == 1;

        if (currentTurnIsPositive)
            storyTagPositiveIter += 1;
        else
            storyTagNegativeIter += 1;

        if (!isActivePlayer)
        {
            if (isStoryView)
                LoadScene(SceneConstants.STORY_SCREEN, true); // Reload the view every new turn

            return;
        }

        PrintLogger.printLog("GameManager (client): Next turn; player: " + currentPlayer);

        if (audioRecorder.recordingsSaved >= 2)
        {
            // Trying to end and trim the third recording may crash, so try saving non-trimmed instead
            audioRecorder.endRecording(false);
            audioRecorder.saveAllRecordings();
        } else
        {
            audioRecorder.endRecording(); // Ends any ongoing audio recording
            audioRecorder.saveAllRecordings(); // Takes a second or two
        }


        // Everyone gets to play twice:
        if (currentRound > maxRounds)
        {
            PrintLogger.printLog("GameManager: Ending game (turnsplayed: "+turnsPlayed +")");
            if (isServer)
                RpcEndGame();
            else
                localPlayer.CmdEndGame();
            return;
        }

        PrintLogger.printLog("GameManager: Starting new turn (scene: " + currentScene + ", current player: " + currentPlayer
            + ", local player: "+ localPlayer.ID + ")");
        if (currentPlayer == localPlayer.ID)
        {
            audioRecorder.startRecording();
            LoadScene(SceneConstants.TURN_START);
        } else
        {
            LoadScene(SceneConstants.WAITING_FOR_PLAYERS);
        }
    }

    [ClientRpc]
    public void RpcEndGame()
    {
        assetManager.printStory(story);

        if (!isActivePlayer)
            return;

        LoadScene(SceneConstants.GAME_END);
    }

    public void nextScene()
    {
        if (currentScene+1 == SceneConstants.GAME_END)
        {
            localPlayer.CmdEndGame();
            return;
        } else if (currentScene+1 == SceneConstants.SELECT_PERSON)
        {
            startGame();
            return;
        }

        LoadScene(currentScene + 1);
    }

    public void previousScene()
    {
        LoadScene(currentScene - 1);
    }

    public void LoadScene(int newScene, bool reload=false)
    {
        // If reload is set to true, the method will load the scene even if it's already loaded
        if (newScene < 0 || currentScene == newScene && !reload)
        {
            PrintLogger.printLog("GameManager: Failed loading scene "+newScene+" (already loaded)");
            return;
        }

        if (isStoryView
            && newScene != SceneConstants.BASE
            && newScene != SceneConstants.STORY_SCREEN)
            return;

        if (currentScene != SceneConstants.BASE
            && currentScene != SceneConstants.WAITING_FOR_PLAYERS
            && newScene != SceneConstants.START
            && newScene < SceneConstants.SELECT_SOLUTION
            && currentPlayer != localPlayer.ID)
        {
            // TODO: Instead use game states to decide when a turn is in progress and when to start/end the game
            newScene = SceneConstants.WAITING_FOR_PLAYERS;
            PrintLogger.printLog("GameManager: Switching to waiting screen (currentPlayer: " + currentPlayer + ")");

        } else if (newScene == SceneConstants.SELECT_PERSON && turnsPlayed >= 1)
        {
            newScene = SceneConstants.TURN_START;
        }

        int scenesUnloaded = 0;
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (scene.buildIndex != 0)
            {
                SceneManager.UnloadSceneAsync(scene);
                scenesUnloaded++;
            }
        }

        PrintLogger.printLog("GameManager: Switching to scene " + newScene + " from scene " + currentScene
            + " (current turn: " + turnsPlayed + ", unloaded " + scenesUnloaded + " scenes)");

        SceneManager.LoadScene(newScene, LoadSceneMode.Additive);
        currentScene = newScene;
    }

    public void startGame()
    {
        if (localPlayer.ID != currentPlayer)
        {
            LoadScene(SceneConstants.WAITING_FOR_PLAYERS);
            return;
        }
        PrintLogger.printLog("GameManager: Starting game as player "+ localPlayer.ID);

        audioRecorder.startRecording(); 
        // Since starting and ending 3 recordings in one session sometimes crashes android, this one is commented out

        LoadScene(SceneConstants.SELECT_PERSON);
    }

    public void setPerson(Person person)
    {
        print("Converting person for transmission");
        setPerson(assetManager.getID(person));
    }

    public void setPerson(int personID)
    {
        if (!isServer)
        {
            print("Rerouting set person through player command");
            localPlayer.CmdSetPerson(personID);
            return;
        }
        print("Server: Setting person");

        RpcSetPerson(personID);

    }

    public void addStoryTag(StoryTag sTag, StoryLink sLink)
    {
        print("Converting storytag for transmission");
        var sTagID = assetManager.getID(sTag);

        var sLinkStruct = new StoryLinkStruct();
        sLinkStruct.endStoryTagID = sTagID;
        
        if (sLink.traitLink != null)
        {
            sLinkStruct.isLinkedToTrait = true;
            sLinkStruct.linkedElementID = assetManager.getID(sLink.traitLink);
        }
        else if (sLink.storyTagLink != null)
        {
            sLinkStruct.isLinkedToTrait = false;
            sLinkStruct.linkedElementID = assetManager.getID(sLink.storyTagLink);
        } else
        {
            return;
        }

        addStoryTag(sTagID, sLinkStruct);
    }

    public void addStoryTag(int sTagID, StoryLinkStruct sLinkStruct)
    {
        if (!isServer)
        {
            print("Rerouting adding story tag to local player command");
            localPlayer.CmdAddStoryTag(sTagID, sLinkStruct);
            return;
        }
        print("Adding storyTag");

        RpcAddStoryTag(sTagID, sLinkStruct);
    }

    [ClientRpc] // Called by the server to each client
    public void RpcSetPerson(int personID)
    {
        story.setPerson(AssetManagerScript.instance.personList[personID]);
    }

    [ClientRpc] // Called by the server to each client
    public void RpcAddStoryTag(int storyTagID, StoryLinkStruct sLink)
    {
        if (lastStoryTagTurn == turnsPlayed)
            return;

        lastStoryTagTurn = turnsPlayed;
        var sTag = AssetManagerScript.instance.getStoryTagByID(storyTagID);

        if (sLink.isLinkedToTrait)
        {
            story.addStoryTag(sTag,
                new StoryLink(sTag, AssetManagerScript.instance.traitList[sLink.linkedElementID]));
        }
        else
        {
            story.addStoryTag(sTag,
                new StoryLink(sTag, AssetManagerScript.instance.getStoryTagByID(sLink.linkedElementID)));
        }
    }

    public int getIdentifier()
    {
        try {
            return localPlayer.ID;
        } catch (NullReferenceException) {
            return -1;
        }
    }
    public string getFullIdentifier()
    {
        var stringID = "null";
        try
        {
            stringID = localPlayer.ID.ToString();
        } catch (NullReferenceException) { }

        return "Player "+ stringID + " Grp" + networkManager.sessionID +" "+ DateTime.Now.ToString("yyyy'-'MM'-'dd hh.mm");
    }

    public void findNewNetworkMatch()
    {
        PrintLogger.printLog("GameManager: Finding new session / match");
        networkManager.findNewMatch();
    }

    public void endAudioRecording() // Called from EventSystem on scene loead
    {
        PrintLogger.printLog("GameManager: Ending and Saving auding recording");

        audioRecorder.endRecording();  
        audioRecorder.saveAllRecordings();
    }
}

static class SceneConstants
{
    public const int BASE = 0;
    public const int START = 1;
    public const int SELECT_PERSON = 2;
    public const int TURN_START = 5;
    public const int WAITING_FOR_PLAYERS = 9;
    public const int SELECT_SOLUTION = 10;
    public const int GAME_END = 13;
    public const int STORY_SCREEN = 14;
    public const int OFFLINE = 1;
}