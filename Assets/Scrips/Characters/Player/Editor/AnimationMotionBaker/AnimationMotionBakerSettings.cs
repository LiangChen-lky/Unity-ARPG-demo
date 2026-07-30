using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

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
    public bool HasConfiguration => scanRoots.Count > 0 || samplePrefab != null;

    public void Apply(
        IReadOnlyList<ScriptableObject> roots,
        GameObject prefab,
        AnimationMotionSampleRateMode rateMode)
    {
        // 设置对象持有独立列表，避免窗口后续修改列表时绕过显式保存。
        scanRoots.Clear();

        foreach (ScriptableObject root in roots)
        {
            scanRoots.Add(root);
        }

        samplePrefab = prefab;
        sampleRateMode = rateMode;
    }

    public void SaveSettings()
    {
        Save(true);
    }
}
