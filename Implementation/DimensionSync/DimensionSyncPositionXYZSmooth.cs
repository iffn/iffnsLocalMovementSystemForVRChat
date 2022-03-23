using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace iffnsStuff.iffnsVRCStuff.iffnsLocalMovementSystemForVRChat
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
    public class DimensionSyncPositionXYZSmooth : UdonSharpBehaviour
    {
        [UdonSynced(UdonSyncMode.Smooth)] Vector3 PositionSync;

        [SerializeField] DimensionController LinkedDimension;

        void Update()
        {
            if (Networking.IsOwner(gameObject))
            {
                PositionSync = LinkedDimension.LocalDimensionPosition;
            }
            else
            {
                LinkedDimension.LocalDimensionPosition = PositionSync;
            }
        }
    }
}