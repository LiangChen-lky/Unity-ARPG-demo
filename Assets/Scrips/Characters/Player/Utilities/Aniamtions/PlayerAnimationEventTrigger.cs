using UnityEngine;

public class PlayerAnimationEventTrigger : MonoBehaviour
{
    private Player player;

    private void Awake()
    {
        player = transform.parent.GetComponent<Player>();
    }

    public void TriggerOnMovementStateAnimationEnterEvent()
    {
        player.OnMovementStateAnimationEnterEvent();
    }
    
    public void TriggerOnMovementStateAnimationExitEvent(AnimationEvent animationEvent)
    {
        player.OnMovementStateAnimationExitEvent(animationEvent);
    }
    
    public void TriggerOnMovementStateAnimationTransitionEvent()
    {
        player.OnMovementStateAnimationTransitionEvent();
    }

    private bool IsInAnimationTransition(int layerIndex = 0)
    {
        return player.Animator.IsInTransition(layerIndex);
    }
}
