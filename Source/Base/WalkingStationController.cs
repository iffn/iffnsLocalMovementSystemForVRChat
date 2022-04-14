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
        [UdonSynced] int AttachedDimensionId = 0;

        //Runtime variables:
        [HideInInspector] public VRCStation LinkedVRCStation;

        DimensionController AttachedDimension;
        MainDimensionAndStationController LinkedMainController;
        MainDimensionController LinkedMainDimensionController;

        public DimensionController GetAttachedDimension()
        {
            return AttachedDimension;
        }

        const float deserializationTimeThreshold = 1;
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

        int previousDimensionID = -1;

        Transform attachedDimensionTransform;
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
            if (LinkedStationManualSync.LinkedWalkingStationController != this)
                LinkedStationAssigner.GetLinkedMainController().OutputLogWarning("LinkedStationManualSync.LinkedStationAutoSync not correctly assigned in Station " + transform.name);
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
            LinkedMainController = LinkedStationAssigner.GetLinkedMainController();
            LinkedMainDimensionController = LinkedMainController.GetLinkedDimensionController();
        }

        public void ResetStation()
        {
            LocalPlayerPosition = Vector3.zero;
            //LocalPlayerRotation = Quaternion.identity;
            LocalPlayerHeading = 0;
            stationState = -1;
            transform.parent = LinkedStationAssigner.transform;
            AttachedDimensionId = 0;


            lastDeserializationTime = 0;
            lastDeserializationDeltaTime = 0;

            LocalPlayerPosition = Vector3.zero;
            lastSycnedLocalPositionValue = Vector3.zero;
            lastLocalPositionValue = Vector3.zero;
            localPositionSpeed = Vector3.zero;

            LocalPlayerHeading = 0;
            lastSyncedLocalPlayerHeading = 0;
            lastLocalHeadingValue = 0;
            localHeadingSpeed = 0;

            previousDimensionID = -1;

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

                    //StationTransformationHelper.position = Networking.LocalPlayer.GetPosition(); //2 Errors happen when you leave the world: Ignore
                    //StationTransformationHelper.rotation = Networking.LocalPlayer.GetRotation();

                    //LocalPlayerPosition = StationTransformationHelper.localPosition;
                    //LocalPlayerRotation = StationTransformationHelper.localRotation;
                    SyncedLocalPlayerPosition = attachedDimensionTransform.InverseTransformPoint(Networking.LocalPlayer.GetPosition());
                    SyncedLocalPlayerHeading = (Quaternion.Inverse(attachedDimensionTransform.rotation) * Networking.LocalPlayer.GetRotation()).eulerAngles.y;

                    //SyncedLocalPlayerPosition =  StationTransformationHelper.localPosition;
                    //SyncedLocalPlayerHeading = StationTransformationHelper.localRotation.eulerAngles.y;
                    //LocalPlayerHeading = StationTransformationHelper.localRotation.eulerAngles.y;
                    break;

                case 1:
                    //1 = Station of other player

                    //Freeze player during transition due to large coordinate change, which messes with the smooth sync
                    //ToDo: Make transition smooth
                    if (AttachedDimensionId != previousDimensionID)
                    {
                        AttachedDimension = LinkedMainDimensionController.GetDimension(AttachedDimensionId);
                        transform.parent = AttachedDimension.transform;

                        previousDimensionID = AttachedDimensionId;

                        LocalPlayerPosition = transform.localPosition;
                        LocalPlayerHeading = transform.localRotation.eulerAngles.y;

                        Deserialize();
                    }
                    else if ((lastSycnedLocalPositionValue - SyncedLocalPlayerPosition).magnitude > 0.001f
                        || Mathf.Abs(lastSyncedLocalPlayerHeading - SyncedLocalPlayerHeading) > 0.01f
                        || Time.time - lastDeserializationTime > deserializationTimeThreshold)
                    {
                        Deserialize();
                    }
                    else
                    {
                        SetCurrentLocalPositionAndRotationValues();
                    }

                    transform.localPosition = LocalPlayerPosition;
                    transform.localRotation = Quaternion.Euler(Vector3.up * LocalPlayerHeading);

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
                LinkedMainController.OutputLogText("Player velocity during station exit = " + Networking.LocalPlayer.GetVelocity());
                     
                Networking.LocalPlayer.SetVelocity(Vector3.zero);

                LinkedStationAssigner.LocalPlayerExitedStation();
            }
        }

        void Deserialize()
        {
            //Time
            lastDeserializationDeltaTime = Time.time - lastDeserializationTime - Time.deltaTime;
            if (lastDeserializationDeltaTime > deserializationTimeThreshold) lastDeserializationDeltaTime = deserializationTimeThreshold;

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

            float headingOffset = SyncedLocalPlayerHeading - lastLocalHeadingValue;

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
            //Apparently sometimes called at least 1 frame before the values are updated
        }

        void SetCurrentLocalPositionAndRotationValues()
        {
            float currentDeltaTime = Time.time - lastDeserializationTime;

            if (currentDeltaTime < lastDeserializationDeltaTime)
            {
                LocalPlayerPosition = lastLocalPositionValue + localPositionSpeed * currentDeltaTime;
                LocalPlayerHeading = lastLocalHeadingValue + localHeadingSpeed * currentDeltaTime;
            }
            else
            {
                LocalPlayerPosition = SyncedLocalPlayerPosition;
                LocalPlayerHeading = SyncedLocalPlayerHeading;
            }
        }

        public void UpdateDimensionAttachment()
        {
            if (AttachedDimensionId == -1) return;
            AttachedDimension = LinkedMainDimensionController.GetDimension(AttachedDimensionId);
            LinkedStationAssigner.StationTransformationHelper.parent = AttachedDimension.transform;
        }

        //Dimension stuff
        public void SetAttachedDimensionReference(DimensionController newDimension)
        {
            AttachedDimension = newDimension;
            AttachedDimensionId = newDimension.GetDimensionId();
            LinkedStationAssigner.StationTransformationHelper.parent = newDimension.transform;
            attachedDimensionTransform = newDimension.transform;    
            //StationTransformationHelper.parent = newDimension.transform;
            //RequestSerialization();
        }

        public void SetupDimensionAttachment()
        {
            //Set dimension
            AttachedDimension = LinkedMainDimensionController.GetDimension(AttachedDimensionId);
            attachedDimensionTransform = AttachedDimension.transform;

            //Assign transformation helper
            StationTransformationHelper = LinkedStationAssigner.StationTransformationHelper;
            LinkedStationAssigner.StationTransformationHelper.parent = AttachedDimension.transform;
        }

        public string GetCurrentDebugState()
        {
            string returnString = "";
            returnString += "Walking staion debug:" + newLine;

            returnString += "Station name = " + transform.name + newLine;
            returnString += "Current station = " + (LinkedStationAssigner.MyStation == this) + newLine;
            returnString += "PlayerMobility = " + LinkedVRCStation.PlayerMobility + newLine;
            returnString += "AttachedDimensionId= " + AttachedDimensionId + newLine;
            if(AttachedDimension != null) returnString += "AttachedDimension name= " + AttachedDimension.transform.name + newLine;
            returnString += "Parent name = " + transform.parent.name + newLine;
            returnString += "SyncedLocalPlayerPosition = " + SyncedLocalPlayerPosition + newLine;
            returnString += "SyncedLocalPlayerHeading = " + SyncedLocalPlayerHeading + newLine;
            if(LinkedStationAssigner.MyStation != this)
            {
                returnString += "Time since last location deserialization = " + (Time.time - lastDeserializationDeltaTime) + newLine;
                returnString += "LocalPlayerPosition = " + LocalPlayerPosition + newLine;
                returnString += "LocalPlayerHeading = " + LocalPlayerHeading + newLine;
            }
            returnString += "Current owner ID of Auto = " + Networking.GetOwner(gameObject).playerId + newLine;
            returnString += "Current owner ID of Manual = " + Networking.GetOwner(LinkedStationManualSync.gameObject).playerId + newLine;

            returnString += LinkedStationManualSync.GetCurrentDebugState();

            return returnString;
        }
    }
}