using Boo.Lang;
using System;
using UnityEngine;

public class AudioRecorder
{
    AudioClip recording;
    string fileName;

    string micDevice;
    int micFreq = 11025; // Frequency lowered from 44100 to 11025 for compression
    List<AudioClip> unsavedRecList;
    public int recordingsSaved;

    public AudioRecorder()
    {
        unsavedRecList = new List<AudioClip>();

        micDevice = Microphone.devices[0];
        int devCapMin, devCapMax;
        Microphone.GetDeviceCaps(micDevice, out devCapMin, out devCapMax);

        PrintLogger.printLog("AudioRecorder: Using device '" + micDevice
            + "' (freqRange: " + devCapMin + "-" + devCapMax + ")");

        micFreq = Math.Min(devCapMax, Math.Max(devCapMin, micFreq)); // Clamp frequency to the device's range
    }

    public void startRecording()
    {
        PrintLogger.printLog("Starting audio recording");

        fileName = "AudioLog " + GameManagerScript.instance.getFullIdentifier()
            + " Turn " + GameManagerScript.instance.turnsPlayed;
        
        recording = Microphone.Start(micDevice, false, 500, micFreq);
        recording.name = fileName;
    }

    public void endRecording(bool trimToSize=true)
    {
        if (Microphone.IsRecording(micDevice))
        {
            PrintLogger.printLog("Ending audio recording");
            if (recording == null)
            {
                PrintLogger.printLog("Failed ending recording: recording became null");
                return;
            }

            if (trimToSize) // Sometimes causes crashes at 3 recordings on Android
            {

                //// Following code copied from 
                //// https://answers.unity.com/questions/544264/record-dynamic-length-from-microphone.html

                //Capture the current clip data
                var position = Microphone.GetPosition(micDevice);

                var recording2 = recording;

                float[] soundData = new float[1];
                
                try
                {
                    soundData = new float[recording2.samples * recording2.channels];
                    PrintLogger.printLog("End 0.5");
                    recording2.GetData(soundData, 0);
                } catch (Exception e)
                {
                    PrintLogger.printLog("Caught exception: " + e.Message);
                }

                //Create shortened array for the data that was used for recording
                var newData = new float[position * recording2.channels];

                //Copy the used samples to a new array
                for (int i = 0; i < newData.Length; i++)
                {
                    newData[i] = soundData[i];
                }

                //One does not simply shorten an AudioClip,
                //    so we make a new one with the appropriate length
                var newClip = AudioClip.Create(recording2.name, position, recording2.channels,
                                                recording2.frequency,  false, false);

                newClip.SetData(newData, 0);        //Give it the data from the old clip

                //Replace the old clip
                AudioClip.Destroy(recording);
                recording = newClip;

                //// End copied code
            }

            Microphone.End(micDevice);

            unsavedRecList.Add(recording);
        }
    }

    public void saveAllRecordings() // In case we want to save at another time than when we end the recording
    {
        PrintLogger.printLog("Saving "+ unsavedRecList.Count +" recordings");
        foreach(var rec in unsavedRecList)
        {
            SavWav.Save(rec.name, rec);

            recordingsSaved += 1;
            unsavedRecList.Remove(rec);
        }

    }
}