using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovementState : IState, ITriggerHandler
{
    protected PlayerMovementStateMachine stateMachine;

    protected PlayerGroundedData GroundedData;
    protected PlayerAirborneData AirborneData;
    protected PlayerAnimationData AnimationData;
    
    public PlayerMovementState(PlayerMovementStateMachine stateMachine)
    {
        this.stateMachine = stateMachine;
        GroundedData = stateMachine.Player.Data.GroundedData;
        AirborneData = stateMachine.Player.Data.AirborneData;
        AnimationData = stateMachine.Player.Data.AnimationData;
        
        InitializeData();
    }

    private void InitializeData()
    {
        SetRotationData(GroundedData.BaseRotationData);
    }

    #region IState Methods
    public virtual void Enter()
    {
        Debug.Log(GetType().Name);
        
        AddInputActionCallbacks();
    }

    public virtual void Exit()
    {
        RemoveInputActionCallbacks();
    }

    public virtual void HandleInput()
    {
        ReadMovementInput();
    }

    public virtual void Update()
    {
        
    }

    public virtual void PhysicsUpdate()
    {
        Move();
    }

    public virtual void OnAnimationEnterEvent()
    {
        
    }

    public virtual void OnAnimationExitEnvent()
    {
        
    }

    public virtual void OnAnimationTransitionEvent()
    {
        
    }

    #endregion

    #region ITrigger Methods

    public void OnTriggerEnter(Collider collider)
    {
        OnContactWithGround();
    }

    #endregion
    
    #region Main Methods
    
    private void ReadMovementInput()
    {
        stateMachine.ReusableData.MovementInput = stateMachine.Player.Input.PlayerActions.Movement.ReadValue<Vector2>();
    }

    private void Move()
    {
        
        if (stateMachine.ReusableData.MovementSpeedModifier == 0f || stateMachine.ReusableData.MovementInput == Vector2.zero)
        {
            return;
        }

        Vector3 movementDirection = GetMovementInput();
        float targetYAngle = Rotate(movementDirection);
        Vector3 targetDirection = GetTargetRotationDirection(targetYAngle);
        
        Vector3 currentHorizontalVelocity = GetPlayerHorizontalVelocity();
        float speed = GetMovementSpeed();
        
        stateMachine.Player.Rigidbody.AddForce(targetDirection * speed - currentHorizontalVelocity, ForceMode.VelocityChange);
    }

    private float Rotate(Vector3 direction)
    {
        float directionAngle = UpdateTargetRotation(direction);

        RotateTowardTargetRotation();

        return directionAngle;
    }

    private float AddCameraRotationToAngle(float directionAngle)
    {
        directionAngle += stateMachine.Player.MainCameraTransform.eulerAngles.y;
        if (directionAngle > 360)
        {
            directionAngle %= 360;
        }

        return directionAngle;
    }

    private static float GetDirectionAngle(Vector3 direction)
    {
        float directionAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        if (directionAngle < 0f)
        {
            directionAngle += 360f;
        }

        return directionAngle;
    }

    #endregion
    
    #region Reusable Methods

    protected float GetMovementSpeed()
    {
        return stateMachine.ReusableData.MovementSpeedModifier * GroundedData.BaseSpeed *
               stateMachine.ReusableData.MovementOnSlopeSpeedModifier;
    }

    protected Vector3 GetMovementInput()
    {
        return new Vector3(stateMachine.ReusableData.MovementInput.x, 0f, stateMachine.ReusableData.MovementInput.y);
    }
    
    protected Vector3 GetPlayerHorizontalVelocity()
    {
        Vector3 horizontalVelocity = stateMachine.Player.Rigidbody.velocity;
        horizontalVelocity.y = 0f;
        return horizontalVelocity;
    }

    protected Vector3 GetPlayerVerticalVelocity()
    {
        return new Vector3(0f, stateMachine.Player.Rigidbody.velocity.y, 0f);
    }
    
    protected void ResetVelocity()
    {
        stateMachine.Player.Rigidbody.velocity = Vector3.zero;
    }
    
    protected Vector3 GetTargetRotationDirection(float targetAngle)
    {
        return Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
    }
    
    protected float UpdateTargetRotation(Vector3 direction, bool shouldConsiderCameraRotation = true)
    {
        float directionAngle = GetDirectionAngle(direction);

        if (shouldConsiderCameraRotation)
        {
            directionAngle = AddCameraRotationToAngle(directionAngle);
        }

        if (!Mathf.Approximately(directionAngle, stateMachine.ReusableData.CurrentTargetRotation.y))
        {
            UpdateTargetRotationData(directionAngle);
        }
        return directionAngle;
    }

    private void UpdateTargetRotationData(float directionAngle)
    {
        stateMachine.ReusableData.CurrentTargetRotation.y = directionAngle;
        stateMachine.ReusableData.DampedTargetRotationPassedTime.y = 0f;
    }


    protected void RotateTowardTargetRotation()
    {
        float currentYAngle = stateMachine.Player.Rigidbody.rotation.eulerAngles.y;

        if (Mathf.Approximately(currentYAngle, stateMachine.ReusableData.CurrentTargetRotation.y))
        {
            return;
        }

        float smoothingYAngle = Mathf.SmoothDampAngle(currentYAngle, stateMachine.ReusableData.CurrentTargetRotation.y,
            ref stateMachine.ReusableData.DampedTargetRotationVelocity.y, stateMachine.ReusableData.TimeToReachTargetRotation.y - stateMachine.ReusableData.DampedTargetRotationPassedTime.y);
        stateMachine.ReusableData.DampedTargetRotationPassedTime.y += Time.deltaTime;
        
        Quaternion targetRotation = Quaternion.Euler(0f, smoothingYAngle, 0f);
        stateMachine.Player.Rigidbody.MoveRotation(targetRotation);
    }

    protected void DecelerateHorizontally()
    {
        Vector3 horizontalVelocity = GetPlayerHorizontalVelocity();
        stateMachine.Player.Rigidbody.AddForce(
            -horizontalVelocity * stateMachine.ReusableData.MovementDecelerationForce,
            ForceMode.Acceleration);
    }

    protected bool IsMovingHorizontally(float minimumMagnitude = 0.1f)
    {
        Vector3 horizontalVelocity = GetPlayerHorizontalVelocity();
        Vector2 velocity = new Vector2(horizontalVelocity.x, horizontalVelocity.z);
        return velocity.magnitude > minimumMagnitude;
    }
    
    protected void SetRotationData(PlayerRotationData rotationData)
    {
        stateMachine.ReusableData.RotationData = rotationData;
        stateMachine.ReusableData.TimeToReachTargetRotation.y =
            stateMachine.ReusableData.RotationData.TargetRotationReachTime.y;
    }

    protected void SetBaseRotationData()
    {
        SetRotationData(GroundedData.BaseRotationData);
    }
    
    protected bool IsMovingUp(float minimum = 0.1f)
    {
        return GetPlayerVerticalVelocity().y > minimum;
    }
    
    protected bool IsMovingDown(float minimum = 0.1f)
    {
        return GetPlayerVerticalVelocity().y < -minimum;
    }
    
    protected virtual void OnContactWithGround()
    {
        
    }
    
    protected virtual void AddInputActionCallbacks()
    {
        stateMachine.Player.Input.PlayerActions.Movement.canceled += OnMovementCanceled;

        stateMachine.Player.Input.PlayerActions.Attack.started += OnAttackStarted;
    }

    protected virtual void RemoveInputActionCallbacks()
    {
        stateMachine.Player.Input.PlayerActions.Movement.canceled -= OnMovementCanceled;
        
        stateMachine.Player.Input.PlayerActions.Attack.started -= OnAttackStarted;
    }
    
    #endregion

    #region Input Methods
    
    protected virtual void OnMovementCanceled(InputAction.CallbackContext context)
    {
        
    }
    protected virtual void OnMovementStarted(InputAction.CallbackContext context)
    {
        stateMachine.ChangeState(stateMachine.RunningState);
    }
    
    protected virtual void OnAttackStarted(InputAction.CallbackContext context)
    {
        stateMachine.ChangeState(stateMachine.AttackState);
    }
    #endregion
}
