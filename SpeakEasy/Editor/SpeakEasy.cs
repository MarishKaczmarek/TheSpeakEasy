using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using System.IO;

public class SpeakEasy : EditorWindow
{
    //
    //This is the main EDITOR script, it defines and creates the Editor Element.
    //

    private string pathToResources = "Assets/Modules/SpeakEasy/Resources/";
    private string conversationFolder = "Conversations";
    private SpeakEasyFile currentFile;

    private SpeakEasyGraph graph;

    //UI Elements
    private Toolbar toolbar;

    private TextField fileNameLabel;
    private Button saveButton;
    private Button loadButton;
    private Button addSpeechButton;
    private Button addResponseButton;
    private Button displayLocalizationList;

    [MenuItem("Elyse/SpeakEasy")]
    public static void CreateSpeakEasyWindow()
    {
        //
        //This handles the creation of the window.
        //
        SpeakEasy window = GetWindow<SpeakEasy>();
        window.titleContent = new GUIContent("SpeakEasy");
    }

    private void OnEnable()
    {
        //Fired when we open the Window
        //ConstructGraph();
        GenerateToolbar();
    }

    private void OnDisable()
    {
        if(graph != null)
        {
            rootVisualElement.Remove(graph);
        }
    }

    private void ConstructGraph()
    {
        graph = new SpeakEasyGraph();
        graph.StretchToParentSize();
        rootVisualElement.Add(graph);
        //When constructin the graph, make sure that the toolbar is pushed forward
        toolbar.PlaceInFront(graph);
        //renable controls
        saveButton.SetEnabled(true);
        addSpeechButton.SetEnabled(true);
        addResponseButton.SetEnabled(true);

    }

    private void DeconstructGraph()
    {
        rootVisualElement.Remove(graph);
        graph = null;
        saveButton.SetEnabled(false);
        addSpeechButton.SetEnabled(false);
        addResponseButton.SetEnabled(false);
    }

    private void GenerateToolbar()
    {
        //
        //This handles the initalization of the toolbar
        //
        toolbar = new Toolbar();

        //Conversation File Name text field
        /*
        TextField conversationNameTextField = new TextField("Conversation Name:");
        conversationNameTextField.SetValueWithoutNotify(fileName);
        conversationNameTextField.MarkDirtyRepaint();
        conversationNameTextField.RegisterValueChangedCallback(evt => fileName = evt.newValue);
        toolbar.Add(conversationNameTextField);

        //Save and Open Buttons
        

        //Add Node Button
        toolbar.Add(new Button(() => AddNewNode()) { text = "Add New Node" });
        */

        //What tools do we need
        fileNameLabel = new TextField("Conversation File:");
        fileNameLabel.SetValueWithoutNotify("");
        fileNameLabel.SetEnabled(false);
        toolbar.Add(fileNameLabel);
        fileNameLabel.SetValueWithoutNotify("No file loaded");

        ToolbarMenu fileListMenu = new ToolbarMenu();
        fileListMenu.SetEnabled(false); //Disable it until we are positive we have files
        PopulateFileMenuDropdown(fileListMenu);
        toolbar.Add(fileListMenu);

        saveButton = new Button(() => SaveConversation()) { text = "SAVE" };
        loadButton = new Button(() => LoadConversation()) { text = "LOAD" };
        addSpeechButton = new Button(() => AddNewNode(NodeType.speech)) { text = "ADD SPEECH" };
        addResponseButton = new Button(() => AddNewNode(NodeType.response)) { text = "ADD RESPONSE" };
        displayLocalizationList = new Button(() => DisplayLocalizationList()) { text = "DISPLAY LOCALIZATION LIST"};
        //Button checkLocationButton = new Button(() => CheckLocation()) { text = "CHECK LOCATION "};
        //Button checkIfDirty = new Button(() => RunDirtTest()) {text = "CHECK IF FILE IS DIRTY"};

        saveButton.SetEnabled(false);
        loadButton.SetEnabled(false);
        addSpeechButton.SetEnabled(false);
        addResponseButton.SetEnabled(false);

        toolbar.Add(saveButton);
        toolbar.Add(loadButton);
        toolbar.Add(addSpeechButton);
        toolbar.Add(addResponseButton);
        toolbar.Add(displayLocalizationList);
        //toolbar.Add(checkLocationButton);
        //toolbar.Add(checkIfDirty);

        toolbar.style.backgroundColor = new Color(0.25f, 0.25f, 0.25f, 1); //until we make a styleSheet

        rootVisualElement.Add(toolbar); // Adds the actual toolbar to the Editor Window
    }

    private void PopulateFileMenuDropdown(ToolbarMenu dropdown)
    {
        if (AssetDatabase.IsValidFolder(pathToResources + conversationFolder))
        {
            DirectoryInfo directory = new DirectoryInfo(pathToResources + conversationFolder);
            FileInfo[] fileInfo = directory.GetFiles("*.*", SearchOption.AllDirectories);
            bool filesFound = false;
            foreach (FileInfo file in fileInfo)
            {
                if(file.Extension == ".asset")
                {
                    string directoryFileName = file.Name.Remove(file.Name.Length - 6);
                    filesFound = true;
                    dropdown.menu.AppendAction(directoryFileName, evt => LoadFile(directoryFileName));
                    
                }
            }

            if(filesFound)
            {
                dropdown.SetEnabled(true);
            }

            else
            {
                Debug.Log("No files found at " + pathToResources + conversationFolder);
                dropdown.SetEnabled(false);
            }
        }

        else
        {
            Debug.Log("Unable to find the CONVERSATION folder at " + pathToResources + conversationFolder);
            dropdown.SetEnabled(false);
        }
    }

    private void LoadFile(string loadedFileName)
    {
        Debug.Log("THIS FIRES WHEN!?");
        //This is fired when we change the current File from a dropdown
        //So we need to run the dirt test here too
        if(RunDirtTest())
        {
            return;
        }

        //If we have a graph loaded, deconstruct it
        if(graph != null)
        {
            DeconstructGraph();
        }

        Debug.Log("Attempting to Load File " + loadedFileName);
        currentFile = Resources.Load<SpeakEasyFile>(conversationFolder + "/" + loadedFileName);
        if(currentFile == null)
        {
            EditorUtility.DisplayDialog("ERROR!", "Unable to find the file at " + pathToResources + conversationFolder, "Ok.");
        }

        else
        {
            fileNameLabel.SetValueWithoutNotify(loadedFileName);
            loadButton.SetEnabled(true);
        }
    }

    private void SaveConversation()
    {
        //The logic for the SaveConversation Button
        graph.SaveData(currentFile);
    }

    private void LoadConversation()
    {
        //The logic for the LoadConversation Button
        //Only construct graph, if there is a file loaded
        //We need to check if we have a graph already loaded
        //Let's run a test first
        Debug.Log("Preparing to unload conversation");
        if(RunDirtTest())
        {
            return;
        }

        Debug.Log("Alright, moving on.");
        //First we need to deconstruct the graph if one is loaded
        if (graph != null)
        {
            DeconstructGraph();
        }

        //And reconstruct it.
        if (currentFile != null)
        {
            ConstructGraph();
            graph.LoadData(currentFile);
        }

        else
        {
            EditorUtility.DisplayDialog("Unable to load Graph", "Can't construct graph, because no file is specified!", "OK");
        }
    }

    private void AddNewNode(NodeType nodeType)
    {
        //The logic for the AddNewNode Button
        graph.CreateNewNode(GetViewPortLocation(), nodeType);
    }

    private Vector2 GetViewPortLocation()
    {
        //This is finicky as fuck, mostly because viewTransform just acts oddly

        //Hold up a second... let's grab the base value
        float x = (graph.viewTransform.position.x * -1) + (this.position.width / 2);
        float y = (graph.viewTransform.position.y * -1) + (this.position.height / 2);

        //And then get half

        return new Vector2(x, y);
    }

    private void DisplayLocalizationList()
    {
        if(currentFile != null && graph != null)
        {
            foreach(LocalizedText text in graph.localText.localizedText)
            {
                Debug.Log("---");
                Debug.Log("ID: " + text.id);
                Debug.Log("Text: " + text.text);
                Debug.Log("---");
            }
        }
    }

    private bool RunDirtTest()
    {
        //First we need to see if we have a file loaded
        if (currentFile != null)
        {
            //Then we need to check if we have a graph loaded
            if (graph != null)
            {
                //Then we run the test
                if (graph.GraphIsDirty(currentFile))
                {
                    if (!EditorUtility.DisplayDialog("Warning", "Loading a new file will make you lose all unsaved changes to this conversation. Continue?", "Yes", "No"))
                    {
                        Debug.Log("We pressed NO.");
                        return true;
                    }

                    else
                    {
                        Debug.Log("We pressed YES.");
                        return false;
                    }
                }

                else return false;
            }

            else return false;
        }

        else return false;
    }
}
