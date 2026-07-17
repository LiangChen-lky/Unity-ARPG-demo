using UnityEngine;

public class PlayerAttackRecoveryState : PlayerGroundedState
{
    private readonly PlayerAttackData attackData;
    private float recoveryElapsedTime;

    public PlayerAttackRecoveryState(PlayerMovementStateMachine stateMachine) : base(stateMachine)
    {
        attackData = stateMachine.Player.Data.AttackData;
    }

    #region IState Methods

    public override void Enter()
    {
        base.Enter();

        // 保持攻击结束时的姿势，不再切到待机动画。
        recoveryElapsedTime = 0f;
        stateMachine.ReusableData.MovementSpeedModifier = 0f;
        stateMachine.ReusableData.CurrentJumpForce = AirborneData.JumpData.StationaryForce;
        ResetVelocity();
    }

    public override void Update()
    {
        base.Update();

        if (stateMachine.ReusableData.MovementInput == Vector2.zero)
        {
            recoveryElapsedTime += Time.deltaTime;

            if (recoveryElapsedTime < attackData.RecoveryDuration)
            {
                return;
            }

            stateMachine.ChangeState(stateMachine.IdlingState);
            return;
        }

        OnMove();
    }

    #endregion
}
