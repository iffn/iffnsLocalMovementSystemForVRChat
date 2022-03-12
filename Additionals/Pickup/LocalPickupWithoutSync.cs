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

        void Start()
        {
            LinkedDimensionController = LinkedMainDimensionAndStationController.GetLinkedDimensionController();
            transform.parent = InitiallyLinkedDimension.transform;
        }

        public override void OnDrop()
        {
            DimensionController attachedDimension = LinkedDimensionController.GetCurrentDimension();
            transform.parent = attachedDimension.transform;
        }
    }
}