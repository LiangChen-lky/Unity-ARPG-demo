using UnityEngine.InputSystem;

public class SwordAttackingState : SwordBaseState
{
    public SwordAttackingState(SwordStateMachine stateMachine) : base(stateMachine)
    {
    }

    #region IState Methods

    public override void Update()
    {
        base.Update();

        stateMachine.Sword.transform.position = stateMachine.Sword.WeaponAttackingPosition.position;
        stateMachine.Sword.transform.rotation = stateMachine.Sword.WeaponAttackingPosition.rotation;
    }

    #endregion

    #region Reusable Methods

    protected override void AddPlayerInputActionCallbacks()
    {
        base.AddPlayerInputActionCallbacks();
        
        stateMachine.Sword.PlayerInput.PlayerActions.Dash.started += OnPlayerAttackCanceled;
    }

    protected override void RemovePlayerInputActionCallbacks()
    {
        base.RemovePlayerInputActionCallbacks();
        
        stateMachine.Sword.PlayerInput.PlayerActions.Dash.started -= OnPlayerAttackCanceled;
    }

    #endregion

    #region Input Methods

    private void OnPlayerAttackCanceled(InputAction.CallbackContext context)
    {
        stateMachine.ChangeState(stateMachine.IdlingState);
    }

    #endregion
}
