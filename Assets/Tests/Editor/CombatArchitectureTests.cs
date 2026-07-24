using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

public class CombatArchitectureTests
{
    [Test]
    public void HitContextContainsOnlyAttackerOwnedSignalData()
    {
        HitContext context = new HitContext(
            Vector3.zero,
            Vector3.forward,
            AttackForce.Medium,
            12f);

        Assert.That(context.SourcePosition, Is.EqualTo(Vector3.zero));
        Assert.That(context.SourceForward, Is.EqualTo(Vector3.forward));
        Assert.That(context.Force, Is.EqualTo(AttackForce.Medium));
        Assert.That(context.Damage, Is.EqualTo(12f));
        Assert.That(typeof(HitContext).GetProperty("HitAnimationName"), Is.Null);
        Assert.That(typeof(HitContext).GetProperty("Weapon"), Is.Null);
        Assert.That(typeof(HitContext).GetProperty("Movement"), Is.Null);
        Assert.That(typeof(ComboInteractionConfig).GetField("HitName"), Is.Null);
        Assert.That(typeof(ComboInteractionConfig).GetField("Hit_AirName"), Is.Null);
        Assert.That(typeof(ComboInteractionConfig).GetField("Weapon"), Is.Null);
    }

    [Test]
    public void HitReceiverBaseUsesHitContextContract()
    {
        Assert.That(typeof(IHitReceiver).IsAssignableFrom(typeof(HitReceiverBase)), Is.True);

        MethodInfo receiveHit = typeof(HitReceiverBase).GetMethod(nameof(IHitReceiver.ReceiveHit));
        Assert.That(receiveHit, Is.Not.Null);
        Assert.That(receiveHit.GetParameters(), Has.Length.EqualTo(1));
        Assert.That(receiveHit.GetParameters()[0].ParameterType, Is.EqualTo(typeof(HitContext)));
        Assert.That(typeof(PlayerAttackData).GetProperty("HitFXList"), Is.Null);
        Assert.That(typeof(PlayerAttackData).GetProperty("HitFXPosition"), Is.Null);
        Assert.That(typeof(HitReceiverBase).GetProperty("FXPositionList"), Is.Null);
        Assert.That(typeof(HitReceiverBase).GetProperty("HitFXPosition").PropertyType, Is.EqualTo(typeof(Transform)));
    }

    [Test]
    public void CombatExecutorDoesNotSpecifyTargetReactions()
    {
        string executorSource = File.ReadAllText(ProjectPath(
            "Assets/Scrips/Characters/Player/AttackSystem/CombatExecutor.cs"));
        string receiverSource = File.ReadAllText(ProjectPath(
            "Assets/Scrips/Characters/Combat/HitReceiverBase.cs"));

        Assert.That(executorSource, Does.Not.Contain("TryGetTargetMoveOffsetConfig"));
        Assert.That(executorSource, Does.Not.Contain("HitAnimationName"));
        Assert.That(executorSource, Does.Not.Contain("interactionConfig.Weapon"));
        Assert.That(receiverSource, Does.Not.Contain("animator.Play"));
        Assert.That(receiverSource, Does.Not.Contain("ExecuteMoveOffset"));
    }

    [Test]
    public void AttackStateDelegatesWorldInteractionToCombatExecutor()
    {
        string source = File.ReadAllText(ProjectPath(
            "Assets/Scrips/Characters/Player/Statemachine/Movement/State/Attack/PlayerAttackState.cs"));
        string[] forbiddenDependencies =
        {
            "Physics.",
            "ToolManager",
            "HitReceiverBase",
            "ComboInteractionConfig",
            "MoveOffsetConfig"
        };

        foreach (string dependency in forbiddenDependencies)
        {
            Assert.That(source, Does.Not.Contain(dependency));
        }
    }

    [Test]
    public void SharedCombatTypesStayOutsidePlayerAttackSystem()
    {
        string[] sharedCombatPaths =
        {
            "Assets/Scrips/Characters/Combat/HitContract.cs",
            "Assets/Scrips/Characters/Combat/CombatEffectSpawner.cs",
            "Assets/Scrips/Characters/Combat/HitFXConfig.cs",
            "Assets/Scrips/Characters/Combat/HitReceiverBase.cs"
        };
        string[] removedNestedCombatPaths =
        {
            "Assets/Scrips/Characters/Combat/Contracts/IHitReceiver.cs",
            "Assets/Scrips/Characters/Combat/Contracts/HitContext.cs",
            "Assets/Scrips/Characters/Combat/Contracts/AttackForce.cs",
            "Assets/Scrips/Characters/Combat/Effects/CombatEffectSpawner.cs",
            "Assets/Scrips/Characters/Combat/Effects/HitFXConfig.cs",
            "Assets/Scrips/Characters/Combat/Receivers/CombatReceiverBase.cs",
            "Assets/Scrips/Characters/Combat/CombatReceiverBase.cs",
            "Assets/Scrips/Characters/Player/AttackSystem/IHitReceiver.cs",
            "Assets/Scrips/Characters/Player/AttackSystem/HitContext.cs",
            "Assets/Scrips/Characters/Player/AttackSystem/CombatControllerBase.cs",
            "Assets/Scrips/Characters/Player/AttackSystem/Effect/CombatEffectSpawner.cs",
            "Assets/Scrips/Characters/Player/AttackSystem/Effect/HitFXConfig.cs"
        };

        foreach (string path in sharedCombatPaths)
        {
            Assert.That(File.Exists(ProjectPath(path)), Is.True, path);
        }

        foreach (string path in removedNestedCombatPaths)
        {
            Assert.That(File.Exists(ProjectPath(path)), Is.False, path);
        }

    }

    [Test]
    public void CombatExecutorDoesNotControlOwnerFacing()
    {
        string source = File.ReadAllText(ProjectPath(
            "Assets/Scrips/Characters/Player/AttackSystem/CombatExecutor.cs"));

        Assert.That(source, Does.Not.Contain("currentTarget"));
        Assert.That(source, Does.Not.Contain("FindTarget"));
        Assert.That(source, Does.Not.Contain("LookAtTarget"));
        Assert.That(source, Does.Not.Contain("owner.forward ="));
        Assert.That(source, Does.Contain("owner.forward"));
    }

    [Test]
    public void AttackDetectionFrameUsesItsComboClipTiming()
    {
        AnimationClip attackClip = new AnimationClip { frameRate = 30f };
        attackClip.SetCurve(
            string.Empty,
            typeof(Transform),
            "localPosition.x",
            AnimationCurve.Linear(0f, 0f, 3f, 0f));
        ComboConfig comboConfig = ScriptableObject.CreateInstance<ComboConfig>();
        AttackDetectionConfig detectionConfig = new AttackDetectionConfig { StartFrame = 31 };

        comboConfig.AttackClip = attackClip;

        // 第 31 帧对应 30 个帧间隔，30 FPS、3 秒动画的归一化进度应为三分之一。
        Assert.That(
            comboConfig.GetAttackDetectionNormalizedTime(detectionConfig),
            Is.EqualTo(1f / 3f).Within(0.0001f));
        Assert.That(typeof(AttackDetectionConfig).GetField("StartTime"), Is.Null);

        Object.DestroyImmediate(comboConfig);
        Object.DestroyImmediate(attackClip);
    }

    [Test]
    public void EnemyStatsOwnsRuntimeHealth()
    {
        GameObject enemyObject = new GameObject();
        EnemyStats stats = enemyObject.AddComponent<EnemyStats>();

        stats.Initialize(3f);
        stats.TakeDamage(1f);
        Assert.That(stats.MaxHealth, Is.EqualTo(3f));
        Assert.That(stats.CurrentHealth, Is.EqualTo(2f));
        Assert.That(stats.IsDead, Is.False);

        stats.TakeDamage(2f);
        stats.TakeDamage(1f);
        Assert.That(stats.CurrentHealth, Is.EqualTo(0f));
        Assert.That(stats.IsDead, Is.True);

        Object.DestroyImmediate(enemyObject);
    }

    [Test]
    public void EnemyHitReceiverConvertsHitContextDamageToStats()
    {
        GameObject enemyObject = new GameObject();
        EnemyStats stats = enemyObject.AddComponent<EnemyStats>();
        EnemyHitReceiver hitReceiver = enemyObject.AddComponent<EnemyHitReceiver>();
        HitContext context = new HitContext(
            Vector3.zero,
            Vector3.forward,
            AttackForce.Easy,
            1f);

        stats.Initialize(3f);
        hitReceiver.Initialize(stats);
        hitReceiver.ReceiveHit(context);

        // 受击组件只转发命中伤害，生命值写入始终由 EnemyStats 负责。
        Assert.That(stats.CurrentHealth, Is.EqualTo(2f));
        Assert.That(typeof(IHitReceiver).IsAssignableFrom(typeof(EnemyHitReceiver)), Is.True);
        Assert.That(typeof(HitReceiverBase).IsAssignableFrom(typeof(EnemyHitReceiver)), Is.True);

        stats.TakeDamage(2f);
        hitReceiver.ReceiveHit(context);
        Assert.That(stats.CurrentHealth, Is.EqualTo(0f));

        Object.DestroyImmediate(enemyObject);
    }

    [Test]
    public void CombatExecutorDoesNotDependOnConcreteEnemyComponents()
    {
        string executorSource = File.ReadAllText(ProjectPath(
            "Assets/Scrips/Characters/Player/AttackSystem/CombatExecutor.cs"));

        Assert.That(executorSource, Does.Not.Contain("EnemyStats"));
        Assert.That(executorSource, Does.Not.Contain("EnemyHitReceiver"));
        Assert.That(executorSource, Does.Not.Contain("GetComponent<Enemy"));
    }

    [Test]
    public void PlayerAttackDetectionGizmoMatchesOverlapBoxCoordinates()
    {
        GameObject ownerObject = new GameObject();
        ownerObject.transform.SetPositionAndRotation(
            new Vector3(3f, 1f, -2f),
            Quaternion.Euler(0f, 90f, 0f));
        AttackDetectionConfig detectionConfig = new AttackDetectionConfig
        {
            Position = new Vector3(2f, 1f, 4f),
            Rotation = new Vector3(0f, 15f, 0f),
            Scale = new Vector3(0.65f, 0.9f, 1f)
        };

        MethodInfo getWorldDetectionBox = typeof(PlayerAttackDetectionGizmo).GetMethod(
            "GetWorldDetectionBox",
            BindingFlags.Static | BindingFlags.NonPublic);
        object[] parameters = { ownerObject.transform, detectionConfig, null, null };

        Assert.That(getWorldDetectionBox, Is.Not.Null);
        getWorldDetectionBox.Invoke(null, parameters);

        // 旋转后的浮点坐标存在微小误差，使用容差验证真实的检测框位置。
        Assert.That(
            Vector3.Distance((Vector3)parameters[2], new Vector3(7f, 2f, -4f)),
            Is.LessThan(0.001f));
        Assert.That(
            Quaternion.Angle((Quaternion)parameters[3], Quaternion.Euler(0f, 105f, 0f)),
            Is.LessThan(0.001f));

        Object.DestroyImmediate(ownerObject);
    }

    [Test]
    public void AttackDetectionGizmoStaysIndependentFromCombatExecutor()
    {
        string executorSource = File.ReadAllText(ProjectPath(
            "Assets/Scrips/Characters/Player/AttackSystem/CombatExecutor.cs"));
        string gizmoSource = File.ReadAllText(ProjectPath(
            "Assets/Scrips/Characters/Player/Utilities/Debug/AttackDetection/PlayerAttackDetectionGizmo.cs"));

        Assert.That(executorSource, Does.Not.Contain("AttackDetectionBoxCalculator"));
        Assert.That(gizmoSource, Does.Not.Contain("CombatExecutor"));
    }

    [Test]
    public void AttackDetectionGizmoPreviewsOneConfiguredCombo()
    {
        FieldInfo previewComboIndex = typeof(PlayerAttackDetectionGizmo).GetField(
            "previewComboIndex",
            BindingFlags.Instance | BindingFlags.NonPublic);
        string gizmoSource = File.ReadAllText(ProjectPath(
            "Assets/Scrips/Characters/Player/Utilities/Debug/AttackDetection/PlayerAttackDetectionGizmo.cs"));

        Assert.That(previewComboIndex, Is.Not.Null);
        Assert.That(previewComboIndex.FieldType, Is.EqualTo(typeof(int)));
        Assert.That(gizmoSource, Does.Contain("comboConfigs[previewComboIndex]"));
        Assert.That(gizmoSource, Does.Not.Contain("foreach (ComboConfig comboConfig"));
    }

    [Test]
    public void TransitionAnimationEventsCarrySourceClip()
    {
        MethodInfo stateTransitionMethod = typeof(IState).GetMethod(
            nameof(IState.OnAnimationTransitionEvent));
        MethodInfo triggerTransitionMethod = typeof(PlayerAnimationEventTrigger).GetMethod(
            nameof(PlayerAnimationEventTrigger.TriggerOnMovementStateAnimationTransitionEvent));

        Assert.That(stateTransitionMethod, Is.Not.Null);
        Assert.That(triggerTransitionMethod, Is.Not.Null);
        Assert.That(stateTransitionMethod.GetParameters(), Has.Length.EqualTo(1));
        Assert.That(triggerTransitionMethod.GetParameters(), Has.Length.EqualTo(1));
        Assert.That(stateTransitionMethod.GetParameters()[0].ParameterType, Is.EqualTo(typeof(AnimationEvent)));
        Assert.That(triggerTransitionMethod.GetParameters()[0].ParameterType, Is.EqualTo(typeof(AnimationEvent)));

        string attackStateSource = File.ReadAllText(ProjectPath(
            "Assets/Scrips/Characters/Player/Statemachine/Movement/State/Attack/PlayerAttackState.cs"));

        Assert.That(attackStateSource, Does.Contain("animationEvent.animatorClipInfo.clip"));
        Assert.That(attackStateSource, Does.Contain("sourceClip.name != currentComboName"));
    }

    [Test]
    public void SampleSceneHasOnePlayerInputOwnerAndNoLegacyCombatData()
    {
        string scene = File.ReadAllText(ProjectPath("Assets/Scenes/SampleScene.unity"));
        string inputMeta = File.ReadAllText(ProjectPath(
            "Assets/Scrips/Characters/Player/Utilities/Input/PlayerInput.cs.meta"));
        string gizmoMeta = File.ReadAllText(ProjectPath(
            "Assets/Scrips/Characters/Player/Utilities/Debug/AttackDetection/PlayerAttackDetectionGizmo.cs.meta"));
        Match guidMatch = Regex.Match(inputMeta, @"(?m)^guid:\s*(\w+)\s*$");
        Match gizmoGuidMatch = Regex.Match(gizmoMeta, @"(?m)^guid:\s*(\w+)\s*$");

        Assert.That(guidMatch.Success, Is.True);
        Assert.That(gizmoGuidMatch.Success, Is.True);
        int inputCount = Regex.Matches(scene, $@"guid:\s*{Regex.Escape(guidMatch.Groups[1].Value)}").Count;
        int gizmoCount = Regex.Matches(
            scene,
            $@"guid:\s*{Regex.Escape(gizmoGuidMatch.Groups[1].Value)}").Count;

        Assert.That(inputCount, Is.EqualTo(1));
        Assert.That(gizmoCount, Is.EqualTo(1));
        Assert.That(scene, Does.Match(@"weaponControllerSource:\s*\{fileID:\s*(?!0\b)\d+\}"));
        Assert.That(scene, Does.Not.Contain("<CurrentComboList>k__BackingField"));
        Assert.That(scene, Does.Not.Contain("<TargetLayer>k__BackingField"));
    }

    [Test]
    public void SampleSceneUsesEnemyComposition()
    {
        string scene = File.ReadAllText(ProjectPath("Assets/Scenes/SampleScene.unity"));
        string enemyMeta = File.ReadAllText(ProjectPath(
            "Assets/Scrips/Characters/Enemy/Enemy.cs.meta"));
        string statsMeta = File.ReadAllText(ProjectPath(
            "Assets/Scrips/Characters/Enemy/EnemyStats.cs.meta"));
        string receiverMeta = File.ReadAllText(ProjectPath(
            "Assets/Scrips/Characters/Enemy/EnemyHitReceiver.cs.meta"));
        string baseReceiverMeta = File.ReadAllText(ProjectPath(
            "Assets/Scrips/Characters/Combat/HitReceiverBase.cs.meta"));
        string enemyAssetMeta = File.ReadAllText(ProjectPath(
            "Assets/ScriptableObjects/Characters/Enemy/Enemy.asset.meta"));
        Match enemyGuid = Regex.Match(enemyMeta, @"(?m)^guid:\s*(\w+)\s*$");
        Match statsGuid = Regex.Match(statsMeta, @"(?m)^guid:\s*(\w+)\s*$");
        Match receiverGuid = Regex.Match(receiverMeta, @"(?m)^guid:\s*(\w+)\s*$");
        Match baseReceiverGuid = Regex.Match(baseReceiverMeta, @"(?m)^guid:\s*(\w+)\s*$");
        Match enemyAssetGuid = Regex.Match(enemyAssetMeta, @"(?m)^guid:\s*(\w+)\s*$");

        Assert.That(enemyGuid.Success, Is.True);
        Assert.That(statsGuid.Success, Is.True);
        Assert.That(receiverGuid.Success, Is.True);
        Assert.That(baseReceiverGuid.Success, Is.True);
        Assert.That(enemyAssetGuid.Success, Is.True);
        Assert.That(scene, Does.Contain($"guid: {enemyGuid.Groups[1].Value}"));
        Assert.That(scene, Does.Contain($"guid: {statsGuid.Groups[1].Value}"));
        Assert.That(scene, Does.Contain($"guid: {receiverGuid.Groups[1].Value}"));
        Assert.That(scene, Does.Contain($"guid: {enemyAssetGuid.Groups[1].Value}"));
        Assert.That(scene, Does.Not.Contain($"guid: {baseReceiverGuid.Groups[1].Value}"));
    }

    [Test]
    public void RemovedInfrastructureStaysRemoved()
    {
        string[] removedPaths =
        {
            "Assets/Scrips/Characters/Player/AttackSystem/Effect/ToolManager.cs",
            "Assets/Scrips/Characters/Player/AttackSystem/Effect/EffectBase.cs",
            "Assets/Scrips/Characters/Player/AttackSystem/PlayerCombatController.cs",
            "Assets/Scrips/Core/Events/Bus/EventBus.cs",
            "Assets/Scrips/Core/Events/Events/IEvent.cs",
            "Assets/Scrips/Core/Patterns/Singleton/Singleton.cs",
            "Assets/Scrips/Core/Patterns/Singleton/MonoSingleton.cs",
            "Assets/Scrips/Weapons/Data/WeaponReusableData.cs",
            // 当前没有执行链路的玩家攻击位移数据必须随实现一起移除。
            "Assets/Scrips/Characters/Player/AttackSystem/ExpandClass.cs",
            "Assets/Scrips/Characters/Player/AttackSystem/MoveOffsetDirection.cs"
        };

        foreach (string path in removedPaths)
        {
            Assert.That(File.Exists(ProjectPath(path)), Is.False, path);
        }
    }

    private static string ProjectPath(string relativePath)
    {
        return Path.Combine(Directory.GetParent(Application.dataPath).FullName, relativePath);
    }
}
