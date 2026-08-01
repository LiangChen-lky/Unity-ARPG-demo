public interface IState
{
    void Enter();
    void Exit();
    void HandleInput();
    void Update();
    void PhysicsUpdate();
    void OnAnimationEnterEvent();
    // 动画事件只作为状态切换通知，具体状态不再读取事件对象。
    void OnAnimationExitEvent();
    void OnAnimationTransitionEvent();
}
