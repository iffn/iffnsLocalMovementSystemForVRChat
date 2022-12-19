using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace iffnsStuff.iffnsVRCStuff.iffnsLocalMovementSystemForVRChat.Additionals
{
    public class LocalTeleportation : UdonSharpBehaviour
    {
        [SerializeField] Transform TargetTransform;
        [SerializeField] DimensionController TargetDimension;

        private void Start()
        {
            if(TargetTransform == null || TargetDimension == null)
            {
                Debug.LogWarning(Time.time + " Error with assignment on teleport interactor called " + transform.name + " -> Disabling teleporter");
                transform.GetComponent<Collider>().enabled = false;
            }
        }

        public override void Interact()
        {
            Networking.LocalPlayer.TeleportTo(teleportPos: TargetTransform.position, teleportRot: TargetTransform.rotation, teleportOrientation: VRC_SceneDescriptor.SpawnOrientation.Default, lerpOnRemote: false);

            TargetDimension.SetAsCurrentDimension(5);
        }
    }
}