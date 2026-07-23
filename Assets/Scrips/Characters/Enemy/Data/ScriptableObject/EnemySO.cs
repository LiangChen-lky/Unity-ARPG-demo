using UnityEngine;

[CreateAssetMenu(fileName = "Enemy", menuName = "Custom/Character/Enemy")]
public class EnemySO : ScriptableObject
{
    // EnemySO 只保存静态初始值，运行时生命由 EnemyStats 单独维护。
    [field: SerializeField, Min(0.01f)] public float MaxHealth { get; private set; } = 3f;
}
