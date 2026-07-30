using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

internal sealed class AnimationMotionBakerWindow : EditorWindow
{
    // 保留窗口字段用于承接脚本重载前的配置，长期数据由 BakerSettings 持久化。
    [SerializeField] private List<ScriptableObject> scanRoots =
        new List<ScriptableObject>();
    [SerializeField] private GameObject samplePrefab;
    [SerializeField] private AnimationMotionSampleRateMode sampleRateMode =
        AnimationMotionSampleRateMode.FromClip;

    private readonly List<AnimationMotionBakeTarget> targets =
        new List<AnimationMotionBakeTarget>();
    private readonly List<string> scanErrors = new List<string>();
    private Vector2 targetScroll;

    [MenuItem("Tools/Animation/Animation Motion Baker")]
    public static void ShowWindow()
    {
        GetWindow<AnimationMotionBakerWindow>("Motion Baker");
    }

    private void OnEnable()
    {
        RestoreConfiguration();

        if (scanRoots.Count > 0)
        {
            RefreshTargets();
        }
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("动画运动离线烘焙器", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUI.BeginChangeCheck();
        DrawRootList();

        samplePrefab = (GameObject)EditorGUILayout.ObjectField(
            "采样模型",
            samplePrefab,
            typeof(GameObject),
            false);
        sampleRateMode = (AnimationMotionSampleRateMode)EditorGUILayout.EnumPopup(
            "采样率",
            sampleRateMode);

        if (EditorGUI.EndChangeCheck())
        {
            SaveConfiguration();
            targets.Clear();
            scanErrors.Clear();
        }

        EditorGUILayout.HelpBox(
            "采样模型必须包含有效的 Humanoid Animator。烘焙只在临时实例上开启 Root Motion，" +
            "不会修改玩家场景对象或运行时配置。",
            MessageType.Info);

        EditorGUILayout.Space();
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("扫描目标", GUILayout.Height(28f)))
            {
                RefreshTargets();
            }

            using (new EditorGUI.DisabledScope(targets.Count == 0))
            {
                if (GUILayout.Button("烘焙全部", GUILayout.Height(28f)))
                {
                    BakeAll();
                }
            }
        }

        DrawScanResult();
    }

    private void RestoreConfiguration()
    {
        AnimationMotionBakerSettings settings =
            AnimationMotionBakerSettings.instance;

        if (settings.HasConfiguration)
        {
            scanRoots = new List<ScriptableObject>(settings.ScanRoots);
            samplePrefab = settings.SamplePrefab;
            sampleRateMode = settings.SampleRateMode;
            return;
        }

        if (scanRoots.Count > 0 || samplePrefab != null)
        {
            // 首次升级时把当前窗口实例中的旧配置迁移到持久化设置。
            SaveConfiguration();
        }
    }

    private void SaveConfiguration()
    {
        AnimationMotionBakerSettings settings =
            AnimationMotionBakerSettings.instance;
        settings.Apply(scanRoots, samplePrefab, sampleRateMode);
        settings.SaveSettings();
    }

    private void DrawRootList()
    {
        EditorGUILayout.LabelField("ScriptableObject 扫描根", EditorStyles.boldLabel);

        for (int index = 0; index < scanRoots.Count; index++)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                scanRoots[index] = (ScriptableObject)EditorGUILayout.ObjectField(
                    $"根 {index + 1}",
                    scanRoots[index],
                    typeof(ScriptableObject),
                    false);

                if (GUILayout.Button("删除", GUILayout.Width(48f)))
                {
                    scanRoots.RemoveAt(index);
                    targets.Clear();
                    scanErrors.Clear();
                    break;
                }
            }
        }

        if (GUILayout.Button("添加扫描根"))
        {
            scanRoots.Add(null);
            targets.Clear();
            scanErrors.Clear();
        }
    }

    private void RefreshTargets()
    {
        targets.Clear();
        scanErrors.Clear();

        List<AnimationMotionBakeTarget> scannedTargets =
            AnimationMotionTargetScanner.Scan(scanRoots);
        targets.AddRange(scannedTargets);

        foreach (AnimationMotionBakeTarget target in targets)
        {
            scanErrors.AddRange(AnimationMotionBakeValidator.ValidateTarget(target));
        }

        if (targets.Count == 0)
        {
            scanErrors.Add("当前扫描根中没有找到 AnimationMotionData。");
        }
    }

    private void DrawScanResult()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField(
            $"扫描结果：{targets.Count} 个目标，{scanErrors.Count} 个错误",
            EditorStyles.boldLabel);

        foreach (string error in scanErrors)
        {
            EditorGUILayout.HelpBox(error, MessageType.Error);
        }

        targetScroll = EditorGUILayout.BeginScrollView(targetScroll);
        foreach (AnimationMotionBakeTarget target in targets)
        {
            string clipName = target.Clip == null ? "未配置 Clip" : target.Clip.name;
            EditorGUILayout.LabelField(target.DisplayName, clipName);
        }
        EditorGUILayout.EndScrollView();
    }

    private void BakeAll()
    {
        try
        {
            RefreshTargets();
            List<string> errors = AnimationMotionBakeValidator.ValidateConfiguration(
                scanRoots,
                samplePrefab);
            errors.AddRange(scanErrors);

            if (errors.Count > 0)
            {
                throw new InvalidOperationException(string.Join("\n", errors));
            }

            List<AnimationMotionBakeEntry> entries =
                new List<AnimationMotionBakeEntry>(targets.Count);
            List<string> bakeErrors = new List<string>();

            using (AnimationMotionSampler sampler =
                   new AnimationMotionSampler(samplePrefab))
            {
                for (int index = 0; index < targets.Count; index++)
                {
                    AnimationMotionBakeTarget target = targets[index];
                    bool cancelled = EditorUtility.DisplayCancelableProgressBar(
                        "动画运动离线烘焙",
                        $"正在采样 {target.Clip.name}",
                        (float)index / targets.Count);

                    if (cancelled)
                    {
                        throw new OperationCanceledException();
                    }

                    AnimationMotionBakeResult result =
                        sampler.Sample(target.Clip, sampleRateMode);
                    List<string> resultErrors =
                        AnimationMotionBakeValidator.ValidateResult(
                            target,
                            result);

                    if (resultErrors.Count == 0)
                    {
                        entries.Add(new AnimationMotionBakeEntry(target, result));
                    }
                    else
                    {
                        bakeErrors.AddRange(resultErrors);
                    }
                }
            }

            if (bakeErrors.Count > 0)
            {
                scanErrors.Clear();
                scanErrors.AddRange(bakeErrors);
                throw new InvalidOperationException(string.Join("\n", bakeErrors));
            }

            // 全部采样与校验完成后再统一写回，失败或取消不会留下半批资产。
            AnimationMotionBakeWriter.Write(entries);
            ShowNotification(new GUIContent($"烘焙完成：{entries.Count} 个目标"));
            RefreshTargets();
        }
        catch (OperationCanceledException)
        {
            ShowNotification(new GUIContent("已取消烘焙"));
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorUtility.DisplayDialog("动画运动烘焙失败", exception.Message, "确定");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }
}
