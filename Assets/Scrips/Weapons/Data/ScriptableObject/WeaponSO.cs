using UnityEngine;

/// <summary>
/// 武器的可配置表现参数。
/// 具体武器引用该资产读取参数，避免把调参数据硬编码在 MonoBehaviour 中。
/// </summary>
[CreateAssetMenu(fileName = "Weapon", menuName = "Custom/Weapon")]
public class WeaponSO : ScriptableObject
{
    // 武器回到收刀挂点时的位置平滑时间，单位为秒。
    [field: SerializeField] public float SmoothTime { get; private set; } = 0.08f;
}
