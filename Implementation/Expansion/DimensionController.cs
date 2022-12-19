using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;

namespace iffnsStuff.iffnsVRCStuff.iffnsLocalMovementSystemForVRChat
{
    public class DimensionController : UdonSharpBehaviour
    {
        //Unity assignments
        [Header("To be set manually. None for World Dimension")]
        [Tooltip("Links to the parent dimension. None in the World Dimension")]
        [SerializeField] DimensionController linkedDimensionController;
        [Header("To be set manually")]
        [Tooltip("Enable if the player should respawn in this dimension. Usually only for world dimension")]
        [SerializeField] bool EnableRespawnHeight = false;
        [Tooltip("Respawn heigh relative to this Transform if the player is attached to it")]
        [SerializeField] float RespawnHeightIfAttached = -90f;

        //Runtime variables
        [HideInInspector] public Vector3 LocalDimensionPosition = Vector3.zero;
        [HideInInspector] public Quaternion LocalDimensionRotation = Quaternion.identity;
        [HideInInspector] public bool isCurrentDimension = false;
        MainDimensionController LinkedMainDimensionController;
        DimensionController InversedDimension;
        int dimensionId;
        readonly string newLine = "\n";

        public int GetDimensionId() { return dimensionId; }

        public MainDimensionController GetLinkedMainDimensionController()
        {
            return LinkedMainDimensionController;
        }

        public DimensionController LinkedDimensionController
        {
            get
            {
                return linkedDimensionController;
            }
        }

        public void SetLinkedDimensionControllerIfNotAlreadySet(DimensionController linkedDimensionController)
        {
            if (this.linkedDimensionController != null) return;

            this.linkedDimensionController = linkedDimensionController;

            transform.parent = linkedDimensionController.transform;
        }

        public void Setup(MainDimensionController LinkedMainDimensionController, int dimensionNumber)
        {
            this.LinkedMainDimensionController = LinkedMainDimensionController;

            if ((transform.localScale - Vector3.one).magnitude > 0.0001f)
                LinkedMainDimensionController.LinkedMainController.OutputLogWarning("Error in station " + transform.name + " with ID " + dimensionId + ": Local scale is not (1, 1, 1) but " + transform.localScale);

            dimensionId = dimensionNumber;

            LocalDimensionPosition = transform.localPosition;
            LocalDimensionRotation = transform.localRotation;
        }

        void Start()
        {
            //Use setup instead
        }

        void Update()
        {
            UpdatePosition();

            CheckRespawnHeight();
        }

        void CheckRespawnHeight()
        {
            if (!EnableRespawnHeight) return;
            if (!isCurrentDimension) return;

            if(Networking.LocalPlayer.GetPosition().y - transform.position.y < RespawnHeightIfAttached) //Error happens when you leave the world: Ignore
            {
                LinkedMainDimensionController.LinkedMainController.OutputLogText("Rewpawning player due to respawn height");

                Networking.LocalPlayer.Respawn();
            }
        }

        int errorCount = 0;

        public void UpdatePosition() //ToDo: Encapsulate
        {
            if(LinkedMainDimensionController == null)
            {
                if(errorCount == 1) // Ignore the first error since the Update was probably called before the setup is completed
                {
                    Debug.LogWarning(Time.time + ": Error with Dimension controller of " + transform.name + " -> Setup not complete.");
                }
                errorCount++;

                return;
            }

            if (LinkedMainDimensionController.CurrentDimension == this) //If current dimension
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

        //Set and reset this during dimension change
        public void SetInversedDimensionSetting(DimensionController inversedDimensionReference) //ToDo: Encapsulate
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

        //Reset this during dimension change
        public void ResetInversedDimensionSetting() //ToDo: Encapsulate
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

        public void PositionDimensionAsCurrent(Transform PlayerShouldBeLocation) //ToDo: Encapsulate
        {
            PlayerShouldBeLocation.localRotation = LinkedMainDimensionController.GetHeadingRotationFromRotation(PlayerShouldBeLocation.localRotation);

            PlayerShouldBeLocation.parent = LinkedMainDimensionController.transform;

            transform.parent = PlayerShouldBeLocation;
            PlayerShouldBeLocation.position = Networking.LocalPlayer.GetPosition();
            PlayerShouldBeLocation.rotation = LinkedMainDimensionController.GetHeadingRotationFromRotation(Networking.LocalPlayer.GetRotation()); //In case player is in rotated seat during transition

            //LinkedMainDimensionController.LinkedMainController.OutputLogText("Player position after completion = " + PlayerShouldPeLocation.position);

            //Set hierarchy
            transform.parent = LinkedMainDimensionController.transform;

            //Set inversed dimension settings to linked dimensions
            if (LinkedDimensionController != null) LinkedDimensionController.SetInversedDimensionSetting(inversedDimensionReference: this);
        }

        public void SetAsCurrentDimension(int origin)
        {
            LinkedMainDimensionController.LinkedMainController.SetCurrentDimension(this, origin);
        }

        public string GetCurrentDebugState()
        {
            string returnString = "";
            returnString += "Dimension controller debug:" + newLine;
            returnString += "Name = " + transform.name + newLine;
            returnString += "Current dimension = " + isCurrentDimension + newLine;
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