using System;
using UnityEngine;

[CreateAssetMenu(fileName = "ComboList", menuName = "ScriptableObject/Combat/ComboList")]
public class ComboList : ScriptableObject //招式表
{
    [field: SerializeField] public ComboConfig[] ComboConfigs { get; private set; }

    /// <summary>
    /// 校验整张连招表，把数据规则的审查留在数据层。
    /// 校验失败抛出 InvalidOperationException，错误信息包含 ComboList 名称、段号、命中事件号、字段与允许范围。
    /// </summary>
    public void ValidateConfiguration()
    {
        // 招式表未配置任何段时无法进入攻击，属于开发配置错误。
        if (ComboConfigs == null || ComboConfigs.Length == 0)
        {
            throw new InvalidOperationException(
                $"ComboList \"{name}\" 未配置任何 ComboConfig，至少需要一段招式。");
        }

        for (int comboIndex = 0; comboIndex < ComboConfigs.Length; comboIndex++)
        {
            ComboConfig comboConfig = ComboConfigs[comboIndex];

            if (comboConfig == null)
            {
                throw new InvalidOperationException(
                    $"ComboList \"{name}\" 第 {comboIndex + 1} 段招式：ComboConfig 引用不能为 null。");
            }

            // 单段具体校验交给 ComboConfig 自己负责，这里只负责遍历与命名定位。
            comboConfig.ValidateConfiguration(comboIndex, name);
        }
    }

    public int TryGetComboConfigsCount()
    {
        return ComboConfigs.Length;
    }
    
    public string TryGetComboName(int comboIndex)
    {
        if (comboIndex < 0 || comboIndex >= ComboConfigs.Length)
        {
            return null;
        }
        return ComboConfigs[comboIndex].ComboName;
    }
    
    public ComboInteractionConfig TryGetComboInteractionConfig(int comboIndex, int eventIndex)
    {
        if (comboIndex < 0 || comboIndex >= ComboConfigs.Length)
        {
            return null;
        }
        if (eventIndex < 0 || eventIndex >= ComboConfigs[comboIndex].InteractionConfig.Length)
        {
            return null;
        }
        return ComboConfigs[comboIndex].InteractionConfig[eventIndex];
    }
    
    public AttackDetectionConfig TryGetAttackDetectionConfig(int comboIndex, int eventIndex)
    {
        if (comboIndex < 0 || comboIndex >= ComboConfigs.Length)
        {
            return null;
        }
        if (eventIndex < 0 || eventIndex >= ComboConfigs[comboIndex].AttackDetectionConfig.Length)
        {
            return null;
        }
        return ComboConfigs[comboIndex].AttackDetectionConfig[eventIndex];
    }

    // 当前攻击段已经确认存在检测事件后，按该段绑定的动画计算其触发进度。
    public float GetAttackDetectionNormalizedTime(int comboIndex, int eventIndex)
    {
        ComboConfig comboConfig = ComboConfigs[comboIndex];
        return comboConfig.GetAttackDetectionNormalizedTime(
            comboConfig.AttackDetectionConfig[eventIndex]);
    }

    public float GetRecoveryStartNormalizedTime(int comboIndex)
    {
        return ComboConfigs[comboIndex].GetRecoveryStartNormalizedTime();
    }

    public AttackFeedbackConfig TryGetAttackFeedbackConfig(int comboIndex, int eventIndex)
    {
        if (comboIndex < 0 || comboIndex >= ComboConfigs.Length)
        {
            return null;
        }
        if (eventIndex < 0 || eventIndex >= ComboConfigs[comboIndex].AttackFeedbackConfig.Length)
        {
            return null;
        }
        return ComboConfigs[comboIndex].AttackFeedbackConfig[eventIndex];
    }
    
    public FXConfig TryGetFXConfig(int comboIndex, int eventIndex)
    {
        if (comboIndex < 0 || comboIndex >= ComboConfigs.Length)
        {
            return null;
        }
        if (eventIndex < 0 || eventIndex >= ComboConfigs[comboIndex].FXConfig.Length)
        {
            return null;
        }
        return ComboConfigs[comboIndex].FXConfig[eventIndex];
    }
    
    public SFXConfig TryGetSFXConfig(int comboIndex, int eventIndex)
    {
        if (comboIndex < 0 || comboIndex >= ComboConfigs.Length)
        {
            return null;
        }
        if (eventIndex < 0 || eventIndex >= ComboConfigs[comboIndex].SFXConfig.Length)
        {
            return null;
        }
        return ComboConfigs[comboIndex].SFXConfig[eventIndex];
    }
    
}
