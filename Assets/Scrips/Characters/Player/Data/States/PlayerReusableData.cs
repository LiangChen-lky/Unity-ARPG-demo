using UnityEngine;

public class PlayerReusableData
{
    public Vector2 MovementInput { get; set; }
    public float MovementSpeedModifier { get; set; }
    public float MovementOnSlopeSpeedModifier { get; set; }
    public PlayerRotationData RotationData { get; set; }
    
    public Vector3 CurrentJumpForce { get; set; }
    
    public bool ShouldWalk { get; set; }
    public bool ShouldSprint { get; set; }
    
    private Vector3 currentTargetRotation;
    private Vector3 timeToReachTargetRotation;
    private Vector3 dampedTargetRotationVelocity;
    private Vector3 dampedTargetRotationPassedTime;

    public ref Vector3 CurrentTargetRotation
    {
        get
        {
            return ref currentTargetRotation;
        }
    }
    public ref Vector3 TimeToReachTargetRotation
    {
        get
        {
            return ref timeToReachTargetRotation;
        }
    }
    public ref Vector3 DampedTargetRotationVelocity
    {
        get
        {
            return ref dampedTargetRotationVelocity;
        }
    }
    public ref Vector3 DampedTargetRotationPassedTime
    {
        get
        {
            return ref dampedTargetRotationPassedTime;
        }
    }
    
}
