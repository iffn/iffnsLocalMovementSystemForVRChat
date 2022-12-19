using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;
//using iffnsStuff.iffnsVRCStuff.DebugOutput;

namespace iffnsStuff.iffnsVRCStuff.iffnsLocalMovementSystemForVRChat
{
    /*
    General information:
    --------------------
    - Local movement system made by iffn
    - GitHub repository: https://github.com/iffn/iffnsLocalMovementSystemForVRChat
    - How to use guide: https://docs.google.com/presentation/d/1AEL2s8zkA7NxHWXrC2KvhZrL5BJv1oBwBJ77J8YthIY
    - Class diagram: https://drive.google.com/file/d/1TUnL5gAsZWfoxOLVdrFRFhgGzA1g62VA
    
    Written in UdonSharp 0.20.3
    https://github.com/vrchat-community/UdonSharp
    - No Enums
    - No new class derivatives 
    - No Get Set Properties
    - No static fields
    - Bunch of other C# stuff not supported
    
    Current test world problems:
    ----------------------------


    Stuff to be fixed:
    ------------------
    - In some cases, joining players to not become the owner of their stations, at least from the main controller owners perspective -> implement error check and resulting fix on owner side

    Future improvements:
    --------------------
    - Use OnRespawn function to detect respawn in order to allow more spawn points
    - Encapsulate stuff (When U# 1.0 is released)
    - Far away players need to be recentered
    - Option to keep player vertical if the dimension rotates
        Use Main dimension controller as player position
        Use part of the following code:
            CurrentDimension.transform.parent = null;
            MainDimensionController.transform.position = Networking.LocalPlayer.GetPosition;
            CurrentDimension.transform.parent = MainDimensionController.parent;
            UpdateDimensionTilt
    - Allow avatar stations: Differentiate between respawn button and entering a avatar station by measuring the distance -> Also identify dimension transitions of the station player
    - Add moveable (re)spawn point attached to dimensions

    Tests to be done:
    -----------------
    - Check if immobilize (Station) would work better
    - Leave and join behavior

    If bugs occur:
    --------------
    - Check debug logs and look out for error messages
    - Respawn puts the player in the wrong position: Check if the Respawn point in the world dimension is assigned to the VRCWorld prefab -> Does it reset sometimes???

    Limitations:
    ------------
    - Movement of other players not very smooth and delayed
    - Portals are set relative to the world position and will therefore appear in the wrong location for other players
    - Avatar pen drawings will be in weird places when moving in differnet stations
    - Normal VRChat pens would need to be fixed to the current dimension
    - Some avatars have sitting animations that automatically activate when the player is in a station. Mostly for desktop players, Legs for Head&Hand VR players, probably no issue for full body players
    */

    public class MainDimensionAndStationController : UdonSharpBehaviour
    {
        //Unity assignments
        [Header("To be set manually")]
        [Tooltip("Links to the Main dimension controller")]
        [SerializeField] MainDimensionController linkedMainDimensionController;
        [Tooltip("Enable if the log test should be saved. Can be accessed using the LogText value")]
        [SerializeField] bool SaveLogText;
        [Header("Already set in the prefab")]
        [SerializeField] MainStationController LinkedMainStationController;
        [SerializeField] Transform DimensionTransformationHelper;
        //[SerializeField] SingleScriptDebugState LinkedLogOutput;

        //Runtime variables
        [HideInInspector] public string LogText = "";
        uint logLines = 0;
        uint oldLogLines = 0;
        readonly uint maxLogLines = 200;
        string DebugStringTitle = "iffns LocalMovementSystem ";
        readonly string newLine = "\n";

        public MainDimensionController LinkedDimensionController
        {
            get
            {
                return linkedMainDimensionController;
            }
        }

        public MainStationController GetLinkedMainStationController
        {
            get
            {
                return LinkedMainStationController;
            }
        }

        public void OutputLogText(string message)
        {
            string DebugStringLog = "Log: ";

            string text = Time.time + ": " + DebugStringTitle + DebugStringLog + message;

            Debug.Log(text);
            if (SaveLogText)
            {
                LogText += text + newLine;
                logLines++;
                CheckLogLines();
            }
        }

        public void OutputLogWarning(string message)
        {
            string DebugStringLog = "Warning: ";

            string text = Time.time + ": " + DebugStringTitle + DebugStringLog + message;

            Debug.LogWarning(text);
            if (SaveLogText)
            {
                LogText += text + newLine;
                logLines++;
                CheckLogLines();
            }
        }

        public void OutputLogError(string message)
        {
            string DebugStringLog = "Error: ";

            string text = Time.time + ": " + DebugStringTitle + DebugStringLog + message;

            Debug.LogError(text);
            if (SaveLogText)
            {
                LogText += text + newLine;
                logLines++;
                CheckLogLines();
            }
        }

        void CheckLogLines()
        {
            if(logLines > maxLogLines)
            {
                oldLogLines += logLines;
                logLines = 0;
                LogText = "--> Reset log lines at " + Time.time + " with a total of " + oldLogLines + " lines removed";
            }
        }

        void Start()
        {
            //Checks
            if (LinkedMainStationController == null)
            {
                OutputLogWarning("LinkedMainStationController is not set in Main controller");
                return;
            }
            if (linkedMainDimensionController == null)
            {
                OutputLogWarning("LinkedMainDimensionController is not set in Main controller");
                return;
            }
            if (DimensionTransformationHelper == null)
            {
                OutputLogWarning("DimensionTransformationHelper is not set");
                return;
            }

            //Setup
            linkedMainDimensionController.Setup(linkedMainController: this, DimensionTransformationHelper: DimensionTransformationHelper);

            OutputCurrentDebug(2);

            LinkedMainStationController.SetupFromMainController(this);

            OutputCurrentDebug(3);
        }

        public void OutputCurrentDebug(int index)
        {
            OutputLogText($"Debug {index}: Main dimension: = {linkedMainDimensionController.CurrentDimension.transform.name} at {linkedMainDimensionController.CurrentDimension.transform.position}, player position = {Networking.LocalPlayer.GetPosition()}");
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Home))
            {
                OutputCurrentDebug(-1);
            }
        }

        public void SetWorldDimensionAsActiveAndResetPosition() //ToDo: Encapsulate
        {
            DimensionController worldDimension = linkedMainDimensionController.GetDimension(0);

            SetCurrentDimension(worldDimension);

            worldDimension.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        }

        public void SetCurrentDimension(DimensionController newDimension)
        {
            if (linkedMainDimensionController.CurrentDimension == newDimension) return; //Ignore transition to same dimension

            LinkedMainStationController.SetDimensionAttachment(newDimension);

            linkedMainDimensionController.SetMyDimension(newDimension);
        }

        public string GetCurrentDebugState()
        {
            string returnString = "";

            returnString += LinkedMainStationController.GetCurrentDebugState() + newLine;
            returnString += linkedMainDimensionController.GetCurrentDebugState() + newLine;

            return returnString;
        }
    }
}