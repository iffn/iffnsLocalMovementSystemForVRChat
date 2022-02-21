using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;

namespace iffnsStuff.iffnsVRCStuff.iffnsLocalMovementSystemForVRChat
{
    public class WalkingStationController : UdonSharpBehaviour
    {
        //Unity Assignments:
        [SerializeField] public WalkingStationControllerManualSync LinkedStationManualSync;
        [SerializeField] public StationAssignmentController LinkedStationAssigner;
        [SerializeField] public VRCStation LinkedVRCStation;

        //Synced variables:
        [UdonSynced(UdonSyncMode.Smooth)] public Vector3 LocalPlayerPosition = Vector3.zero;
        [UdonSynced(UdonSyncMode.Smooth)] Quaternion LocalPlayerRotation = Quaternion.identity; //Currently using Quaternion since heading angle transition from 360 to 0 causes a spin

        //Runtime variables:
        Vector3 previousPlayerPosition = Vector3.zero;
        Quaternion previousPlayerRotation = Quaternion.identity;
        public int DeserializationsSinceLastTransition = 0;

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
            //Error checks
            if (LinkedStationManualSync == null)
                LinkedStationAssigner.GetLinkedMainController().OutputLogWarning("LinkedStationManualSync not assigned in Station " + transform.name);
            if (LinkedStationManualSync.LinkedStationAutoSync == null)
                LinkedStationAssigner.GetLinkedMainController().OutputLogWarning("LinkedStationManualSync.LinkedStationAutoSync not assigned in Station " + transform.name);
            if (LinkedVRCStation == null)
                LinkedStationAssigner.GetLinkedMainController().OutputLogWarning("LinkedVRCStation not assigned in Station " + transform.name);
            if (LinkedVRCStation.stationEnterPlayerLocation == null)
                LinkedStationAssigner.GetLinkedMainController().OutputLogWarning("LinkedVRCStation.EnterLocation not assigned in Station " + transform.name);
            if (LinkedVRCStation.stationExitPlayerLocation == null)
                LinkedStationAssigner.GetLinkedMainController().OutputLogWarning("LinkedVRCStation.ExitLocation not assigned in Station " + transform.name);

            //Setup
            LinkedStationManualSync.Setup();
        }

        public void ResetStation()
        {
            LocalPlayerPosition = Vector3.zero;
            LocalPlayerRotation = Quaternion.identity;
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
                    StationTransformationHelper.position = Networking.LocalPlayer.GetPosition();
                    StationTransformationHelper.rotation = Networking.LocalPlayer.GetRotation();

                    LocalPlayerPosition = StationTransformationHelper.localPosition;
                    LocalPlayerRotation = StationTransformationHelper.localRotation;
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
                    }

                    //transform.localRotation = Quaternion.Euler(Vector3.up * LocalPlayerRotation);
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
            returnString += "Current owner ID of Auto = " + Networking.GetOwner(gameObject).playerId + newLine;
            returnString += "Current owner ID of Manual = " + Networking.GetOwner(LinkedStationManualSync.gameObject).playerId + newLine;
            returnString += "DeserializationsSinceLastTransition= " + DeserializationsSinceLastTransition + newLine;

            returnString += LinkedStationManualSync.GetCurrentDebugState();

            return returnString;
        }
    }
}