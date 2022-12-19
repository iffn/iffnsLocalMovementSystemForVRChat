using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace iffnsStuff.iffnsVRCStuff.iffnsLocalMovementSystemForVRChat
{
    public class MainStationController : UdonSharpBehaviour
    {
        [SerializeField] WalkingStationController[] WalkingStationControllers;

        MainDimensionAndStationController linkedMainController;

        WalkingStationController myMainStation;

        readonly string newLine = "\n";

        public bool PlayerIsCurrentlyUsingOtherStation;

        public void SetupFromMainController(MainDimensionAndStationController linkedMainController)
        {
            this.linkedMainController = linkedMainController;

            foreach(WalkingStationController controller in WalkingStationControllers)
            {
                controller.Setup(linkedMainController);
            }
        }

        public void SetupFromMainStation(WalkingStationController myMainStation)
        {
            this.myMainStation = myMainStation;
        }

        public void SetDimensionAttachment(DimensionController newDimension)
        {
            myMainStation.SetDimensionAttachment(newDimension);
        }

        public string GetCurrentDebugState()
        {
            string returnString = "";
            returnString += "Main station controller debug at " + Time.time + newLine;
            returnString += $"Main controller set = {linkedMainController != null}" + newLine;
            returnString += $"Main station set = {myMainStation != null}" + newLine;
            
            foreach(WalkingStationController controller in WalkingStationControllers)
            {
                if (!controller.gameObject.activeInHierarchy) continue;
                returnString += newLine;
                returnString += controller.GetCurrentDebugState() + newLine;
            }

            return returnString;
        }

        //VRChat functions
        public override void OnPlayerRespawn(VRCPlayerApi player)
        {
            if (!player.isLocal) return;
            
            linkedMainController.OutputLogText("Local respawn triggered");

            linkedMainController.SetWorldDimensionAsActiveAndResetPosition();
            myMainStation.EnterStation();
        }
    }
}