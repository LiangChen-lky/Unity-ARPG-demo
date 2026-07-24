using UnityEngine;

[CreateAssetMenu(fileName = "ComboList", menuName = "ScriptableObject/Combat/ComboList")]
public class ComboList : ScriptableObject //招式表
{
    [field: SerializeField] public ComboConfig[] ComboConfigs { get; private set; }

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
