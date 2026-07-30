using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 烘焙器的项目级配置。落在 ProjectSettings 下而非 EditorPrefs，使扫描根与采样模型随项目进版本管理，
/// 团队成员和脚本重载后都能拿到同一份配置，窗口本身不再是配置的权威来源。
/// </summary>
[FilePath(
    "ProjectSettings/AnimationMotionBakerSettings.asset",
    FilePathAttribute.Location.ProjectFolder)]
internal sealed class AnimationMotionBakerSettings :
    ScriptableSingleton<AnimationMotionBakerSettings>
{
    [SerializeField] private List<ScriptableObject> scanRoots =
        new List<ScriptableObject>();
    [SerializeField] private GameObject samplePrefab;
    [SerializeField] private AnimationMotionSampleRateMode sampleRateMode =
        AnimationMotionSampleRateMode.FromClip;

    public IReadOnlyList<ScriptableObject> ScanRoots => scanRoots;
    public GameObject SamplePrefab => samplePrefab;
    public AnimationMotionSampleRateMode SampleRateMode => sampleRateMode;
    // 判断是否已有可恢复的配置，窗口据此决定读取持久化数据还是迁移自身的旧配置。
    public bool HasConfiguration => scanRoots.Count > 0 || samplePrefab != null;

    /// <summary>
    /// 接收窗口当前配置并复制到项目级设置对象；这里只更新内存，实际落盘由 SaveSettings 统一触发。
    /// </summary>
    public void Apply(
        IReadOnlyList<ScriptableObject> roots,
        GameObject prefab,
        AnimationMotionSampleRateMode rateMode)
    {
        // 逐项复制而非直接持有传入列表：若共享同一个 List 引用，窗口后续增删会就地改写已持久化的数据，
        // 相当于绕过 SaveSettings 的显式保存时机，未确认的编辑也会被写进 ProjectSettings。
        scanRoots.Clear();

        foreach (ScriptableObject root in roots)
        {
            scanRoots.Add(root);
        }

        samplePrefab = prefab;
        sampleRateMode = rateMode;
    }

    /// <summary>
    /// 将当前设置写入 ProjectSettings，确保关闭窗口或重启 Unity 后仍可恢复。
    /// </summary>
    public void SaveSettings()
    {
        Save(true);
    }
}
