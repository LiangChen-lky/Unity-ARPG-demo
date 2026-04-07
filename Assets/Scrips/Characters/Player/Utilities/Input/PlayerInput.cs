using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInput : MonoBehaviour
{
    public PlayerInputAction InputAction { get; private set; }
    public PlayerInputAction.PlayerActions PlayerActions { get; private set; }

    private void Awake()
    {
        EnsureInitialized();
    }

    private void OnEnable()
    {
        EnsureInitialized();
        InputAction.Enable();
    }

    private void OnDisable()
    {
        if (InputAction != null)
        {
            InputAction.Disable();
        }
    }

    public void EnsureInitialized()
    {
        if (InputAction != null)
        {
            return;
        }

        InputAction = new PlayerInputAction();
        PlayerActions = InputAction.Player;
    }

    public void DisableActionFor(InputAction action, float seconds)
    {
        StartCoroutine(DisableAction(action, seconds));
    }

    private IEnumerator DisableAction(InputAction action, float seconds)
    {
        action.Disable();

        yield return new WaitForSeconds(seconds);
        
        action.Enable();
    }
}
