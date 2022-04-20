using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;

namespace iffnsStuff.iffnsVRCStuff.iffnsLocalMovementSystemForVRChat
{
    public class StationAssignmentController : UdonSharpBehaviour
    {
        //Unity assignments
        [SerializeField] WalkingStationController[] WalkingStationControllers;
        //[SerializeField] SingleScriptDebugState LinkedStateOutput;
        [SerializeField] Transform SpawnPoint;
        [SerializeField] NanLandFixerForPlayerInLocalMovementSystem LinkedNanLandFixer;
        [SerializeField] public bool DisableUsingPlayerStationsOnStart = true;

        //Public variables
        [HideInInspector] public WalkingStationController MyStation;

        //Settings
        float enterDelay = 2;

        //Runtime variables
        public Transform StationTransformationHelper;
        [HideInInspector] public bool PlayerIsCurrentlyUsingOtherStation = false;
        MainDimensionAndStationController linkedMainController;
        bool localPlayerIsInStation = false;
        float startTime = 0;
        readonly string newLine = "\n";

        //Variable access
        public MainDimensionAndStationController GetLinkedMainController() { return linkedMainController; }
        public NanLandFixerForPlayerInLocalMovementSystem GetLinkedNanLandFixer() { return LinkedNanLandFixer; }


        public void Setup(MainDimensionAndStationController linkedMainController)
        {
            this.linkedMainController = linkedMainController;

            startTime = Time.time;

            //Error checks
            if (WalkingStationControllers.Length < 1)
                this.linkedMainController.OutputLogWarning("No stations assigned in StationAssignmentController");
            else if (WalkingStationControllers.Length < 4)
                this.linkedMainController.OutputLogWarning("Less than 4 stations assigned in StationAssignmentController");
            if (StationTransformationHelper == null)
                this.linkedMainController.OutputLogWarning("StationTransformationHelper not assigned in StationAssignmentController");
            if (SpawnPoint == null)
                this.linkedMainController.OutputLogWarning("SpawnPoint not assigned in StationAssignmentController");
            if (LinkedNanLandFixer == null)
                this.linkedMainController.OutputLogWarning("LinkedNanLandFixer not assigned in StationAssignmentController");

            //Setup
            for (int i = 0; i < WalkingStationControllers.Length; i++)
            {
                WalkingStationControllers[i].Setup(LinkedStationAssigner: this);
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

        public void JoinAsFirstPlayer() //ToDo: Encapsulate
        {
            WalkingStationController availableStation = NextAvailableStation();

            if (availableStation == null) return; //Error: Not enough stations

            linkedMainController.OutputLogText("Setting attached player ID of " + availableStation.transform.name + " to " + Networking.LocalPlayer.playerId + "(Join as fisrt)");
            availableStation.LinkedStationManualSync.AttachedPlayerId = Networking.LocalPlayer.playerId;
            //availableStation.AttachedDimensionId = 0;

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
                if(controller.LinkedStationManualSync.AttachedPlayerId == 0)
                {
                    controller.enabled = false;
                }
            }
        }

        public void JoinAsFollowingPlayer() //ToDo: Encapsulate
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

        public void RespawnPlayer()
        {
            //linkedMainController.SetWorldDimensionAsActiveAndResetPosition();

            Networking.LocalPlayer.TeleportTo(teleportPos: SpawnPoint.position, teleportRot: SpawnPoint.rotation);

            Networking.LocalPlayer.SetVelocity(velocity: Vector3.zero);
        }

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
                            linkedMainController.SetWorldDimensionAsActiveAndResetPosition();
                            linkedMainController.OutputLogText("Respawn detected. Resetting world");
                        }
                        else
                        {
                            linkedMainController.OutputLogText("Station exit detected with discance = " + ((Networking.LocalPlayer.GetPosition() - SpawnPoint.position).magnitude));
                        }

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
                    //linkedMainController.OutputLogWarning("Local player is not in station. Station not yet assigned. Waiting for Deserialization");
                }
            }
        }

        public void PlayerJoined(VRCPlayerApi player) //ToDo: Encapsulate
        {
            if (Networking.IsOwner(gameObject)) //Only run this function for the owner
            {
                //linkedMainController.OutputLogText("Player joined with ID " + player.playerId);

                WalkingStationController availableStation = NextAvailableStation();

                //linkedMainController.OutputLogText("Serched for available station");

                if (availableStation == null)
                {
                    linkedMainController.OutputLogWarning("Not enough stations");
                    return;
                }

                availableStation.enabled = true;

                //linkedMainController.OutputLogText("Available station found called " + availableStation.transform.name);

                //linkedMainController.OutputLogWarning("Setting attached player ID of " + availableStation.transform.name + " to " + Networking.LocalPlayer.playerId + "(Other player joined)");

                if (!Networking.IsOwner(availableStation.LinkedStationManualSync.gameObject))
                    Networking.SetOwner(player: Networking.LocalPlayer, obj: availableStation.LinkedStationManualSync.gameObject);

                availableStation.LinkedStationManualSync.AttachedPlayerId = player.playerId;

                availableStation.LinkedStationManualSync.RequestSerializationOnThis();

                //linkedMainController.OutputLogText("Completed assignment from owner");
            }
        }

        public void ResetAndSyncStation(VRCPlayerApi player) //ToDo: Encapsulate
        {
            for (int i = 0; i < WalkingStationControllers.Length; i++)
            {
                WalkingStationController currentStation = WalkingStationControllers[i];

                if (currentStation.LinkedStationManualSync.AttachedPlayerId != player.playerId) continue; //Error happens when you leave the world: Ignore

                currentStation.LinkedStationManualSync.ResetAndSyncStation();

                currentStation.ResetStation();

                currentStation.enabled = false;

                linkedMainController.OutputLogText("Reset station called " + currentStation.transform.name);

                break;
            }
        }

        public override void OnOwnershipTransferred(VRCPlayerApi player) //ToDo: Encapsulate
        {
            if (player != Networking.LocalPlayer) return;

            DisableUnusedStations();
        }

        WalkingStationController NextAvailableStation()
        {
            for (int i = 0; i < WalkingStationControllers.Length; i++)
            {
                WalkingStationController currentController = WalkingStationControllers[i];

                if (currentController.LinkedStationManualSync.AttachedPlayerId == 0)
                {
                    return currentController;
                }
            }
            return null;
        }

        public void SetMyDimension(DimensionController newDimension) //ToDo: Encapsulate
        {
            //Update my station number
            MyStation.SetAttachedDimensionReference(newDimension: newDimension);
        }

        public string GetCurrentDebugState()
        {
            string returnString = "";
            returnString += "peakPlayerVelocity = " + LinkedNanLandFixer.GetPeakPlayerVelocity() + newLine;
            returnString += "lastPlayerVelocity = " + LinkedNanLandFixer.GetLastPlayerVelocity() + newLine;
            returnString += "Local player is owner = " + Networking.IsOwner(gameObject) + newLine;

            returnString += newLine;

            for (int i = 0; i < WalkingStationControllers.Length; i++)
            {
                if (WalkingStationControllers[i].LinkedStationManualSync.AttachedPlayerId > 0)
                //if (i < 5)
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