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
        [HideInInspector] [UdonSynced(UdonSyncMode.Smooth)] public Vector3 LocalPlayerPosition = Vector3.zero;
        [UdonSynced(UdonSyncMode.Smooth)] Quaternion LocalPlayerRotation = Quaternion.identity; //Currently using Quaternion since heading angle transition from 360 to 0 causes a spin
        //[UdonSynced] float LocalPlayerHeading =0; //No special sync mode. Angle lerp calculated manually due to 360 to 0 overflow

        //Runtime variables:
        [HideInInspector] public VRCStation LinkedVRCStation;
        float lastRecievedValue = 0;
        float savedValue = 0;
        float lastSyncTime = 0;
        float timeSinceLastSync; //Used to check serialization frequency
        Vector3 previousPlayerPosition = Vector3.zero;
        Quaternion previousPlayerRotation = Quaternion.identity;
        [HideInInspector] public int DeserializationsSinceLastTransition = 0;

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
            LocalPlayerRotation = Quaternion.identity;
            //LocalPlayerHeading = 0;
            stationState = -1;
            previousPlayerPosition = Vector3.zero;
            previousPlayerRotation = Quaternion.identity;
            DeserializationsSinceLastTransition = 0;
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

                    LocalPlayerPosition = StationTransformationHelper.localPosition;
                    LocalPlayerRotation = StationTransformationHelper.localRotation;
                    //LocalPlayerHeading = StationTransformationHelper.localRotation.eulerAngles.y;
                    break;

                case 1:
                    //1 = Station of other player

                    //Freeze player during transition due to large coordinate change, which messes with the smooth sync
                    //ToDo: Make transition smooth
                    if (DeserializationsSinceLastTransition == 1)
                    {
                        transform.parent = LinkedStationManualSync.GetAttachedDimension().transform;

                        transform.position = previousPlayerPosition;
                        transform.rotation = previousPlayerRotation;
                    }
                    else if (DeserializationsSinceLastTransition == 2)
                    {
                        transform.position = previousPlayerPosition;
                        transform.rotation = previousPlayerRotation;
                    }
                    else
                    {
                        transform.localPosition = LocalPlayerPosition;
                        transform.localRotation = LocalPlayerRotation;
                        //ToDo: Calculate heading
                    }

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

        public void PlayerIsTransitioning()
        {
            DeserializationsSinceLastTransition = 0;
            previousPlayerPosition = transform.position;
            previousPlayerRotation = transform.rotation;
        }

        public override void OnDeserialization()
        {
            DeserializationsSinceLastTransition++;

            lastRecievedValue = savedValue;
            savedValue = LocalPlayerRotation.eulerAngles.y;
            //savedValue = LocalPlayerHeading;

            timeSinceLastSync = Time.time - lastSyncTime;
            lastSyncTime = Time.time;
        }

        public string GetCurrentDebugState()
        {
            string returnString = "";
            returnString += "Walking staion debug:" + newLine;

            returnString += "Station name = " + transform.name + newLine;
            returnString += "Current station = " + (LinkedStationAssigner.MyStation == this) + newLine;
            returnString += "PlayerMobility = " + LinkedVRCStation.PlayerMobility + newLine;
            returnString += "LocalPlayerPosition = " + LocalPlayerPosition + newLine;
            returnString += "LocalPlayerRotation = " + LocalPlayerRotation + newLine;
            //returnString += "LocalPlayerHeading = " + LocalPlayerHeading + newLine;
            returnString += "Current owner ID of Auto = " + Networking.GetOwner(gameObject).playerId + newLine;
            returnString += "Current owner ID of Manual = " + Networking.GetOwner(LinkedStationManualSync.gameObject).playerId + newLine;
            returnString += "DeserializationsSinceLastTransition= " + DeserializationsSinceLastTransition + newLine;
            returnString += "timeSinceLastSync = " + timeSinceLastSync + newLine;

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