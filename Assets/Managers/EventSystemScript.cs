using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class EventSystemScript : MonoBehaviour {

    protected GameManagerScript gameManager = GameManagerScript.instance;
    
	void Start () {
        gameManager = GameManagerScript.instance;

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (scene.buildIndex == SceneConstants.GAME_END)
            {
                //gameManager.endAudioRecording();
            }
        }
	}
	
	void Update () {

    }

    public void nextScene()
    {
        var finishButton =  GameObject.Find("Finished Button");
        if (finishButton != null)
        {
            print("Destroying finish button");
            Destroy(finishButton.gameObject);

            Invoke("nextScene", 0.1f);
        }
        else
        {
            PrintLogger.printLog("EventSystem: Going to next scene");
            gameManager.nextScene();
        }


    }

    public void previousScene()
    {
        PrintLogger.printLog("EventSystem: Going to previous scene");
        gameManager.previousScene();
    }

    public void nextTurn()
    {
        var finishButton = GameObject.Find("Finished Button");

        if (finishButton != null)
        {
            print("Destroying finish button");
            Destroy(finishButton.gameObject);

            Invoke("nextTurn", 0.1f);
        } else
        {
            gameManager.nextTurn();

        }
    }

    public void setPlayerCount(int players)
    {
        GameManagerScript.instance.playerCount = players;
    }

    public void setPlayerCount(Dropdown playerCountDropdown)
    {
        GameManagerScript.instance.playerCount = playerCountDropdown.value+1;
    }

    public void toggleHidden(MaskableGraphic UIElement)
    {
        UIElement.color = new Color(UIElement.color.r, UIElement.color.g, UIElement.color.b, UIElement.color.a == 0f ? 255f : 0);
    }
}
