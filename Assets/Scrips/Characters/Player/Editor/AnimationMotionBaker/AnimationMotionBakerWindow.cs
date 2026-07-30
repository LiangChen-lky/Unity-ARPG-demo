using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 动画运动离线烘焙器的编辑器入口。主流程为：恢复配置 → 扫描目标 → 校验 → 采样 → 统一写回，
/// 每一环分别委派给 BakerSettings、TargetScanner、BakeValidator、Sampler、BakeWriter，窗口只负责串联与呈现。
/// </summary>
internal sealed class AnimationMotionBakerWindow : EditorWindow
{
    // 这三个窗口字段只是持久化配置的编辑副本，其权威来源是 AnimationMotionBakerSettings；
    // 之所以仍标记 [SerializeField]，是为了承接尚未迁移到 Settings 的旧窗口配置（见 RestoreConfiguration）。
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
        // 主流程第一步：先恢复持久化配置，再据此预扫描一次，使窗口重开或脚本重载后直接呈现上次的目标列表。
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
            // 配置一改动就落盘并清空上一次的扫描结果，避免拿旧目标列表去烘焙新配置。
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

    /// <summary>
    /// 恢复配置：优先读取项目级持久化设置；只有在设置为空而窗口字段仍有内容时，才把旧窗口配置迁移过去。
    /// </summary>
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
            // 首次升级时把当前窗口实例中的旧配置迁移到持久化设置，迁移后窗口字段不再作为配置来源。
            SaveConfiguration();
        }
    }

    /// <summary>
    /// 把窗口中的配置副本同步到项目级设置并立即落盘。
    /// </summary>
    private void SaveConfiguration()
    {
        AnimationMotionBakerSettings settings =
            AnimationMotionBakerSettings.instance;
        settings.Apply(scanRoots, samplePrefab, sampleRateMode);
        settings.SaveSettings();
    }

    /// <summary>
    /// 绘制可手动增删的 ScriptableObject 扫描入口；列表变化时清空旧结果，等待用户重新扫描。
    /// </summary>
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

    /// <summary>
    /// 扫描目标并逐项校验：扫描只负责找出所有内嵌的 AnimationMotionData，Clip 缺失或不合法一律留给校验阶段报错，
    /// 因此这里的 targets 可能包含带错误的条目，需与 scanErrors 一并展示。
    /// </summary>
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

    /// <summary>
    /// 同时展示扫描目标和校验错误，让缺少 Clip 的目标仍保留在列表中并能定位到所属资产与字段。
    /// </summary>
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

    /// <summary>
    /// 烘焙全部：重新扫描 → 校验配置与目标 → 逐个采样并校验结果 → 全部通过后统一写回。
    /// 任一环节报错或用户取消都直接抛出，写回不会执行，磁盘上的资产保持原样。
    /// </summary>
    private void BakeAll()
    {
        try
        {
            // 不复用界面上的旧扫描结果，重扫一次以防资产在两次操作之间被改动。
            RefreshTargets();
            // 先校验配置（扫描根、采样模型），再合入逐目标的校验错误，任何一条都足以中止整批烘焙。
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

            // 整批共用一个采样器实例，只创建一次临时模型；using 保证异常和取消时也会销毁。
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
                // 把结果校验错误顶替到界面错误列表上，便于对照具体是哪些目标的曲线不合法。
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
