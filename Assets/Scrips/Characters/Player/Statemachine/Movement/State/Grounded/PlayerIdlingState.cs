using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerIdlingState : PlayerGroundedState
{
    public PlayerIdlingState(PlayerMovementStateMachine stateMachine) : base(stateMachine)
    {
    }

    #region IState Methods

    public override void Enter()
    {
        base.Enter();

        ResetVelocity();
        
        stateMachine.ReusableData.MovementSpeedModifier = 0f;
        stateMachine.ReusableData.CurrentJumpForce = AirborneData.JumpData.StationaryForce;

        stateMachine.Player.Animator.CrossFade(
            AnimationData.GetAnimationHash(AnimationData.IdlingAnimationName),
            AnimationData.TransitionDuration);
    }

    public override void Update()
    {
        base.Update();

        if (stateMachine.ReusableData.MovementInput == Vector2.zero)
        {
            return;
        }
        
        stateMachine.ChangeState(stateMachine.RunningState);
    }

    #endregion
}
