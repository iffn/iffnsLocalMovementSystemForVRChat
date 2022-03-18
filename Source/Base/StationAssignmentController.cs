using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;
using iffnsStuff.iffnsVRCStuff.DebugOutput;
using iffnsStuff.iffnsVRCStuff.BugFixing;

namespace iffnsStuff.iffnsVRCStuff.iffnsLocalMovementSystemForVRChat
{
    public class StationAssignmentController : UdonSharpBehaviour
    {
        //Unity assignments
        [SerializeField] MainDimensionAndStationController LinkedMainController;
        [SerializeField] WalkingStationController[] WalkingStationControllers;
        [SerializeField] SingleScriptDebugState LinkedStateOutput;
        [SerializeField] Transform SpawnPoint;
        [SerializeField] NanLandFixerForPlayer LinkedNanLandFixer;


        public Transform StationTransformationHelper;

        [HideInInspector] public bool PlayerIsCurrentlyUsingOtherStation = false;

        /*
        bool playerwasUsingOtherStation = false;
        bool playerIsCurrentlyUsingOtherStation = false;

        public void PlayerIsCurrentlyUsingOtherStation(bool state)
        {
            playerIsCurrentlyUsingOtherStation = state;

            if (!state) playerwasUsingOtherStation = true;
        }
        */

        //Public variables
        [HideInInspector] public WalkingStationController MyStation;

        //Settings
        float enterDelay = 2;

        //Runtime variables
        bool localPlayerIsInStation = false;
        float startTime = 0;

        //Variable access
        public MainDimensionAndStationController GetLinkedMainController() { return LinkedMainController; }
        public NanLandFixerForPlayer GetLinkedNanLandFixer() { return LinkedNanLandFixer; }

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
            if (SpawnPoint == null)
                LinkedMainController.OutputLogWarning("SpawnPoint not assigned in StationAssignmentController");
            if (LinkedNanLandFixer == null)
                LinkedMainController.OutputLogWarning("LinkedNanLandFixer not assigned in StationAssignmentController");

            //Setup
            //Networking.LocalPlayer.Immobilize(true); //Player is set free, once they join the mobile station

            for (int i = 0; i < WalkingStationControllers.Length; i++)
            {
                if (WalkingStationControllers[i].LinkedStationAssigner == null) LinkedMainController.OutputLogWarning("LinkedStationAssignmentController not set in station " + i + " called " + WalkingStationControllers[i].transform.name);
                WalkingStationControllers[i].Setup();
            }
        }

        public WalkingStationController GetStationControllerFromPlayerId(int playerId)
        {
            for(int i = 0; i<WalkingStationControllers.Length; i++)
            {
                if(WalkingStationControllers[i].LinkedStationManualSync.AttachedPlayerId == playerId)
                {
                    return WalkingStationControllers[i];
                }
            }

            return null;
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

            DisableUnusedStations();
        }

        void DisableUnusedStations()
        {
            /*
            General enable disable logic:
            - Disable stations to reduce network use
            - Only the owner disables the stations
            - Disable all unused stations when the first player joins
            - Enable station when new player joins
            - Disbale station for owner when player leaves
            - Disable all unused stations on ownership transfer
            */

            foreach(WalkingStationController controller in WalkingStationControllers)
            {
                if(controller.LinkedStationManualSync.AttachedPlayerId < 1)
                {
                    controller.enabled = false;
                }
            }
        }

        public void JoinAsFollowingPlayer()
        {
            //Do nothing in here
            enterDelay = 2;
        }

        public void LocalPlayerEnteredStation()
        {
            localPlayerIsInStation = true;
            //GetLinkedMainController().OutputLogText("Player Joined Station");
        }

        public void LocalPlayerExitedStation()
        {
            localPlayerIsInStation = false;
        }

        int errors = 0;

        void Update()
        {
            if (Time.time - startTime < enterDelay) return;

            if (!localPlayerIsInStation)
            {
                if (MyStation != null)
                {
                    if (!PlayerIsCurrentlyUsingOtherStation)
                    {
                        if((Networking.LocalPlayer.GetPosition() - SpawnPoint.position).magnitude < 0.1f)
                        {
                            LinkedMainController.SetWorldDimensionAsActiveAndResetPosition();
                            LinkedMainController.OutputLogText("Respawn detected. Resetting world");
                        }
                        else
                        {
                            LinkedMainController.OutputLogText("Station exit detected with discance = " + ((Networking.LocalPlayer.GetPosition() - SpawnPoint.position).magnitude));
                        }

                        //LinkedMainController.OutputLogText("Player position on walking station reentry = " + Networking.LocalPlayer.GetPosition());
                        //Respawn
                        //Activate world dimension

                        MyStation.transform.position = Networking.LocalPlayer.GetPosition();
                        MyStation.transform.rotation = Networking.LocalPlayer.GetRotation();
                        MyStation.LinkedVRCStation.UseStation(Networking.LocalPlayer); //.IsValid()
                                                                                       //GetLinkedMainController().OutputLogText("Using station");
                    }
                }
                else
                {
                    //LinkedMainController.OutputLogWarning("Local player is not in station. Station not yet assigned. Waiting for Deserialization");
                }
            }

            PrepareDebugState();
        }

        public void PlayerJoined(VRCPlayerApi player)
        {
            if (Networking.IsOwner(gameObject)) //Only run this function for the owner
            {
                //LinkedMainController.OutputLogText("Player joined with ID " + player.playerId);

                WalkingStationController availableStation = NextAvailableStation();

                //LinkedMainController.OutputLogText("Serched for available station");

                if (availableStation == null)
                {
                    LinkedMainController.OutputLogError("Not enough stations");
                    return;
                }

                availableStation.enabled = true;

                //LinkedMainController.OutputLogText("Available station found called " + availableStation.transform.name);

                //LinkedMainController.OutputLogWarning("Setting attached player ID of " + availableStation.transform.name + " to " + Networking.LocalPlayer.playerId + "(Other player joined)");

                if (!Networking.IsOwner(availableStation.LinkedStationManualSync.gameObject))
                    Networking.SetOwner(player: Networking.LocalPlayer, obj: availableStation.LinkedStationManualSync.gameObject);

                availableStation.LinkedStationManualSync.AttachedPlayerId = player.playerId;

                availableStation.LinkedStationManualSync.RequestSerializationOnThis();

                //LinkedMainController.OutputLogText("Completed assignment from owner");
            }
        }

        public void PlayerLeft(VRCPlayerApi player)
        {
            //LinkedMainController.OutputLogText("Player left with ID " + player.playerId);

            WalkingStationController abandonedStation = null;

            for (int i = 0; i < WalkingStationControllers.Length; i++)
            {
                WalkingStationController currentStation = WalkingStationControllers[i];

                if (currentStation.LinkedStationManualSync.AttachedPlayerId == player.playerId)
                {
                    if (Networking.IsOwner(gameObject))
                    {
                        Networking.SetOwner(player: Networking.LocalPlayer, obj: currentStation.gameObject);
                        Networking.SetOwner(player: Networking.LocalPlayer, obj: currentStation.LinkedStationManualSync.gameObject);
                    }

                    currentStation.ResetStation();

                    abandonedStation = currentStation;

                    //LinkedMainController.OutputLogText("Reset station called " + currentStation.transform.name);

                    break;
                }
            }

            if (Networking.IsOwner(gameObject)) //Only run this function for the owner
            {
                if(abandonedStation != null)
                {
                    abandonedStation.enabled = false;
                }
            }
        }

        public override void OnOwnershipTransferred(VRCPlayerApi player)
        {
            if (player != Networking.LocalPlayer) return;

            DisableUnusedStations();
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
            currentState += "peakPlayerVelocity = " + LinkedNanLandFixer.GetPeakPlayerVelocity() + newLine;
            currentState += "lastPlayerVelocity = " + LinkedNanLandFixer.GetLastPlayerVelocity() + newLine;

            currentState += "Local player is owner = " + Networking.IsOwner(gameObject);

            for (int i = 0; i < WalkingStationControllers.Length; i++)
            {
                //if (i < 5)
                if (WalkingStationControllers[i].LinkedStationManualSync.AttachedPlayerId > 0)
                {
                    currentState += WalkingStationControllers[i].GetCurrentDebugState();
                    currentState += "Station enabled = " + WalkingStationControllers[i].enabled + newLine;
                    currentState += newLine;
                }
            }

            LinkedStateOutput.SetCurrentState(displayName: name, currentState: currentState);
        }

        public string GetCurrentDebugState()
        {
            string returnString = "";
            returnString += "Station assignment controller debug at " + Time.time + newLine;
            returnString += "Local player is owner = " + Networking.IsOwner(gameObject);

            for (int i = 0; i < WalkingStationControllers.Length; i++)
            {
                //if (i < 5)
                if (WalkingStationControllers[i].LinkedStationManualSync.AttachedPlayerId > 0)
                {
                    returnString += WalkingStationControllers[i].GetCurrentDebugState();
                    returnString += "Station enabled = " + WalkingStationControllers[i].enabled + newLine;
                    returnString += newLine;
                }
            }

            return returnString;
        }
    }
}