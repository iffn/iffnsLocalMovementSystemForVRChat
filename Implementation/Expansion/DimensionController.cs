using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;

namespace iffnsStuff.iffnsVRCStuff.iffnsLocalMovementSystemForVRChat
{
    public class DimensionController : UdonSharpBehaviour
    {
        [HideInInspector] [UdonSynced(UdonSyncMode.Smooth)] public Vector3 LocalDimensionPosition = Vector3.zero;
        [HideInInspector] [UdonSynced(UdonSyncMode.Smooth)] public Quaternion LocalDimensionRotation = Quaternion.identity;

        [SerializeField] DimensionController LinkedDimensionController;
        [SerializeField] MainDimensionController LinkedMainDimensionController;

        DimensionController InversedDimension;

        int dimensionId;

        //newLine = backslash n which is interpreted as a new line when showing the code in a text field
        string newLine = "\n";

        public int GetDimensionId()
        {
            return dimensionId;
        }

        public MainDimensionController GetLinkedMainDimensionController()
        {
            return LinkedMainDimensionController;
        }

        public void Setup(int dimensionNumber)
        {
            if ((transform.localScale - Vector3.one).magnitude > 0.0001f)
                LinkedMainDimensionController.GetLinkedMainController().OutputLogWarning("Error in station " + transform.name + " with ID " + dimensionId + ": Local scale is not (1, 1, 1) but " + transform.localScale);

            dimensionId = dimensionNumber;

            if (Networking.IsOwner(gameObject))
            {
                LocalDimensionPosition = transform.localPosition;
                LocalDimensionRotation = transform.localRotation;
            }
        }

        void Start()
        {
            //Use setup instead
        }

        private void Update()
        {
            UpdatePosition();
        }

        public void UpdatePosition()
        {
            if (LinkedMainDimensionController.GetCurrentDimension() == this) //If current dimension
            {
                //Don't move dimension
            }
            else if (InversedDimension == null) //If normal rotation
            {
                SetLocalPosition();
            }
            else
            {
                SetInvertedLocalPosition();
            }
        }

        void SetLocalPosition()
        {
            transform.localPosition = LocalDimensionPosition;
            transform.localRotation = LocalDimensionRotation;
        }

        void SetInvertedLocalPosition()
        {
            //Inverting the local position
            Quaternion inverseLocalRotation = Quaternion.Inverse(InversedDimension.LocalDimensionRotation);

            transform.localPosition = inverseLocalRotation * -InversedDimension.LocalDimensionPosition;
            transform.localRotation = inverseLocalRotation;
        }

        public void SetInversedDimensionSetting(DimensionController inversedDimensionReference) //Set and reset this during dimension change
        {
            //Set hierarchy
            transform.parent = inversedDimensionReference.transform;

            //Set reference
            InversedDimension = inversedDimensionReference;

            if (LinkedDimensionController != null)
            {
                LinkedDimensionController.SetInversedDimensionSetting(this);
            }
        }

        public void ResetInversedDimensionSetting() //Reset this during dimension change
        {
            InversedDimension = null;

            if (LinkedDimensionController != null)
            {
                LinkedDimensionController.transform.parent = LinkedMainDimensionController.transform;
                transform.parent = LinkedDimensionController.transform;

                LinkedDimensionController.ResetInversedDimensionSetting();
            }
            else
            {
                transform.parent = LinkedMainDimensionController.transform;
            }
        }

        public void MakeCurrentDimension(Transform PlayerShouldPeLocation)
        {
            PlayerShouldPeLocation.localRotation = LinkedMainDimensionController.GetHeadingRotationFromRotation(PlayerShouldPeLocation.localRotation);

            PlayerShouldPeLocation.parent = LinkedMainDimensionController.transform;

            transform.parent = PlayerShouldPeLocation;
            PlayerShouldPeLocation.position = Networking.LocalPlayer.GetPosition();
            PlayerShouldPeLocation.rotation = Networking.LocalPlayer.GetRotation();

            //LinkedMainDimensionController.GetLinkedMainController().OutputLogText("Player position after completion = " + PlayerShouldPeLocation.position);

            //Set hierarchy
            transform.parent = LinkedMainDimensionController.transform;

            //Reset position
            /*
            transform.position = distanceOffset;
            transform.rotation = angleOffset;
            */

            //Set inversed dimension settings to linked dimensions
            if (LinkedDimensionController != null) LinkedDimensionController.SetInversedDimensionSetting(inversedDimensionReference: this);
        }

        public string GetCurrentDebugState()
        {
            string returnString = "";
            returnString += "Dimension controller debug:" + newLine;
            returnString += "Name = " + transform.name + newLine;
            returnString += "Current dimension = " + (LinkedMainDimensionController.GetCurrentDimension() == this) + newLine;
            returnString += "dimensionNumber = " + dimensionId + newLine;
            returnString += "Owner = " + Networking.GetOwner(gameObject).playerId + ": " + Networking.GetOwner(gameObject).displayName + newLine;
            returnString += "LocalDimensionPosition = " + LocalDimensionPosition + newLine;
            returnString += "LocalDimensionRotation = " + LocalDimensionRotation.eulerAngles + newLine;
            returnString += "Local position = " + transform.localPosition + newLine;
            returnString += "Local rotation = " + transform.localRotation.eulerAngles + newLine;

            return returnString;
        }
    }
}