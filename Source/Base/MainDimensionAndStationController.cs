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
        [SerializeField] MainDimensionController LinkedMainDimensionController;
        [Tooltip("Enable if the log test should be saved. Can be accessed using the LogText value")]
        [SerializeField] bool SaveLogText;
        [Header("Already set in the prefab")]
        [SerializeField] StationAssignmentController LinkedStationAssigner;
        [SerializeField] Transform DimensionTransformationHelper;
        //[SerializeField] SingleScriptDebugState LinkedLogOutput;

        //Runtime variables
        [HideInInspector] public string LogText = "";
        uint logLines = 0;
        uint oldLogLines = 0;
        readonly uint maxLogLines = 200;
        string DebugStringTitle = "iffns LocalMovementSystem ";
        bool wasOwner = false;
        readonly string newLine = "\n";

        public MainDimensionController GetLinkedDimensionController()
        {
            return LinkedMainDimensionController;
        }

        public StationAssignmentController GetLinkedStationController()
        {
            return LinkedStationAssigner;
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
            #if !UNITY_EDITOR
            wasOwner = Networking.IsOwner(gameObject);
            #endif

            //Checks
            if (LinkedStationAssigner == null)
            {
                OutputLogWarning("LinkedStationAssigner is not set in Main controller");
                return;
            }
            if (LinkedMainDimensionController == null)
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
            LinkedMainDimensionController.Setup(linkedMainController: this, DimensionTransformationHelper: DimensionTransformationHelper);

            #if !UNITY_EDITOR
            LinkedStationAssigner.Setup(linkedMainController: this);

            if (VRCPlayerApi.GetPlayerCount() == 1) //Note: GetPlayerCount seems to be set correctly, while synced variables do not have their correct state yet
            {
                LinkedStationAssigner.JoinAsFirstPlayer();
            }
            else
            {
                LinkedStationAssigner.JoinAsFollowingPlayer();
            }
            #endif
        }

        public void SetWorldDimensionAsActiveAndResetPosition() //ToDo: Encapsulate
        {
            DimensionController worldDimension = LinkedMainDimensionController.GetDimension(0);

            SetCurrentDimension(worldDimension);
            worldDimension.transform.position = Vector3.zero;
            worldDimension.transform.rotation = Quaternion.identity;
        }

        public void SetCurrentDimension(DimensionController newDimension)
        {
            if (LinkedMainDimensionController.GetCurrentDimension() == newDimension) return; //Ignore transition to same dimension

            //OutputLogText("Entering dimension called " + newDimension.transform.name);

            LinkedStationAssigner.SetMyDimension(newDimension);

            LinkedMainDimensionController.SetMyDimension(newDimension);
        }

        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            if (player == Networking.LocalPlayer) return;

            LinkedStationAssigner.PlayerJoined(player: player);
        }

        public override void OnPlayerLeft(VRCPlayerApi player)
        {
            if(Networking.IsOwner(gameObject))
            {
                if (!wasOwner)
                {
                    OutputLogText("Transfering ownership to of MainDimensionAndStationController to myself");
                    wasOwner = true;
                    Networking.SetOwner(player: Networking.LocalPlayer, obj: LinkedStationAssigner.gameObject);
                    Networking.SetOwner(player: Networking.LocalPlayer, obj: LinkedMainDimensionController.gameObject);
                }

                LinkedStationAssigner.ResetAndSyncStation(player: player);
            }
        }

        public string GetCurrentDebugState()
        {
            string returnString = "";

            returnString += LinkedStationAssigner.GetCurrentDebugState() + newLine;
            returnString += LinkedMainDimensionController.GetCurrentDebugState() + newLine;

            return returnString;
        }
    }
}