using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public enum NodeType { speech, response };
[Serializable]
public class SpeakEasyFile_NodeData
{
    //This contains the data for every individual node
    public NodeType nodeType = NodeType.speech;
    public string nodeID = "";
    public string stringReference = "";
    //public AudioClip speechAudio;
    public bool isEntryPoint = false;
    public int nodePriority = 0;
    public SpeakEasyLogics_Test scriptTestRef = null;
    public SpeakEasyLogics_Event scriptEventRef = null;
    //This is useless for the game, but useful for the SpeakEasy
    public Vector2 nodePosition;
    //This is a test
    public List<string> connectionData;
}

[Serializable]
[CreateAssetMenu(fileName = "SpeakEasyTree", menuName = "Elyse/SpeakEasy File")]
public class SpeakEasyFile : ScriptableObject
{
    public string localePath = "";

    public List<SpeakEasyFile_NodeData> nodeData = new List<SpeakEasyFile_NodeData>(); // list of each of our nodes data
}


