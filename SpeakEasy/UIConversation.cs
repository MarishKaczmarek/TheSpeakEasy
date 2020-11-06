using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIConversation : MonoBehaviour
{
    //We'll handle the conversation logic here

    //UI COMPONENTS
    public GameObject uiInstance;
    public Text speechText;
    public GameObject responseButtonPrefab;
    public GameObject responseButtonContainer;
    [SerializeField] private List<GameObject> responseButtonInstance = new List<GameObject>();

    //CONVERSATION PARSER
    private SpeakEasyFile currentFile; //reference to the file we're about to use
    private Locale locale;

    //Core logic
    private void Start()
    {
        uiInstance.SetActive(false);
    }

    public void ActivateScreen(SpeakEasyFile file)
    {
        GameplayManager.gm.ToggleFocus(true);
        GameplayManager.gm.popupNumber++;
        uiInstance.SetActive(true);

        currentFile = file;
        if(currentFile == null)
        {
            DeactivateScreen();
            Debug.Log("One of the required references is not made.");
            return;
        }

        locale = Locale.Load(file.localePath);
        if(locale == null)
        {
            Debug.Log("Unable to find the localization for this conversation.");
        }

        EnterConversation();
    }

    public void DeactivateScreen()
    {
        ClearScreen();

        uiInstance.SetActive(false);
        GameplayManager.gm.popupNumber--;
        GameplayManager.gm.ToggleFocus(false);

        currentFile = null;
        locale = null;
    }

    private void ClearScreen()
    {
        speechText.text = System.String.Empty;

        for (int i = responseButtonInstance.Count - 1; i >= 0; i--)
        {
            Destroy(responseButtonInstance[i]);
        }

        responseButtonInstance.Clear();
    }

    // Conversation Parser Logic
    private void JumpToSpeechNode(SpeakEasyFile_NodeData node)
    {
        //We are assuming that the conditional fired and returned TRUE.
        if (node.nodeType != NodeType.speech)
        {
            Debug.Log("Trying to access a non speech node.");
            DeactivateScreen();
        }
        //This is how we traverse conversation
        //Clear the previous node data
        ClearScreen();

        //Set the Speech Text to this:
        speechText.text = Locale.Lookup(locale, node.stringReference);

        //Execute any OnFire here

        //We need to populate our responses (if any). 
        PopulateResponseList(node.connectionData);
    }

    private void EnterConversation()
    {
        //This is fired on ActivateScreen, when we start a conversation with someone
        //We need to determine our entry points.
        List<SpeakEasyFile_NodeData> potentialNodes = new List<SpeakEasyFile_NodeData>();
        foreach(SpeakEasyFile_NodeData node in currentFile.nodeData)
        {
            if(node.isEntryPoint)
            {
                potentialNodes.Add(node);
            }
        }

        //We need to sort the potentialNodes by priority.

        if(potentialNodes.Count > 0)
        {
            //We have our list of nodes, now we need to figure out which of the nodes we are to start up.
            potentialNodes.Sort((p1, p2) => p2.nodePriority.CompareTo(p1.nodePriority));
            foreach (SpeakEasyFile_NodeData node in potentialNodes)
            {
                //Execute the OnConditional here.
                bool shouldReveal = true;
                if(node.scriptTestRef != null)
                {
                    shouldReveal = node.scriptTestRef.OnTest();
                }
                if(shouldReveal)
                {
                    JumpToSpeechNode(node);
                    return;
                }
            }

            //If for some reason we've gone through all the nodes, we need to exit 
            DeactivateScreen();
        }
        
        else
        {
            Debug.Log("The conversation file did not specify any EntryPoints.");
            DeactivateScreen();
        }
    }

    public void DetermineNextNode(List<string> potentialNodesIDs)
    {
        //This is fired on Buttons to determine which Speech node to move on to.
        //If there are no potential nodes, exit

        List<SpeakEasyFile_NodeData> potentialNodes = GetPotentialNodes(potentialNodesIDs);

        //Check if we have any potential nodes.
        if (potentialNodes.Count > 0)
        {
            foreach(SpeakEasyFile_NodeData node in potentialNodes)
            {
                //Run our conditional here
                bool shouldReveal = true;
                if (node.scriptTestRef != null)
                {
                    shouldReveal = node.scriptTestRef.OnTest();
                }

                if (shouldReveal)
                {
                    JumpToSpeechNode(node);
                    return;
                }
            }

            //If we are unable to get to any potential nodes, we need to exit the conversation.
            DeactivateScreen();
        }

        //If we don't have any potential nodes, exit.
        else
        {
            DeactivateScreen();
        }
    }

    private void PopulateResponseList(List<string> potentialNodesIDs)
    {
        //What needs to happen here, we need to know how many responses we need
        //Check if we have any response
        //Good, so we know we have
        //We need to get our list of response nodes.


        //We need to catch the possibility that a Speech Node leads to another Speech Node.

        // REMEMBER :
        // As of now, the generated Responses do not react to priority, so we need to control that, this will be particularly important
        // because it means that the priority on the Speech to Speech node won't work properly.

        List<SpeakEasyFile_NodeData> potentialNodes = GetPotentialNodes(potentialNodesIDs);

        if(potentialNodes.Count > 0)
        {
            float x = 0;
            float y = 0; //the location of the button.

            foreach (SpeakEasyFile_NodeData node in potentialNodes)
            {
                if (node.nodeType == NodeType.response)
                {
                    //Run the conditional here.
                    bool shouldReveal = true;
                    if (node.scriptTestRef != null)
                    {
                        shouldReveal = node.scriptTestRef.OnTest();
                    }

                    if (shouldReveal)
                    {
                        InstantiateResponseButton(node, x, y);
                        y = y - 30;
                    }

                    //We don't do anything to buttons we can't "see".
                }
            }

            if (responseButtonInstance.Count == 0)
            {
                //This should detect that there are no buttons to be pressed, so our next node must be a SpeechNode.

                //Therefore,
                foreach (SpeakEasyFile_NodeData node in potentialNodes)
                {
                    if (node.nodeType == NodeType.speech)
                    {
                        //Run the conditional here.
                        bool shouldReveal = true;
                        if (node.scriptTestRef != null)
                        {
                            shouldReveal = node.scriptTestRef.OnTest();
                        }

                        if (shouldReveal)
                        {
                            StartCoroutine(MoveToNextSpeechNode(node));
                            return;
                        }
                    }
                }

                //If we can't find any suitable node to jump to, end conversation instead.
                StartCoroutine(EndConversation());
            }
        }

        else
        {
            StartCoroutine(EndConversation()); //end the conversation when the speaker finished speaking.
        }
        
    }

    private void InstantiateResponseButton(SpeakEasyFile_NodeData data, float x, float y)
    {
        //We are assuming that the button has returned true on it's onConditional
        GameObject buttonInstance = Instantiate(responseButtonPrefab, new Vector3(x, y, 0), Quaternion.identity);
        buttonInstance.transform.SetParent(responseButtonContainer.transform, false);

        responseButtonInstance.Add(buttonInstance);

        //Now we populate the data and behaviour of the button itself
        UIConversationResponseButton logic = buttonInstance.GetComponent<UIConversationResponseButton>();
        if(logic == null)
        {
            Debug.Log("Something has gone terribly wrong and we are unable to find the logic for the button.");
            return;
        }

        logic.label.text = Locale.Lookup(locale, data.stringReference); //Localise our response text.
        logic.connectionData = data.connectionData; //Assign the connection data reference to the button itself.
    }

    private List<SpeakEasyFile_NodeData> GetPotentialNodes(List<string>potentialNodes)
    {
        List<SpeakEasyFile_NodeData> list = new List<SpeakEasyFile_NodeData>();

        foreach(string targetID in potentialNodes)
        {
            foreach(SpeakEasyFile_NodeData node in currentFile.nodeData)
            {
                if(targetID == node.nodeID)
                {
                    list.Add(node);
                }
            }
        }

        //Once we have our list, prioritize it.
        list.Sort((p1, p2) => p2.nodePriority.CompareTo(p1.nodePriority));

        foreach(SpeakEasyFile_NodeData node in list)
        {
            Debug.Log("---");
            Debug.Log("Found Node " + node.nodeID);
            Debug.Log("with priority " + node.nodePriority);
            Debug.Log("---");
        }

        return list;
    }

    IEnumerator MoveToNextSpeechNode(SpeakEasyFile_NodeData node)
    {
        yield return new WaitForSeconds(3f);
        JumpToSpeechNode(node);
    }

    IEnumerator EndConversation()
    {
        yield return new WaitForSeconds(3f); //After three seconds, exit the conversation
        DeactivateScreen();
    }
}
