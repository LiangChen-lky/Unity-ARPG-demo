using UnityEngine.InputSystem;

public class PlayerRunningState : PlayerMovingState
{
    // Start is called before the first frame update
    public PlayerRunningState(PlayerMovementStateMachine stateMachine) : base(stateMachine)
    {
    }

    #region IState Methods

    public override void Enter()
    {
        base.Enter();

        stateMachine.ReusableData.MovementSpeedModifier = GroundedData.RunData.SpeedModifier;
        stateMachine.ReusableData.CurrentJumpForce = AirborneData.JumpData.MediumForce;
        stateMachine.Player.Animator.CrossFade(AnimationData.GetAnimationHash(AnimationData.RunningAnimationName),
            0);
    }

    #endregion

    #region Input Methods

    protected override void OnMovementCanceled(InputAction.CallbackContext context)
    {
        base.OnMovementCanceled(context);
        
        stateMachine.ChangeState(stateMachine.MediumStoppingState);
    }

    #endregion
    
}
