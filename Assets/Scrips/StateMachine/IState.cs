using UnityEngine;

public interface IState
{
    void Enter();
    void Exit();
    void HandleInput();
    void Update();
    void PhysicsUpdate();
    void OnAnimationEnterEvent();
    // 传递触发事件的动画来源，供状态区分过渡中的旧动画 Clip。
    void OnAnimationExitEnvent(AnimationEvent animationEvent);
    void OnAnimationTransitionEvent();
}
