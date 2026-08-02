using System.Collections.Generic;
using Animancer;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 从配置资产中找出所有待烘焙的 AnimationMotionData 字段。
/// 过程为：扫描根 → 展开 ComboList 取出其中的 ComboConfig → 按引用去重得到 Owner 列表 →
/// 用 SerializedObject 遍历每个 Owner 的内嵌 AnimationMotionData，逐个产出 BakeTarget。
/// </summary>
internal static class AnimationMotionTargetScanner
{
    public static List<AnimationMotionBakeTarget> Scan(
        IReadOnlyList<ScriptableObject> scanRoots)
    {
        List<ScriptableObject> owners = CollectOwners(scanRoots);
        List<AnimationMotionBakeTarget> targets = new List<AnimationMotionBakeTarget>();

        foreach (ScriptableObject owner in owners)
        {
            ScanOwner(owner, targets);
        }

        return targets;
    }

    /// <summary>
    /// 展开扫描根并去重：ComboList 这类容器资产本身不含 MotionData，需要额外取出它引用的 ComboConfig。
    /// 去重保证同一资产既被直接配置为扫描根、又被容器引用时也只烘焙一次。
    /// </summary>
    private static List<ScriptableObject> CollectOwners(
        IReadOnlyList<ScriptableObject> scanRoots)
    {
        List<ScriptableObject> owners = new List<ScriptableObject>();
        // visited 以引用相等去重，Owner 顺序沿用配置顺序以保持扫描结果稳定。
        HashSet<ScriptableObject> visited = new HashSet<ScriptableObject>();

        if (scanRoots == null)
        {
            return owners;
        }

        foreach (ScriptableObject root in scanRoots)
        {
            AddOwner(root, owners, visited);

            // ComboList 是当前唯一需要显式展开的 SO 容器，避免无边界追踪所有资源引用。
            if (root is ComboList comboList && comboList.ComboConfigs != null)
            {
                foreach (ComboConfig comboConfig in comboList.ComboConfigs)
                {
                    AddOwner(comboConfig, owners, visited);
                }
            }
        }

        return owners;
    }

    private static void AddOwner(
        ScriptableObject owner,
        List<ScriptableObject> owners,
        HashSet<ScriptableObject> visited)
    {
        if (owner != null && visited.Add(owner))
        {
            owners.Add(owner);
        }
    }

    /// <summary>
    /// 深度遍历单个 Owner 的序列化字段，按类型名匹配出所有 AnimationMotionData，
    /// 从而无需为每种配置资产单独写扫描逻辑，嵌套在数组或子结构里的字段同样能被找到。
    /// </summary>
    private static void ScanOwner(
        ScriptableObject owner,
        List<AnimationMotionBakeTarget> targets)
    {
        SerializedObject serializedOwner = new SerializedObject(owner);
        SerializedProperty property = serializedOwner.GetIterator();

        // NextVisible(true) 进入子层级，AnimationMotionData 是自定义 Serializable 类，故按 Generic + 类型名筛选。
        while (property.NextVisible(true))
        {
            if (property.propertyType != SerializedPropertyType.Generic ||
                property.type != nameof(AnimationMotionData))
            {
                continue;
            }

            // Clip 允许为空，交由校验阶段统一报错，避免扫描结果与界面上的错误提示对不上。
            SerializedProperty animationProperty = property.FindPropertyRelative("animation");
            SerializedProperty clipProperty =
                animationProperty.FindPropertyRelative(ClipTransition.ClipFieldName);
            AnimationClip clip = clipProperty.objectReferenceValue as AnimationClip;

            targets.Add(new AnimationMotionBakeTarget(
                owner,
                property.propertyPath,
                clip));
        }
    }
}
