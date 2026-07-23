using UnityEngine;

// Enemy 只负责把静态配置装配到运行时组件，不直接处理受击或属性计算。
public class Enemy : MonoBehaviour
{
    [field: Header("Data")]
    [field: SerializeField] public EnemySO Data { get; private set; }

    [field: Header("Runtime Components")]
    [field: SerializeField] public EnemyStats Stats { get; private set; }
    [field: SerializeField] public EnemyHitReceiver HitReceiver { get; private set; }

    private void Awake()
    {
        // 组合根在启动时注入初始生命值与受击组件依赖，避免子组件互相查询场景对象。
        Stats.Initialize(Data.MaxHealth);
        HitReceiver.Initialize(Stats);
    }
}
