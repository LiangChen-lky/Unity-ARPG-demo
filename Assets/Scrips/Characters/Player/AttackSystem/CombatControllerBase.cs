using UnityEngine;

public class CombatControllerBase : MonoBehaviour, IHitReceiver
{
    [field: SerializeField] public HitFXConfig[] HitFXList { get; private set; }
    [field: SerializeField] public Transform[] FXPositionList { get; private set; }

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

        if (FXPositionList == null || FXPositionList.Length == 0 || FXPositionList[0] == null)
        {
            return;
        }

        GameObject fxObject = hitFXConfig.TryGetOneFXObject();
        if (fxObject == null)
        {
            return;
        }

        effectSpawner.SpawnOneShot(fxObject, FXPositionList[0].position, Vector3.zero, Vector3.one);
    }

}
