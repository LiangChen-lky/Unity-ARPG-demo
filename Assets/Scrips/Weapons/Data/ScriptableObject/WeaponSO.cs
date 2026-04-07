using UnityEngine;

[CreateAssetMenu(fileName = "Weapon", menuName = "Custom/Weapon")]
public class WeaponSO : ScriptableObject
{
    [field: SerializeField] public float SmoothTime { get; private set; } = 0.08f;
}
