using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace iffnsStuff.iffnsVRCStuff.iffnsLocalMovementSystemForVRChat
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
    public class PlayerPositionSync : UdonSharpBehaviour
    {
        [UdonSynced, FieldChangeCallback(nameof(LocalPositionAndHeadingSync))] Vector4 syncedLocalPlayerPositionAndHeading = Vector4.zero;
        [UdonSynced, FieldChangeCallback(nameof(AttachedDimensionSync))] int syncedAttachedDimensionId = -1;

        Vector3 syncedLocalPosition;
        float syncedLocalHeadingDeg;

        MainDimensionController linkedMainDimensionController;

        bool ownedByMe = false;
        bool synced = true;

        Transform stationTransform;

        DimensionController attachedDimension;

        //Sync helpers
        const float deserializationTimeThreshold = 1;
        float lastDeserializationDeltaTime = 0;
        float lastDeserializationTime = 0;
        Vector3 localPositionSpeed = Vector3.zero;
        Vector3 lastLocalPosition = Vector3.zero;
        float lastLocalHeadingDeg = 0;
        float localHeadingSpeed = 0;

        bool updatedPositionAndRotation = false;
        bool updatedDimension = false;

        public Vector4 LocalPositionAndHeadingSync
        {
            get
            {
                return syncedLocalPlayerPositionAndHeading;
            }
            set
            {
                lastLocalPosition = syncedLocalPosition;
                lastLocalHeadingDeg = syncedLocalHeadingDeg;

                syncedLocalPlayerPositionAndHeading = value; //Otherwise called each frame

                syncedLocalPosition = value;
                syncedLocalHeadingDeg = value.w;

                updatedPositionAndRotation = true;

                //CalculatePositionAndHeadingSpeed();
            }
        }

        public int AttachedDimensionSync
        {
            get
            {
                return syncedAttachedDimensionId;
            }
            set
            {
                syncedAttachedDimensionId = value; //Otherwise called each frame

                updatedDimension = true;
                //UpdateDimensionAttachment();
            }
        }

        Transform AttachedDimensionTransform
        {
            get
            {
                return stationTransform.parent;
            }
        }

        public void Setup(WalkingStationController linkedStation, MainDimensionController linkedMainDimensionController)
        {
            this.linkedMainDimensionController = linkedMainDimensionController;
            stationTransform = linkedStation.transform;
        }

        //Owner functions:
        public void Claim()
        {
            if(!Networking.IsOwner(gameObject)) Networking.SetOwner(Networking.LocalPlayer, gameObject);
            ownedByMe = true;

            DimensionController attachedDimension = linkedMainDimensionController.CurrentDimension;

            syncedAttachedDimensionId = attachedDimension.GetDimensionId();
            stationTransform.parent = attachedDimension.transform;
        }

        public void SetDimensionAttachment(DimensionController newDimension)
        {
            syncedAttachedDimensionId = newDimension.GetDimensionId();
            stationTransform.parent = newDimension.transform;
        }

        //Update:
        private void Update()
        {
            if (ownedByMe)
            {
                if (synced)
                {
                    syncedLocalPosition = AttachedDimensionTransform.InverseTransformPoint(Networking.LocalPlayer.GetPosition()); //Error happens when you leave the world: Ignore
                    syncedLocalHeadingDeg = (Quaternion.Inverse(AttachedDimensionTransform.rotation) * Networking.LocalPlayer.GetRotation()).eulerAngles.y;
                    syncedLocalPlayerPositionAndHeading = new Vector4(syncedLocalPosition.x, syncedLocalPosition.y, syncedLocalPosition.z, syncedLocalHeadingDeg);
                }
            }
            else
            {
                if (synced)
                {
                    if (updatedDimension)
                    {
                        UpdateDimensionAttachment();

                        CalculatePositionAndHeadingSpeed();

                        updatedDimension = false;
                        updatedPositionAndRotation = false;
                    }
                    else if (updatedPositionAndRotation)
                    {
                        CalculatePositionAndHeadingSpeed();

                        updatedPositionAndRotation = false;
                    }

                    UpdatePositionAndRotationValues();
                }
            }
        }

        

        //Non-owner functions
        public void UpdateDimensionAttachment() //ToDo: Encapsulate
        {
            attachedDimension = linkedMainDimensionController.GetDimension(syncedAttachedDimensionId);

            Transform newDimensionTransform = attachedDimension.transform;

           //Ensure smooth sync during station transition
            lastLocalPosition = newDimensionTransform.InverseTransformPoint(AttachedDimensionTransform.TransformPoint(lastLocalPosition));

            Vector3 headingDirection = Quaternion.Euler(0, lastLocalHeadingDeg, 0) * Vector3.forward;
            headingDirection = newDimensionTransform.InverseTransformDirection(AttachedDimensionTransform.TransformDirection(headingDirection));
            lastLocalHeadingDeg = Quaternion.LookRotation(headingDirection).eulerAngles.y;

            stationTransform.parent = newDimensionTransform;
        }

        void CalculatePositionAndHeadingSpeed()
        {
            //Time
            lastDeserializationDeltaTime = Time.time - lastDeserializationTime - Time.deltaTime;
            if (lastDeserializationDeltaTime > deserializationTimeThreshold) lastDeserializationDeltaTime = deserializationTimeThreshold;

            lastDeserializationTime = Time.time;

            //Position speed
            localPositionSpeed = (syncedLocalPosition - lastLocalPosition) / lastDeserializationDeltaTime;

            //Rotation speed
            float headingOffset = syncedLocalHeadingDeg - lastLocalHeadingDeg;

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

        void UpdatePositionAndRotationValues()
        {
            float currentDeltaTime = Time.time - lastDeserializationTime;

            Vector3 localPlayerPosition;
            float localPlayerHeadingDeg;

            if (currentDeltaTime < lastDeserializationDeltaTime)
            {
                localPlayerPosition = lastLocalPosition + localPositionSpeed * currentDeltaTime;
                localPlayerHeadingDeg = lastLocalHeadingDeg + localHeadingSpeed * currentDeltaTime;
            }
            else
            {
                localPlayerPosition = syncedLocalPosition;
                localPlayerHeadingDeg = syncedLocalHeadingDeg;
            }

            stationTransform.localPosition = localPlayerPosition;
            stationTransform.localRotation = Quaternion.Euler(localPlayerHeadingDeg * Vector3.up);
        }

        public string GetCurrentDebugState()
        {
            string returnString = "";
            returnString += $"Player position sync debug at {Time.time}\n";

            returnString += $"Owned by me = {ownedByMe}\n";
            returnString += $"Synced positon and heading = {syncedLocalPlayerPositionAndHeading}\n";
            returnString += $"Synced attached dimension id = {syncedAttachedDimensionId}\n";

            if(stationTransform != null)
            {
                returnString += $"{nameof(AttachedDimensionTransform)}.name = {AttachedDimensionTransform.name}\n";
            }
            else
            {
                linkedMainDimensionController.LinkedMainController.OutputLogWarning("attachedDimensionTransform = null\n");
            }

            return returnString;
        }
    }
}