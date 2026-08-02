using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 流水线末环：把整批已通过校验的烘焙结果写回配置资产。
/// 流程为：按 Owner 分组 → 一次性登记 Undo → 每个 Owner 用一个 SerializedObject 写完所有条目 → 统一保存。
/// </summary>
internal static class AnimationMotionBakeWriter
{
    public static void Write(IReadOnlyList<AnimationMotionBakeEntry> entries)
    {
        // 按 Owner 分组：同一资产可能内嵌多个 MotionData（如 PlayerSO 的三段 Stop），
        // 分组后每个资产只需一个 SerializedObject 与一次 ApplyModifiedProperties。
        Dictionary<ScriptableObject, List<AnimationMotionBakeEntry>> groupedEntries =
            GroupByOwner(entries);
        ScriptableObject[] owners = new ScriptableObject[groupedEntries.Count];
        groupedEntries.Keys.CopyTo(owners, 0);

        // 整批资产登记为一个撤销组，使一次烘焙可被一次 Ctrl+Z 完整回退。
        Undo.RecordObjects(owners, "烘焙动画运动数据");

        foreach (KeyValuePair<ScriptableObject, List<AnimationMotionBakeEntry>> pair
                 in groupedEntries)
        {
            SerializedObject serializedOwner = new SerializedObject(pair.Key);

            foreach (AnimationMotionBakeEntry entry in pair.Value)
            {
                WriteEntry(serializedOwner, entry);
            }

            serializedOwner.ApplyModifiedProperties();
            EditorUtility.SetDirty(pair.Key);
        }

        // 所有 Owner 都写完后才落盘一次，避免逐个保存产生多次资产导入。
        AssetDatabase.SaveAssets();
    }

    /// <summary>
    /// 按外层 ScriptableObject 聚合写回条目，使一个资产内的多份 MotionData 在同一个 SerializedObject 上提交。
    /// </summary>
    private static Dictionary<ScriptableObject, List<AnimationMotionBakeEntry>> GroupByOwner(
        IReadOnlyList<AnimationMotionBakeEntry> entries)
    {
        Dictionary<ScriptableObject, List<AnimationMotionBakeEntry>> groupedEntries =
            new Dictionary<ScriptableObject, List<AnimationMotionBakeEntry>>();

        foreach (AnimationMotionBakeEntry entry in entries)
        {
            ScriptableObject owner = entry.Target.Owner;
            if (!groupedEntries.TryGetValue(owner, out List<AnimationMotionBakeEntry> ownerEntries))
            {
                ownerEntries = new List<AnimationMotionBakeEntry>();
                groupedEntries.Add(owner, ownerEntries);
            }

            ownerEntries.Add(entry);
        }

        return groupedEntries;
    }

    /// <summary>
    /// 通过扫描阶段记录的 PropertyPath 定位内嵌的 AnimationMotionData，逐字段写回。
    /// 走 SerializedProperty 而非公开 setter，是为了让 AnimationMotionData 的字段对运行时保持只读，
    /// 同时沿用 Unity 的脏标记与撤销机制。
    /// </summary>
    private static void WriteEntry(
        SerializedObject serializedOwner,
        AnimationMotionBakeEntry entry)
    {
        SerializedProperty motionData =
            serializedOwner.FindProperty(entry.Target.PropertyPath);

        // 扫描与写回之间资产被改动（字段重命名、数组缩短）时路径会失效，此处显式报错而不是静默跳过。
        if (motionData == null)
        {
            throw new InvalidOperationException(
                $"{entry.Target.DisplayName}：写回时找不到原 SerializedProperty。");
        }

        // ClipTransition 是作者配置的唯一动画来源，烘焙器只写回由它生成的运动结果。
        motionData.FindPropertyRelative("speedCurve").animationCurveValue =
            entry.Result.SpeedCurve;
        motionData.FindPropertyRelative("rotationCurve").animationCurveValue =
            entry.Result.RotationCurve;
        motionData.FindPropertyRelative("bakedDuration").floatValue =
            entry.Result.BakedDuration;
    }
}
