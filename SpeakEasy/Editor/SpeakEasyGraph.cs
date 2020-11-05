using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UI;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

using Button = UnityEngine.UIElements.Button;

public class SpeakEasyGraph : GraphView
{
    //
    // This script handles the actual logic of the "Graph".
    //

    //The default dimensions of the node
    public readonly Vector2 defaultNodeSize = new Vector2(200, 150);
    public readonly Color speechColor = new Color(0.78f, 0.2f, 0.2f);
    public readonly Color responseColor = new Color(0.2f, 0.2f, 0.78f);

    public Locale localText;

    private string path;

    //This is the CONSTRUCTOR for this specific graph class - the constructor handles the initalization logic on creation
    public SpeakEasyGraph()
    {
        styleSheets.Add(Resources.Load<StyleSheet>("SpeakEasyGraphSheet"));
        SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale); //Enables the ability to zoom in and out of the graph.

        //Few comforts.
        this.AddManipulator(new ContentDragger());
        this.AddManipulator(new SelectionDragger());
        this.AddManipulator(new RectangleSelector());
        this.AddManipulator(new FreehandSelector());

        GridBackground grid = new GridBackground();
        Insert(0, grid);
        grid.StretchToParentSize();
    }

    public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
    {
        //Handles the logic for determining which ports can be connected to.
        SpeakEasyNode startNode = (startPort.node as SpeakEasyNode);
        Debug.Log("Determining compatible ports for node: " + startNode.nodeID);
        List<Port> compatiblePorts = new List<Port>();
        Port startPortView = startPort; //startPort is the port that we are "holding".
        //OOOH GOD YOU ARE SO STUPID - you were making an array of Edges EACH time you searched for a port.. dummy.

        ports.ForEach((port) =>
        {
            Port availablePort = port; //availablePort are the available ports that we can connect to.

            //Do not allow the port to connect to itself through the same port or the same node.
            if (startPortView == availablePort || startPortView.node == availablePort.node)
            {
                return;
            }

            //Do not allow to connect to the port of the same direction
            if(startPortView.direction == availablePort.direction)
            {
                return;
            }

            //Prevent connection between Response nodes.
            if(startNode.nodeType == NodeType.response && (availablePort.node as SpeakEasyNode).nodeType == NodeType.response)
            {
                return;
            }
            //If no conditions were met, add as an avilable port.
            compatiblePorts.Add(port);
        });

        //Do not allow to connect to the same port twice.
        //We need to get the reference of this ports connections
        //List<Edge> currentConnections = startPort.connections;

        Debug.Log("Found " + compatiblePorts.Count + " eligible ports.");

        Debug.Log("Found " + startPortView.connections.ToArray<Edge>().Length + " connections.");
        //I can't help but consider there's an easier way to grab this information.
        foreach (Edge e in startPortView.connections.ToArray<Edge>())
        {
            if (startPortView.direction == Direction.Input)
            {
                SpeakEasyNode node = (e.output.node as SpeakEasyNode);
                Debug.Log("Found an output connection with node " + node.nodeID);
                for(int i = compatiblePorts.Count - 1; i >= 0; i--)
                {
                    if(node == compatiblePorts[i].node)
                    {
                        compatiblePorts.RemoveAt(i);
                    }
                }
            }

            else
            {
                SpeakEasyNode node = (e.input.node as SpeakEasyNode);
                Debug.Log("Found an input connection with node " + node.nodeID);
                for (int i = compatiblePorts.Count - 1; i >= 0; i--)
                {
                    if (node == compatiblePorts[i].node)
                    {
                        compatiblePorts.RemoveAt(i);
                    }
                }
            }
        }

        return compatiblePorts;
    }

    public void CreateNewNode(Vector2 position, NodeType nodeType)
    {
        //This creates a BRAND new node through the Add Speech/Response button
        SpeakEasyNode node = new SpeakEasyNode
        {
            nodeID = Guid.NewGuid().ToString(), //Generates the GUID for this node
            nodeType = nodeType
        };
        AddElement(InitializeNode(node, position, nodeType));
    }

    private void LoadNode(SpeakEasyFile_NodeData data)
    {
        Vector2 dataPosition = data.nodePosition;
        NodeType dataType = data.nodeType;
        SpeakEasyNode node = new SpeakEasyNode
        {
            nodeID = data.nodeID,
            nodeType = dataType,
            stringReference = data.stringReference,
            isEntryPoint = data.isEntryPoint,
            priority = data.nodePriority,
            scriptEvent = data.scriptEventRef,
            scriptTest = data.scriptTestRef,
        };

        AddElement(InitializeNode(node, dataPosition, dataType));
    }

    public SpeakEasyNode InitializeNode(SpeakEasyNode node, Vector2 position, NodeType nodeTypeChoice)
    {
        //Actual logic for the creation of the node
        //This is the UI layout

        //Set the color of the node so it's not super transparent.
        node.mainContainer.style.backgroundColor = new Color(0.25f, 0.25f, 0.25f, 0.75f);

        //Initialize the Label of the Node, determining if it's a SPEECH or RESPONSE node
        Label nodeTypeLabel = new Label();
        nodeTypeLabel.style.fontSize = 15;
        nodeTypeLabel.style.alignSelf = Align.Center;
        if (node.nodeType == NodeType.speech)
        {
            nodeTypeLabel.text = "SPEECH";
            node.titleContainer.style.backgroundColor = speechColor;

        }

        else
        {
            nodeTypeLabel.text = "RESPONSE";
            node.titleContainer.style.backgroundColor = responseColor;
        }

        node.titleContainer.Add(nodeTypeLabel);

        //The integer field, this field references the index in the Localization file. It's a read only field.
        IntegerField stringIndexField = new IntegerField("String Index:");
        stringIndexField.SetEnabled(false); //We shouldn't be able to directly edit this value.
        node.mainContainer.Add(stringIndexField);

        //This is the "text preview" which we morphed into a auto-localization file. The editor on save stores this data back into the specified XML file.
        TextField textPreviewField = new TextField();
        textPreviewField.multiline = true;
        textPreviewField.style.maxWidth = 350;
        textPreviewField.style.whiteSpace = WhiteSpace.Normal;
        textPreviewField.SetEnabled(false);
        textPreviewField.isDelayed = true;
        node.mainContainer.Add(textPreviewField);
        

        //adds an ID reference so we can see the GUID
        Label idReference = new Label(node.nodeID);
        node.mainContainer.Add(idReference);
        idReference.style.color = new Color(0.75f, 0.75f, 0.75f);


        //This is the stringRefField, aka the identifier for our string. This is what we store in our file data.
        TextField stringRefField = new TextField("String Reference:");
        stringRefField.isDelayed = true;
        stringRefField.RegisterValueChangedCallback(evt =>
        {
            node.stringReference = evt.newValue; //set the node's string reference to the new Value
            Debug.Log("Saving localization data");
            SetLocalizationIdentifier(node.stringIndex, evt.newValue); //Sets the localization identifier for the index if one is specified.
        });
        node.mainContainer.Add(stringRefField);
        stringRefField.SetValueWithoutNotify(node.stringReference);
        stringIndexField.SetValueWithoutNotify(FindStringIndex(node.stringReference)); //Look up our string reference identifier and return the index if one exists.
        //We need to set the stringIndex of the node too.
        node.stringIndex = stringIndexField.value; // set the node's internal value to the stringIndexField value.
        textPreviewField.SetValueWithoutNotify(GetStringIndex(node.stringIndex)); //Update the localized box auto match the .. you know what this does.

        textPreviewField.RegisterValueChangedCallback(evt =>
        {
            Debug.Log("Saving localization data");
            SetLocalizationText(node.stringIndex, evt.newValue); //If we change the text in this field, update the localization table to match our new entry.
        });

        //This button will give us a brand new spanking place in the localization file hashtag smiley face
        Button addNewTableEntryButton = null ; //I do not understand this logic...
        addNewTableEntryButton = new Button(() => AddNewTableEntry(addNewTableEntryButton, node, stringIndexField, textPreviewField))
        {
            text = "Add Table Entry"
        };
        
        if(localText == null)
        {
            addNewTableEntryButton.SetEnabled(false);
        }

        node.mainContainer.Add(addNewTableEntryButton);
        //Check if we already have this data
        if(CheckIfStringReferenceExists(node.stringReference))
        {
            //We already have our connection
            addNewTableEntryButton.SetEnabled(false);
            textPreviewField.SetEnabled(true);
        }

        //Serialize Save/Load for Audio Clips and Animation Trigger
        ObjectField audioField = new ObjectField("Audio Field");
        audioField.objectType = typeof(AudioClip);
        audioField.allowSceneObjects = false;
        audioField.SetEnabled(false);
        node.mainContainer.Add(audioField);

        TextField animationTriggerField = new TextField("Animation Trigger");
        animationTriggerField.SetEnabled(false);
        node.mainContainer.Add(animationTriggerField);

        UnityEngine.UIElements.Toggle entryPointToggle = new UnityEngine.UIElements.Toggle("Entry Point");
        entryPointToggle.RegisterValueChangedCallback(evt =>
        {
            node.isEntryPoint = evt.newValue;
        });
        node.mainContainer.Add(entryPointToggle);
        
        entryPointToggle.SetValueWithoutNotify(node.isEntryPoint);

        IntegerField priorityField = new IntegerField("Priority:");
        priorityField.RegisterValueChangedCallback(evt =>
        {
            node.priority = evt.newValue;
        });
        priorityField.SetValueWithoutNotify(node.priority);
        node.mainContainer.Add(priorityField);

        /*
        ObjectField testRef = new ObjectField("OnConditional");
        testRef.objectType = typeof(SpeakEasyLogics_Test);
        testRef.allowSceneObjects = false;
        node.mainContainer.Add(testRef);

        ObjectField eventRef = new ObjectField("OnFire");
        eventRef.objectType = typeof(SpeakEasyLogics_Event);
        eventRef.allowSceneObjects = false;
        node.mainContainer.Add(eventRef);
        */

        //adds a OnConditional reference
        ObjectField testRefField = new ObjectField("Test Script Ref:");
        testRefField.objectType = typeof(SpeakEasyLogics_Test);
        testRefField.allowSceneObjects = false;
        testRefField.RegisterValueChangedCallback(evt =>
        {
            node.scriptTest = (evt.newValue as SpeakEasyLogics_Test);
            //SpeakEasyNode startNode = (startPort.node as SpeakEasyNode);
        });

        node.mainContainer.Add(testRefField);
        testRefField.SetValueWithoutNotify(node.scriptTest);

        //adds a OnFire reference
        ObjectField eventRefField = new ObjectField("Event Script Ref:");
        eventRefField.objectType = typeof(SpeakEasyLogics_Event);
        eventRefField.allowSceneObjects = false;
        eventRefField.RegisterValueChangedCallback(evt =>
        {
            node.scriptEvent = (evt.newValue as SpeakEasyLogics_Event);
        });

        node.mainContainer.Add(eventRefField);
        eventRefField.SetValueWithoutNotify(node.scriptEvent);

        //This port is the IN connection, this is where we will lead the conversation TO. The Input can recieve a number of connections from conversation choices
        Port inputPort = GetPortInstance(node, Direction.Input, Port.Capacity.Multi);
        node.inputConnection = inputPort;
        inputPort.portName = "Connections";
        node.inputContainer.Add(inputPort);

        
        Port outputPort = GetPortInstance(node, Direction.Output, Port.Capacity.Multi);
        node.outputConnection = outputPort;
        outputPort.portName = "Responses";
        node.outputContainer.Add(outputPort);
        

        //
        //node.outputContainer.style.flexDirection <-- this is what I was looking for last night! controls the direction of the container?
        
        node.RefreshExpandedState();
        node.RefreshPorts();

        node.SetPosition(new Rect(position, defaultNodeSize));

        Button debugTestValues = new Button(() => PrintValues(node, inputPort, outputPort))
        {
            text = "Print Values"
        };

        node.mainContainer.Add(debugTestValues);

        return node;
    }

    private Port GetPortInstance(SpeakEasyNode node, Direction nodeDirection, Port.Capacity capacity = Port.Capacity.Multi)
    {
        //Returns an instance (look at you using big programmer words) of the Node. The "typeofFloat" is arbirtary here, because we have no intention of passing any data between the nodes itself

        return node.InstantiatePort(Orientation.Horizontal, nodeDirection, capacity, typeof(float));
    }

    private void PrintValues(SpeakEasyNode node, Port ingoing, Port outgoing)
    {
        Debug.Log("Our Node ID is: " + node.nodeID);
        Debug.Log("Our Node Type is: " + node.nodeType);
        Debug.Log("Are we an EntryPoint node?: " + node.isEntryPoint);
        Debug.Log("Our Node's priority is: " + node.priority);
        Debug.Log("Our String Reference is: " + node.stringReference);
        Debug.Log("Our Test Script reference ID is: " + node.scriptTest);
        Debug.Log("Our Event Script reference reference ID is: " + node.scriptEvent);

        Debug.Log("The number of incoming connections we have are: " + ingoing.connections.ToArray<Edge>().Length);
        Debug.Log("The number of outgoing connections we have are: " + outgoing.connections.ToArray<Edge>().Length);


        int i = 0;
        foreach (Edge e in ingoing.connections.ToArray<Edge>())
        {

            //We need to cast the node
            SpeakEasyNode output = (e.output.node as SpeakEasyNode);
            Debug.Log("Edge " + i + " recieves a speech from node: " + output.nodeID);

            i++;
        }
        

        i = 0;

        foreach (Edge e in outgoing.connections.ToArray<Edge>())
        {

            SpeakEasyNode input = (e.input.node as SpeakEasyNode);
            Debug.Log("Edge " + i + " leads to response node: " + input.nodeID);

            i++;
        }
    }

    public void SaveData(SpeakEasyFile file)
    {
        if(file != null)
        {
            List<Edge> edgeList = edges.ToList();
            List<SpeakEasyNode> nodeList = nodes.ToList().Cast<SpeakEasyNode>().ToList();

            Debug.Log("Found " + edgeList.Count + " edges in the Graph");
            Debug.Log("Found " + nodeList.Count + " nodes in the Graph");

            //Clear the list in the file
            file.nodeData.Clear();

            EditorUtility.SetDirty(file);

            foreach(SpeakEasyNode n in nodeList)
            {
                //We need to grab our connections first.
                List<string> savedConnectionData = new List<string>();
                foreach(Edge e in n.outputConnection.connections)
                {
                    //Retrieve the guid of each node.
                    SpeakEasyNode input = (e.input.node as SpeakEasyNode);
                    savedConnectionData.Add(input.nodeID);
                    Debug.Log("Saved connection starting at " + n.nodeID + " node and ending with " + input.nodeID + " node.");
                }


                file.nodeData.Add(new SpeakEasyFile_NodeData
                {
                    nodeType = n.nodeType,
                    nodeID = n.nodeID,
                    stringReference = n.stringReference,
                    isEntryPoint = n.isEntryPoint,
                    nodePriority = n.priority,
                    scriptTestRef = n.scriptTest,
                    scriptEventRef = n.scriptEvent,
                    nodePosition = n.GetPosition().position,
                    connectionData = savedConnectionData
                });
                Debug.Log("Saved node " + n.nodeID);
            }

            //We need to re-save the list
            localText.Save(file.localePath);

            /*
            foreach(Edge e in edgeList)
            {
                SpeakEasyNode input = (e.input.node as SpeakEasyNode);
                SpeakEasyNode output = (e.output.node as SpeakEasyNode);
                string startingPoint = output.nodeID;
                string endingPoint = input.nodeID;
                file.connectionData.Add(new SpeakEasyFile_ConnectionData
                {
                    originNodeID = startingPoint,
                    targetNodeID = endingPoint
                });

                Debug.Log("Saved connection starting at " + startingPoint + " node and ending with " + endingPoint + " node.");
            }
            */
        }
    }

    public bool GraphIsDirty(SpeakEasyFile file)
    {
        bool isDirty = false;
        //Compare data, returning a "isDirty" state.
        //We need a copy of the Nodes and Edges

        Debug.Log("Checking lenghts...");
        

        List<SpeakEasyNode> nodeList = nodes.ToList().Cast<SpeakEasyNode>().ToList();
        foreach (SpeakEasyNode n in nodeList)
        {
            Debug.Log(n.nodeID);
        }

        Debug.Log(nodeList.Count);
        Debug.Log(file.nodeData.Count);

        if (file.nodeData.Count == nodeList.Count)
        {
            //The length of the file's nodeData and nodeList in the graph are the same, keep going.
            //Now we need to compare individual node's data

            Debug.Log("Saved Data and NodeList appear to be of the same length");

            List<SpeakEasyFile_NodeData> data = file.nodeData;
            for(int i = 0; i < nodeList.Count; i++)
            {
                //Iterate through the graph's nodeList.
                if (nodeList[i].nodeType == data[i].nodeType &&
                    nodeList[i].nodeID == data[i].nodeID &&
                    nodeList[i].stringReference == data[i].stringReference &&
                    nodeList[i].isEntryPoint == data[i].isEntryPoint &&
                    nodeList[i].priority == data[i].nodePriority &&
                    nodeList[i].scriptTest == data[i].scriptTestRef &&
                    nodeList[i].scriptEvent == data[i].scriptEventRef &&
                    nodeList[i].GetPosition().position == data[i].nodePosition
                    )
                {
                    //Now we need to iterate through this node's outputs
                    Debug.Log("The Data in this node appears to be match the Data in the saved file.");

                    List<string> connectedNodes = new List<string>();
                    foreach (Edge e in nodeList[i].outputConnection.connections)
                    {
                        //Retrieve the guid of each node.
                        SpeakEasyNode input = (e.input.node as SpeakEasyNode);
                        connectedNodes.Add(input.nodeID);
                    }

                    //And run compare it to the saved data
                    if (connectedNodes.Count == file.nodeData[i].connectionData.Count)
                    {
                        Debug.Log("The connection data in this node has the same amount of connections as the saved file for this node.");

                        for (int j = 0; j < connectedNodes.Count; j++)
                        {
                            if (connectedNodes[j] == file.nodeData[i].connectionData[j])
                            {
                                Debug.Log("The connection data in this node matches the connection data in the saved file.");
                                Debug.Log("This file appears to be clean.");
                            }

                            else isDirty = true;
                        }
                    }

                    else isDirty = true;

                }

                else isDirty = true; //If any of the above isn't matching, then we made a change to the nodeList.
            }
        }

        else isDirty = true;

        return isDirty;
    }

    public void LoadData(SpeakEasyFile file)
    {
        //Now we transfer the saved data onto the graph

        //Debug.Log("Found " + file.connectionData.Count + " edges in the Saved File");
        Debug.Log("Found " + file.nodeData.Count + " nodes in the Saved File");
        Debug.Log("Loading Nodes");
        //Create a temporary list

        //Assign the localization data
        localText = Locale.Load(file.localePath);

        foreach (SpeakEasyFile_NodeData n in file.nodeData)
        {
            LoadNode(n); //We load the node Data and initalize the nodes back into place.
        }
        Debug.Log("Loading Connections");
        List<SpeakEasyNode> nodeList = nodes.ToList().Cast<SpeakEasyNode>().ToList(); //At this stage we should have the nodes in place

        //Now we need to retrieve a reference of the string list for the connections.

        foreach (SpeakEasyFile_NodeData n in file.nodeData)
        {
            string originID = n.nodeID; //Grab the origin node's ID
            foreach(string e in n.connectionData)
            {
                //iterate through the list of connections
                string targetID = e;
                //Now we need to grab the nodes
                foreach(SpeakEasyNode nS in nodeList)
                {
                    if(originID == nS.nodeID)
                    {
                        foreach(SpeakEasyNode nE in nodeList)
                        {
                            if(targetID == nE.nodeID)
                            {
                                Edge tempEdge = nS.outputConnection.ConnectTo(nE.inputConnection);
                                Add(tempEdge);
                                Debug.Log("Successfully added an edge");
                            }
                        }
                    }
                }
            }
        }
    }

    private int FindStringIndex(string identifier)
    {
        //like in the other one, if our identifier is empty or null, return with 999
        if(identifier == "" || identifier == System.String.Empty)
        {
            return 999;
        }

        for (int i = 0; i < localText.localizedText.Count; i++)
        {
            if(identifier == localText.localizedText[i].id)
            {
                return i; // This should return the index in the currentLocalization
            }
        }

        return 999; //a ridiculous value that will tells us there is no valid string index for us.
    }

    private void AddNewTableEntry(Button pressedButton, SpeakEasyNode node, IntegerField indexField, TextField textField)
    {
        //Check if we by any chance already have a reference.
        for(int i = 0; i < localText.localizedText.Count; i++)
        {
            if(node.stringReference == "" || node.stringReference == System.String.Empty)
            {
                EditorUtility.DisplayDialog("Oops!", "Sorry, cannot add a new entry without a proper Identifier.", "OK");
                return;
            }

            if (node.stringReference == localText.localizedText[i].id)
            {
                //We already have a point of reference
                pressedButton.SetEnabled(false);
                node.stringIndex = i;
                indexField.SetValueWithoutNotify(node.stringIndex);
                textField.SetValueWithoutNotify(GetStringIndex(node.stringIndex));
                textField.SetEnabled(true);

                return;
            }

        }

        pressedButton.SetEnabled(false); //We no longer need a new reference point.

        //Give this node a new string ID
        node.stringIndex = localText.localizedText.Count; //the Count is always +1 which means it will place the new reference at the right index.. right?
        localText.localizedText.Add(new LocalizedText() {id = node.stringReference}); //we add the new address for the list.

        //Now we need to connect the three fields
        indexField.SetValueWithoutNotify(node.stringIndex);
        textField.SetValueWithoutNotify(GetStringIndex(node.stringIndex));
        textField.SetEnabled(true);
    }

    private bool CheckIfStringReferenceExists(string identifier)
    {
        //We need to catch the situation where the identifier is blank and fail on purpose, otherwise multiple spawned nodes will get trapped with a specific identifier
        if(identifier == "" || identifier == System.String.Empty)
        {
            return false;
        }

        foreach (LocalizedText text in localText.localizedText)
        {
            if(text.id == identifier)
            {
                return true;
            }
        }

        return false;
    }

    private string GetStringIndex(int index)
    {
        Debug.Log("Searching for string at index " + index);
        if (index == 999)
        {
            return System.String.Empty;
        }

        return localText.localizedText[index].text;
    }

    private void SetLocalizationIdentifier(int index, string identifier)
    {
        if(index == 999)
        {
            Debug.Log("Index is invalid, and should not be used.");
            return;
        }

        localText.localizedText[index].id = identifier;
    }

    private void SetLocalizationText(int index, string text)
    {
        if (index == 999)
        {
            Debug.Log("Index is invalid, and should not be used.");
            return;
        }

        localText.localizedText[index].text = text;
    }
}
