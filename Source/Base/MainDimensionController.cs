using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace iffnsStuff.iffnsVRCStuff.iffnsLocalMovementSystemForVRChat
{
    public class MainDimensionController : UdonSharpBehaviour
    {
        //Unity assignments
        [Header("Add all Dimensions here")]
        [Tooltip("Add all dimesions here. The world dimension is already added in the Prefab")]
        [SerializeField] DimensionController[] Dimensions;
        [SerializeField] DimensionPrefabOrganizer[] PrefabOrganizers;

        //Runtime variables
        Transform DimensionTransformationHelper;
        DimensionController currentDimension;
        MainDimensionAndStationController linkedMainController;
        readonly string newLine = "\n";

        public MainDimensionAndStationController LinkedMainController
        {
            get
            {
                return linkedMainController;
            }
        }

        public DimensionController CurrentDimension
        {
            get
            {
                return currentDimension;
            }
        }

        public DimensionController GetDimension(int index)
        {
            if (Dimensions.Length - 1 < index)
            {
                linkedMainController.OutputLogWarning("Error: Out of scope dimension " + index + " called. Array length = " + Dimensions.Length);

                return Dimensions[0]; //Error
            }

            return Dimensions[index];
        }

        DimensionController[] CombineArray(DimensionController[] FirstArray, DimensionController[] SecondArray)
        {
            int[] doublePositions = new int[SecondArray.Length];
            int doubleCount = 0;

            for(int i = 0; i< SecondArray.Length; i++)
            {
                DimensionController currentController = SecondArray[i];

                bool isDouble = false;

                foreach(DimensionController controller in FirstArray)
                {
                    if(controller == currentController)
                    {
                        doublePositions[doubleCount] = i;
                        doubleCount++;
                        linkedMainController.OutputLogWarning("Detected double dimension assignment in Main Dimension controller regarding " + controller.transform.name + ". Fixing now.");
                        isDouble = true;
                        break;
                    }

                }

                if(!isDouble) doublePositions[i] = -1;
            }

            DimensionController[] returnArray = new DimensionController[FirstArray.Length + SecondArray.Length - doubleCount];

            for (int i = 0; i < FirstArray.Length; i++)
            {
                returnArray[i] = FirstArray[i];
            }

            int doubleSkips = 0;

            for (int i = 0; i < SecondArray.Length; i++)
            {
                if(doublePositions[i] == -1)
                {
                    returnArray[FirstArray.Length + i - doubleSkips] = SecondArray[i];
                }
                else
                {
                    doubleSkips++;
                }
            }

            return returnArray;
        }

        public void Setup(MainDimensionAndStationController linkedMainController, Transform DimensionTransformationHelper)
        {
            this.linkedMainController = linkedMainController;

            foreach (DimensionPrefabOrganizer organizer in PrefabOrganizers)
            {
                organizer.Setup();

                Dimensions = CombineArray(Dimensions, organizer.GetLinkedDimensions());
            }
            
            for (int i = 0; i < Dimensions.Length; i++)
            {
                Dimensions[i].Setup(LinkedMainDimensionController: this, dimensionNumber: i);
            }

            currentDimension = Dimensions[0];
            currentDimension.isCurrentDimension = true;

            this.DimensionTransformationHelper = DimensionTransformationHelper;
        }

        void Start()
        {
            //Use setup instead
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
            currentDimension = newDimension;

            //Reset old station
            previousDimension.ResetInversedDimensionSetting();

            //Update all dimension positions
            for (int i = 0; i < Dimensions.Length; i++)
            {
                Dimensions[i].UpdatePosition();
                Dimensions[i].isCurrentDimension = false;
            }

            //LinkedMainController.OutputLogText("Player position before making the current dimension = " + DimensionTransformationHelper.position);

            CurrentDimension.PositionDimensionAsCurrent(PlayerShouldBeLocation: DimensionTransformationHelper);
            CurrentDimension.isCurrentDimension = true;
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