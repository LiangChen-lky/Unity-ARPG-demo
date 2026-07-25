using System;
using UnityEngine;

[CreateAssetMenu(fileName = "ComboConfig", menuName = "ScriptableObject/Combat/ComboConfig")]
public class ComboConfig : ScriptableObject
{
    [Header("基础数据")]
    public string ComboName;
    public AnimationClip AttackClip;

    [Header("命中交互数据")]
    public ComboInteractionConfig[] InteractionConfig;

    [Header("攻击检测数据")]
    public AttackDetectionConfig[] AttackDetectionConfig;

    [Header("打击感数据")]
    public AttackFeedbackConfig[] AttackFeedbackConfig;

    [Header("特效数据")]
    public FXConfig[] FXConfig;

    [Header("音效数据")]
    public SFXConfig[] SFXConfig;

    // 命中配置使用帧号编辑，这里统一换算为 Animator 使用的归一化进度。
    public float GetAttackDetectionNormalizedTime(AttackDetectionConfig detectionConfig)
    {
        return detectionConfig.GetNormalizedStartTime(AttackClip);
    }

    /// <summary>
    /// 校验本段连招配置，保证执行器只需消费已验证的数据。
    /// 校验失败抛出 InvalidOperationException，错误信息包含段号、命中事件号、字段名与允许范围。
    /// </summary>
    public void ValidateConfiguration(int comboIndex, string comboListName)
    {
        string comboPrefix = $"ComboList \"{comboListName}\" 第 {comboIndex + 1} 段招式";

        // 段名缺失属于无法定位的配置错误，必须在最外层拦截。
        if (string.IsNullOrEmpty(ComboName))
        {
            throw new InvalidOperationException($"{comboPrefix}：ComboName 不能为空。");
        }

        // 没有动画片段时，帧号无法换算为归一化进度，后续校验也无法继续。
        if (AttackClip == null)
        {
            throw new InvalidOperationException($"{comboPrefix}（{ComboName}）：AttackClip 不能为 null。");
        }

        // 动画长度或采样率为 0 时换算归一化进度会除零，属于无效动画配置。
        float clipFrameSpan = AttackClip.length * AttackClip.frameRate;
        if (AttackClip.length <= 0f || AttackClip.frameRate <= 0f || clipFrameSpan < 1f)
        {
            throw new InvalidOperationException(
                $"{comboPrefix}（{ComboName}）：AttackClip 的 length（{AttackClip.length}）" +
                $"与 frameRate（{AttackClip.frameRate}）必须都大于 0，" +
                $"且换算出的总帧数（{clipFrameSpan}）必须不小于 1。");
        }

        // 执行器与数据层在读取下列数组时都会访问其 .Length，null 会直接 NullReferenceException；
        // 没有对应事件时请配置为空数组，null 属于配置错误。空数组语义为"没有事件"，合法。
        if (FXConfig == null)
        {
            throw new InvalidOperationException(
                $"{comboPrefix}（{ComboName}）：FXConfig 不能为 null；没有攻击特效时请配置为空数组。");
        }

        if (SFXConfig == null)
        {
            throw new InvalidOperationException(
                $"{comboPrefix}（{ComboName}）：SFXConfig 不能为 null；没有攻击音效时请配置为空数组。");
        }

        if (AttackFeedbackConfig == null)
        {
            throw new InvalidOperationException(
                $"{comboPrefix}（{ComboName}）：AttackFeedbackConfig 不能为 null；没有打击感反馈时请配置为空数组。");
        }

        // 先排除任一方为 null：一边 null、一边空数组（[]）属于"检测盒或交互数据漏配"的隐蔽错误，
        // 必须先于"两边都为空"的无伤害招式判断拦截，否则会绕过下面的 null 校验直接放行。
        if (AttackDetectionConfig == null || InteractionConfig == null)
        {
            throw new InvalidOperationException(
                $"{comboPrefix}（{ComboName}）：AttackDetectionConfig 与 InteractionConfig 必须同时存在或同时为空，" +
                $"当前 AttackDetectionConfig 为 {(AttackDetectionConfig == null ? "null" : $"长度 {AttackDetectionConfig.Length}")}，" +
                $"InteractionConfig 为 {(InteractionConfig == null ? "null" : $"长度 {InteractionConfig.Length}")}。");
        }

        // 两边都是非 null 的空数组才表示这一段是无伤害招式；到此任一方都不为 null，可直接读 Length。
        if (AttackDetectionConfig.Length == 0 && InteractionConfig.Length == 0)
        {
            return;
        }

        if (AttackDetectionConfig.Length != InteractionConfig.Length)
        {
            throw new InvalidOperationException(
                $"{comboPrefix}（{ComboName}）：AttackDetectionConfig 长度（{AttackDetectionConfig.Length}）" +
                $"与 InteractionConfig 长度（{InteractionConfig.Length}）必须一致。");
        }

        // 逐个命中事件校验，记录事件号便于定位具体配置项。
        int lastStartFrame = 0;
        for (int eventIndex = 0; eventIndex < AttackDetectionConfig.Length; eventIndex++)
        {
            ValidateHitEvent(comboPrefix, eventIndex, lastStartFrame, clipFrameSpan, ref lastStartFrame);
        }
    }

    // 校验单条命中事件：检测配置与交互配置同索引一一对应，二者均不能为空引用。
    private void ValidateHitEvent(
        string comboPrefix,
        int eventIndex,
        int previousStartFrame,
        float clipFrameSpan,
        ref int lastStartFrame)
    {
        string eventPrefix = $"{comboPrefix} 第 {eventIndex + 1} 个命中事件";

        AttackDetectionConfig detectionConfig = AttackDetectionConfig[eventIndex];
        ComboInteractionConfig interactionConfig = InteractionConfig[eventIndex];

        if (detectionConfig == null)
        {
            throw new InvalidOperationException(
                $"{eventPrefix}：AttackDetectionConfig[{eventIndex}] 引用不能为 null。");
        }

        if (interactionConfig == null)
        {
            throw new InvalidOperationException(
                $"{eventPrefix}：InteractionConfig[{eventIndex}] 引用不能为 null。");
        }

        // StartFrame 从 1 开始计数，保证换算出的归一化进度非负。
        if (detectionConfig.StartFrame < 1)
        {
            throw new InvalidOperationException(
                $"{eventPrefix}：StartFrame（{detectionConfig.StartFrame}）必须不小于 1。");
        }

        // 帧号不得超过该动画可触发的最后一帧，保证换算出的归一化时间严格小于 1。
        int lastTriggerableFrame = Mathf.FloorToInt(clipFrameSpan);
        if (detectionConfig.StartFrame > lastTriggerableFrame)
        {
            throw new InvalidOperationException(
                $"{eventPrefix}：StartFrame（{detectionConfig.StartFrame}）不得超过 AttackClip 可触发的最后一帧（{lastTriggerableFrame}），" +
                $"否则换算出的归一化时间不严格小于 1。");
        }

        // 命中事件按 StartFrame 非递减排列，允许同一帧存在多个碰撞盒。
        if (detectionConfig.StartFrame < previousStartFrame)
        {
            throw new InvalidOperationException(
                $"{eventPrefix}：StartFrame（{detectionConfig.StartFrame}）小于前一事件的帧号（{previousStartFrame}），" +
                $"命中事件必须按 StartFrame 非递减排列。");
        }

        lastStartFrame = detectionConfig.StartFrame;

        // Scale 是 Physics.OverlapBox 的半尺寸，三分量都必须大于 0。
        if (detectionConfig.Scale.x <= 0f || detectionConfig.Scale.y <= 0f || detectionConfig.Scale.z <= 0f)
        {
            throw new InvalidOperationException(
                $"{eventPrefix}：Scale（{detectionConfig.Scale}）的 x/y/z 必须都大于 0，" +
                $"因为它实际作为 Physics.OverlapBox 的半尺寸使用。");
        }

        // 伤害允许为 0 表示纯表现性命中，但不允许负数。
        if (interactionConfig.Damage < 0f)
        {
            throw new InvalidOperationException(
                $"{eventPrefix}：Damage（{interactionConfig.Damage}）必须不小于 0。");
        }

        // AttackForce 必须是已定义的枚举值，避免反序列化出未定义档位后静默通过。
        if (!System.Enum.IsDefined(typeof(AttackForce), interactionConfig.AttackForce))
        {
            throw new InvalidOperationException(
                $"{eventPrefix}：AttackForce（{interactionConfig.AttackForce}）必须是 Easy/Medium/Hard 中的有效值。");
        }
    }
}

[Serializable]
public class ComboInteractionConfig
{
    // 攻击方只传递命中力度与伤害，目标表现由受击方自行决定。
    public AttackForce AttackForce;
    public float Damage;
}

[Serializable]
public class AttackDetectionConfig
{
    // 从动画的第 1 帧开始计数，避免策划配置时手动换算归一化进度。
    [Min(1)] public int StartFrame = 1;
    public Vector3 Position;
    public Vector3 Rotation;
    public Vector3 Scale;

    // 以动画实际长度和采样率换算，保证不同帧数的招式都能直接按帧配置。
    public float GetNormalizedStartTime(AnimationClip attackClip)
    {
        return (StartFrame - 1) / (attackClip.length * attackClip.frameRate);
    }
}

[Serializable]
public class FXConfig
{
    // 触发时间点和特效信息。
    public float StartTime;
    public GameObject FXObject;
    public Vector3 Position;
    public Vector3 Rotation;
    public Vector3 Scale;
}

[Serializable]
public class SFXConfig
{
    // 触发时间点和音效信息。
    public float StartTime;
    public AudioClip SFXClip;
    public float Volume;
    public float Duration;
}

[Serializable]
public class AttackFeedbackConfig
{
    // 屏幕震动参数。
    public float Strength;
    public float Frequency;
    public float Duration;
}
