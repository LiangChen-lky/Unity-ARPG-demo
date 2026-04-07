using UnityEngine;
using UnityEngine.InputSystem;

public class SwordIdlingState : SwordBaseState
{
    public SwordIdlingState(SwordStateMachine stateMachine) : base(stateMachine)
    {
    }

    #region IState Methods

    public override void Enter()
    {
        base.Enter();
        
        ResetWeaponTransform();
    }

    public override void Update()
    {
        base.Update();
        
        Move();
    }

    #endregion
    
    #region Main Methods

    public void Move()
    {
        if (stateMachine.Sword.transform == stateMachine.Sword.WeaponIdlingPosition)
        {
            return;
        }
        
        ResetWeaponRotaion();
        
        stateMachine.Sword.transform.position =
            Vector3.SmoothDamp(stateMachine.Sword.transform.position, stateMachine.Sword.WeaponIdlingPosition.position,
                ref stateMachine.ReusableData.dampedMovementVelocity, stateMachine.Sword.Data.SmoothTime);

    }

    #endregion

    #region Main Methods

    private void ResetWeaponTransform()
    {
        ResetWeaponPosition();
        ResetWeaponRotaion();
    }
    
    private void ResetWeaponPosition()
    {
        stateMachine.Sword.transform.position = stateMachine.Sword.WeaponIdlingPosition.position;
    }
    
    private void ResetWeaponRotaion()
    {
        stateMachine.Sword.transform.rotation = stateMachine.Sword.WeaponIdlingPosition.rotation;
    }

    #endregion

    #region Reusable Methods

    protected override void AddPlayerInputActionCallbacks()
    {
        base.AddPlayerInputActionCallbacks();
        
        stateMachine.Sword.PlayerInput.PlayerActions.Attack.started += OnPlayerAttackStarted;
    }

    protected override void RemovePlayerInputActionCallbacks()
    {
        base.RemovePlayerInputActionCallbacks();
        
        stateMachine.Sword.PlayerInput.PlayerActions.Attack.started -= OnPlayerAttackStarted;
    }

    #endregion

    #region Input Methods

    private void OnPlayerAttackStarted(InputAction.CallbackContext context)
    {
        stateMachine.ChangeState(stateMachine.AttackingState);
    }

    #endregion
}
