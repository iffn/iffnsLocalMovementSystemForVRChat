using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace iffnsStuff.iffnsVRCStuff.iffnsLocalMovementSystemForVRChat.Additionals
{
    [RequireComponent(typeof(VRC_Pickup))]
    public class LocalPickupWithoutSync : UdonSharpBehaviour
    {
        //Unity assignments
        [SerializeField] MainDimensionAndStationController LinkedMainDimensionAndStationController;
        [SerializeField] DimensionController InitiallyLinkedDimension;

        //Synced variables
        MainDimensionController LinkedDimensionController;
        VRC_Pickup linkedPickup;

        //Fixed variables
        readonly string newLine = "\n";

        void Start()
        {
            linkedPickup = (VRC_Pickup)transform.GetComponent(typeof(VRC_Pickup));
            LinkedDimensionController = LinkedMainDimensionAndStationController.GetLinkedDimensionController();
            transform.parent = InitiallyLinkedDimension.transform;
        }

        private void Update()
        {
            if (linkedPickup.IsHeld)
            {
                DimensionController attachedDimension = LinkedDimensionController.GetCurrentDimension();
                transform.parent = attachedDimension.transform;
            }
        }

        public override void OnDrop()
        {
            DimensionController attachedDimension = LinkedDimensionController.GetCurrentDimension();
            transform.parent = attachedDimension.transform;
        }

        //Debug
        public string GetCurrentDebugState()
        {
            string returnString = "";

            returnString += "Local pickup without sync debug:" + newLine;

            returnString += "Parent = " + transform.parent.name + newLine;
            returnString += "Local position = " + transform.localPosition + newLine;
            returnString += "Local rotation = " + transform.localRotation.eulerAngles + newLine;

            return returnString;
        }
    }
}