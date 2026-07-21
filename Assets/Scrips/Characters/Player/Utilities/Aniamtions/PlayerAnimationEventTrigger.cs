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
    
    public void TriggerOnMovementStateAnimationTransitionEvent(AnimationEvent animationEvent)
    {
        // 将 AnimationEvent 原样转发，供攻击状态识别事件来自哪个动画 Clip。
        player.OnMovementStateAnimationTransitionEvent(animationEvent);
    }

    private bool IsInAnimationTransition(int layerIndex = 0)
    {
        return player.Animator.IsInTransition(layerIndex);
    }
}
