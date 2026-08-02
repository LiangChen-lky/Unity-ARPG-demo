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

        // TODO：Animancer 渐进迁移验证通过后删除旧 Animator 播放代码。
        // stateMachine.Player.Animator.CrossFadeInFixedTime(
        //     AnimationData.IdlingAnimationHash,
        //     AnimationData.FixedTransitionDuration);

        stateMachine.Player.Animancer.Play(GroundedData.IdleData.Animation);
    }

    public override void Update()
    {
        base.Update();

        if (stateMachine.ReusableData.MovementInput == Vector2.zero)
        {
            return;
        }
        
        OnMove();
    }

    #endregion
}
