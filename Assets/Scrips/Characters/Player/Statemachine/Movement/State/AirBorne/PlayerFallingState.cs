using Animancer;
using UnityEngine;

public class PlayerFallingState : PlayerAirborneState
{
    private Vector3 playerPositionOnEnter;
    private readonly PlayerFallData fallData;

    public PlayerFallingState(PlayerMovementStateMachine stateMachine) : base(stateMachine)
    {
        fallData = AirborneData.FallData;
    }

    #region IState Methods

    public override void Enter()
    {
        base.Enter();

        stateMachine.ReusableData.MovementSpeedModifier = 0f;
        playerPositionOnEnter = stateMachine.Player.transform.position;
        
        ResetVerticalVelocity();

        ClipTransition fallAnimation = fallData.Animation;
        // Fall 到 Landing 仍由接地检测决定，每次进入下落状态都从头播放动画。
        stateMachine.Player.Animancer.Play(
            fallAnimation,
            fallAnimation.FadeDuration,
            FadeMode.FromStart);
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
        
        LimitVerticalVelocity();
    }

    #endregion

    #region Main Methods

    private void LimitVerticalVelocity()
    {
        Vector3 playerVerticalVelocity = GetPlayerVerticalVelocity();
        if (playerVerticalVelocity.y >= -fallData.FallSpeedLimit)
        {
            return;
        }
        
        Vector3 limitedVelocity = new Vector3(0f, -fallData.FallSpeedLimit - playerVerticalVelocity.y, 0f);
        
        stateMachine.Player.Rigidbody.AddForce(limitedVelocity, ForceMode.VelocityChange);
    }

    #endregion

    #region Reusable Methods

    protected override void OnContactWithGround()
    {
        float fallDistance = playerPositionOnEnter.y - stateMachine.Player.transform.position.y;

        if (fallDistance < fallData.MinimumDistanceToBeConsideredHardFall)
        {
            stateMachine.ChangeState(stateMachine.LightLandingState);
            return;
        }

        if (stateMachine.ReusableData.ShouldWalk && !stateMachine.ReusableData.ShouldSprint ||
            stateMachine.ReusableData.MovementInput == Vector2.zero)
        {
            stateMachine.ChangeState(stateMachine.HardLandingState);
            return;
        }

        stateMachine.ChangeState(stateMachine.RollingState);
    }

    #endregion

}
