using UnityEngine;

public class PlayerMovementStateMachine : StateMachine
{
    public Player Player { get; }
    public PlayerReusableData ReusableData { get; }
    public CombatExecutor CombatExecutor { get; }
    public PlayerActionBuffer ActionBuffer { get; }

    public PlayerIdlingState IdlingState { get; }
    public PlayerDashingState DashingState { get; }
    
    public PlayerWalkingState WalkingState { get; }
    public PlayerRunningState RunningState { get; }
    public PlayerSprintingState SprintingState { get; }
    
    public PlayerLightStoppingState LightStoppingState { get; }
    public PlayerMediumStoppingState MediumStoppingState { get; }
    public PlayerHardStoppingState HardStoppingState { get; }

    public PlayerJumpingState JumpingState { get; }
    public PlayerFallingState FallingState { get; }

    public PlayerLightLandingState LightLandingState { get; }
    public PlayerRollingState RollingState { get; }
    public PlayerHardLandingState HardLandingState { get; }
    
    public PlayerAttackState AttackState { get; }
    
    public PlayerMovementStateMachine(Player player)
    {
        Player = player;
        ReusableData = new PlayerReusableData();
        CombatExecutor = new CombatExecutor(player.transform, player.Data.AttackData);
        // 缓冲器必须先于各状态创建，攻击状态在构造时就会取得引用。
        ActionBuffer = new PlayerActionBuffer();

        IdlingState = new PlayerIdlingState(this);
        DashingState = new PlayerDashingState(this);
        
        WalkingState = new PlayerWalkingState(this);
        RunningState = new PlayerRunningState(this);
        SprintingState = new PlayerSprintingState(this);
        
        LightStoppingState = new PlayerLightStoppingState(this);
        MediumStoppingState = new PlayerMediumStoppingState(this);
        HardStoppingState = new PlayerHardStoppingState(this);

        JumpingState = new PlayerJumpingState(this);
        FallingState = new PlayerFallingState(this);

        LightLandingState = new PlayerLightLandingState(this);
        RollingState = new PlayerRollingState(this);
        HardLandingState = new PlayerHardLandingState(this);
        
        AttackState = new PlayerAttackState(this);
    }

    public void OnTriggerEnter(Collider collider)
    {
        if (Player.LayerData.IsGroundLayer(collider.gameObject.layer))
        {
            if (currentState is ITriggerHandler handler)
            {
                handler.OnTriggerEnter(collider);
            }
        }
    }

    public void OnTriggerExit(Collider collider)
    {
        if (Player.LayerData.IsGroundLayer(collider.gameObject.layer))
        {
            if (currentState is ITriggerHandler handler)
            {
                handler.OnTriggerExit(collider);
            }
        }
        
    }
}
