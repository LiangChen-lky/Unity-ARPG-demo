using UnityEngine;

public class PlayerMovementStateMachine : StateMachine
{
    public Player Player { get; }
    public PlayerReusableData ReusableData { get; }

    public PlayerIdlingState IdlingState { get; }
    public PlayerDashingState DashingState { get; }
    
    public PlayerRunningState RunningState { get; }
    public PlayerSprintingState SprintingState { get; }
    
    public PlayerMediumStoppingState MediumStoppingState { get; }
    public PlayerHardStoppingState HardStoppingState { get; }

    public PlayerJumpingState JumpingState { get; }
    public PlayerFallingState FallingState { get; }
    
    public PlayerAttackStateMachine AttackStateMachine { get; }
    
    public PlayerMovementStateMachine(Player player)
    {
        Player = player;
        ReusableData = new PlayerReusableData();

        IdlingState = new PlayerIdlingState(this);
        DashingState = new PlayerDashingState(this);
        
        RunningState = new PlayerRunningState(this);
        SprintingState = new PlayerSprintingState(this);
        
        MediumStoppingState = new PlayerMediumStoppingState(this);
        HardStoppingState = new PlayerHardStoppingState(this);

        JumpingState = new PlayerJumpingState(this);
        FallingState = new PlayerFallingState(this);

        AttackStateMachine = new PlayerAttackStateMachine(this);
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
}
