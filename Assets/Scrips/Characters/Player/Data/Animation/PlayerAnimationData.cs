using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PlayerAnimationData
{
    [field: SerializeField] public float TransitionDuration { get; private set; } = 0.05f;

    [field: Header("Player State Names")]
    [field: SerializeField] private string idlingAnimationName = "Idle";
    [field: SerializeField] private string runningAnimationName = "Run";
    [field: SerializeField] private string dashingAnimationName = "Dash";
    [field: SerializeField] private string sprintingAnimationName = "Sprint";
    [field: SerializeField] private string mediumStoppingAnimationName = "MediumStop";
    [field: SerializeField] private string hardStoppingAnimationName = "HardStop";
    [field: SerializeField] private string jumpingAnimationName = "Jump";
    [field: SerializeField] private string fallingAnimationName = "Fall";

    public int IdlingAnimationHash { get; private set; }
    public int RunningAnimationHash { get; private set; }
    public int DashingAnimationHash { get; private set; }
    public int SprintingAnimationHash { get; private set; }
    public int MediumStoppingAnimationHash { get; private set; }
    public int HardStoppingAnimationHash { get; private set; }
    public int JumpingAnimationHash { get; private set; }
    public int FallingAnimationHash { get; private set; }

    public void Initialize()
    {
        IdlingAnimationHash = Animator.StringToHash(idlingAnimationName);
        RunningAnimationHash = Animator.StringToHash(runningAnimationName);
        DashingAnimationHash = Animator.StringToHash(dashingAnimationName);
        SprintingAnimationHash = Animator.StringToHash(sprintingAnimationName);
        MediumStoppingAnimationHash = Animator.StringToHash(mediumStoppingAnimationName);
        HardStoppingAnimationHash = Animator.StringToHash(hardStoppingAnimationName);
        JumpingAnimationHash = Animator.StringToHash(jumpingAnimationName);
        FallingAnimationHash = Animator.StringToHash(fallingAnimationName);
    }
}
