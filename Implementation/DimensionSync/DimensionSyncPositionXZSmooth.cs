using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace iffnsStuff.iffnsVRCStuff.iffnsLocalMovementSystemForVRChat
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
    public class DimensionSyncPositionXZSmooth : UdonSharpBehaviour
    {
        [UdonSynced(UdonSyncMode.Smooth)] Vector2 PositionSync = new Vector2(100, 100); //ToDo: Fix initially at 0 issue -> Late joiner puts dimension at origin and enters trigger

        [SerializeField] DimensionController LinkedDimension;
        [SerializeField] bool ResetYPosition = true;

        void Update()
        {
            if (Networking.IsOwner(gameObject))
            {
                Vector3 currentPosition = LinkedDimension.LocalDimensionPosition;
                PositionSync = new Vector2(currentPosition.x, currentPosition.z);
            }
            else
            {
                if (ResetYPosition)
                {
                    LinkedDimension.LocalDimensionPosition = new Vector3(PositionSync.x, 0, PositionSync.y);
                }
                else
                {
                    LinkedDimension.LocalDimensionPosition = new Vector3(PositionSync.x, LinkedDimension.LocalDimensionPosition.y, PositionSync.y);
                }
            }
        }
    }
}