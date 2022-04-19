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


    }
}