using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Experimental.GraphView;


public class SpeakEasyNode : Node
{
    //This our core of Node Data,

    public NodeType nodeType = NodeType.speech; //Type of node to use. If Speech, it is the main "talking". the Response, is the choice the player can make.
    public string nodeID; //the ID of our NODE in the form of a GUID
    public bool isEntryPoint; //is this the "first" speech (initiator)
    public int priority = 0; //The priority used to determine which entry point to use when starting a conversation.
    public int stringIndex = 999; //the index in the localization file.
    public string stringReference = System.String.Empty; //the string reference that will translate to the actual text to be displayed (localization and stuff)
    public SpeakEasyLogics_Test scriptTest = null; //the script reference point that fires when evaluating if this Node displays or not.
    public SpeakEasyLogics_Event scriptEvent = null; //the script reference point that fires when this displays or is selected.

    public Port inputConnection;
    public Port outputConnection;
}
