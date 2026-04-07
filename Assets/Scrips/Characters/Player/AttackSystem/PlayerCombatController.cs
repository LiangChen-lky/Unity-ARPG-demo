using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCombatController : CombatControllerBase
{
    private void Start()
    {
        AddInputActionCallbacks();
    }
    
    private void OnDisable()
    {
        RemoveInputActionCallbacks();
    }


    private void AddInputActionCallbacks()
    {
        Input.PlayerActions.Attack.started += OnAttackStarted;
    }
    
    private void RemoveInputActionCallbacks()
    {
        Input.PlayerActions.Attack.started -= OnAttackStarted;
    }

    private void OnAttackStarted(InputAction.CallbackContext context)
    {
        Debug.Log("Attack input received");
        if (canExecuteCombo)
        {
            ExecuteCombo();
        }
    }
}
