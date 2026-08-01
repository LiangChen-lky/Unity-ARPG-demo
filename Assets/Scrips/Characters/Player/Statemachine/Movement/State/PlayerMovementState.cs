using System.Collections.Generic;
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

    public virtual void OnTriggerEnter(Collider collider)
    {
        OnContactWithGround();
    }

    public virtual void OnTriggerExit(Collider collider)
    {
        OnExitWithGround();
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
        if (stateMachine.Player.MainCameraTransform == null)
        {
            return directionAngle;
        }

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
        Vector3 horizontalVelocity = stateMachine.Player.Rigidbody.linearVelocity;
        horizontalVelocity.y = 0f;
        return horizontalVelocity;
    }

    protected Vector3 GetPlayerVerticalVelocity()
    {
        return new Vector3(0f, stateMachine.Player.Rigidbody.linearVelocity.y, 0f);
    }
    
    protected void ResetVelocity()
    {
        stateMachine.Player.Rigidbody.linearVelocity = Vector3.zero;
    }

    protected void ResetHorizontalVelocity()
    {
        stateMachine.Player.Rigidbody.linearVelocity = GetPlayerVerticalVelocity();
    }

    protected void ResetVerticalVelocity()
    {
        Vector3 playerHorizontalVelocity = GetPlayerHorizontalVelocity();
        stateMachine.Player.Rigidbody.linearVelocity = playerHorizontalVelocity;
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

    // TODO：MotionDriver PlayMode 验证通过后删除旧停止减速方法。
    // protected void DecelerateHorizontally()
    // {
    //     Vector3 horizontalVelocity = GetPlayerHorizontalVelocity();
    //     stateMachine.Player.Rigidbody.AddForce(
    //         -horizontalVelocity * stateMachine.ReusableData.MovementDecelerationForce,
    //         ForceMode.Acceleration);
    // }

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

    protected virtual void OnExitWithGround()
    {
        
    }

    protected void UpdateCameraRecenteringState(Vector2 movementInput)
    {
        if (movementInput == Vector2.zero || stateMachine.Player.MainCameraTransform == null)
        {
            return;
        }

        if (movementInput == Vector2.up)
        {
            DisableCameraRecentering();
            return;
        }

        float cameraVerticalAngle = stateMachine.Player.MainCameraTransform.eulerAngles.x;
        if (cameraVerticalAngle >= 270f)
        {
            cameraVerticalAngle -= 360f;
        }

        cameraVerticalAngle = Mathf.Abs(cameraVerticalAngle);

        if (movementInput == Vector2.down)
        {
            SetCameraRecenteringState(cameraVerticalAngle, GroundedData.BackwardsCameraRecenteringData);
            return;
        }

        SetCameraRecenteringState(cameraVerticalAngle, GroundedData.SidewaysCameraRecenteringData);
    }

    protected void EnableCameraRecentering(float waitTime = -1f, float recenteringTime = -1f)
    {
        float movementSpeed = GetMovementSpeed();

        if (movementSpeed == 0f)
        {
            movementSpeed = GroundedData.BaseSpeed;
        }

        stateMachine.Player.CameraUtility?.EnableRecentering(waitTime, recenteringTime, GroundedData.BaseSpeed, movementSpeed);
    }

    protected void DisableCameraRecentering()
    {
        stateMachine.Player.CameraUtility?.DisableRecentering();
    }

    protected void SetCameraRecenteringState(float cameraVerticalAngle, List<PlayerCameraRecenteringData> cameraRecenteringData)
    {
        if (cameraRecenteringData == null)
        {
            DisableCameraRecentering();
            return;
        }

        foreach (PlayerCameraRecenteringData recenteringData in cameraRecenteringData)
        {
            if (!recenteringData.IsWithinRange(cameraVerticalAngle))
            {
                continue;
            }

            EnableCameraRecentering(recenteringData.WaitTime, recenteringData.RecenteringTime);
            return;
        }

        DisableCameraRecentering();
    }
    
    protected virtual void AddInputActionCallbacks()
    {
        stateMachine.Player.Input.PlayerActions.WalkToggle.started += OnWalkToggleStarted;
        stateMachine.Player.Input.PlayerActions.Look.started += OnMouseMovementStarted;
        stateMachine.Player.Input.PlayerActions.Movement.performed += OnMovementPerformed;
        stateMachine.Player.Input.PlayerActions.Movement.canceled += OnMovementCanceled;
        stateMachine.Player.Input.PlayerActions.Attack.started += OnAttackStarted;
    }

    protected virtual void RemoveInputActionCallbacks()
    {
        stateMachine.Player.Input.PlayerActions.WalkToggle.started -= OnWalkToggleStarted;
        stateMachine.Player.Input.PlayerActions.Look.started -= OnMouseMovementStarted;
        stateMachine.Player.Input.PlayerActions.Movement.performed -= OnMovementPerformed;
        stateMachine.Player.Input.PlayerActions.Movement.canceled -= OnMovementCanceled;
        stateMachine.Player.Input.PlayerActions.Attack.started -= OnAttackStarted;
    }
    
    #endregion

    #region Input Methods
    
    protected virtual void OnMovementCanceled(InputAction.CallbackContext context)
    {
        DisableCameraRecentering();
    }

    protected virtual void OnMovementStarted(InputAction.CallbackContext context)
    {
        
    }

    protected virtual void OnWalkToggleStarted(InputAction.CallbackContext context)
    {
        stateMachine.ReusableData.ShouldWalk = !stateMachine.ReusableData.ShouldWalk;
    }

    private void OnMouseMovementStarted(InputAction.CallbackContext context)
    {
        UpdateCameraRecenteringState(stateMachine.ReusableData.MovementInput);
    }

    private void OnMovementPerformed(InputAction.CallbackContext context)
    {
        UpdateCameraRecenteringState(context.ReadValue<Vector2>());
    }
    
    protected virtual void OnAttackStarted(InputAction.CallbackContext context)
    {
        // 攻击状态会在 Enter 的最开始校验配置，避免在两个入口重复执行同一份校验。
        stateMachine.ChangeState(stateMachine.AttackState);
    }
    #endregion
}
