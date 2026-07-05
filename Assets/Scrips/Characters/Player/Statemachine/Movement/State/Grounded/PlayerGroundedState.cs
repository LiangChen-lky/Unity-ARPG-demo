using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerGroundedState : PlayerMovementState
{
    protected SlopeData slopeData;
    
    public PlayerGroundedState(PlayerMovementStateMachine stateMachine) : base(stateMachine)
    {
        slopeData = stateMachine.Player.ColliderUtility.SlopeData;
    }

    #region IState Methods

    public override void Enter()
    {
        base.Enter();
        
        UpdateShouldSprintState();
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
        
        Float();
    }

    #endregion

    #region Main Methods

    private void Float()
    {
        Vector3 capsuleColliderCenterInWorldSpace =
            stateMachine.Player.ColliderUtility.CapsuleColliderData.Collider.bounds.center;

        Ray downwardsInColliderCenter = new Ray(capsuleColliderCenterInWorldSpace, Vector3.down);

        if (Physics.Raycast(downwardsInColliderCenter, out RaycastHit hit, slopeData.FloatRayDistance,
                stateMachine.Player.LayerData.GroundLayer, QueryTriggerInteraction.Ignore))
        {
            float slopeAngle = Vector3.Angle(hit.normal, -downwardsInColliderCenter.direction);

            float slopeSpeedModifier = SetSlopeSpeedModifierOnAngle(slopeAngle);
            if (slopeSpeedModifier == 0f)
            {
                return;
            }
            
            float distanceToFloatPoint =
                stateMachine.Player.ColliderUtility.CapsuleColliderData.ColliderCenterInLocalSpace.y *
                stateMachine.Player.transform.localScale.y - hit.distance;

            if (distanceToFloatPoint == 0f)
            {
                return;
            }

            Vector3 currentVerticalVelocity = GetPlayerVerticalVelocity();
            Vector3 floatForce = new Vector3(0f, slopeData.StepReachForce * distanceToFloatPoint, 0f);
            
            stateMachine.Player.Rigidbody.AddForce(floatForce - currentVerticalVelocity, ForceMode.VelocityChange);
        }
    }

    private float SetSlopeSpeedModifierOnAngle(float slopeAngle)
    {
        float slopeSpeedModifier = GroundedData.SlopeSpeedAngle.Evaluate(slopeAngle);
        stateMachine.ReusableData.MovementOnSlopeSpeedModifier = slopeSpeedModifier;
        return slopeSpeedModifier;
    }
    
    private void UpdateShouldSprintState()
    {
        if (!stateMachine.ReusableData.ShouldSprint)
            return;
        if (stateMachine.ReusableData.MovementInput != Vector2.zero)
            return;

        stateMachine.ReusableData.ShouldSprint = false;
    }
    #endregion

    #region Reusable Methods

    protected void OnMove()
    {
        if (stateMachine.ReusableData.ShouldSprint)
        {
            stateMachine.ChangeState(stateMachine.SprintingState);
            return;
        }
        stateMachine.ChangeState(stateMachine.RunningState);
    }
    
    protected override void OnExitWithGround()
    {
        base.OnExitWithGround();
        
        if (IsGroundUnderneath())
        {
            return;
        }
        
        Vector3 CapsuleColliderCenterInWorldSpace =
            stateMachine.Player.ColliderUtility.CapsuleColliderData.Collider.bounds.center;
        if (Physics.Raycast(
                CapsuleColliderCenterInWorldSpace -
                stateMachine.Player.ColliderUtility.CapsuleColliderData.ColliderVerticalExtents, Vector3.down, out _,
                GroundedData.GroundToFallRayDistance, stateMachine.Player.LayerData.GroundLayer,
                QueryTriggerInteraction.Ignore)) 
        {
            return;
        }

        OnFalling();
    }
    
    protected virtual void OnFalling()
    {
        stateMachine.ChangeState(stateMachine.FallingState);
    }

    protected override void AddInputActionCallbacks()
    {
        base.AddInputActionCallbacks();
        
        stateMachine.Player.Input.PlayerActions.Dash.started += OnDashStarted;

        stateMachine.Player.Input.PlayerActions.Jump.started += OnJumpStarted;
    }

    protected override void RemoveInputActionCallbacks()
    {
        base.RemoveInputActionCallbacks();
        
        stateMachine.Player.Input.PlayerActions.Dash.started -= OnDashStarted;
        
        stateMachine.Player.Input.PlayerActions.Jump.started -= OnJumpStarted;
    }

    private bool IsGroundUnderneath()
    {
        BoxCollider groundCheckCollider = stateMachine.Player.ColliderUtility.TriggerColliderData.GroundCheckCollider;
        Vector3 groundCheckColliderCenterInWorldSpace = groundCheckCollider.bounds.center;

        Collider[] overlappedGroundColliders = Physics.OverlapBox(groundCheckColliderCenterInWorldSpace,
            groundCheckCollider.bounds.extents, Quaternion.identity, stateMachine.Player.LayerData.GroundLayer,
            QueryTriggerInteraction.Ignore);
        
        return overlappedGroundColliders.Length > 0;

    }
    #endregion

    #region Input Methods

    protected virtual void OnDashStarted(InputAction.CallbackContext context)
    {
        stateMachine.ChangeState(stateMachine.DashingState);
    }
    
    protected virtual void OnJumpStarted(InputAction.CallbackContext context)
    {
        stateMachine.ChangeState(stateMachine.JumpingState);
    }

    #endregion
}
