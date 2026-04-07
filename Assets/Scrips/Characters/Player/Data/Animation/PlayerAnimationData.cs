using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PlayerAnimationData
{
    [field: SerializeField] public float TransitionDuration { get; private set; } = 0.05f;

    [field: Header("Player State Names")]
    [field: SerializeField] public string IdlingAnimationName { get; private set; } = "Idle";
    [field: SerializeField] public string RunningAnimationName { get; private set; } = "Run";
    [field: SerializeField] public string DashingAnimationName { get; private set; } = "Dash";
    [field: SerializeField] public string SprintingAnimationName { get; private set; } = "Sprint";
    [field: SerializeField] public string MediumStoppingAnimationName { get; private set; } = "MediumStop";
    [field: SerializeField] public string HardStoppingAnimationName { get; private set; } = "HardStop";
    [field: SerializeField] public string JumpingAnimationName { get; private set; } = "Jump";
    [field: SerializeField] public string FallingAnimationName { get; private set; } = "Fall";
    [field: SerializeField] public string Combo1AnimationName { get; private set; } = "Standing Melee Attack Downward";

    private Dictionary<string, int> AnimationsDictionary = new Dictionary<string, int>();

    public void Initialize()
    {
        AnimationsDictionary[IdlingAnimationName] = Animator.StringToHash(IdlingAnimationName);
        AnimationsDictionary[RunningAnimationName] = Animator.StringToHash(RunningAnimationName);
        AnimationsDictionary[DashingAnimationName] = Animator.StringToHash(DashingAnimationName);
        AnimationsDictionary[SprintingAnimationName] = Animator.StringToHash(SprintingAnimationName);
        AnimationsDictionary[MediumStoppingAnimationName] = Animator.StringToHash(MediumStoppingAnimationName);
        AnimationsDictionary[HardStoppingAnimationName] = Animator.StringToHash(HardStoppingAnimationName);
        AnimationsDictionary[JumpingAnimationName] = Animator.StringToHash(JumpingAnimationName);
        AnimationsDictionary[FallingAnimationName] = Animator.StringToHash(FallingAnimationName);
        AnimationsDictionary[Combo1AnimationName] = Animator.StringToHash(Combo1AnimationName);
    }

    public int GetAnimationHash(string AnimationName)
    {
        return AnimationsDictionary[AnimationName];
    }
}
