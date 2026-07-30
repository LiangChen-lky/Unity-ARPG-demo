using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

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

    private static List<ScriptableObject> CollectOwners(
        IReadOnlyList<ScriptableObject> scanRoots)
    {
        List<ScriptableObject> owners = new List<ScriptableObject>();
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

    private static void ScanOwner(
        ScriptableObject owner,
        List<AnimationMotionBakeTarget> targets)
    {
        SerializedObject serializedOwner = new SerializedObject(owner);
        SerializedProperty property = serializedOwner.GetIterator();

        while (property.NextVisible(true))
        {
            if (property.propertyType != SerializedPropertyType.Generic ||
                property.type != nameof(AnimationMotionData))
            {
                continue;
            }

            SerializedProperty clipProperty = property.FindPropertyRelative("clip");
            AnimationClip clip = clipProperty.objectReferenceValue as AnimationClip;

            targets.Add(new AnimationMotionBakeTarget(
                owner,
                property.propertyPath,
                clip));
        }
    }
}
