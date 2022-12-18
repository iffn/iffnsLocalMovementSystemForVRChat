using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

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
        readonly string newLine = "\n";

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
                return;
            }

            if (attachedPlayerID == myPlayerId)
            {
                DimensionController attachedDimension = LinkedDimensionController.GetCurrentDimension();
                transform.parent = attachedDimension.transform;

                int currentDimensionId = attachedDimension.GetDimensionId();

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
        public string GetCurrentDebugState()
        {
            string returnString = "";

            returnString += "Local pickup with hand sync debug:" + newLine;

            returnString += "attachedDimensionId = " + attachedDimensionId + newLine;
            returnString += "attachedPlayerID = " + attachedPlayerID + newLine;
            returnString += "rightInsteadOfLeftHand = " + rightInsteadOfLeftHand + newLine;
            returnString += "LocalPositionSync = " + LocalPositionSync + newLine;
            returnString += "LocalEulerRotationSync = " + LocalEulerRotationSync + newLine;
            returnString += "myPlayerId = " + myPlayerId + newLine;
            if (attachedPlayer == null) returnString += "attachedPlayer = null" + newLine;
            else returnString += "attachedPlayer.id = " + attachedPlayer.playerId + newLine;

            return returnString;
        }
    }
}