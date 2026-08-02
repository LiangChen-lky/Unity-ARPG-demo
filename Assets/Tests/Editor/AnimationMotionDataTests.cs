using System;
using Animancer;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class AnimationMotionDataTests
{
    [Test]
    public void MotionDataIsEmbeddedSerializableData()
    {
        Assert.That(typeof(AnimationMotionData).IsDefined(typeof(SerializableAttribute), false), Is.True);
        Assert.That(typeof(ScriptableObject).IsAssignableFrom(typeof(AnimationMotionData)), Is.False);
    }

    [Test]
    public void ComboConfigCreatesMotionDataAndExposesStableSerializedFields()
    {
        ComboConfig comboConfig = ScriptableObject.CreateInstance<ComboConfig>();
        SerializedObject serializedCombo = new SerializedObject(comboConfig);
        SerializedProperty motionData = serializedCombo.FindProperty("motionData");

        Assert.That(comboConfig.MotionData, Is.Not.Null);
        Assert.That(comboConfig.MotionData.SpeedCurve, Is.Not.Null);
        Assert.That(comboConfig.MotionData.RotationCurve, Is.Not.Null);
        Assert.That(motionData, Is.Not.Null);
        SerializedProperty animation = motionData.FindPropertyRelative("animation");
        Assert.That(animation, Is.Not.Null);
        Assert.That(
            animation.FindPropertyRelative(ClipTransition.ClipFieldName),
            Is.Not.Null);
        // 独立 Clip 字段必须彻底移除，避免重新出现动画双来源。
        Assert.That(motionData.FindPropertyRelative("clip"), Is.Null);
        Assert.That(motionData.FindPropertyRelative("speedCurve"), Is.Not.Null);
        Assert.That(motionData.FindPropertyRelative("rotationCurve"), Is.Not.Null);
        Assert.That(motionData.FindPropertyRelative("bakedDuration"), Is.Not.Null);

        UnityEngine.Object.DestroyImmediate(comboConfig);
    }

    [Test]
    public void StopDataCreatesIndependentMotionDataForEveryStopLevel()
    {
        PlayerStopData stopData = new PlayerStopData();

        Assert.That(stopData.LightMotionData, Is.Not.Null);
        Assert.That(stopData.MediumMotionData, Is.Not.Null);
        Assert.That(stopData.HardMotionData, Is.Not.Null);
        Assert.That(stopData.LightMotionData.Animation, Is.Not.Null);
        Assert.That(stopData.MediumMotionData.Animation, Is.Not.Null);
        Assert.That(stopData.HardMotionData.Animation, Is.Not.Null);
        Assert.That(stopData.LightMotionData, Is.Not.SameAs(stopData.MediumMotionData));
        Assert.That(stopData.MediumMotionData, Is.Not.SameAs(stopData.HardMotionData));
    }

    [Test]
    public void ExistingPlayerAndComboAssetsCreateEmbeddedMotionData()
    {
        PlayerSO playerData = AssetDatabase.LoadAssetAtPath<PlayerSO>(
            "Assets/ScriptableObjects/Characters/Player/Player.asset");
        ComboList comboList = AssetDatabase.LoadAssetAtPath<ComboList>(
            "Assets/ScriptableObjects/Characters/Player/CombatSO/AM_ComboList.asset");

        Assert.That(playerData, Is.Not.Null);
        Assert.That(playerData.GroundedData.StopData.LightMotionData, Is.Not.Null);
        Assert.That(playerData.GroundedData.StopData.MediumMotionData, Is.Not.Null);
        Assert.That(playerData.GroundedData.StopData.HardMotionData, Is.Not.Null);
        Assert.That(comboList, Is.Not.Null);

        // 旧连招资产没有运动字段时，也必须由字段初始化器补出可供烘焙器写入的实例。
        foreach (ComboConfig comboConfig in comboList.ComboConfigs)
        {
            Assert.That(comboConfig.MotionData, Is.Not.Null, comboConfig.name);
        }
    }

    [Test]
    public void ExistingPlayerUsesIndependentDashTransitionsWithTheSameClip()
    {
        PlayerSO playerData = AssetDatabase.LoadAssetAtPath<PlayerSO>(
            "Assets/ScriptableObjects/Characters/Player/Player.asset");
        PlayerDashData dashData = playerData.GroundedData.DashData;

        Assert.That(dashData.ForwardAnimation, Is.Not.Null);
        Assert.That(dashData.BackwardAnimation, Is.Not.Null);
        Assert.That(dashData.LeftAnimation, Is.Not.Null);
        Assert.That(dashData.RightAnimation, Is.Not.Null);

        // 当前美术设计让四个方向共用 Dash Clip，但 Transition 必须保持独立配置。
        Assert.That(dashData.BackwardAnimation, Is.Not.SameAs(dashData.ForwardAnimation));
        Assert.That(dashData.LeftAnimation, Is.Not.SameAs(dashData.ForwardAnimation));
        Assert.That(dashData.RightAnimation, Is.Not.SameAs(dashData.ForwardAnimation));
        Assert.That(dashData.BackwardAnimation.Clip, Is.SameAs(dashData.ForwardAnimation.Clip));
        Assert.That(dashData.LeftAnimation.Clip, Is.SameAs(dashData.ForwardAnimation.Clip));
        Assert.That(dashData.RightAnimation.Clip, Is.SameAs(dashData.ForwardAnimation.Clip));
    }

    [Test]
    public void ExistingPlayerUsesJumpAndFallTransitions()
    {
        PlayerSO playerData = AssetDatabase.LoadAssetAtPath<PlayerSO>(
            "Assets/ScriptableObjects/Characters/Player/Player.asset");
        PlayerAirborneData airborneData = playerData.AirborneData;

        Assert.That(airborneData.JumpData.Animation, Is.Not.Null);
        Assert.That(airborneData.FallData.Animation, Is.Not.Null);
        // 空中状态的动画来源必须归属于各自数据，避免继续依赖 Animator State 名称。
        Assert.That(
            AssetDatabase.GetAssetPath(airborneData.JumpData.Animation.Clip),
            Is.EqualTo("Assets/Animations/Characters/Player/Clip/Movement/Airborne/Jump.anim"));
        Assert.That(
            AssetDatabase.GetAssetPath(airborneData.FallData.Animation.Clip),
            Is.EqualTo("Assets/Animations/Characters/Player/Clip/Movement/Airborne/Fall.anim"));
    }

    [Test]
    public void ExistingPlayerUsesLandingAndRollTransitions()
    {
        PlayerSO playerData = AssetDatabase.LoadAssetAtPath<PlayerSO>(
            "Assets/ScriptableObjects/Characters/Player/Player.asset");
        PlayerGroundedData groundedData = playerData.GroundedData;

        Assert.That(groundedData.LandingData, Is.Not.Null);
        Assert.That(groundedData.LandingData.LightAnimation, Is.Not.Null);
        Assert.That(groundedData.LandingData.HardAnimation, Is.Not.Null);
        Assert.That(groundedData.RollData.Animation, Is.Not.Null);
        // 落地与翻滚的动画来源必须归属于各自数据，避免继续依赖 Animator State 名称。
        // 事件时间由手动调整，这里只校验 Clip 指向，不做时间断言。
        Assert.That(
            AssetDatabase.GetAssetPath(groundedData.LandingData.LightAnimation.Clip),
            Is.EqualTo("Assets/Animations/Characters/Player/Clip/Movement/Grounded/Landing/LightLand.anim"));
        Assert.That(
            AssetDatabase.GetAssetPath(groundedData.LandingData.HardAnimation.Clip),
            Is.EqualTo("Assets/Animations/Characters/Player/Clip/Movement/Grounded/Landing/HardLand.anim"));
        Assert.That(
            AssetDatabase.GetAssetPath(groundedData.RollData.Animation.Clip),
            Is.EqualTo("Assets/Animations/Characters/Player/Clip/Movement/Grounded/Landing/Roll.anim"));
    }

    [Test]
    public void SerializedMotionDataCanBeReadThroughPublicAccessors()
    {
        ComboConfig comboConfig = ScriptableObject.CreateInstance<ComboConfig>();
        AnimationClip clip = BuildClip();
        SerializedObject serializedCombo = new SerializedObject(comboConfig);
        SerializedProperty motionData = serializedCombo.FindProperty("motionData");

        // 模拟后续烘焙器通过 SerializedProperty 写入离线结果。
        motionData
            .FindPropertyRelative("animation")
            .FindPropertyRelative(ClipTransition.ClipFieldName)
            .objectReferenceValue = clip;
        motionData.FindPropertyRelative("speedCurve").animationCurveValue =
            AnimationCurve.Linear(0f, 0f, 1f, 2f);
        motionData.FindPropertyRelative("rotationCurve").animationCurveValue =
            AnimationCurve.Linear(0f, 0f, 1f, 90f);
        motionData.FindPropertyRelative("bakedDuration").floatValue = 1f;
        serializedCombo.ApplyModifiedPropertiesWithoutUndo();

        Assert.That(comboConfig.MotionData.Clip, Is.SameAs(clip));
        Assert.That(comboConfig.MotionData.Animation.Clip, Is.SameAs(clip));
        Assert.That(comboConfig.MotionData.SpeedCurve.Evaluate(1f), Is.EqualTo(2f).Within(0.001f));
        Assert.That(comboConfig.MotionData.RotationCurve.Evaluate(1f), Is.EqualTo(90f).Within(0.001f));
        Assert.That(comboConfig.MotionData.BakedDuration, Is.EqualTo(1f));

        UnityEngine.Object.DestroyImmediate(clip);
        UnityEngine.Object.DestroyImmediate(comboConfig);
    }

    private static AnimationClip BuildClip()
    {
        AnimationClip clip = new AnimationClip { frameRate = 30f };
        clip.SetCurve(
            string.Empty,
            typeof(Transform),
            "localPosition.x",
            AnimationCurve.Linear(0f, 0f, 1f, 0f));
        return clip;
    }
}
