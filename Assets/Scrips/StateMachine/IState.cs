public interface IState
{
    void Enter();
    void Exit();
    void HandleInput();
    void Update();
    void PhysicsUpdate();
    void OnAnimationEnterEvent();
    void OnAnimationExitEnvent();
    void OnAnimationTransitionEvent();
}
