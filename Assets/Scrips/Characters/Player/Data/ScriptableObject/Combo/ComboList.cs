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
