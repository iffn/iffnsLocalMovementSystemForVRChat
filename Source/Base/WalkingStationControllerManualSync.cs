using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace iffnsStuff.iffnsVRCStuff.iffnsLocalMovementSystemForVRChat
{
    public class WalkingStationControllerManualSync : UdonSharpBehaviour
    {
        //Synced variables
        [HideInInspector] [UdonSynced] public int AttachedPlayerId = 0;
        
        //Runtime variables
        [HideInInspector] public WalkingStationController LinkedWalkingStationController;
        StationAssignmentController LinkedStationAssigner;
        int previousPlayerId = 0;
        bool requestDelayedSerialization = false;
        float joinTime = 0;
        float serializationTimeDelay = 0.5f;
        readonly string newLine = "\n";

        public void Setup(WalkingStationController LinkedWalkingStationController)
        {
            this.LinkedWalkingStationController = LinkedWalkingStationController;
            LinkedStationAssigner = LinkedWalkingStationController.LinkedStationAssigner;
        }

        void Start()
        {
            //Use setup instead
        }

        void Update()
        {
            if(requestDelayedSerialization && Time.time - joinTime > serializationTimeDelay)
            {
                requestDelayedSerialization = false;
                RequestSerialization();
            }
        }

        public void ResetAndSyncStation()
        {
            if (!Networking.IsOwner(gameObject)) Networking.SetOwner(player: Networking.LocalPlayer, obj: gameObject);
            if (!Networking.IsOwner(LinkedWalkingStationController.gameObject)) Networking.SetOwner(player: Networking.LocalPlayer, obj: LinkedWalkingStationController.gameObject);

            AttachedPlayerId = 0;

            RequestSerialization();
        }

        
        public void ResetStation()
        {
            previousPlayerId = 0;
            //LinkedMainController.OutputLogWarning("Setting attached player ID of " + LinkedStationAutoSync.transform.name + " to -1 (Reset)");
        }
        

        public void RequestSerializationOnThis()
        {
            RequestSerialization();
        }

        void RequestDelayedSerialization()
        {
            requestDelayedSerialization = true;
            joinTime = Time.time;
        }

        public override void OnPlayerJoined(VRCPlayerApi player)
        {
            /*
            Code to fix weird issue:
            - Needs at least 3 people in the world to reproduce
            - The owner of the main controller rejions the world
            -> The ownership is transfered to the next player
            - The serialization on the other players manual sync station is not called when the player rejoins
            - Requesting the serialization only works with a time delay

            Potential bug description:
            - 3 players are in the world (ID 1, 2, 3)
                - Player 1 is the owner of the instance
                - Player 1 owns a manual sync object 1
                - Player 2 owns a manual sync object 2
                - Player 3 owns a manual sync object 3
            - Player 1 (owner) clicks rejoin and leaves the world
                - Ownership of the instance is transfered to player 2
                - Ownership of the manual sync object 1 is transfered to player 2
            - Player 1 (now 4) rejoins
                - Bug: Manual sync object 3 is not serialized
            */
            if (
                    player == Networking.LocalPlayer
                    || AttachedPlayerId != Networking.LocalPlayer.playerId
                    || !Networking.IsOwner(gameObject)
                )
                return;

            RequestDelayedSerialization();

            //LinkedMainController.OutputLogText("Player joined with ID " + player.playerId + " -> Requesting serialization in " + serializationTimeDelay + "s");

            //RequestSerialization(); //-> Not received by rejoining owner as described above
        }

        public override void OnDeserialization()
        {
            //LinkedMainController.OutputLogText("Deserialization called on " + LinkedStationAutoSync.transform.name + " with player ID = " + AttachedPlayerId + " and dimension ID " + AttachedDimensionId);

            if (AttachedPlayerId == Networking.LocalPlayer.playerId) //Should be true if: The player joins but isn't the first player in the world
            {
                //Run initial setup
                SetupMyStation();
            }
            else
            {
                // If Player ID changed
                if (previousPlayerId != AttachedPlayerId)
                {
                    UpdatePlayerState();
                    previousPlayerId = AttachedPlayerId;
                    LinkedWalkingStationController.UpdateDimensionAttachment();
                }
            }
        }

        public void SetupMyStation()
        {
            //Assign ownership
            Networking.SetOwner(player: Networking.LocalPlayer, obj: gameObject);
            Networking.SetOwner(player: Networking.LocalPlayer, obj: LinkedWalkingStationController.transform.gameObject);

            //Setup station
            LinkedWalkingStationController.LinkedVRCStation.PlayerMobility = VRCStation.Mobility.Mobile;
            LinkedWalkingStationController.LinkedVRCStation.disableStationExit = true;

            //LinkedStationAssigner.GetLinkedMainController().OutputLogText("Setting up station");

            LinkedWalkingStationController.transform.position = Networking.LocalPlayer.GetPosition();
            LinkedWalkingStationController.transform.rotation = Networking.LocalPlayer.GetRotation();
            
            if (Networking.LocalPlayer.IsValid())
            {
                LinkedStationAssigner.GetLinkedMainController().OutputLogText("Putting player into walking station with player position = " + Networking.LocalPlayer.GetPosition());

                LinkedWalkingStationController.LinkedVRCStation.UseStation(player: Networking.LocalPlayer);
            }
            else
            {
                LinkedStationAssigner.GetLinkedMainController().OutputLogWarning("Local player is not yet valid");
            }

            //Set states and references
            LinkedWalkingStationController.LinkedStationAssigner.MyStation = LinkedWalkingStationController;
            LinkedWalkingStationController.stationState = 0;

            //Dimension setup
            LinkedWalkingStationController.SetupDimensionAttachment();

            //Mark update as complete
            previousPlayerId = AttachedPlayerId;

            RequestSerialization();
        }

        public void UpdatePlayerState()
        {
            if (AttachedPlayerId == 0)
            {
                LinkedWalkingStationController.ResetStation();
            }
            else if (AttachedPlayerId == Networking.LocalPlayer.playerId)
            {
                //Do nothing
            }
            else
            {
                //Always set to stationState 2 = Player is in a different dimension
                LinkedWalkingStationController.LinkedVRCStation.PlayerMobility = VRCStation.Mobility.Immobilize; //ToDo: Check if Immobilize for vehicle also works
                LinkedWalkingStationController.stationState = 1;
            }
        }

        public string GetCurrentDebugState()
        {
            string returnString = "";
            returnString += "Walking staion manual sync debug:" + newLine;
            returnString += "AttachedPlayerId = " + AttachedPlayerId + newLine;
            returnString += "previousPlayerId = " + previousPlayerId + newLine;

            if (VRCPlayerApi.GetPlayerById(AttachedPlayerId) != null)
                returnString += "AttachedPlayerName = " + VRCPlayerApi.GetPlayerById(AttachedPlayerId).displayName + newLine;

            return returnString;
        }

        public override void OnPreSerialization()
        {
            //LinkedMainController.OutputLogText("Serialization called");
        }
    }
}