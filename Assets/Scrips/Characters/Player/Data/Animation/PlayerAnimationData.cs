using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public class PlayerAnimationData
{
    [field: FormerlySerializedAs("<TransitionDuration>k__BackingField")]
    [field: SerializeField] public float NormalizedTransitionDuration { get; private set; } = 0.1f;
    [field: SerializeField] public float FixedTransitionDuration { get; private set; } = 0.1f;

    [field: Header("Player State Names")]
    [field: SerializeField] private string idlingAnimationName = "Idle";
    [field: SerializeField] private string walkingAnimationName = "Walk";
    [field: SerializeField] private string runningAnimationName = "Run";
    [field: SerializeField] private string dashingAnimationName = "Dash";
    [field: SerializeField] private string dodgeForwardAnimationName = "Dodge Forward";
    [field: SerializeField] private string dodgeBackwardAnimationName = "Dodge Backward";
    [field: SerializeField] private string dodgeLeftAnimationName = "Dodge Left";
    [field: SerializeField] private string dodgeRightAnimationName = "Dodge Right";
    [field: SerializeField] private string sprintingAnimationName = "Sprint";
    [field: SerializeField] private string lightStoppingAnimationName = "LightStop";
    [field: SerializeField] private string mediumStoppingAnimationName = "MediumStop";
    [field: SerializeField] private string hardStoppingAnimationName = "HardStop";
    [field: SerializeField] private string lightLandingAnimationName = "LightLand";
    [field: SerializeField] private string hardLandingAnimationName = "HardLand";
    [field: SerializeField] private string rollingAnimationName = "Roll";
    [field: SerializeField] private string jumpingAnimationName = "Jump";
    [field: SerializeField] private string fallingAnimationName = "Fall";

    public int IdlingAnimationHash { get; private set; }
    public int WalkingAnimationHash { get; private set; }
    public int RunningAnimationHash { get; private set; }
    public int DashingAnimationHash { get; private set; }
    public int DodgeForwardAnimationHash { get; private set; }
    public int DodgeBackwardAnimationHash { get; private set; }
    public int DodgeLeftAnimationHash { get; private set; }
    public int DodgeRightAnimationHash { get; private set; }
    public int SprintingAnimationHash { get; private set; }
    public int LightStoppingAnimationHash { get; private set; }
    public int MediumStoppingAnimationHash { get; private set; }
    public int HardStoppingAnimationHash { get; private set; }
    public int LightLandingAnimationHash { get; private set; }
    public int HardLandingAnimationHash { get; private set; }
    public int RollingAnimationHash { get; private set; }
    public int JumpingAnimationHash { get; private set; }
    public int FallingAnimationHash { get; private set; }

    public void Initialize()
    {
        IdlingAnimationHash = Animator.StringToHash(idlingAnimationName);
        WalkingAnimationHash = Animator.StringToHash(walkingAnimationName);
        RunningAnimationHash = Animator.StringToHash(runningAnimationName);
        DashingAnimationHash = Animator.StringToHash(dashingAnimationName);
        DodgeForwardAnimationHash = Animator.StringToHash(dodgeForwardAnimationName);
        DodgeBackwardAnimationHash = Animator.StringToHash(dodgeBackwardAnimationName);
        DodgeLeftAnimationHash = Animator.StringToHash(dodgeLeftAnimationName);
        DodgeRightAnimationHash = Animator.StringToHash(dodgeRightAnimationName);
        SprintingAnimationHash = Animator.StringToHash(sprintingAnimationName);
        LightStoppingAnimationHash = Animator.StringToHash(lightStoppingAnimationName);
        MediumStoppingAnimationHash = Animator.StringToHash(mediumStoppingAnimationName);
        HardStoppingAnimationHash = Animator.StringToHash(hardStoppingAnimationName);
        LightLandingAnimationHash = Animator.StringToHash(lightLandingAnimationName);
        HardLandingAnimationHash = Animator.StringToHash(hardLandingAnimationName);
        RollingAnimationHash = Animator.StringToHash(rollingAnimationName);
        JumpingAnimationHash = Animator.StringToHash(jumpingAnimationName);
        FallingAnimationHash = Animator.StringToHash(fallingAnimationName);
    }
}
