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
        [SerializeField] DimensionController LinkedEnterDimension;
        [SerializeField] DimensionController LinkedExitDimension;

        //Runtime variables
        MainDimensionAndStationController LinkedMainController;
        MainStationController linkedMainStationController;
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

        public void SetLinkedDimensionsIfNotAlreadySet(DimensionController linkedDimension)
        {
            if (LinkedEnterDimension == null) LinkedEnterDimension = linkedDimension;
            if (LinkedExitDimension == null) LinkedExitDimension = linkedDimension;
        }

        void Start()
        {
            if (LinkedEnterDimension != null)
            {
                if(LinkedEnterDimension.GetLinkedMainDimensionController() == null || LinkedEnterDimension.GetLinkedMainDimensionController().LinkedMainController == null)
                {
                    SendCustomEventDelayedFrames(eventName: nameof(Setup), delayFrames: 1, eventTiming: VRC.Udon.Common.Enums.EventTiming.Update);
                }
                else
                {
                    Setup();
                }
            }
            else if (LinkedExitDimension != null)
            {
                if (LinkedExitDimension.GetLinkedMainDimensionController() == null || LinkedExitDimension.GetLinkedMainDimensionController().LinkedMainController == null)
                {
                    SendCustomEventDelayedFrames(eventName: nameof(Setup), delayFrames: 1, eventTiming: VRC.Udon.Common.Enums.EventTiming.Update);
                }
                else
                {
                    Setup();
                }
            }
        }

        public void Setup()
        {
            if (LinkedEnterDimension != null)
            {
                LinkedMainController = LinkedEnterDimension.GetLinkedMainDimensionController().LinkedMainController;
            }
            else if (LinkedExitDimension != null)
            {
                LinkedMainController = LinkedExitDimension.GetLinkedMainDimensionController().LinkedMainController;
            }
            else
            {
                Debug.LogWarning(Time.time + ": iffns LocalMovementSystem Assignment problem: Entry and exit dimension of LocalStationController is not assigned in transform " + transform.name + " of parent " + transform.parent.name);
            }

            linkedVRCStation = (VRCStation)transform.GetComponent(typeof(VRCStation));
            linkedMainStationController = LinkedMainController.GetLinkedMainStationController;
        }

        public void UseAttachedStation()
        {
            if (!stationInUse)
            {
                //LinkedMainController.OutputLogText("Attempting station entry");
                linkedVRCStation.UseStation(Networking.LocalPlayer);
                linkedMainStationController.PlayerIsCurrentlyUsingOtherStation = true;
            }
        }

        public override void OnStationEntered(VRCPlayerApi player)
        {
            attachedPlayerId = player.playerId;

            LockStationEntry(true);

            if (player == Networking.LocalPlayer)
            {
                linkedMainStationController.PlayerIsCurrentlyUsingOtherStation = true;
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
                linkedMainStationController.PlayerIsCurrentlyUsingOtherStation = false;
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