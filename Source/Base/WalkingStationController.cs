using Cyan.PlayerObjectPool;
using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Exceptions;
using static VRC.Core.ApiAvatar;

namespace iffnsStuff.iffnsVRCStuff.iffnsLocalMovementSystemForVRChat
{
    [RequireComponent(typeof(VRCStation))]
    public class WalkingStationController : CyanPlayerObjectPoolObject
    {
        //Unity Assignments:
        [SerializeField] PlayerPositionSync LinkedPositionSync;

        //Synced variables:
        [UdonSynced] bool usingStation = false;

        //Runtime variables:
        MainDimensionAndStationController linkedMainController;
        VRCStation attachedStation;

        //Variable access:
        public int OwnerId
        {
            get
            {
                return Owner.playerId;
            }
        }

        public void Setup(MainDimensionAndStationController linkedMainController)
        {
            if (LinkedPositionSync == null)
            {
                this.linkedMainController.OutputLogWarning("LinkedPositionSync not set");
            }

            attachedStation = GetComponent<VRCStation>();

            this.linkedMainController = linkedMainController;

            LinkedPositionSync.Setup(linkedStation: this, linkedMainController.LinkedDimensionController);
        }

        public string GetCurrentDebugState()
        {
            string returnString = "";
            returnString += $"Walking station controller debug at {Time.time}\n";

            if (Owner == null) return returnString;

            returnString += $"Assigned player = {Owner.playerId}: {Owner.displayName}\n";

            returnString += $"{nameof(inStation)} = {inStation}\n";

            returnString += LinkedPositionSync.GetCurrentDebugState();

            return returnString;
        }

        public void SetDimensionAttachment(DimensionController newDimension)
        {
            LinkedPositionSync.SetDimensionAttachment(newDimension);
        }

        public void EnterStation()
        {
            transform.SetPositionAndRotation(Networking.LocalPlayer.GetPosition(), Networking.LocalPlayer.GetRotation());

            attachedStation.UseStation(Networking.LocalPlayer);
        }

        bool inStation = false;

        public void CheckStationEntry()
        {
            if (!inStation)
            {
                linkedMainController.OutputLogWarning("Owner not yet in station");
                EnterStation();
                SendCustomEventDelayedFrames(nameof(CheckStationEntry), 0, VRC.Udon.Common.Enums.EventTiming.Update);
            }
        }

        #region ObjectPool;

        //Object pool object functions:

        // This method will be called on all clients when the object is enabled and the Owner has been assigned.
        [PublicAPI]
        public override void _OnOwnerSet()
        {
            //linkedMainController.OutputLogText($"Main dimension position 4 = {linkedMainController.LinkedDimensionController.CurrentDimension.transform.position}");
            linkedMainController.OutputCurrentDebug(4);

            //attachedStation.seated = false;

            LinkedPositionSync.gameObject.SetActive(true);

            if (Owner.isLocal)
            {
                attachedStation.PlayerMobility = VRCStation.Mobility.Mobile;
                EnterStation();

                SendCustomEventDelayedFrames(nameof(CheckStationEntry), 0, VRC.Udon.Common.Enums.EventTiming.Update);

                usingStation = true;
                RequestSerialization();

                LinkedPositionSync.Claim();

                linkedMainController.GetLinkedMainStationController.SetupFromMainStation(this);
            }
            else
            {
                attachedStation.PlayerMobility = VRCStation.Mobility.Immobilize;
            }

            linkedMainController.OutputCurrentDebug(5);
        }

        // This method will be called on all clients when the original owner has left and the object is about to be disabled.
        [PublicAPI]
        public override void _OnCleanup()
        {
            usingStation = false;
            LinkedPositionSync.gameObject.SetActive(false);
        }

        #endregion

        #region VRChat functions;

        //VRChat functions:
        public override void OnStationEntered(VRCPlayerApi player)
        {
            if (player.isLocal)
            {
                linkedMainController.OutputLogText("Player entered station");
            }

            if(player == Owner)
            {
                inStation = true;
            }
        }

        public override void OnStationExited(VRCPlayerApi player)
        {
            if (player.isLocal)
            {
                linkedMainController.OutputLogText("Owner exited station");
            }

            if(player == Owner)
            {
                inStation = false;
            }
        }

        #endregion
    }
}