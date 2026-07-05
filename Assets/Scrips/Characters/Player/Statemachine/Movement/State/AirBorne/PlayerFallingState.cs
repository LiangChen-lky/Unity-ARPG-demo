using UnityEngine;

public class PlayerFallingState : PlayerAirborneState
{
    public PlayerFallingState(PlayerMovementStateMachine stateMachine) : base(stateMachine)
    {
    }

    #region IState Methods

    public override void Enter()
    {
        base.Enter();

        stateMachine.ReusableData.MovementSpeedModifier = 0f;
        
        ResetVerticalVelocity();
        
        // stateMachine.Player.Animator.Play(AnimationData.GetAnimationHash(AnimationData.FallingAnimationName));
        stateMachine.Player.Animator.CrossFade(AnimationData.FallingAnimationHash,
            AnimationData.TransitionDuration);
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
        if (playerVerticalVelocity.y >= -AirborneData.FallData.FallSpeedLimit)
        {
            return;
        }
        
        Vector3 limitedVelocity = new Vector3(0f, -AirborneData.FallData.FallSpeedLimit - playerVerticalVelocity.y, 0f);
        
        stateMachine.Player.Rigidbody.AddForce(limitedVelocity, ForceMode.VelocityChange);
    }

    #endregion

}
