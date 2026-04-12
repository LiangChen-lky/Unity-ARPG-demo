using UnityEngine;

public class PlayerJumpingState : PlayerAirborneState
{
    private bool shouldKeepRotating;
    private bool canStartFalling;
    
    public PlayerJumpingState(PlayerMovementStateMachine stateMachine) : base(stateMachine)
    {
    }

    #region IState Methods

    public override void Enter()
    {
        base.Enter();

        stateMachine.ReusableData.MovementSpeedModifier = 0f;
        SetRotationData(AirborneData.JumpData.RotationData);
        
        stateMachine.Player.Animator.Play(AnimationData.JumpingAnimationHash);

        shouldKeepRotating = stateMachine.ReusableData.MovementInput != Vector2.zero;

        Jump();
    }

    public override void Update()
    {
        base.Update();

        if (!canStartFalling && IsMovingUp())
        {
            canStartFalling = true;
        }

        if (!canStartFalling || GetPlayerVerticalVelocity().y > 0)
        {
            return;
        }
        
        stateMachine.ChangeState(stateMachine.FallingState);
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
        
        if (shouldKeepRotating)
        {
            RotateTowardTargetRotation();
        }
    }

    public override void Exit()
    {
        base.Exit();

        SetBaseRotationData();
        canStartFalling = false;
    }

    #endregion

    #region Main Methods

    private void Jump()
    {
        Vector3 jumpForce = stateMachine.ReusableData.CurrentJumpForce;

        Vector3 jumpDirection = stateMachine.Player.transform.forward;

        if (shouldKeepRotating)
        {
            jumpDirection = GetTargetRotationDirection(stateMachine.ReusableData.CurrentTargetRotation.y);
        }

        jumpForce.x *= jumpDirection.x;
        jumpForce.z *= jumpDirection.z;

        Vector3 capsuleColliderCenterInWorldSpace =
            stateMachine.Player.ColliderUtility.CapsuleColliderData.Collider.bounds.center;
        Ray downwardsRayFromColliderCenter = new Ray(capsuleColliderCenterInWorldSpace, Vector3.down);
        if (Physics.Raycast(downwardsRayFromColliderCenter, out var hit, AirborneData.JumpData.JumpToGroundRayDistance,
                stateMachine.Player.LayerData.GroundLayer, QueryTriggerInteraction.Ignore))
        {
            float slopeAngle = Vector3.Angle(-downwardsRayFromColliderCenter.direction, hit.normal);
            
            if (IsMovingUp())
            {
                float forceModifier = AirborneData.JumpData.JumpForceModifierOnSlopeUpwards.Evaluate(slopeAngle);
                
                jumpForce.x *= forceModifier;
                jumpForce.z *= forceModifier;
            }

            if (IsMovingDown())
            {
                float forceModifier = AirborneData.JumpData.JumpForceModifierOnSlopeDownwards.Evaluate(slopeAngle);
                
                jumpForce.y *= forceModifier;
            }
        }
        
        ResetVelocity();
        
        stateMachine.Player.Rigidbody.AddForce(jumpForce, ForceMode.VelocityChange);
    }

    #endregion
}
