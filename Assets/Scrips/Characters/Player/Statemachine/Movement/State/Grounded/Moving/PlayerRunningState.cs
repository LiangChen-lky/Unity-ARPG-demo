using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerRunningState : PlayerMovingState
{
    private float startTime;

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

        // TODO：Animancer 渐进迁移验证通过后删除旧 Animator 播放代码。
        // stateMachine.Player.Animator.CrossFade(
        //     AnimationData.RunningAnimationHash,
        //     0f);

        stateMachine.Player.Animancer.Play(
            GroundedData.RunData.Animation);

        startTime = Time.time;
    }

    public override void Update()
    {
        base.Update();

        if (!stateMachine.ReusableData.ShouldWalk)
        {
            return;
        }

        if (Time.time < startTime + GroundedData.SprintData.RunToWalkTime)
        {
            return;
        }

        StopRunning();
    }

    #endregion

    #region Main Methods

    private void StopRunning()
    {
        if (stateMachine.ReusableData.MovementInput == Vector2.zero)
        {
            stateMachine.ChangeState(stateMachine.IdlingState);
            return;
        }

        stateMachine.ChangeState(stateMachine.WalkingState);
    }

    #endregion

    #region Input Methods

    protected override void OnMovementCanceled(InputAction.CallbackContext context)
    {
        base.OnMovementCanceled(context);
        
        stateMachine.ChangeState(stateMachine.MediumStoppingState);
    }

    protected override void OnWalkToggleStarted(InputAction.CallbackContext context)
    {
        base.OnWalkToggleStarted(context);

        stateMachine.ChangeState(stateMachine.WalkingState);
    }

    #endregion
    
}
