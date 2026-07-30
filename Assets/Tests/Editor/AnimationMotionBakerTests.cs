using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 动画运动离线烘焙器的 EditMode 测试，按流水线分六组：配置持久化、目标扫描、曲线换算、校验、写回，
/// 以及跑在项目真实资产上的端到端采样。
/// </summary>
public class AnimationMotionBakerTests
{
    // ---- 配置：验证窗口配置被复制进持久化设置，而非共享引用 ----

    [Test]
    public void BakerSettingsCopiesWindowConfiguration()
    {
        AnimationMotionBakerSettings settings =
            ScriptableObject.CreateInstance<AnimationMotionBakerSettings>();
        ComboConfig comboConfig = ScriptableObject.CreateInstance<ComboConfig>();
        GameObject samplePrefab = new GameObject("Sample");

        settings.Apply(
            new List<ScriptableObject> { comboConfig },
            samplePrefab,
            AnimationMotionSampleRateMode.Fps60);

        Assert.That(settings.ScanRoots, Has.Count.EqualTo(1));
        Assert.That(settings.ScanRoots[0], Is.SameAs(comboConfig));
        Assert.That(settings.SamplePrefab, Is.SameAs(samplePrefab));
        Assert.That(
            settings.SampleRateMode,
            Is.EqualTo(AnimationMotionSampleRateMode.Fps60));

        Object.DestroyImmediate(samplePrefab);
        Object.DestroyImmediate(comboConfig);
        Object.DestroyImmediate(settings);
    }

    // ---- 扫描：验证内嵌 MotionData 的定位、容器展开与去重，最后一项跑在真实 Player.asset 上 ----

    [Test]
    public void ScannerFindsMotionDataEmbeddedInComboConfig()
    {
        ComboConfig comboConfig = ScriptableObject.CreateInstance<ComboConfig>();
        AnimationClip clip = BuildClip("Combo");
        SetComboClips(comboConfig, clip, clip);

        List<AnimationMotionBakeTarget> targets =
            AnimationMotionTargetScanner.Scan(
                new List<ScriptableObject> { comboConfig });

        Assert.That(targets, Has.Count.EqualTo(1));
        Assert.That(targets[0].Owner, Is.SameAs(comboConfig));
        Assert.That(targets[0].PropertyPath, Is.EqualTo("motionData"));
        Assert.That(targets[0].Clip, Is.SameAs(clip));

        Object.DestroyImmediate(clip);
        Object.DestroyImmediate(comboConfig);
    }

    // ComboList 与其成员之一同时作为扫描根传入，结果应展开容器且该成员不被重复烘焙。
    [Test]
    public void ScannerExpandsComboListAndRemovesDuplicateOwners()
    {
        ComboConfig firstCombo = ScriptableObject.CreateInstance<ComboConfig>();
        ComboConfig secondCombo = ScriptableObject.CreateInstance<ComboConfig>();
        ComboList comboList = ScriptableObject.CreateInstance<ComboList>();
        AnimationClip firstClip = BuildClip("First");
        AnimationClip secondClip = BuildClip("Second");

        SetComboClips(firstCombo, firstClip, firstClip);
        SetComboClips(secondCombo, secondClip, secondClip);
        SetComboList(comboList, firstCombo, secondCombo);

        List<AnimationMotionBakeTarget> targets =
            AnimationMotionTargetScanner.Scan(
                new List<ScriptableObject> { comboList, firstCombo });

        Assert.That(targets, Has.Count.EqualTo(2));
        Assert.That(targets.Select(target => target.Owner), Is.Unique);

        Object.DestroyImmediate(firstClip);
        Object.DestroyImmediate(secondClip);
        Object.DestroyImmediate(firstCombo);
        Object.DestroyImmediate(secondCombo);
        Object.DestroyImmediate(comboList);
    }

    [Test]
    public void ScannerFindsThreeStopTargetsInsidePlayerAsset()
    {
        PlayerSO playerData = AssetDatabase.LoadAssetAtPath<PlayerSO>(
            "Assets/ScriptableObjects/Characters/Player/Player.asset");

        List<AnimationMotionBakeTarget> targets =
            AnimationMotionTargetScanner.Scan(
                new List<ScriptableObject> { playerData });

        Assert.That(
            targets.Count(target => target.PropertyPath.EndsWith("MotionData")),
            Is.EqualTo(3));
    }

    // ---- 曲线换算：验证本地 Z 有符号速度、累计偏航以及忽略横向位移的既定数据契约 ----

    [Test]
    public void CurveBuilderProducesSignedForwardSpeedAndAccumulatedYaw()
    {
        List<AnimationMotionRootSample> samples =
            new List<AnimationMotionRootSample>
            {
                new AnimationMotionRootSample(
                    0f,
                    Vector3.zero,
                    Quaternion.identity),
                new AnimationMotionRootSample(
                    1f,
                    Vector3.forward * 2f,
                    Quaternion.Euler(0f, 90f, 0f))
            };

        AnimationMotionBakeResult result =
            AnimationMotionSampler.BuildResult(samples);

        Assert.That(result.SpeedCurve.Evaluate(0.5f), Is.EqualTo(2f).Within(0.001f));
        Assert.That(result.RotationCurve.Evaluate(1f), Is.EqualTo(90f).Within(0.001f));
        Assert.That(result.BakedDuration, Is.EqualTo(1f));
        Assert.That(
            AnimationUtility.GetKeyRightTangentMode(result.SpeedCurve, 0),
            Is.EqualTo(AnimationUtility.TangentMode.Linear));
    }

    [Test]
    public void CurveBuilderUsesLocalForwardSpeedAndIgnoresLateralDisplacement()
    {
        List<AnimationMotionRootSample> samples =
            new List<AnimationMotionRootSample>
            {
                new AnimationMotionRootSample(
                    0f,
                    Vector3.zero,
                    Quaternion.identity),
                new AnimationMotionRootSample(
                    1f,
                    new Vector3(0.25f, 0f, -1f),
                    Quaternion.identity)
            };

        AnimationMotionBakeResult result =
            AnimationMotionSampler.BuildResult(samples);

        Assert.That(result.SpeedCurve.Evaluate(0.5f), Is.EqualTo(-1f).Within(0.001f));
    }

    // ---- 校验：Combo 仍由 AttackClip 播放时，MotionData 不能引用另一段动画 ----

    [Test]
    public void ValidatorRejectsDifferentComboClipSources()
    {
        ComboConfig comboConfig = ScriptableObject.CreateInstance<ComboConfig>();
        AnimationClip attackClip = BuildClip("Attack");
        AnimationClip motionClip = BuildClip("Motion");
        SetComboClips(comboConfig, attackClip, motionClip);

        AnimationMotionBakeTarget target =
            AnimationMotionTargetScanner.Scan(
                new List<ScriptableObject> { comboConfig })[0];
        List<string> errors =
            AnimationMotionBakeValidator.ValidateTarget(target);

        Assert.That(errors, Has.Count.EqualTo(1));
        Assert.That(errors[0], Does.Contain("MotionData.Clip"));

        Object.DestroyImmediate(attackClip);
        Object.DestroyImmediate(motionClip);
        Object.DestroyImmediate(comboConfig);
    }

    // ---- 写回：验证扫描所得 PropertyPath 能把曲线写回内嵌数据，同时保持运行时访问器只读 ----

    [Test]
    public void WriterUpdatesEmbeddedMotionDataThroughPropertyPath()
    {
        ComboConfig comboConfig = ScriptableObject.CreateInstance<ComboConfig>();
        AnimationClip clip = BuildClip("Write");
        SetComboClips(comboConfig, clip, clip);

        AnimationMotionBakeTarget target =
            AnimationMotionTargetScanner.Scan(
                new List<ScriptableObject> { comboConfig })[0];
        AnimationMotionBakeResult result = new AnimationMotionBakeResult(
            AnimationCurve.Linear(0f, 1f, 1f, 2f),
            AnimationCurve.Linear(0f, 0f, 1f, 45f),
            1f);

        AnimationMotionBakeWriter.Write(
            new List<AnimationMotionBakeEntry>
            {
                new AnimationMotionBakeEntry(target, result)
            });

        Assert.That(comboConfig.MotionData.Clip, Is.SameAs(clip));
        Assert.That(comboConfig.MotionData.SpeedCurve.Evaluate(1f), Is.EqualTo(2f));
        Assert.That(comboConfig.MotionData.RotationCurve.Evaluate(1f), Is.EqualTo(45f));
        Assert.That(comboConfig.MotionData.BakedDuration, Is.EqualTo(1f));

        Object.DestroyImmediate(clip);
        Object.DestroyImmediate(comboConfig);
    }

    // ---- 真实资产：使用当前 Y Bot、Combo 与 Stop 动画验证 Humanoid Root Motion 可以被完整采样 ----

    [Test]
    public void CurrentHumanoidSampleProducesRootMotionCurves()
    {
        GameObject samplePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Model/Characters/Player/Y Bot.fbx");
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(
            "Assets/Animations/Characters/Player/Clip/Attack/AM_Attack01.anim");

        Assert.That(samplePrefab, Is.Not.Null);
        Assert.That(clip, Is.Not.Null);

        using (AnimationMotionSampler sampler =
               new AnimationMotionSampler(samplePrefab))
        {
            AnimationMotionBakeResult result = sampler.Sample(
                clip,
                AnimationMotionSampleRateMode.FromClip);

            bool hasForwardMotion = result.SpeedCurve.keys.Any(
                key => Mathf.Abs(key.value) > 0.001f);
            bool hasRotation = result.RotationCurve.keys.Any(
                key => Mathf.Abs(key.value) > 0.001f);

            Assert.That(
                hasForwardMotion || hasRotation,
                Is.True,
                "当前 Humanoid 采样模型没有提取到 RootT/RootQ 运动。");
            Assert.That(result.BakedDuration, Is.EqualTo(clip.length).Within(0.0001f));
        }
    }

    [Test]
    public void CurrentComboAndStopClipsCanBeSampledAsScalarMotionData()
    {
        string[] clipPaths =
        {
            "Assets/Animations/Characters/Player/Clip/Attack/AM_Attack01.anim",
            "Assets/Animations/Characters/Player/Clip/Attack/AM_Attack02.anim",
            "Assets/Animations/Characters/Player/Clip/Attack/AM_Attack03.anim",
            "Assets/Animations/Characters/Player/Clip/Attack/AM_Attack04.anim",
            "Assets/Animations/Characters/Player/Clip/Attack/AM_Attack05.anim",
            "Assets/Animations/Characters/Player/Clip/Movement/Grounded/Stopping/LightStop.anim",
            "Assets/Animations/Characters/Player/Clip/Movement/Grounded/Stopping/MediumStop.anim",
            "Assets/Animations/Characters/Player/Clip/Movement/Grounded/Stopping/HardStop.anim"
        };
        GameObject samplePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Model/Characters/Player/Y Bot.fbx");

        using (AnimationMotionSampler sampler =
               new AnimationMotionSampler(samplePrefab))
        {
            foreach (string clipPath in clipPaths)
            {
                AnimationClip clip =
                    AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
                AnimationMotionBakeResult result = sampler.Sample(
                    clip,
                    AnimationMotionSampleRateMode.FromClip);

                Assert.That(result.SpeedCurve.length, Is.GreaterThan(1));
                Assert.That(result.RotationCurve.length, Is.GreaterThan(1));
                Assert.That(
                    result.BakedDuration,
                    Is.EqualTo(clip.length).Within(0.0001f));
            }
        }
    }

    // 构造只包含前向位移的临时动画，供扫描、校验和写回测试隔离使用。
    private static AnimationClip BuildClip(string clipName)
    {
        AnimationClip clip = new AnimationClip
        {
            name = clipName,
            frameRate = 30f
        };
        clip.SetCurve(
            string.Empty,
            typeof(Transform),
            "localPosition.z",
            AnimationCurve.Linear(0f, 0f, 1f, 1f));
        return clip;
    }

    // 通过 SerializedProperty 配置内嵌 MotionData.Clip，模拟烘焙器面对的真实序列化结构。
    private static void SetComboClips(
        ComboConfig comboConfig,
        AnimationClip attackClip,
        AnimationClip motionClip)
    {
        comboConfig.AttackClip = attackClip;

        SerializedObject serializedCombo = new SerializedObject(comboConfig);
        SerializedProperty motionData = serializedCombo.FindProperty("motionData");
        motionData.FindPropertyRelative("clip").objectReferenceValue = motionClip;
        serializedCombo.ApplyModifiedPropertiesWithoutUndo();
    }

    // 填充 ComboList 的自动属性后备字段，用于验证容器展开和 Owner 去重。
    private static void SetComboList(
        ComboList comboList,
        params ComboConfig[] comboConfigs)
    {
        SerializedObject serializedList = new SerializedObject(comboList);
        SerializedProperty configs =
            serializedList.FindProperty("<ComboConfigs>k__BackingField");
        configs.arraySize = comboConfigs.Length;

        for (int index = 0; index < comboConfigs.Length; index++)
        {
            configs.GetArrayElementAtIndex(index).objectReferenceValue =
                comboConfigs[index];
        }

        serializedList.ApplyModifiedPropertiesWithoutUndo();
    }
}
