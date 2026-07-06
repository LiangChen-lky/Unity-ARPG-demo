using System.Collections;
using UnityEngine;

public class CombatControllerBase : MonoBehaviour
{
    [field: SerializeField] public HitFXConfig[] HitFXList { get; private set; }
    [field: SerializeField] public Transform[] FXPositionList { get; private set; }

    private Animator animator;
    private Coroutine executeMoveOffsetCoroutine;

    protected virtual void Awake()
    {
        animator = GetComponentInChildren<Animator>();
    }

    public void CharacterBeHit(ComboInteractionConfig interactionConfig, Transform attacker, MoveOffsetConfig moveOffsetConfig)
    {
        if (interactionConfig == null || attacker == null)
        {
            return;
        }

        transform.forward = -attacker.forward;

        if (animator != null && !string.IsNullOrEmpty(interactionConfig.HitName))
        {
            animator.Play(interactionConfig.HitName);
        }

        PlayHitFX(interactionConfig);
        ExecuteMoveOffset(moveOffsetConfig, attacker);
    }

    private void PlayHitFX(ComboInteractionConfig interactionConfig)
    {
        int hitFXIndex = (int)interactionConfig.AttackForce;
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

        ToolManager.Instance.PlayOneFX(fxObject, FXPositionList[0].position, Vector3.zero, Vector3.one);
    }

    private void ExecuteMoveOffset(MoveOffsetConfig moveOffsetConfig, Transform user)
    {
        if (moveOffsetConfig == null || moveOffsetConfig.MoveCurve == null || user == null)
        {
            return;
        }

        if (executeMoveOffsetCoroutine != null)
        {
            StopCoroutine(executeMoveOffsetCoroutine);
        }

        Vector3 direction = user.GetMoveOffsetDirection(moveOffsetConfig.MoveOffsetDirection);
        executeMoveOffsetCoroutine = StartCoroutine(ExecuteMoveOffsetIEnumerator(moveOffsetConfig, direction));
    }

    private IEnumerator ExecuteMoveOffsetIEnumerator(MoveOffsetConfig moveOffsetConfig, Vector3 direction)
    {
        if (animator == null)
        {
            executeMoveOffsetCoroutine = null;
            yield break;
        }

        while (animator.GetCurrentAnimatorStateInfo(0).normalizedTime < moveOffsetConfig.Duration)
        {
            yield return null;
            float value = moveOffsetConfig.MoveCurve.Evaluate(animator.GetCurrentAnimatorStateInfo(0).normalizedTime);
        }

        executeMoveOffsetCoroutine = null;
    }
}
