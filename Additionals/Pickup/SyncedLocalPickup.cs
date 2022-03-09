using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using iffnsStuff.iffnsVRCStuff.SyncControllers;

namespace iffnsStuff.iffnsVRCStuff.iffnsLocalMovementSystemForVRChat.Additionals
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    [RequireComponent(typeof(VRC_Pickup))]
    public class SyncedLocalPickup : UdonSharpBehaviour
    {
        //Unity assignments
        [SerializeField] SyncControllerVector3Linear LinkedLocalPositionSync;
        [SerializeField] SyncControllerQuaternionLinear LinkedLocalRotationSync;
        [SerializeField] MainDimensionAndStationController LinkedMainDimensionAndStationController;
        [SerializeField] DimensionController InitiallyLinkedDimension;
        
        //Synced variables
        [UdonSynced] int attachedDimensionId = 0;
        [UdonSynced] int attachedPlayerID = -1;

        //Runtime variables
        VRC_Pickup linkedPickup;
        int myPlayerId = 0;
        MainDimensionController LinkedDimensionController;
        [UdonSynced] int prevAttachedDimensionId = 0;
        [UdonSynced] int prevAttachedPlayerID = -1;

        private void Start()
        {
            linkedPickup = (VRC_Pickup)transform.GetComponent(typeof(VRC_Pickup));
            myPlayerId = Networking.LocalPlayer.playerId;
            LinkedDimensionController = LinkedMainDimensionAndStationController.GetLinkedDimensionController();

            if(VRCPlayerApi.GetPlayerCount() == 1)
            {
                LinkedLocalPositionSync.SetValue(transform.localPosition);
                LinkedLocalRotationSync.SetValue(transform.localRotation);
            }
        }

        void Update()
        {
            if (attachedPlayerID == myPlayerId) //If currently held by myself
            {
                int currentDimensionId = LinkedDimensionController.GetCurrentDimension().GetDimensionId();

                if (attachedDimensionId != currentDimensionId)
                {
                    attachedDimensionId = currentDimensionId;
                    RequestSerialization();
                }

                LinkedLocalPositionSync.SetValue(transform.localPosition);
                LinkedLocalRotationSync.SetValue(transform.localRotation);
            }
            else
            {
                transform.localPosition = LinkedLocalPositionSync.GetValue();
                transform.localRotation = LinkedLocalRotationSync.GetValue();
            }
        }

        public override void OnPickup()
        {
            attachedPlayerID = myPlayerId;
            RequestSerialization();
        }

        public override void OnDrop()
        {
            attachedPlayerID = -1;

            DimensionController attachedDimension = LinkedDimensionController.GetCurrentDimension();

            attachedDimensionId = attachedDimension.GetDimensionId();

            transform.parent = attachedDimension.transform;

            RequestSerialization();
        }

        public override void OnDeserialization()
        {
            linkedPickup.pickupable = attachedPlayerID < 1 || attachedPlayerID == myPlayerId;

            if (prevAttachedDimensionId != attachedDimensionId)
            {
                transform.parent = LinkedDimensionController.GetDimension(attachedDimensionId).transform;
            }

            prevAttachedDimensionId = attachedDimensionId;
            prevAttachedPlayerID = attachedPlayerID;
        }

        
    }
}