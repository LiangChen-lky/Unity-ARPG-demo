using System;
using Animancer;

public class PlayerLandingState : PlayerGroundedState
{
    private AnimancerState landingAnimationState;

    public PlayerLandingState(PlayerMovementStateMachine stateMachine) : base(stateMachine)
    {
    }

    public override void Exit()
    {
        // 落地动画被其他状态提前打断时，旧动画不得在淡出结束后再次切换 HFSM。
        if (landingAnimationState != null)
        {
            landingAnimationState.Events(this).OnEnd = null;
            landingAnimationState = null;
        }

        base.Exit();
    }

    // 只共享「播一次性动画 → 绑定本状态的 End → 离开状态时解绑」这段生命周期，
    // 具体的结束行为由各子类通过 onEnd 自己决定。
    protected void PlayLandingAnimation(ClipTransition transition, Action onEnd)
    {
        // 落地动画可重复触发（连续跳跃、Dash 接落地），每次进入状态都必须从头播放。
        landingAnimationState = stateMachine.Player.Animancer.Play(
            transition, transition.FadeDuration, FadeMode.FromStart);
        landingAnimationState.Events(this).OnEnd = onEnd;
    }
}
