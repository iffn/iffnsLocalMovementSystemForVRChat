﻿using UdonSharp;
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

        public void JoinAsFirstPlayer()
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
                    //LinkedMainController.OutputLogWarning("Local player is not in station. Station not yet assigned. Waiting for Deserialization");
                }
            }
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
                    linkedMainController.OutputLogWarning("Not enough stations");
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

                if (currentStation.LinkedStationManualSync.AttachedPlayerId == player.playerId) //Error happens when you leave the world: Ignore
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
            MyStation.SetAttachedDimensionReference(newDimension: newDimension);
        }

        public string GetCurrentDebugState()
        {
            string returnString = "";
            returnString += "peakPlayerVelocity = " + LinkedNanLandFixer.GetPeakPlayerVelocity() + newLine;
            returnString += "lastPlayerVelocity = " + LinkedNanLandFixer.GetLastPlayerVelocity() + newLine;
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