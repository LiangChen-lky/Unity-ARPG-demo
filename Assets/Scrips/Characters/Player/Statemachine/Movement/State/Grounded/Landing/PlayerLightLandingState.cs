using UnityEngine;

public class PlayerLightLandingState : PlayerLandingState
{
    public PlayerLightLandingState(PlayerMovementStateMachine stateMachine) : base(stateMachine)
    {
    }

    public override void Enter()
    {
        base.Enter();

        stateMachine.ReusableData.MovementSpeedModifier = 0f;
        stateMachine.ReusableData.CurrentJumpForce = AirborneData.JumpData.StationaryForce;
        PlayLandingAnimation(GroundedData.LandingData.LightAnimation, OnLightLandingAnimationEnded);

        ResetVelocity();
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

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();

        if (!IsMovingHorizontally())
        {
            return;
        }

        ResetVelocity();
    }

    private void OnLightLandingAnimationEnded()
    {
        stateMachine.ChangeState(stateMachine.IdlingState);
    }
}
