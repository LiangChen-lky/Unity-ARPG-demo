using System.Collections;
using UnityEngine;

public class CombatControllerBase : MonoBehaviour, IHitReceiver
{
    [field: SerializeField] public HitFXConfig[] HitFXList { get; private set; }
    [field: SerializeField] public Transform[] FXPositionList { get; private set; }

    private Animator animator;
    private ICombatEffectSpawner effectSpawner;
    private Coroutine executeMoveOffsetCoroutine;

    protected virtual void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        effectSpawner = new CombatEffectSpawner();
    }

    protected virtual void OnDisable()
    {
        if (executeMoveOffsetCoroutine == null)
        {
            return;
        }

        StopCoroutine(executeMoveOffsetCoroutine);
        executeMoveOffsetCoroutine = null;
    }

    public virtual void ReceiveHit(HitContext context)
    {
        Vector3 targetForward = -context.SourceForward;
        targetForward.y = 0f;
        if (targetForward.sqrMagnitude > Mathf.Epsilon)
        {
            transform.forward = targetForward.normalized;
        }

        if (animator != null && !string.IsNullOrEmpty(context.HitAnimationName))
        {
            animator.Play(context.HitAnimationName);
        }

        PlayHitFX(context.Force);
        ExecuteMoveOffset(context.Movement);
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

    private void ExecuteMoveOffset(HitMovement movement)
    {
        if (!movement.IsValid || animator == null)
        {
            return;
        }

        if (executeMoveOffsetCoroutine != null)
        {
            StopCoroutine(executeMoveOffsetCoroutine);
        }

        executeMoveOffsetCoroutine = StartCoroutine(ExecuteMoveOffsetCoroutine(movement));
    }

    private IEnumerator ExecuteMoveOffsetCoroutine(HitMovement movement)
    {
        yield return null;

        float previousValue = movement.Evaluate(movement.StartTime);

        while (true)
        {
            float normalizedTime = animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
            if (normalizedTime < movement.StartTime)
            {
                yield return null;
                continue;
            }

            float sampleTime = Mathf.Min(normalizedTime, movement.Duration);
            float currentValue = movement.Evaluate(sampleTime);
            transform.position += movement.Direction * (currentValue - previousValue);
            previousValue = currentValue;

            if (normalizedTime >= movement.Duration)
            {
                break;
            }

            yield return null;
        }

        executeMoveOffsetCoroutine = null;
    }
}
