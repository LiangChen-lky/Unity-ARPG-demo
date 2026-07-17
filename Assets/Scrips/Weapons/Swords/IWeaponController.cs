using UnityEngine;

/// <summary>
/// 玩家状态机与具体武器之间的最小交互边界。
/// 实现类只处理武器自身的姿态和表现，不应在内部创建第二套输入监听。
/// </summary>
public interface IWeaponController
{
    // 供外部读取武器是否已经进入攻击表现。
    bool IsAttacking { get; }

    // 检查武器自身是否允许开始一次攻击表现。
    bool CanStartAttack();

    // 开始和取消武器表现；攻击判定由战斗执行器负责。
    void StartAttack();
    void CancelAttack();

    // 由武器组件转发 Unity 的帧更新和物理更新。
    void Update(float deltaTime);
    void PhysicsUpdate(float fixedDeltaTime);

    // 提供水平朝向，供需要独立朝向表现的武器实现使用。
    void SetFacingDirection(Vector3 worldDirection, float minimum = 0.0001f);
}
