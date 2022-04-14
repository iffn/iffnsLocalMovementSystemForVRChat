using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using iffnsStuff.iffnsVRCStuff.DebugOutput;

namespace iffnsStuff.iffnsVRCStuff.iffnsLocalMovementSystemForVRChat
{
    public class MainDimensionController : UdonSharpBehaviour
    {
        //Unity assignments
        [SerializeField] DimensionController[] Dimensions;
        [SerializeField] SingleScriptDebugState LinkedStateOutput;

        //Runtime variables
        Transform DimensionTransformationHelper;
        DimensionController CurrentDimension;
        MainDimensionAndStationController LinkedMainController;

        //newLine = backslash n which is interpreted as a new line when showing the code in a text field
        string newLine = "\n";

        public MainDimensionAndStationController GetLinkedMainController()
        {
            return LinkedMainController;
        }

        public DimensionController GetCurrentDimension()
        {
            return CurrentDimension;
        }

        public DimensionController GetDimension(int index)
        {
            if (Dimensions.Length - 1 < index) return Dimensions[0]; //Error

            return Dimensions[index];
        }

        public void Setup(MainDimensionAndStationController linkedMainController, Transform DimensionTransformationHelper)
        {
            this.LinkedMainController = linkedMainController;

            for (int i = 0; i < Dimensions.Length; i++)
            {
                Dimensions[i].Setup(LinkedMainDimensionController: this, dimensionNumber: i);
            }

            CurrentDimension = Dimensions[0];

            this.DimensionTransformationHelper = DimensionTransformationHelper;
        }

        void Start()
        {
            //Use setup instead
        }

        void Update()
        {
            PrepareDebugState();
        }

        public Quaternion GetHeadingRotationFromRotation(Quaternion rotation)
        {
            Vector3 heading = rotation * Vector3.forward;

            heading = new Vector3(heading.x, 0, heading.z);

            return Quaternion.LookRotation(heading, Vector3.up);
        }

        public void SetMyDimension(DimensionController newDimension)
        {
            DimensionTransformationHelper.position = Networking.LocalPlayer.GetPosition();
            DimensionTransformationHelper.rotation = Networking.LocalPlayer.GetRotation();

            //LinkedMainController.OutputLogText("Starting dimension transition with position = " + DimensionTransformationHelper.position);

            DimensionTransformationHelper.parent = newDimension.transform;

            /*
            Vector3 stationPosition = newDimension.transform.position;
            Quaternion stationRotation = newDimension.transform.rotation;
            stationRotation = GetHeadingRotationFromRotation(stationRotation); //Only rotate the world heading: Make sure that the world is not tilted
            LinkedMainController.OutputLogText("Station transition with station position = " + stationPosition + " and rotation = " + stationRotation.eulerAngles);
            */

            //Assign new dimension
            DimensionController previousDimension = CurrentDimension;
            CurrentDimension = newDimension;

            //Reset old station
            previousDimension.ResetInversedDimensionSetting();

            //Update all dimension positions
            for (int i = 0; i < Dimensions.Length; i++)
            {
                Dimensions[i].UpdatePosition();
            }

            //LinkedMainController.OutputLogText("Player position before making the current dimension = " + DimensionTransformationHelper.position);

            CurrentDimension.PositionDimensionAsCurrent(PlayerShouldBeLocation: DimensionTransformationHelper);
        }

        public void PrepareDebugState()
        {
            if (LinkedStateOutput == null) return;

            if (!LinkedStateOutput.IsReadyForOutput()) return;

            string name = "MainDimensionController";

            string currentState = "";

            for (int i = 0; i < Dimensions.Length; i++)
            {
                currentState += Dimensions[i].GetCurrentDebugState() + newLine;
            }

            LinkedStateOutput.SetCurrentState(displayName: name, currentState: currentState);
        }

        public string GetCurrentDebugState()
        {
            string returnString = "";
            returnString += "Main dimension controller debug at " + Time.time + newLine;

            for (int i = 0; i < Dimensions.Length; i++)
            {
                returnString += Dimensions[i].GetCurrentDebugState() + newLine;
            }

            return returnString;
        }
    }
}