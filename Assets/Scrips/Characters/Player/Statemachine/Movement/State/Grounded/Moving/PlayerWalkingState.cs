using UnityEngine.InputSystem;

public class PlayerWalkingState : PlayerMovingState
{
    public PlayerWalkingState(PlayerMovementStateMachine stateMachine) : base(stateMachine)
    {
    }

    public override void Enter()
    {
        base.Enter();

        stateMachine.ReusableData.MovementSpeedModifier = GroundedData.WalkData.SpeedModifier;
        stateMachine.ReusableData.CurrentJumpForce = AirborneData.JumpData.WeakForce;

        // TODO：Animancer 渐进迁移验证通过后删除旧 Animator 播放代码。
        // stateMachine.Player.Animator.CrossFade(
        //     AnimationData.WalkingAnimationHash,
        //     AnimationData.NormalizedTransitionDuration);

        stateMachine.Player.Animancer.Play(
            GroundedData.WalkData.Animation);
    }

    protected override void OnMovementCanceled(InputAction.CallbackContext context)
    {
        base.OnMovementCanceled(context);

        stateMachine.ChangeState(stateMachine.LightStoppingState);
    }

    protected override void OnWalkToggleStarted(InputAction.CallbackContext context)
    {
        base.OnWalkToggleStarted(context);

        stateMachine.ChangeState(stateMachine.RunningState);
    }
}
