using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class PrintLogger : MonoBehaviour {

    static PrintLogger instance;
    string filepath;
    private int currentUserID;

    void Start()
    {
        print("PrintLogger starting");
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        print("GameManager: " + GameManagerScript.instance);
        print("Game");
    }

    public static void printLog(object message)
    {
        if (instance == null ||
            instance.filepath == null ||
            instance.currentUserID != GameManagerScript.instance.getIdentifier())
        {
            if (!createLogFile())
            {
                print(message);
                return;
            }
        }

        var newMessage = GameManagerScript.instance.getFullIdentifier() + " | "
            + "Round "+ GameManagerScript.instance.currentRound + " | "
            + DateTime.Now.ToString() + " | "
            + message;

        print(newMessage);
        File.AppendAllText(instance.filepath, newMessage + Environment.NewLine);
    }

    public static bool createLogFile()
    {
        try
        {
            if (GameManagerScript.instance.getIdentifier() == -1)
                return false;

            var filename = "Log " + GameManagerScript.instance.getFullIdentifier() + ".txt";
            var filepath = Path.Combine(Application.persistentDataPath, filename);

            Directory.CreateDirectory(Path.GetDirectoryName(filepath));
            var sw = new StreamWriter(filepath);
            sw.WriteLine(" ");
            sw.Close();
            
            print("Created logfile at " + filepath);

            instance.currentUserID = GameManagerScript.instance.getIdentifier();
            instance.filepath = filepath;
            return true;
        }
        catch (NullReferenceException e)
        {
            print("Unable to create log file: No GameManager? (" + e.Message + ")");
            return false;
        }
    }
}
