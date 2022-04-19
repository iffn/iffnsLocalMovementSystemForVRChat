using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace iffnsStuff.iffnsVRCStuff.iffnsLocalMovementSystemForVRChat
{
    public class NanLandFixerForPlayerInLocalMovementSystem : UdonSharpBehaviour
    {
        [SerializeField] MainDimensionAndStationController LinkedMainDimensionAndStationController;

        //Settings
        const float playerVelocityMax = 5.5f; //Run and jump with run speed 4 amd jump impulse 3 = 5.1 m/s
        const float maxVelocityIncreaseFactorPerFixedFrame = 1.2f;

        //Runtime variables
        float lastPlayerVelocity = 0;
        float peakPlayerVelocity = 0;
        Vector3 lastPosition2 = Vector3.zero;
        Vector3 lastPosition = Vector3.zero;
        VRCPlayerApi LocalPlayer;

        public float GetLastPlayerVelocity() { return lastPlayerVelocity; }
        public float GetPeakPlayerVelocity() { return peakPlayerVelocity; }

        private void Start()
        {
            LocalPlayer = Networking.LocalPlayer;
        }

        void FixedUpdate()
        {
            #if UNITY_EDITOR
            return;
            #endif

            if (LocalPlayer == null)
            {
                string outputText = "Error with NanLandFixer: LocalPlayer not assigned. Likely that FixedUpdate was run before Start of this object -> Ignore if once";

                if (LinkedMainDimensionAndStationController != null) LinkedMainDimensionAndStationController.OutputLogWarning(outputText);
                else Debug.LogWarning(outputText);
                return;
            }

            Vector3 velocity = LocalPlayer.GetVelocity(); //Error happens when you leave the world: Ignore
            float velocityMagnitude = velocity.magnitude;

            if (velocityMagnitude > playerVelocityMax && velocityMagnitude > lastPlayerVelocity * maxVelocityIncreaseFactorPerFixedFrame)
            {
                string outputText = "Velocity fixed with velocity = " + velocity + ", position = " + Networking.LocalPlayer.GetPosition() + ", last position = " + lastPosition + ", last position 2 = " + lastPosition2;

                if (LinkedMainDimensionAndStationController != null) LinkedMainDimensionAndStationController.OutputLogWarning(outputText);
                else Debug.LogWarning(Time.time + ": " + outputText);

                Vector3 velocityNormalized = velocity.normalized;

                //Networking.LocalPlayer.SetVelocity(velocityNormalized * playerVelocityMax);
                Networking.LocalPlayer.SetVelocity(Vector3.zero);

                //lastPosition += velocityNormalized * playerVelocityMax * Time.fixedDeltaTime;

                Networking.LocalPlayer.TeleportTo(lastPosition2, Networking.LocalPlayer.GetRotation());

            }
            else
            {
                lastPosition2 = lastPosition;
                lastPosition = LocalPlayer.GetPosition();
            }

            lastPlayerVelocity = velocityMagnitude;
            if (peakPlayerVelocity < lastPlayerVelocity) peakPlayerVelocity = lastPlayerVelocity;
        }

        public override void OnOwnershipTransferred(VRCPlayerApi player)
        {
            if (player != Networking.LocalPlayer) return;
        }
        
    }
    
    
}