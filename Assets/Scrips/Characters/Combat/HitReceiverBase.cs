using UnityEngine;

// 通用受击表现的默认实现，不是 Enemy 基类，也不处理生命、死亡或 AI。
// 玩家和 Enemy 可在各自脚本中继承此类，补充独立的角色逻辑。
public class HitReceiverBase : MonoBehaviour, IHitReceiver
{
    [field: SerializeField] public HitFXConfig[] HitFXList { get; private set; }
    [field: SerializeField] public Transform HitFXPosition { get; private set; }

    private ICombatEffectSpawner effectSpawner;

    protected virtual void Awake()
    {
        effectSpawner = new CombatEffectSpawner();
    }

    public virtual void ReceiveHit(HitContext context)
    {
        Vector3 targetForward = -context.SourceForward;
        targetForward.y = 0f;
        if (targetForward.sqrMagnitude > Mathf.Epsilon)
        {
            transform.forward = targetForward.normalized;
        }

        PlayHitFX(context.Force);
    }

    private void PlayHitFX(AttackForce force)
    {
        int hitFXIndex = (int)force;
        // HitFXList 未配置或力度档位未覆盖属于表现缺失，可以静默跳过这次受击特效。
        if (HitFXList == null || hitFXIndex < 0 || hitFXIndex >= HitFXList.Length)
        {
            return;
        }

        HitFXConfig hitFXConfig = HitFXList[hitFXIndex];
        // 数组槽位存在却为空引用，说明序列化丢了对象，属于配置错误而非表现缺失，必须暴露。
        if (hitFXConfig == null)
        {
            throw new System.InvalidOperationException(
                $"HitFXList[{hitFXIndex}] 引用为空，请在 Inspector 重新绑定受击特效配置。");
        }
        // 子特效列表未配置同样属于表现缺失，可以静默跳过。
        if (hitFXConfig.HitFXList == null || hitFXConfig.HitFXList.Length == 0)
        {
            return;
        }

        if (HitFXPosition == null)
        {
            return;
        }

        GameObject fxObject = hitFXConfig.TryGetOneFXObject();
        if (fxObject == null)
        {
            return;
        }

        effectSpawner.SpawnOneShot(fxObject, HitFXPosition.position, Vector3.zero, Vector3.one);
    }
}
