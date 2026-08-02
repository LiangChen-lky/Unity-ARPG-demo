public class PlayerLightStoppingState : PlayerStoppingState
{
    public PlayerLightStoppingState(PlayerMovementStateMachine stateMachine) : base(stateMachine)
    {
    }

    public override void Enter()
    {
        base.Enter();

        PlayStoppingAnimation(GroundedData.StopData.LightMotionData);

        stateMachine.ReusableData.CurrentJumpForce = AirborneData.JumpData.WeakForce;
    }
}
