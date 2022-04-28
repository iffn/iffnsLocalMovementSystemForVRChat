
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using iffnsStuff.iffnsVRCStuff.iffnsLocalMovementSystemForVRChat;
using iffnsStuff.iffnsVRCStuff.iffnsLocalMovementSystemForVRChat.Additionals;

public class DimensionPrefabOrganizer : UdonSharpBehaviour
{
    [Header("To be assigned during implementation")]
    [SerializeField] DimensionController ParentDimension;

    [Header("To be assined by the creator")]
    [SerializeField] DimensionController[] LinkedDimensions;
    [SerializeField] DimensionTransitionTriggerDetector[] TransitionDetectors;

    public DimensionController[] GetLinkedDimensions()
    {
        return LinkedDimensions;
    }

    public void Setup()
    {
        //Reset hierarchy and position if not 0;
        transform.parent = ParentDimension.transform;

        //Set dimension links
        foreach(DimensionController dimension in LinkedDimensions)
        {
            dimension.SetLinkedDimensionControllerIfNotAlreadySet(ParentDimension);
        }

        foreach(DimensionTransitionTriggerDetector detector in TransitionDetectors)
        {
            detector.SetOtherDimensionTransitionIfNotAlreadySet(ParentDimension);
        }
    }

    void Start()
    {
        
    }
}
