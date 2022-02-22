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

        //newLine = backslash n which is interpreted as a new line when showing the code in a text field
        string newLine = "\n";

        public override void OnPlayerTriggerEnter(VRCPlayerApi player)
        {
            if (player == Networking.LocalPlayer)
            {
                Debug.Log("Entering dimesion");
                if (LinkedEnterDimension != null) LinkedEnterDimension.GetLinkedMainDimensionController().GetLinkedMainController().SetCurrentDimension(LinkedEnterDimension);
            }
        }

        public override void OnPlayerTriggerExit(VRCPlayerApi player)
        {
            if (player == Networking.LocalPlayer)
            {
                if (LinkedExitDimension != null) LinkedExitDimension.GetLinkedMainDimensionController().GetLinkedMainController().SetCurrentDimension(LinkedExitDimension);
            }
        }
    }
}