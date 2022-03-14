using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace iffnsStuff.iffnsVRCStuff.iffnsLocalMovementSystemForVRChat.Additionals
{
    [RequireComponent(typeof(VRCStation))]
    public class LocalStationController : UdonSharpBehaviour
    {
        //Unity assignments
        [SerializeField] MainDimensionAndStationController LinkedMainController;
        [SerializeField] DimensionController LinkedEnterDimension;
        [SerializeField] DimensionController LinkedExitDimension;

        //Runtime variables
        StationAssignmentController linkedStationAssignmentController;
        VRCStation linkedVRCStation;
        int attachedPlayerId = -1;
        bool stationInUse;
        LocalStationEntry LinkedStationEntry;

        //Get
        public int GetAttachedPlayerId() { return attachedPlayerId; }
        public bool IsStationInUse() { return stationInUse; }

        public void RegisterStationEntry(LocalStationEntry stationEntry) //Currently only 1 available
        {
            LinkedStationEntry = stationEntry;
        }

        void Start()
        {
            if(LinkedMainController == null)
            {
                Debug.LogWarning(Time.time + ": iffns LocalMovementSystem Assignment problem: LinkedMainController of LocalStationController is not assigned");
                return;
            }

            linkedVRCStation = (VRCStation)transform.GetComponent(typeof(VRCStation));
            linkedStationAssignmentController = LinkedMainController.GetLinkedStationController();
        }

        public void UseAttachedStation()
        {
            if (!stationInUse)
            {
                //LinkedMainController.OutputLogText("Attempting station entry");
                linkedVRCStation.UseStation(Networking.LocalPlayer);
                linkedStationAssignmentController.PlayerIsCurrentlyUsingOtherStation = true;
            }
        }

        public override void OnStationEntered(VRCPlayerApi player)
        {
            attachedPlayerId = player.playerId;

            LockStationEntry(true);

            if (player == Networking.LocalPlayer)
            {
                linkedStationAssignmentController.PlayerIsCurrentlyUsingOtherStation = true;
                //LinkedMainController.OutputLogText("Player position on seating station entry = " + Networking.LocalPlayer.GetPosition());
                if (LinkedEnterDimension != null) LinkedMainController.SetCurrentDimension(LinkedEnterDimension);
            }
        }

        public override void OnStationExited(VRCPlayerApi player)
        {
            attachedPlayerId = -1;

            LockStationEntry(false);

            if (player == Networking.LocalPlayer)
            {
                linkedStationAssignmentController.PlayerIsCurrentlyUsingOtherStation = false;
                //LinkedMainController.OutputLogText("Player position on seating station exit = " + Networking.LocalPlayer.GetPosition());
                if(LinkedExitDimension != null) LinkedMainController.SetCurrentDimension(LinkedExitDimension);
            }
        }

        void LockStationEntry(bool newState)
        {
            stationInUse = newState;

            if (LinkedStationEntry != null) LinkedStationEntry.LockStationEntry(newState);
        }
    }
}