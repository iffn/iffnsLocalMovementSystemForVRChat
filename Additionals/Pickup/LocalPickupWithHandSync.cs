﻿using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using iffnsStuff.iffnsVRCStuff.DebugOutput;

namespace iffnsStuff.iffnsVRCStuff.iffnsLocalMovementSystemForVRChat.Additionals
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    [RequireComponent(typeof(VRC_Pickup))]
    public class LocalPickupWithHandSync : UdonSharpBehaviour
    {
        //Unity assignments
        [SerializeField] MainDimensionAndStationController LinkedMainDimensionAndStationController;
        [SerializeField] DimensionController InitiallyLinkedDimension;

        //Synced variables
        [UdonSynced] int attachedDimensionId = 0;
        [UdonSynced] int attachedPlayerID = -1;
        [UdonSynced] bool rightInsteadOfLeftHand = true;
        [UdonSynced] Vector3 LocalPositionSync;
        [UdonSynced] Vector3 LocalEulerRotationSync; //Euler angles are enough since no transition between rotations are needed

        //Runtime variables
        VRC_Pickup linkedPickup;
        int myPlayerId = -1;
        MainDimensionController LinkedDimensionController;
        VRCPlayerApi attachedPlayer;

        readonly Quaternion offsetRotation = Quaternion.Euler(new Vector3(0, 90, 90));

        void Start()
        {
            linkedPickup = (VRC_Pickup)transform.GetComponent(typeof(VRC_Pickup));
            myPlayerId = Networking.LocalPlayer.playerId;
            LinkedDimensionController = LinkedMainDimensionAndStationController.GetLinkedDimensionController();

            if (VRCPlayerApi.GetPlayerCount() == 1)
            {
                attachedDimensionId = InitiallyLinkedDimension.GetDimensionId();
                transform.parent = InitiallyLinkedDimension.transform;
                LocalPositionSync = transform.localPosition;
                LocalEulerRotationSync = transform.localRotation.eulerAngles;
            }
        }

        void Update()
        {
            if (attachedPlayerID < 1)
            {
                PrepareDebugState();
                return;
            }

            if (attachedPlayerID == myPlayerId)
            {
                int currentDimensionId = LinkedDimensionController.GetCurrentDimension().GetDimensionId();

                if (currentDimensionId != attachedDimensionId) //Only needed if the player leaves while holding the pickup. Can be removed if this information is not lost
                {
                    attachedDimensionId = currentDimensionId;
                    RequestSerialization();
                }
            }
            else
            {
                HumanBodyBones attachedHand;

                if (rightInsteadOfLeftHand) attachedHand = HumanBodyBones.RightHand;
                else attachedHand = HumanBodyBones.LeftHand;

                transform.position = attachedPlayer.GetBonePosition(attachedHand);
                transform.rotation = attachedPlayer.GetBoneRotation(attachedHand) * offsetRotation; //Offset because VRChat is weird
            }
            
            PrepareDebugState();
        }

        public override void OnPickup()
        {
            SetOwner();

            //Sync values
            attachedPlayerID = myPlayerId;
            rightInsteadOfLeftHand = linkedPickup.currentHand == VRC_Pickup.PickupHand.Right;

            RequestSerialization();
        }

        public override void OnDrop()
        {
            SetOwner();

            //Local values
            DimensionController attachedDimension = LinkedDimensionController.GetCurrentDimension();
            transform.parent = attachedDimension.transform;

            //Sync values
            attachedPlayerID = -1;
            attachedDimensionId = attachedDimension.GetDimensionId();
            LocalPositionSync = transform.localPosition;
            LocalEulerRotationSync = transform.localRotation.eulerAngles;
            
            RequestSerialization();
        }

        public override void OnDeserialization()
        {
            //Ability to pickup
            if(attachedPlayerID > 0)
            {
                linkedPickup.pickupable = false;
                attachedPlayer = VRCPlayerApi.GetPlayerById(attachedPlayerID);
            }
            else
            {
                linkedPickup.pickupable = true;
                transform.parent = LinkedDimensionController.GetDimension(attachedDimensionId).transform;
                attachedPlayer = null;
                transform.localPosition = LocalPositionSync;
                transform.localRotation = Quaternion.Euler(LocalEulerRotationSync);
            }
        }

        public override void OnOwnershipTransferred(VRCPlayerApi player)
        {
            if (player != Networking.LocalPlayer) return;   //Ignore if not transfered to me
            if (attachedPlayerID < 1) return;               //Ignore if not attached to a player
            if (attachedPlayerID == myPlayerId) return;     //Ignore if attached to me

            //Resync position if someone leaves while holding the item
            attachedPlayerID = -1;
            attachedPlayer = null;
            transform.parent = LinkedDimensionController.GetDimension(attachedDimensionId).transform;
            LocalPositionSync = transform.localPosition;
            LocalEulerRotationSync = transform.localRotation.eulerAngles;
            linkedPickup.pickupable = true;

            RequestSerialization();
        }

        void SetOwner()
        {
            if (!Networking.IsOwner(gameObject)) Networking.SetOwner(player: Networking.LocalPlayer, obj: gameObject);
        }

        //Debug
        const string newLine = "\n";
        [SerializeField] SingleScriptDebugState LinkedStateOutput;

        public void PrepareDebugState()
        {
            if (LinkedStateOutput == null) return;

            if (!LinkedStateOutput.IsReadyForOutput()) return;

            string name = "LocalPickupWithHandSync";

            string currentState = "";

            currentState += "attachedDimensionId = " + attachedDimensionId + newLine;
            currentState += "attachedPlayerID = " + attachedPlayerID + newLine;
            currentState += "rightInsteadOfLeftHand = " + rightInsteadOfLeftHand + newLine;
            currentState += "LocalPositionSync = " + LocalPositionSync + newLine;
            currentState += "LocalEulerRotationSync = " + LocalEulerRotationSync + newLine;
            currentState += "myPlayerId = " + myPlayerId + newLine;
            if (attachedPlayer == null) currentState += "attachedPlayer = null" + newLine;
            else currentState += "attachedPlayer.id = " + attachedPlayer.playerId + newLine;

            LinkedStateOutput.SetCurrentState(displayName: name, currentState: currentState);
        }
    }
}