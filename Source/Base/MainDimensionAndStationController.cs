using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;
using iffnsStuff.iffnsVRCStuff.DebugOutput;

namespace iffnsStuff.iffnsVRCStuff.iffnsLocalMovementSystemForVRChat
{
    public class MainDimensionAndStationController : UdonSharpBehaviour
    {
        [SerializeField] MainDimensionController LinkedMainDimensionController;
        [SerializeField] StationAssignmentController LinkedStationAssigner;
        [SerializeField] Transform DimensionTransformationHelper;
        [SerializeField] bool SaveLogText;
        [SerializeField] SingleScriptDebugState LinkedLogOutput;

        //newLine = backslash n which is interpreted as a new line when showing the code in a text field
        string newLine = "\n";
        string DebugStringTitle = "iffns LocalMovementSystem ";
        string logText = "";
        public string GetLogText() { return logText; }
        bool wasOwner = false;

        /*
        Current test world problems:
        ----------------------------
        

        Stuff to be fixed:
        ------------------
        - Going too far down by jumping off a high dimension will mean the player eventually reaches the respawn height
            - Set the respawn height of the VRCWorldController to -10000
            - Create a custom respawn script relative to the current dimension that kicks the player out of the station with exit location -10000 -> To be tested
        - Smoother dimension transitions for other players -> Currently frozen for 2 deserializations due to the smooth lerp effects during large coordinate jumps

        Future improvements:
        --------------------
        - Far away players need to be recentered
        - Option to keep player vertical if the dimension rotates
            Use Main dimension controller as player position
            Use part of the following code:
                CurrentDimension.transform.parent = null;
                MainDimensionController.transform.position = Networking.LocalPlayer.GetPosition;
                CurrentDimension.transform.parent = MainDimensionController.parent;
                UpdateDimensionTilt
        - Implement stations with custom enter triggers (Since entering stations is disabled because of avatar stations)
        - Allow avatar stations: Differentiate between respawn button and entering a avatar station by measuring the distance -> Also identify dimension transitions of the station player
        - Allow movement before initialization complete by moving entry point
        - Add moveable (re)spawn point attached to dimensions

        Tests to be done:
        -----------------
        - Smooth vs linear player position sync
        - Check if immobilize (Station) would work better
        - Leave and join behavior

        If bugs occur:
        --------------
        - Check debug logs and look out for error messages
        - Respawn puts the player in the wrong position: Check if the Respawn point in the world dimension is assigned to the VRCWorld prefab -> Does it reset sometimes???

        Limitations:
        ------------
        - Movement of other players not very smooth
        - Portals are set relative to the world position and will therefore appear in the wrong location for other players
        - Avatar pen drawings will be in weird places when moving in differnet stations
        - Normal VRChat pens would need to be fixed to the current dimension
        - Some avatars have sitting animations that automatically activate when the player is in a station. Mostly for desktop players, Legs for Head&Hand VR players, probably no issue for full body players
        */

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
                logText += text + newLine;
                SendLogText();
            }
        }

        public void OutputLogWarning(string message)
        {
            string DebugStringLog = "Warning: ";

            string text = Time.time + ": " + DebugStringTitle + DebugStringLog + message;

            Debug.Log(text);
            if (SaveLogText) logText += text + newLine;
        }

        public void OutputLogError(string message)
        {
            string DebugStringLog = "Error: ";

            string text = Time.time + ": " + DebugStringTitle + DebugStringLog + message;

            Debug.Log(text);
            if (SaveLogText) logText += text + newLine;
        }

        void Start()
        {
            wasOwner = Networking.IsOwner(gameObject);

            //Checks
            if (LinkedStationAssigner == null)
                OutputLogWarning("LinkedStationAssigner is not set in Main controller");
            if (LinkedMainDimensionController == null)
                OutputLogWarning("LinkedMainDimensionController is not set in Main controller");
            if (DimensionTransformationHelper == null)
                OutputLogWarning("DimensionTransformationHelper is not set");
            if (LinkedStationAssigner.GetLinkedMainController() == null)
                OutputLogWarning("LinkedStationAssigner.GetLinkedMainController is not set");
            if (LinkedMainDimensionController.GetLinkedMainController() == null)
                OutputLogWarning("LinkedMainDimensionController.GetLinkedMainController is not set");

            //Setup
            LinkedMainDimensionController.Setup(DimensionTransformationHelper: DimensionTransformationHelper);
            LinkedStationAssigner.Setup();

            if (VRCPlayerApi.GetPlayerCount() == 1) //Note: GetPlayerCount seems to be set correctly, while synced variables do not have their correct state yet
            {
                LinkedStationAssigner.JoinAsFirstPlayer();
            }
            else
            {
                LinkedStationAssigner.JoinAsFollowingPlayer();
            }
        }

        public void SetWorldDimensionAsActiveAndResetPosition()
        {
            DimensionController worldDimension = LinkedMainDimensionController.GetDimension(0);

            SetCurrentDimension(worldDimension);
            worldDimension.transform.position = Vector3.zero;
            worldDimension.transform.rotation = Quaternion.identity;
        }

        public void SetCurrentDimension(DimensionController newDimension)
        {
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
            if(Networking.IsOwner(gameObject) && !wasOwner)
            {
                OutputLogText("Transfering ownership to of MainDimensionAndStationController to myself");
                wasOwner = true;
                Networking.SetOwner(player: Networking.LocalPlayer, obj: LinkedStationAssigner.gameObject);
                Networking.SetOwner(player: Networking.LocalPlayer, obj: LinkedMainDimensionController.gameObject);
            }

            LinkedStationAssigner.PlayerLeft(player: player);
        }

        public void SendLogText()
        {
            if (LinkedLogOutput == null) return;

            string name = "Local movement system log";

            string currentState = logText;

            LinkedLogOutput.SetCurrentState(displayName: name, currentState: currentState);
        }

        public string GetCurrentDebugState()
        {
            string returnString = "";
            returnString += "Main local movement system debug" + newLine;

            returnString += LinkedStationAssigner.GetCurrentDebugState() + newLine;
            returnString += LinkedMainDimensionController.GetCurrentDebugState() + newLine;

            return returnString;
        }
    }
}