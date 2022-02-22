using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace iffnsStuff.iffnsVRCStuff.iffnsLocalMovementSystemForVRChat
{
    public class WalkingStationControllerManualSync : UdonSharpBehaviour
    {
        [SerializeField] public WalkingStationController LinkedStationAutoSync;

        MainDimensionAndStationController LinkedMainController;
        StationAssignmentController LinkedStationAssigner;
        MainDimensionController LinkedMainDimensionController;

        DimensionController AttachedDimension;
        public DimensionController GetAttachedDimension()
        {
            return AttachedDimension;
        }

        [HideInInspector] [UdonSynced] public int AttachedDimensionId = 0;
        [HideInInspector] [UdonSynced] public int AttachedPlayerId = -1;

        int previousDimensionId = -1;
        int previousPlayerId = -1;
        bool SetupStationLater = false;

        bool requestDelayedSerialization = false;
        float joinTime = 0;
        float serializationTimeDelay = 0.5f;

        //newLine = backslash n which is interpreted as a new line when showing the code in a text field
        string newLine = "\n";

        public void Setup()
        {
            LinkedStationAssigner = LinkedStationAutoSync.LinkedStationAssigner;
            LinkedMainController = LinkedStationAssigner.GetLinkedMainController();
            LinkedMainDimensionController = LinkedMainController.GetLinkedDimensionController();
        }

        void Start()
        {
            //Use setup instead
        }

        private void Update()
        {
            if(requestDelayedSerialization && Time.time - joinTime > serializationTimeDelay)
            {
                requestDelayedSerialization = false;
                RequestSerialization();
            }

            if (SetupStationLater && LinkedStationAssigner.MyStation != null)
            {
                UpdatePlayerState();
                UpdateDimensionAttachment();
                SetupStationLater = false;
            }
        }

        public void ResetStation()
        {
            previousDimensionId = -1;
            previousPlayerId = -1;
            SetupStationLater = false;
            AttachedDimensionId = 0;
            AttachedPlayerId = -1;
            LinkedMainController.OutputLogWarning("Setting attached player ID of " + LinkedStationAutoSync.transform.name + " to -1 (Reset)");
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

            LinkedMainController.OutputLogText("Player joined with ID " + player.playerId + " -> Requesting serialization in " + serializationTimeDelay + "s");

            //RequestSerialization(); //-> Not received by rejoining owner as described above
        }

        public override void OnDeserialization()
        {
            LinkedMainController.OutputLogText("Deserialization called on " + LinkedStationAutoSync.transform.name + " with player ID = " + AttachedPlayerId + " and dimension ID " + AttachedDimensionId);

            if (AttachedPlayerId == Networking.LocalPlayer.playerId) //Should be true if: The player joins but isn't the first player in the world
            {
                //Run initial setup
                SetupMyStation();
            }
            else
            {
                // If dimension changed
                if (previousDimensionId != AttachedDimensionId)
                {
                    UpdateDimensionAttachment();
                    previousDimensionId = AttachedDimensionId;
                }

                // If Player ID changed
                if (previousPlayerId != AttachedPlayerId)
                {
                    UpdatePlayerState();
                    previousPlayerId = AttachedPlayerId;
                }
            }
        }

        public void SetAttachedDimensionReference(DimensionController newDimension)
        {
            AttachedDimension = newDimension;
            AttachedDimensionId = newDimension.GetDimensionId();
            LinkedStationAssigner.StationTransformationHelper.parent = newDimension.transform;

            RequestSerialization();
        }

        public void SetupMyStation()
        {
            //Assign ownership
            Networking.SetOwner(player: Networking.LocalPlayer, obj: gameObject);
            Networking.SetOwner(player: Networking.LocalPlayer, obj: LinkedStationAutoSync.transform.gameObject);

            //Setup station
            LinkedStationAutoSync.LinkedVRCStation.PlayerMobility = VRCStation.Mobility.Mobile;
            LinkedStationAutoSync.LinkedVRCStation.disableStationExit = true;

            LinkedStationAssigner.GetLinkedMainController().OutputLogText("Setting up station");

            if (Networking.LocalPlayer.IsValid()) LinkedStationAutoSync.LinkedVRCStation.UseStation(player: Networking.LocalPlayer);
            else
            {
                LinkedStationAssigner.GetLinkedMainController().OutputLogWarning("Local player is not yet valid");
            }

            //Set states and references
            LinkedStationAutoSync.LinkedStationAssigner.MyStation = LinkedStationAutoSync;
            LinkedStationAutoSync.stationState = 0;

            //Dimension setup
            AttachedDimension = LinkedMainDimensionController.GetDimension(AttachedDimensionId);

            //Assign transformation helper
            LinkedStationAutoSync.StationTransformationHelper = LinkedStationAssigner.StationTransformationHelper;
            LinkedStationAssigner.StationTransformationHelper.parent = AttachedDimension.transform;

            //Mark update as complete
            previousDimensionId = AttachedDimensionId;
            previousPlayerId = AttachedPlayerId;

            RequestSerialization();
        }

        public void UpdatePlayerState()
        {
            if (AttachedPlayerId == -1)
            {
                LinkedStationAutoSync.stationState = -1;
            }
            else if (AttachedPlayerId == Networking.LocalPlayer.playerId)
            {
                //Do nothing
            }
            else
            {
                //Always set to stationState 2 = Player is in a different dimension
                LinkedStationAutoSync.LinkedVRCStation.PlayerMobility = VRCStation.Mobility.Immobilize; //ToDo: Check if Immobilize for vehicle also works
                LinkedStationAutoSync.stationState = 1;
            }
        }

        public void UpdateDimensionAttachment()
        {
            

            if (AttachedPlayerId == -1)
            {
                //Do nothing
            }
            else if (AttachedPlayerId == Networking.LocalPlayer.playerId)
            {
                //Do nothing
            }
            else
            {
                LinkedMainController.OutputLogText("Updating dimension attachment of " + transform.name);

                //Set attached dimension
                AttachedDimension = LinkedMainDimensionController.GetDimension(AttachedDimensionId);
                if (previousDimensionId == -1) LinkedStationAutoSync.transform.parent = AttachedDimension.transform;

                //Inform Auto sync -> Do parent change in the next auto sync deserialization
                LinkedStationAutoSync.PlayerIsTransitioning();
            }
        }

        public string GetCurrentDebugState()
        {
            string returnString = "";
            returnString += "Walking staion manual sync debug:" + newLine;
            returnString += "AttachedDimension = " + AttachedDimensionId + newLine;
            returnString += "previousDimensionId = " + previousDimensionId + newLine;
            returnString += "AttachedPlayerId = " + AttachedPlayerId + newLine;
            returnString += "previousPlayerId = " + previousPlayerId + newLine;

            if (VRCPlayerApi.GetPlayerById(AttachedPlayerId) != null)
                returnString += "AttachedPlayerName = " + VRCPlayerApi.GetPlayerById(AttachedPlayerId).displayName + newLine;

            returnString += "Parent of Auto = " + LinkedStationAutoSync.transform.parent.name + newLine;

            return returnString;
        }

        public override void OnPreSerialization()
        {
            LinkedMainController.OutputLogText("Serialization called");
        }
    }
}