
using UnityEngine.UI;

public class StoryTagSelectSystem : EventSystemScript
{
    public StoryTagScript storyTagElement1;
    public StoryTagScript storyTagElement2;
    public StoryTagScript storyTagElement3;

    void Awake () {
        var storyTags = GameManagerScript.instance.assetManager.get3NewStoryTags(GameManagerScript.instance.currentTurnIsPositive);

        storyTagElement1.setStoryTag(storyTags[0]);
        storyTagElement2.setStoryTag(storyTags[1]);
        storyTagElement3.setStoryTag(storyTags[2]);

        PrintLogger.printLog("StoryTag Select: Options: '" + 
            storyTags[0].description + "', '" + storyTags[1].description + "', '" + storyTags[2].description + "'");
    }

    public void selectStoryTagElement(StoryTagScript storyTagElement)
    {
        GameManagerScript.instance.selectedStoryTag = storyTagElement.StoryTagObject;

        PrintLogger.printLog("selected story tag:");
        PrintLogger.printLog(storyTagElement.StoryTagObject.description);

        nextScene();
    }
}
