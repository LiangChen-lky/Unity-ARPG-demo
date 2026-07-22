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
        if (HitFXList == null || hitFXIndex < 0 || hitFXIndex >= HitFXList.Length)
        {
            return;
        }

        HitFXConfig hitFXConfig = HitFXList[hitFXIndex];
        if (hitFXConfig == null || hitFXConfig.HitFXList == null || hitFXConfig.HitFXList.Length == 0)
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
