public class SwordStateMachine : StateMachine
{
    public Sword Sword { get; }
    public WeaponReusableData ReusableData { get; private set; }
    
    public SwordIdlingState IdlingState { get; }
    public SwordAttackingState AttackingState { get; }
    
    public SwordStateMachine(Sword sword)
    {
        Sword = sword;

        ReusableData = new WeaponReusableData();
        
        IdlingState = new SwordIdlingState(this);
        AttackingState = new SwordAttackingState(this);
    }
}
