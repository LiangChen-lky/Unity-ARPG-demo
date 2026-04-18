using UnityEngine;

public class PlayerAttackRecoveryState : PlayerGroundedState
{
    public PlayerAttackRecoveryState(PlayerMovementStateMachine stateMachine) : base(stateMachine)
    {
    }

    #region IState Methods

    public override void Enter()
    {
        base.Enter();

        // 保持攻击结束时的姿势，不再切到待机动画。
        stateMachine.ReusableData.MovementSpeedModifier = 0f;
        stateMachine.ReusableData.CurrentJumpForce = AirborneData.JumpData.StationaryForce;
        ResetVelocity();
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
