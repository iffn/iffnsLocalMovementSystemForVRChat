using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace iffnsStuff.iffnsVRCStuff.iffnsLocalMovementSystemForVRChat
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
    public class DimensionSyncAngleQuaternion : UdonSharpBehaviour
    {
        [UdonSynced(UdonSyncMode.Smooth)] Quaternion RotationSync;

        [SerializeField] DimensionController LinkedDimension;

        void Update()
        {
            if (Networking.IsOwner(gameObject))
            {
                RotationSync = LinkedDimension.LocalDimensionRotation;
            }
            else
            {
                LinkedDimension.LocalDimensionRotation = RotationSync;
            }
        }
    }
}