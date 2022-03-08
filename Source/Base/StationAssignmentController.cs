using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;
using iffnsStuff.iffnsVRCStuff.DebugOutput;

namespace iffnsStuff.iffnsVRCStuff.iffnsLocalMovementSystemForVRChat
{
    public class StationAssignmentController : UdonSharpBehaviour
    {
        //Unity assignments
        [SerializeField] MainDimensionAndStationController LinkedMainController;
        [SerializeField] WalkingStationController[] WalkingStationControllers;
        [SerializeField] SingleScriptDebugState LinkedStateOutput;

        public Transform StationTransformationHelper;

        //Public variables
        [HideInInspector] public WalkingStationController MyStation;

        //Settings
        float enterDelay = 2;

        //Runtime variables
        bool localPlayerIsInStation = false;
        float startTime = 0;

        //Variable access
        public MainDimensionAndStationController GetLinkedMainController()
        {
            return LinkedMainController;
        }

        //newLine = backslash n which is interpreted as a new line when showing the code in a text field
        string newLine = "\n";

        public void Setup()
        {
            startTime = Time.time;

            //Error checks
            if (WalkingStationControllers.Length < 1)
                LinkedMainController.OutputLogWarning("No stations assigned in StationAssignmentController");
            else if (WalkingStationControllers.Length < 4)
                LinkedMainController.OutputLogWarning("Less than 4 stations assigned in StationAssignmentController");
            if (StationTransformationHelper == null)
                LinkedMainController.OutputLogWarning("StationTransformationHelper not assigned in StationAssignmentController");

            //Setup
            Networking.LocalPlayer.Immobilize(true); //Player is set free, once they join the mobile station

            for (int i = 0; i < WalkingStationControllers.Length; i++)
            {
                if (WalkingStationControllers[i].LinkedStationAssigner == null) LinkedMainController.OutputLogWarning("LinkedStationAssignmentController not set in station " + i + " called " + WalkingStationControllers[i].transform.name);
                WalkingStationControllers[i].Setup();
            }
        }

        void Start()
        {
            //Use setup instead
        }

        public void JoinAsFirstPlayer()
        {
            WalkingStationController availableStation = NextAvailableStation();

            if (availableStation == null) return; //Error: Not enough stations

            LinkedMainController.OutputLogText("Setting attached player ID of " + availableStation.transform.name + " to " + Networking.LocalPlayer.playerId + "(Join as fisrt)");
            availableStation.LinkedStationManualSync.AttachedPlayerId = Networking.LocalPlayer.playerId;
            availableStation.LinkedStationManualSync.AttachedDimensionId = 0;

            availableStation.LinkedStationManualSync.SetupMyStation();
        }

        public void JoinAsFollowingPlayer()
        {
            //Do nothing in here
            enterDelay = 2;
        }

        public void LocalPlayerEnteredStation()
        {
            localPlayerIsInStation = true;
            GetLinkedMainController().OutputLogText("Player Joined Station");
        }

        public void LocalPlayerExitedStation()
        {
            localPlayerIsInStation = false;
        }

        int errors = 0;

        private void Update()
        {
            if (Time.time - startTime < enterDelay) return;

            if (!localPlayerIsInStation)
            {
                if (MyStation != null)
                {
                    //Respawn
                    //Activate world dimension
                    LinkedMainController.SetWorldDimensionAsActiveAndResetPosition();

                    MyStation.LinkedVRCStation.UseStation(Networking.LocalPlayer); //.IsValid()
                    GetLinkedMainController().OutputLogText("Using station");
                    
                }
                else
                {
                    LinkedMainController.OutputLogWarning("Local player is not in station. Station not yet assigned. Waiting for Deserialization");
                }
            }

            PrepareDebugState();
        }

        public void PlayerJoined(VRCPlayerApi player)
        {
            if (Networking.IsOwner(gameObject)) //Only run this function for the owner
            {
                LinkedMainController.OutputLogText("Player joined with ID " + player.playerId);

                WalkingStationController availableStation = NextAvailableStation();

                LinkedMainController.OutputLogText("Serched for available station");

                if (availableStation == null)
                {
                    LinkedMainController.OutputLogError("Not enough stations");
                    return;
                }

                LinkedMainController.OutputLogText("Available station found called " + availableStation.transform.name);

                LinkedMainController.OutputLogWarning("Setting attached player ID of " + availableStation.transform.name + " to " + Networking.LocalPlayer.playerId + "(Other player joined)");

                if (!Networking.IsOwner(availableStation.LinkedStationManualSync.gameObject))
                    Networking.SetOwner(player: Networking.LocalPlayer, obj: availableStation.LinkedStationManualSync.gameObject);

                availableStation.LinkedStationManualSync.AttachedPlayerId = player.playerId;

                availableStation.LinkedStationManualSync.RequestSerializationOnThis();

                LinkedMainController.OutputLogText("Completed assignment from owner");
            }
        }

        public void PlayerLeft(VRCPlayerApi player)
        {
            LinkedMainController.OutputLogText("Player left with ID " + player.playerId);

            for (int i = 0; i < WalkingStationControllers.Length; i++)
            {
                WalkingStationController currentController = WalkingStationControllers[i];

                if (currentController.LinkedStationManualSync.AttachedPlayerId == player.playerId)
                {
                    if (Networking.IsOwner(gameObject))
                    {
                        Networking.SetOwner(player: Networking.LocalPlayer, obj: currentController.gameObject);
                        Networking.SetOwner(player: Networking.LocalPlayer, obj: currentController.LinkedStationManualSync.gameObject);
                    }

                    currentController.ResetStation();

                    LinkedMainController.OutputLogText("Reset station called " + currentController.transform.name);

                    break;
                }
            }
        }

        WalkingStationController NextAvailableStation()
        {
            for (int i = 0; i < WalkingStationControllers.Length; i++)
            {
                WalkingStationController currentController = WalkingStationControllers[i];

                if (currentController.LinkedStationManualSync.AttachedPlayerId == -1)
                {
                    return currentController;
                }
            }
            return null;
        }

        public void SetMyDimension(DimensionController newDimension)
        {
            //Update my station number
            MyStation.LinkedStationManualSync.SetAttachedDimensionReference(newDimension: newDimension);
        }

        public void PrepareDebugState()
        {
            if (LinkedStateOutput == null) return;

            if (!LinkedStateOutput.IsReadyForOutput()) return;

            string name = "StationAssignmentController";

            string currentState = "";

            for (int i = 0; i < WalkingStationControllers.Length; i++)
            {
                //if (WalkingStationControllers[i].LinkedStationManualSync.AttachedPlayerId > 0)
                if (i < 5)
                {
                    currentState += WalkingStationControllers[i].GetCurrentDebugState() + newLine;
                }
            }

            LinkedStateOutput.SetCurrentState(displayName: name, currentState: currentState);
        }

        public string GetCurrentDebugState()
        {
            string returnString = "";
            returnString += "Station assignment controller debug at " + Time.time + newLine;

            for (int i = 0; i < WalkingStationControllers.Length; i++)
            {
                //if (i < 5)
                if (WalkingStationControllers[i].LinkedStationManualSync.AttachedPlayerId > 0)
                {
                    returnString += WalkingStationControllers[i].GetCurrentDebugState() + newLine;
                }
            }

            return returnString;
        }
    }
}