using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace iffnsStuff.iffnsVRCStuff.iffnsLocalMovementSystemForVRChat
{
    public class DimensionSyncAngleYLinear : UdonSharpBehaviour
    {
        //Unity assignments
        [SerializeField] DimensionController LinkedDimension;
        [SerializeField] bool ResetXZAngle = true;

        //[UdonSynced] float AngleY;
        [UdonSynced] float RotationSyncY;

        const float deserializationTimeThreshold = 1;
        float lastDeserializationTime = 0;
        float lastDeserializationDeltaTime = 0;

        float lastSyncedRotationSyncY = 0;
        float lastLocalHeadingValue = 0;
        float localHeadingSpeed = 0;

        void Start()
        {

        }

        private void Update()
        {
            if (Networking.IsOwner(gameObject))
            {
                RotationSyncY = LinkedDimension.LocalDimensionRotation.eulerAngles.y;
            }
            else
            {
                if (lastSyncedRotationSyncY != RotationSyncY)
                {
                    Deserialize();
                }

                if (ResetXZAngle)
                {
                    LinkedDimension.LocalDimensionRotation = Quaternion.Euler(Vector3.up * GetCurrentRotationAngle());
                }
                else
                {
                    Vector3 currentEulerRotation = LinkedDimension.LocalDimensionRotation.eulerAngles;

                    LinkedDimension.LocalDimensionRotation = Quaternion.Euler(new Vector3(currentEulerRotation.x, GetCurrentRotationAngle(), currentEulerRotation.z));
                }
            }
        }

        float GetCurrentRotationAngle()
        {
            float currentDeltaTime = Time.time - lastDeserializationTime;

            if (currentDeltaTime < lastDeserializationDeltaTime)
            {
                float returnValue = lastLocalHeadingValue + localHeadingSpeed * currentDeltaTime;

                if (returnValue > 360) returnValue -= 360;
                else if (returnValue < 0) returnValue += 360;

                return returnValue;
            }
            else
            {
                return RotationSyncY;
            }
        }

        void Deserialize()
        {
            //Time
            lastDeserializationDeltaTime = Time.time - lastDeserializationTime - Time.deltaTime;
            if (lastDeserializationDeltaTime > deserializationTimeThreshold) lastDeserializationDeltaTime = deserializationTimeThreshold;

            lastDeserializationTime = Time.time;

            //Rotation
            lastSyncedRotationSyncY = RotationSyncY;
            lastLocalHeadingValue = LinkedDimension.LocalDimensionRotation.eulerAngles.y;

            float headingOffset = RotationSyncY - lastLocalHeadingValue;

            if (headingOffset > 180)
            {
                headingOffset -= 360;
            }
            else if (headingOffset < -180)
            {
                headingOffset += 360;
            }

            localHeadingSpeed = headingOffset / lastDeserializationDeltaTime;
        }
    }
}