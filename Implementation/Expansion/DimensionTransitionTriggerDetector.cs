using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace iffnsStuff.iffnsVRCStuff.iffnsLocalMovementSystemForVRChat
{
    public class DimensionTransitionTriggerDetector : UdonSharpBehaviour
    {
        [SerializeField] DimensionController LinkedEnterDimension;
        [SerializeField] DimensionController LinkedExitDimension;

        public void SetOtherDimensionTransitionIfNotAlreadySet(DimensionController linkedDimension)
        {
            if (LinkedEnterDimension == null) LinkedEnterDimension = linkedDimension;
            else if(LinkedExitDimension == null) LinkedExitDimension = linkedDimension;
        }

        public override void OnPlayerTriggerEnter(VRCPlayerApi player)
        {
            if (player == Networking.LocalPlayer)
            {
                if (LinkedEnterDimension != null) LinkedEnterDimension.SetAsCurrentDimension();
                //if (LinkedEnterDimension != null) LinkedEnterDimension.GetLinkedMainDimensionController().LinkedMainController.SetCurrentDimension(LinkedEnterDimension);
            }
        }

        public override void OnPlayerTriggerExit(VRCPlayerApi player)
        {
            if (player == Networking.LocalPlayer)
            {
                if (LinkedExitDimension != null) LinkedExitDimension.SetAsCurrentDimension();
            }
        }

        public DimensionController GetEntryDimension()
        {
            return LinkedEnterDimension;
        }

        public DimensionController GetExitDimension()
        {
            return LinkedExitDimension;
        }
    }
}