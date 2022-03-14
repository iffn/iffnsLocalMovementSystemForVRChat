using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace iffnsStuff.iffnsVRCStuff.iffnsLocalMovementSystemForVRChat.Additionals
{
    public class LocalStationEntry : UdonSharpBehaviour
    {
        //Unity assignments
        [SerializeField] LocalStationController LinkedLocalStationController;

        //Runtime variables
        MeshRenderer linkedMeshRenderer;
        Collider linkedCollider;

        private void Start()
        {
            if(LinkedLocalStationController == null)
            {
                Debug.LogWarning(Time.time + ": iffns LocalMovementSystem Assignment problem: LinkedLocalStationController of LocalStationEntry is not assigned");
                return;
            } 

            linkedCollider = transform.GetComponent<Collider>();
            linkedMeshRenderer = transform.GetComponent<MeshRenderer>();

            LinkedLocalStationController.RegisterStationEntry(stationEntry: this); //Currently only 1 available
        }

        

        public override void Interact()
        {
            LinkedLocalStationController.UseAttachedStation();
        }
        public void LockStationEntry(bool newState)
        {
            if (linkedMeshRenderer != null) linkedMeshRenderer.enabled = !newState;
            if (linkedCollider != null) linkedCollider.enabled = !newState;
        }
    }
}