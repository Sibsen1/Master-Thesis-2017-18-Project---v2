using UnityEngine;
using System.Collections.Generic;
using System;

public class AssetManagerScript : MonoBehaviour {

    public static AssetManagerScript instance;

    public List<Person> personList;
    public List<StoryTag> storyTagList; // Used for common IDing across the network
    public List<StoryTag> storyTagListNeg;
    public List<StoryTag> storyTagListPos;
    
    public List<Trait> traitList;

    private Dictionary<string, Sprite> portraitSprites;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
    }

    void Start () {
        personList = new List<Person>();
        storyTagList = new List<StoryTag>();
        storyTagListNeg = new List<StoryTag>();
        storyTagListPos = new List<StoryTag>();
        traitList = new List<Trait>();

        portraitSprites = new Dictionary<string, Sprite>();
        
        createAssets();
	}

    private void createAssets()
    {
        foreach (var spr in Resources.LoadAll("Portraits", typeof(Sprite)))
        {
            portraitSprites.Add(spr.name, spr as Sprite);
        }

        traitList.Add(new Trait("Owns a social network company"));
        traitList.Add(new Trait("Has two children"));
        traitList.Add(new Trait("Becomes easily angered"));

        personList.Add(new Person("The  millionaire", true, portraitSprites["Toby"],
            traitList[0], traitList[1], traitList[2]));

        traitList.Add(new Trait("Works in a hotel"));
        traitList.Add(new Trait("Has a spouse"));
        traitList.Add(new Trait("Has a drinking problem"));

        personList.Add(new Person("The assistant manager", true, portraitSprites["Dean"],
            traitList[3], traitList[4], traitList[5]));

        traitList.Add(new Trait("Works in a cleaning company"));
        traitList.Add(new Trait("Has a friend in another country"));
        traitList.Add(new Trait("Has a bad police record"));

        personList.Add(new Person("The homeless person", false, portraitSprites["Laura"],
            traitList[6], traitList[7], traitList[8]));

        storyTagListNeg.Add(new StoryTag("angry"));
        storyTagListNeg.Add(new StoryTag("sad"));
        storyTagListNeg.Add(new StoryTag("ashamed"));
        storyTagListNeg.Add(new StoryTag("bored"));
        storyTagListNeg.Add(new StoryTag("confused"));
        storyTagListNeg.Add(new StoryTag("panicked"));
        storyTagListNeg.Add(new StoryTag("feel that everything is wrong"));
        storyTagListNeg.Add(new StoryTag("frustrated"));
        storyTagListNeg.Add(new StoryTag("hate"));
        storyTagListNeg.Add(new StoryTag("hopeless"));
        storyTagListNeg.Add(new StoryTag("afraid"));

        storyTagListPos.Add(new StoryTag("feel that everything is right"));
        storyTagListPos.Add(new StoryTag("joy"));
        storyTagListPos.Add(new StoryTag("interested"));
        storyTagListPos.Add(new StoryTag("hope"));
        storyTagListPos.Add(new StoryTag("appreciation"));
        storyTagListPos.Add(new StoryTag("kindness"));
        storyTagListPos.Add(new StoryTag("satisfied"));
        storyTagListPos.Add(new StoryTag("love"));
        storyTagListPos.Add(new StoryTag("energetic"));
        storyTagListPos.Add(new StoryTag("peaceful"));
        storyTagListPos.Add(new StoryTag("proud"));

        storyTagList.AddRange(storyTagListNeg);
        storyTagList.AddRange(storyTagListPos);

        // Shuffle the storyTag lists:
        for (int i = 0; i < storyTagListPos.Count; i++)
        {
            var temp = storyTagListPos[i];
            var randomIndex = UnityEngine.Random.Range(i, storyTagListPos.Count);

            storyTagListPos[i] = storyTagListPos[randomIndex];
            storyTagListPos[randomIndex] = temp;
        }
        for (int i = 0; i < storyTagListNeg.Count; i++)
        {
            var temp = storyTagListNeg[i];
            var randomIndex = UnityEngine.Random.Range(i, storyTagListNeg.Count);

            storyTagListNeg[i] = storyTagListNeg[randomIndex];
            storyTagListNeg[randomIndex] = temp;
        }
    }

    public int getID(Person person)
    {
        return personList.IndexOf(person);
    }

    public int getID(Trait trait)
    {
        return traitList.IndexOf(trait);
    }

    public int getID(StoryTag sTag)
    {
        return storyTagList.IndexOf(sTag);
    }

    public StoryTag getStoryTagByID(int ID)
    {
        return storyTagList[ID];
    }

    public List<Person> getPersonsList()
    {
        List<Person> output = new List<Person>();

        output.AddRange(personList);

        return output;
    }

    public List<StoryTag> get3NewStoryTags(bool positiveEmotion)
    {
        var index = positiveEmotion ? 
            GameManagerScript.instance.storyTagPositiveIter * 3 
            : GameManagerScript.instance.storyTagNegativeIter * 3; ;

        var output = new List<StoryTag>();
        var sTagList = positiveEmotion ? storyTagListPos : storyTagListNeg;
        
        for (int i = 0; i<3; i++)
        {
            while (GameManagerScript.instance.story.storytags.Contains(sTagList[index % sTagList.Count])
                && index < sTagList.Count * 2)
            {
                // Try to only add a new storyTag to the story
                index++;
            }
            output.Add(sTagList[index % sTagList.Count]);
            index++;
        }
        return output;        
    }

    public void printStory(Story story)
    {
        PrintLogger.printLog("Story:");
        PrintLogger.printLog(" Person: " + story.person.personName);

        PrintLogger.printLog(" StoryTags:");
        for (int i = 0; i < story.storytags.Count; i++)
        {
            PrintLogger.printLog("  " + (i+1) + ". '" + story.storytags[i].description + "'");
        }

        PrintLogger.printLog(" StoryLinks:");
        for (int i = 0; i < story.storylinks.Count; i++)
        {
            var sLink = story.storylinks[i];
            if (sLink.storyTagLink != null)
                PrintLogger.printLog("  " + (i+1) + ". sTag '" 
                    + sLink.storyTagLink.description
                    + "' to sTag '" + sLink.endStoryTag.description + "'");
            else
                PrintLogger.printLog("  " + (i+1) + ". Trait '"
                    + sLink.traitLink.description
                    + "' to sTag '" + sLink.endStoryTag.description + "'");
        }
    }
}
