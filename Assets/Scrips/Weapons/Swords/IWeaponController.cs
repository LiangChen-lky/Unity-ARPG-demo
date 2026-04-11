using UnityEngine;

public interface IWeaponController
{
    bool IsAttacking { get; }
    bool CanStartAttack();

    void StartAttack();
    void CancelAttack();

    void Update(float deltaTime);
    void PhysicsUpdate(float fixedDeltaTime);

    void SetFacingDirection(Vector3 worldDirection, float minimum = 0.0001f);
}