using System;
using UnityEngine;

// Enemy 的运行时属性入口。当前只管理生命值，未来属性也由这里统一维护。
public class EnemyStats : MonoBehaviour
{
    public float MaxHealth { get; private set; }
    public float CurrentHealth { get; private set; }
    public bool IsDead => CurrentHealth <= 0f;

    public void Initialize(float maxHealth)
    {
        if (maxHealth <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(maxHealth), "最大生命值必须大于 0。");
        }

        // 静态配置只在初始化时写入，之后的生命变化全部保留在运行时组件内。
        MaxHealth = maxHealth;
        CurrentHealth = maxHealth;
    }

    public void TakeDamage(float damage)
    {
        if (damage < 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(damage), "伤害不能为负数。");
        }

        if (IsDead)
        {
            return;
        }

        // 属性写入权集中在这里，任何外部系统都不能直接修改当前生命值。
        CurrentHealth = Mathf.Max(CurrentHealth - damage, 0f);

        // 测试阶段记录每次实际扣血结果，便于在 Console 验证攻击命中链路。
        Debug.Log($"{gameObject.name} 受到 {damage} 点伤害，当前生命：{CurrentHealth}/{MaxHealth}。", this);
    }
}
