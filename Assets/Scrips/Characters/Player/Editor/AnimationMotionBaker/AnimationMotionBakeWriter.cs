using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

internal static class AnimationMotionBakeWriter
{
    public static void Write(IReadOnlyList<AnimationMotionBakeEntry> entries)
    {
        Dictionary<ScriptableObject, List<AnimationMotionBakeEntry>> groupedEntries =
            GroupByOwner(entries);
        ScriptableObject[] owners = new ScriptableObject[groupedEntries.Count];
        groupedEntries.Keys.CopyTo(owners, 0);

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

        AssetDatabase.SaveAssets();
    }

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

    private static void WriteEntry(
        SerializedObject serializedOwner,
        AnimationMotionBakeEntry entry)
    {
        SerializedProperty motionData =
            serializedOwner.FindProperty(entry.Target.PropertyPath);

        if (motionData == null)
        {
            throw new InvalidOperationException(
                $"{entry.Target.DisplayName}：写回时找不到原 SerializedProperty。");
        }

        motionData.FindPropertyRelative("clip").objectReferenceValue =
            entry.Target.Clip;
        motionData.FindPropertyRelative("speedCurve").animationCurveValue =
            entry.Result.SpeedCurve;
        motionData.FindPropertyRelative("rotationCurve").animationCurveValue =
            entry.Result.RotationCurve;
        motionData.FindPropertyRelative("bakedDuration").floatValue =
            entry.Result.BakedDuration;
    }
}
