using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BigStoryViewScript : StoryViewScript {

    public List<GameObject> sTagSlots; // Set in the editor
    public int currentSlot;

    protected new void Awake()
    {
        foreach (var sTagS in sTagSlots)
        {
            sTagS.GetComponent<Text>().enabled = false;
        }
        base.Awake();
    }

    protected new void Start()
    {
        if (GameManagerScript.instance.story.person == null)
        {
            Destroy(GameObject.Find("Person Element"));
            Destroy(GameObject.Find("Traits Box"));

            print("BigStoryView: Destroying unused Person element and Traits Box");
        }

        base.Start();
    }

    public override void addStoryTag(StoryTag sTag) // Add a story tag element to the view
    {
        if (currentSlot >= sTagSlots.Count)
        {
            print("Exceeded slots");
            return;
        }

        GameObject gObject = sTagSlots[currentSlot];
        var storyTagElement = gObject.GetComponent<StoryTagScript>();

        currentSlot++;

        print("gObject: " + gObject);
        print("StorytagElement: " + storyTagElement);
        if (gObject == null)
        {
            print("gObject was null!");
            addStoryTag(sTag);
            return;
        }
        if (storyTagElement == null)
        {
            print("StorytagElement was null, adding new one");
            storyTagElement = gObject.AddComponent<StoryTagScript>();
        }

        storyTagElement.enabled = true;
        storyTagElement.GetComponent<Text>().enabled = true;
        storyTagElement.setStoryTag(sTag);

        print("storyTagElement.enabled: " + storyTagElement.GetComponent<Text>().enabled);

        storyTagElements.Add(storyTagElement);
    }
}
