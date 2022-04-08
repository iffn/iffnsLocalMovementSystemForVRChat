using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;

namespace iffnsStuff.iffnsVRCStuff.iffnsLocalMovementSystemForVRChat
{
    [RequireComponent(typeof(VRCStation))]
    public class WalkingStationController : UdonSharpBehaviour
    {
        //Unity Assignments:
        [SerializeField] public WalkingStationControllerManualSync LinkedStationManualSync;
        [SerializeField] public StationAssignmentController LinkedStationAssigner;

        //Synced variables:
        //[HideInInspector] [UdonSynced(UdonSyncMode.Smooth)] public Vector3 LocalPlayerPosition = Vector3.zero;
        //[UdonSynced(UdonSyncMode.Smooth)] Quaternion LocalPlayerRotation = Quaternion.identity; //Currently using Quaternion since heading angle transition from 360 to 0 causes a spin
        //[UdonSynced] float LocalPlayerHeading =0; //No special sync mode. Angle lerp calculated manually due to 360 to 0 overflow

        [UdonSynced] Vector3 SyncedLocalPlayerPosition = Vector3.zero;
        [UdonSynced] float SyncedLocalPlayerHeading = 0; //Currently using Quaternion since heading angle transition from 360 to 0 causes a spin
        [HideInInspector] [UdonSynced] int AttachedDimensionId;

        //Runtime variables:
        [HideInInspector] public VRCStation LinkedVRCStation;
        float lastRecievedValue = 0;
        float oldSavedValue = 0;
        float oldLastSyncTime = 0;
        float oldTimeSinceLastSync; //Used to check serialization frequency
        Vector3 oldPreviousPlayerPosition = Vector3.zero;
        Quaternion oldPreviousPlayerRotation = Quaternion.identity;
        [HideInInspector] public int oldDeserializationsSinceLastTransition = 0;

        [HideInInspector] public bool StationTransitioning = false;
        bool StationReadyToTransition = true;
        public bool IsStationReadyToTransition()
        {
            return StationReadyToTransition;
        }


        [HideInInspector] public Transform StationTransformationHelper;
        [HideInInspector] public int stationState = -1;
        /*
            Station states:
            -1 = Station not occupied
            0 = Station of local player
            1 = Station of other player
        */

        //newLine = backslash n which is interpreted as a new line when showing the code in a text field
        string newLine = "\n";

        //Functions:
        public void Setup()
        {
            LinkedVRCStation = (VRCStation)transform.GetComponent(typeof(VRCStation));

            //Error checks
            if (LinkedStationManualSync == null)
                LinkedStationAssigner.GetLinkedMainController().OutputLogWarning("LinkedStationManualSync not assigned in Station " + transform.name);
            if (LinkedStationManualSync.LinkedWalkingStationController == null)
                LinkedStationAssigner.GetLinkedMainController().OutputLogWarning("LinkedStationManualSync.LinkedStationAutoSync not assigned in Station " + transform.name);
            if (LinkedVRCStation == null)
                LinkedStationAssigner.GetLinkedMainController().OutputLogWarning("LinkedVRCStation not assigned in Station " + transform.name);
            if (LinkedVRCStation.stationEnterPlayerLocation == null)
                LinkedStationAssigner.GetLinkedMainController().OutputLogWarning("LinkedVRCStation.EnterLocation not assigned in Station " + transform.name);
            if (LinkedVRCStation.stationExitPlayerLocation == null)
                LinkedStationAssigner.GetLinkedMainController().OutputLogWarning("LinkedVRCStation.ExitLocation not assigned in Station " + transform.name);

            //Setup
            LinkedStationManualSync.Setup();

            if (LinkedStationAssigner.DisableUsingPlayerStationsOnStart)
            {
                LinkedVRCStation.canUseStationFromStation = false;
            }
        }

        public void ResetStation()
        {
            LocalPlayerPosition = Vector3.zero;
            //LocalPlayerRotation = Quaternion.identity;
            LocalPlayerHeading = 0;
            stationState = -1;
            oldPreviousPlayerPosition = Vector3.zero;
            oldPreviousPlayerRotation = Quaternion.identity;
            oldDeserializationsSinceLastTransition = 0;
            transform.parent = LinkedStationAssigner.transform;
            StationTransitioning = false;


            LinkedStationManualSync.ResetStation();
        }

        void Start()
        {
            //Use setup instead
        }

        private void Update()
        {
            switch (stationState)
            {
                case -1:
                    //Do nothing
                    break;
                case 0:
                    //0 = Station of local player
                    StationTransformationHelper.position = Networking.LocalPlayer.GetPosition(); //2 Errors happen when you leave the world: Ignore
                    StationTransformationHelper.rotation = Networking.LocalPlayer.GetRotation();

                    //LocalPlayerPosition = StationTransformationHelper.localPosition;
                    //LocalPlayerRotation = StationTransformationHelper.localRotation;
                    SyncedLocalPlayerPosition = StationTransformationHelper.localPosition;
                    SyncedLocalPlayerHeading = StationTransformationHelper.localRotation.eulerAngles.y;
                    AttachedDimensionId = LinkedStationManualSync.AttachedDimensionId;
                    //LocalPlayerHeading = StationTransformationHelper.localRotation.eulerAngles.y;
                    break;

                case 1:
                    //1 = Station of other player

                    //Freeze player during transition due to large coordinate change, which messes with the smooth sync
                    //ToDo: Make transition smooth
                    if (AttachedDimensionId != previousDimensionID)
                    {
                        LinkedStationManualSync.DeserializeDimensionID(newDimensionId: AttachedDimensionId);
                        previousDimensionID = AttachedDimensionId;

                        LocalPlayerPosition = transform.localPosition;
                        LocalPlayerHeading = transform.localRotation.eulerAngles.y;

                        Deserialize();
                    }
                    else if ((lastSycnedLocalPositionValue - SyncedLocalPlayerPosition).magnitude > 0.001f || Mathf.Abs(lastSyncedLocalPlayerHeading - SyncedLocalPlayerHeading) > 0.01f)
                    {
                        Deserialize();
                    }
                    else
                    {
                        SetCurrentLocalPositionAndRotationValues();
                    }

                    transform.localPosition = LocalPlayerPosition;
                    transform.localRotation = Quaternion.Euler(Vector3.up * LocalPlayerHeading);
                    //transform.localRotation = LocalPlayerRotation;


                    /*
                    if (oldDeserializationsSinceLastTransition == 1)
                    {
                        transform.parent = LinkedStationManualSync.GetAttachedDimension().transform;

                        transform.position = oldPreviousPlayerPosition;
                        transform.rotation = oldPreviousPlayerRotation;
                    }
                    else if (oldDeserializationsSinceLastTransition == 2)
                    {
                        transform.position = oldPreviousPlayerPosition;
                        transform.rotation = oldPreviousPlayerRotation;
                    }
                    else
                    {
                        transform.localPosition = LocalPlayerPosition;
                        transform.localRotation = LocalPlayerRotation;
                        //ToDo: Calculate heading
                    }
                    */

                    //transform.localRotation = Quaternion.Euler(Vector3.up * LocalPlayerRotation);

                    /*
                    float heading = RemapFloatAngle(
                        inputMin: lastSyncTime,
                        inputMax: lastSyncTime + timeSinceLastSync,
                        outputMin: lastRecievedValue,
                        outputMax: LocalPlayerHeading,
                        inputValue: Time.time);

                    transform.localRotation = Quaternion.Euler(Vector3.up * heading);
                    */

                    break;
            }
        }

        public override void OnStationEntered(VRCPlayerApi player)
        {
            if (player == Networking.LocalPlayer)
            {
                LinkedStationAssigner.LocalPlayerEnteredStation();
            }
        }

        public override void OnStationExited(VRCPlayerApi player)
        {
            if (player == Networking.LocalPlayer)
            {
                LinkedStationAssigner.LocalPlayerExitedStation();
            }
        }

        bool isTransitioning = false;
        float transitionStartTime = 0;
        Vector3 transitionStartPosition = Vector3.zero;

        public void PlayerIsTransitioning()
        {
            transform.parent = LinkedStationManualSync.GetAttachedDimension().transform;
            isTransitioning = true;
            transitionStartTime = Time.time;
            transitionStartPosition = SyncedLocalPlayerPosition;

            /*
            oldDeserializationsSinceLastTransition = 0;
            oldPreviousPlayerPosition = transform.position;
            oldPreviousPlayerRotation = transform.rotation;
            */
        }

        const float deserializationThreshold = 1;

        float lastDeserializationTime = 0;
        float lastDeserializationDeltaTime = 0;

        Vector3 LocalPlayerPosition = Vector3.zero;
        Vector3 lastSycnedLocalPositionValue = Vector3.zero;
        Vector3 lastLocalPositionValue = Vector3.zero;
        Vector3 localPositionSpeed = Vector3.zero;

        float LocalPlayerHeading = 0;
        float lastSyncedLocalPlayerHeading = 0;
        float lastLocalHeadingValue = 0;
        float localHeadingSpeed = 0;
        /*
        Quaternion LocalPlayerRotation = Quaternion.identity;
        Quaternion SyncedLocalPlayerRotation = Quaternion.identity;
        Vector4 LocalPlayerRotationV4 = Vector4.zero;
        Vector4 SyncedLocalPlayerRotationV4 = Vector4.zero;
        Vector4 lastLocalRotationValueV4 = Vector4.zero;
        Vector4 localRotationSpeedV4 = Vector4.zero;
        */
        float headingOffset = 0;

        void DeserializeDimensionTransition()
        {
            //Time
            lastDeserializationDeltaTime = Time.time - lastDeserializationTime - Time.deltaTime;
            if (lastDeserializationDeltaTime > deserializationThreshold) lastDeserializationDeltaTime = deserializationThreshold;

            lastDeserializationTime = Time.time;

            //Position
            lastSycnedLocalPositionValue = SyncedLocalPlayerPosition; //Sync check
            lastLocalPositionValue = transform.localPosition;
            localPositionSpeed = (SyncedLocalPlayerPosition - lastLocalPositionValue) / lastDeserializationDeltaTime;
        }

        void Deserialize()
        {
            //Time
            lastDeserializationDeltaTime = Time.time - lastDeserializationTime - Time.deltaTime;
            if (lastDeserializationDeltaTime > deserializationThreshold) lastDeserializationDeltaTime = deserializationThreshold;

            lastDeserializationTime = Time.time;

            //Position
            lastSycnedLocalPositionValue = SyncedLocalPlayerPosition; //Sync check
            lastLocalPositionValue = LocalPlayerPosition;
            localPositionSpeed = (SyncedLocalPlayerPosition - lastLocalPositionValue) / lastDeserializationDeltaTime;

            //Rotation
            if (LocalPlayerHeading > 360) LocalPlayerHeading -= 360;
            else if (LocalPlayerHeading < 0) LocalPlayerHeading += 360;

            lastSyncedLocalPlayerHeading = SyncedLocalPlayerHeading;
            lastLocalHeadingValue = LocalPlayerHeading;

            headingOffset = SyncedLocalPlayerHeading - lastLocalHeadingValue;

            if(headingOffset > 180)
            {
                headingOffset -= 360;
            }
            else if (headingOffset < -180)
            {
                headingOffset += 360;
            }

            localHeadingSpeed = headingOffset / lastDeserializationDeltaTime;
        }


        public override void OnDeserialization()
        {
            

            

            //old code
            /*
            oldDeserializationsSinceLastTransition++;

            lastRecievedValue = oldSavedValue;
            oldSavedValue = LocalPlayerRotation.eulerAngles.y;
            //savedValue = LocalPlayerHeading;

            oldTimeSinceLastSync = Time.time - oldLastSyncTime;
            oldLastSyncTime = Time.time;
            */
        }

        float currentDeltaTime = 0;
        int previousDimensionID = 0;

        void SetCurrentLocalPositionAndRotationValues()
        {
            currentDeltaTime = Time.time - lastDeserializationTime;

            if (currentDeltaTime < lastDeserializationDeltaTime)
            {
                LocalPlayerPosition = lastLocalPositionValue + localPositionSpeed * currentDeltaTime;
                LocalPlayerHeading = lastLocalHeadingValue + localHeadingSpeed * currentDeltaTime;

                
                /*
                LocalPlayerRotationV4 = lastLocalRotationValueV4 + localRotationSpeedV4 * currentDeltaTime;

                LocalPlayerRotation = new Quaternion(
                    x: localRotationSpeedV4.x,
                    y: localRotationSpeedV4.y,
                    z: localRotationSpeedV4.z,
                    w: localRotationSpeedV4.w
                    );
                */
            }
            else
            {
                LocalPlayerPosition = SyncedLocalPlayerPosition;
                LocalPlayerHeading = SyncedLocalPlayerHeading;
                //LocalPlayerRotation = SyncedLocalPlayerRotation;
            }
        }

        public string GetCurrentDebugState()
        {
            string returnString = "";
            returnString += "Walking staion debug:" + newLine;

            returnString += "Station name = " + transform.name + newLine;
            returnString += "Current station = " + (LinkedStationAssigner.MyStation == this) + newLine;
            returnString += "PlayerMobility = " + LinkedVRCStation.PlayerMobility + newLine;
            returnString += "---" + newLine;
            returnString += "Deserialization offset position = " + (lastSycnedLocalPositionValue - SyncedLocalPlayerPosition).magnitude + newLine;
            returnString += "Deserialization offset rotation = " + (lastSyncedLocalPlayerHeading - SyncedLocalPlayerHeading) + newLine;
            returnString += "lastDeserializationTime = " + lastDeserializationTime + newLine;
            returnString += "lastDeserializationDeltaTime = " + lastDeserializationDeltaTime + newLine;
            returnString += "currentDeltaTime = " + currentDeltaTime + newLine;
            returnString += "---" + newLine;
            returnString += "SyncedLocalPlayerPosition = " + SyncedLocalPlayerPosition + newLine;
            returnString += "lastSycnedLocalPositionValue = " + lastSycnedLocalPositionValue + newLine;
            returnString += "LocalPlayerPosition = " + LocalPlayerPosition + newLine;
            returnString += "lastLocalPositionValue = " + lastLocalPositionValue + newLine;
            returnString += "localPositionSpeed = " + localPositionSpeed + newLine;
            returnString += "localPositionSpeed.magnitude = " + localPositionSpeed.magnitude + newLine;
            returnString += "---" + newLine;
            returnString += "SyncedLocalPlayerHeading = " + SyncedLocalPlayerHeading + newLine;
            returnString += "lastSyncedLocalPlayerHeading = " + lastSyncedLocalPlayerHeading + newLine;
            returnString += "LocalPlayerHeading = " + LocalPlayerHeading + newLine;
            returnString += "lastLocalHeadingValue = " + lastLocalHeadingValue + newLine;
            returnString += "headingOffset = " + headingOffset + newLine;
            returnString += "localHeadingSpeed = " + localHeadingSpeed + newLine;
            returnString += "---" + newLine;
            //returnString += "LocalPlayerHeading = " + LocalPlayerHeading + newLine;
            returnString += "Current owner ID of Auto = " + Networking.GetOwner(gameObject).playerId + newLine;
            returnString += "Current owner ID of Manual = " + Networking.GetOwner(LinkedStationManualSync.gameObject).playerId + newLine;
            returnString += "DeserializationsSinceLastTransition= " + oldDeserializationsSinceLastTransition + newLine;
            returnString += "timeSinceLastSync = " + oldTimeSinceLastSync + newLine;

            returnString += LinkedStationManualSync.GetCurrentDebugState();

            return returnString;
        }

        public float RemapFloatAngle(float inputMin, float inputMax, float outputMin, float outputMax, float inputValue)
        {
            float t = Mathf.InverseLerp(a: inputMin, b: inputMax, value: inputValue);
            return Mathf.LerpAngle(a: outputMin, b: outputMax, t: t);
        }
    }
}