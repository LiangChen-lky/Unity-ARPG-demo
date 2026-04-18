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

    private void OnFalling()
    {
        stateMachine.ChangeState(stateMachine.FallingState);
    }
    #endregion

    #region Reusable Methods

    protected override void OnExitWithGround()
    {
        base.OnExitWithGround();

        OnFalling();
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

    #endregion

    #region Input Methods

    protected virtual void OnDashStarted(InputAction.CallbackContext context)
    {
        stateMachine.ChangeState(stateMachine.DashingState);
    }
    
    private void OnJumpStarted(InputAction.CallbackContext context)
    {
        stateMachine.ChangeState(stateMachine.JumpingState);
    }

    #endregion
}
