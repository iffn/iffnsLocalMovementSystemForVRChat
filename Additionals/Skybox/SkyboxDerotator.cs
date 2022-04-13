using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace iffnsStuff.iffnsVRCStuff.iffnsLocalMovementSystemForVRChat.Additionals
{
    public class SkyboxDerotator : UdonSharpBehaviour
    {
        public Transform ReferenceTransform;

        private void Update()
        {
            RenderSettings.skybox.SetFloat("_Rotation", -ReferenceTransform.rotation.eulerAngles.y);
        }
    }
}