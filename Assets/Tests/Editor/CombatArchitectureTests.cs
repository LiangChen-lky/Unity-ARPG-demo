using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using Animancer;
using NUnit.Framework;
using UnityEditor;
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

        SetComboMotionClip(comboConfig, attackClip);

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
    public void TransitionAndExitAnimationEventsDoNotCarryUnusedEventObjects()
    {
        MethodInfo stateTransitionMethod = typeof(IState).GetMethod(
            nameof(IState.OnAnimationTransitionEvent));
        MethodInfo triggerTransitionMethod = typeof(PlayerAnimationEventTrigger).GetMethod(
            nameof(PlayerAnimationEventTrigger.TriggerOnMovementStateAnimationTransitionEvent));
        MethodInfo stateExitMethod = typeof(IState).GetMethod(nameof(IState.OnAnimationExitEvent));
        MethodInfo triggerExitMethod = typeof(PlayerAnimationEventTrigger).GetMethod(
            nameof(PlayerAnimationEventTrigger.TriggerOnMovementStateAnimationExitEvent));

        Assert.That(stateTransitionMethod, Is.Not.Null);
        Assert.That(triggerTransitionMethod, Is.Not.Null);
        Assert.That(stateExitMethod, Is.Not.Null);
        Assert.That(triggerExitMethod, Is.Not.Null);
        Assert.That(stateTransitionMethod.GetParameters(), Is.Empty);
        Assert.That(triggerTransitionMethod.GetParameters(), Is.Empty);
        Assert.That(stateExitMethod.GetParameters(), Is.Empty);
        Assert.That(triggerExitMethod.GetParameters(), Is.Empty);
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

    [Test]
    public void ComboValidationLivesInDataLayerNotInExecutorOrAttackState()
    {
        // 攻击状态不得直接触碰检测/交互配置类型，字段校验必须在数据层完成。
        string attackStateSource = File.ReadAllText(ProjectPath(
            "Assets/Scrips/Characters/Player/Statemachine/Movement/State/Attack/PlayerAttackState.cs"));
        Assert.That(attackStateSource, Does.Contain("comboList.ValidateConfiguration()"));
        Assert.That(attackStateSource, Does.Not.Contain("AttackDetectionConfig"));
        Assert.That(attackStateSource, Does.Not.Contain("ComboInteractionConfig"));

        // ComboList 与 ComboConfig 必须各自暴露校验入口，把规则留在数据层。
        string comboListSource = File.ReadAllText(ProjectPath(
            "Assets/Scrips/Characters/Player/Data/ScriptableObject/Combo/ComboList.cs"));
        string comboConfigSource = File.ReadAllText(ProjectPath(
            "Assets/Scrips/Characters/Player/Data/ScriptableObject/Combo/ComboConfig.cs"));
        Assert.That(comboListSource, Does.Contain("public void ValidateConfiguration()"));
        Assert.That(comboConfigSource, Does.Contain("public void ValidateConfiguration(int comboIndex, string comboListName)"));
    }

    [Test]
    public void CombatExecutorDoesNotSilentlySkipMissingInteractionConfig()
    {
        // 命中检测取得后直接交给执行器消费，缺失交互数据必须由前置校验拦截。
        string executorSource = File.ReadAllText(ProjectPath(
            "Assets/Scrips/Characters/Player/AttackSystem/CombatExecutor.cs"));

        Assert.That(executorSource, Does.Not.Contain("if (interactionConfig != null)"));
        Assert.That(executorSource, Does.Not.Contain("if (interactionConfig == null)"));
    }

    [Test]
    public void CombatExecutorReliesOnAttackStateConfigurationValidation()
    {
        string executorSource = File.ReadAllText(ProjectPath(
            "Assets/Scrips/Characters/Player/AttackSystem/CombatExecutor.cs"));
        string attackStateSource = File.ReadAllText(ProjectPath(
            "Assets/Scrips/Characters/Player/Statemachine/Movement/State/Attack/PlayerAttackState.cs"));

        // 配置只能在进入攻击状态前失败，执行阶段不再静默跳过不完整数据。
        Assert.That(executorSource, Does.Not.Contain("HasComboData"));
        Assert.That(attackStateSource, Does.Contain("ComboList comboList = attackData.CurrentComboList"));
        Assert.That(attackStateSource, Does.Contain("comboList.ValidateConfiguration"));
    }

    [Test]
    public void CombatExecutorConsumesAllDueHitEventsInOneUpdate()
    {
        string executorSource = File.ReadAllText(ProjectPath(
            "Assets/Scrips/Characters/Player/AttackSystem/CombatExecutor.cs"));

        // 同一帧号或掉帧跨过的多个事件，都必须在当前 Update 中连续消费。
        Assert.That(
            Regex.IsMatch(executorSource, @"while \(true\)[\s\S]*attackDetectionEventIndex\+\+;"),
            Is.True);
    }

    [Test]
    public void ValidComboConfigurationPassesValidation()
    {
        ComboList comboList = BuildValidComboList("ValidComboList");
        Assert.DoesNotThrow(() => comboList.ValidateConfiguration());

        DestroyComboListAssets(comboList);
        Object.DestroyImmediate(comboList);
    }

    [Test]
    public void EmptyComboListFailsValidation()
    {
        ComboList comboList = ScriptableObject.CreateInstance<ComboList>();
        comboList.name = "EmptyComboList";
        SetComboConfigs(comboList, new ComboConfig[0]);

        var exception = Assert.Throws<System.InvalidOperationException>(() => comboList.ValidateConfiguration());
        Assert.That(exception.Message, Does.Contain("EmptyComboList"));
        Assert.That(exception.Message, Does.Contain("至少需要一段招式"));

        Object.DestroyImmediate(comboList);
    }

    [Test]
    public void NullComboConfigElementFailsValidation()
    {
        ComboList comboList = BuildValidComboList("NullElementComboList");
        comboList.ComboConfigs[1] = null;

        var exception = Assert.Throws<System.InvalidOperationException>(() => comboList.ValidateConfiguration());
        Assert.That(exception.Message, Does.Contain("NullElementComboList"));
        Assert.That(exception.Message, Does.Contain("ComboConfig 引用不能为 null"));

        DestroyComboListAssets(comboList);
        Object.DestroyImmediate(comboList);
    }

    [Test]
    public void MissingMotionClipFailsValidation()
    {
        ComboList comboList = BuildValidComboList("MissingClipComboList");
        AnimationClip clip = comboList.ComboConfigs[0].MotionData.Clip;
        SetComboMotionClip(comboList.ComboConfigs[0], null);

        var exception = Assert.Throws<System.InvalidOperationException>(() => comboList.ValidateConfiguration());
        Assert.That(exception.Message, Does.Contain("MissingClipComboList"));
        Assert.That(exception.Message, Does.Contain("MotionData.Clip 不能为 null"));

        Object.DestroyImmediate(clip);
        DestroyComboListAssets(comboList);
        Object.DestroyImmediate(comboList);
    }

    [Test]
    public void DetectionInteractionLengthMismatchFailsValidation()
    {
        ComboList comboList = BuildValidComboList("MismatchComboList");
        // 只追加一条检测事件，使其与交互事件数量不一致。
        comboList.ComboConfigs[0].AttackDetectionConfig =
            AppendElement(comboList.ComboConfigs[0].AttackDetectionConfig, BuildValidDetection(5));

        var exception = Assert.Throws<System.InvalidOperationException>(() => comboList.ValidateConfiguration());
        Assert.That(exception.Message, Does.Contain("长度"));
        Assert.That(exception.Message, Does.Contain("必须一致"));

        DestroyComboListAssets(comboList);
        Object.DestroyImmediate(comboList);
    }

    [Test]
    public void OnlyDetectionArrayPresentFailsValidation()
    {
        ComboList comboList = BuildValidComboList("HalfComboList");
        // 交互数组为 null、检测数组非空，属于"有检测盒却漏填伤害"的隐蔽错误。
        comboList.ComboConfigs[0].InteractionConfig = null;

        var exception = Assert.Throws<System.InvalidOperationException>(() => comboList.ValidateConfiguration());
        Assert.That(exception.Message, Does.Contain("同时存在或同时为空"));

        DestroyComboListAssets(comboList);
        Object.DestroyImmediate(comboList);
    }

    [Test]
    public void NullDetectionArrayElementFailsValidation()
    {
        ComboList comboList = BuildValidComboList("NullDetectionComboList");
        comboList.ComboConfigs[0].AttackDetectionConfig[0] = null;

        var exception = Assert.Throws<System.InvalidOperationException>(() => comboList.ValidateConfiguration());
        Assert.That(exception.Message, Does.Contain("AttackDetectionConfig[0] 引用不能为 null"));

        DestroyComboListAssets(comboList);
        Object.DestroyImmediate(comboList);
    }

    [Test]
    public void NullInteractionArrayElementFailsValidation()
    {
        ComboList comboList = BuildValidComboList("NullInteractionComboList");
        comboList.ComboConfigs[0].InteractionConfig[0] = null;

        var exception = Assert.Throws<System.InvalidOperationException>(() => comboList.ValidateConfiguration());
        Assert.That(exception.Message, Does.Contain("InteractionConfig[0] 引用不能为 null"));

        DestroyComboListAssets(comboList);
        Object.DestroyImmediate(comboList);
    }

    [Test]
    public void StartFrameBelowOneFailsValidation()
    {
        ComboList comboList = BuildValidComboList("LowFrameComboList");
        comboList.ComboConfigs[0].AttackDetectionConfig[0].StartFrame = 0;

        var exception = Assert.Throws<System.InvalidOperationException>(() => comboList.ValidateConfiguration());
        Assert.That(exception.Message, Does.Contain("StartFrame"));
        Assert.That(exception.Message, Does.Contain("不小于 1"));

        DestroyComboListAssets(comboList);
        Object.DestroyImmediate(comboList);
    }

    [Test]
    public void NullFXConfigFailsValidation()
    {
        ComboList comboList = BuildValidComboList("NullFXConfigComboList");
        comboList.ComboConfigs[0].FXConfig = null;

        var exception = Assert.Throws<System.InvalidOperationException>(() => comboList.ValidateConfiguration());
        Assert.That(exception.Message, Does.Contain("FXConfig 不能为 null"));

        DestroyComboListAssets(comboList);
        Object.DestroyImmediate(comboList);
    }

    [Test]
    public void NullSFXConfigFailsValidation()
    {
        ComboList comboList = BuildValidComboList("NullSFXConfigComboList");
        comboList.ComboConfigs[0].SFXConfig = null;

        var exception = Assert.Throws<System.InvalidOperationException>(() => comboList.ValidateConfiguration());
        Assert.That(exception.Message, Does.Contain("SFXConfig 不能为 null"));

        DestroyComboListAssets(comboList);
        Object.DestroyImmediate(comboList);
    }

    [Test]
    public void NullAttackFeedbackConfigFailsValidation()
    {
        ComboList comboList = BuildValidComboList("NullFeedbackComboList");
        comboList.ComboConfigs[0].AttackFeedbackConfig = null;

        var exception = Assert.Throws<System.InvalidOperationException>(() => comboList.ValidateConfiguration());
        Assert.That(exception.Message, Does.Contain("AttackFeedbackConfig 不能为 null"));

        DestroyComboListAssets(comboList);
        Object.DestroyImmediate(comboList);
    }

    [Test]
    public void EmptyDetectionAndNullInteractionFailsValidation()
    {
        // 第 2 点回归：一边空数组、一边 null，绝不能被误判为合法无伤害招式而放行。
        ComboList comboList = BuildValidComboList("HalfEmptyComboList");
        comboList.ComboConfigs[0].AttackDetectionConfig = new AttackDetectionConfig[0];
        comboList.ComboConfigs[0].InteractionConfig = null;

        var exception = Assert.Throws<System.InvalidOperationException>(() => comboList.ValidateConfiguration());
        Assert.That(exception.Message, Does.Contain("同时存在或同时为空"));
        Assert.That(exception.Message, Does.Contain("InteractionConfig 为 null"));

        DestroyComboListAssets(comboList);
        Object.DestroyImmediate(comboList);
    }

    [Test]
    public void StartFrameBeyondLastTriggerableFrameFailsValidation()
    {
        ComboList comboList = BuildValidComboList("OverFrameComboList");
        // 30 FPS、3 秒动画共 90 帧，最后一帧为 90；91 帧换算出的归一化时间不再严格小于 1。
        comboList.ComboConfigs[0].AttackDetectionConfig[0].StartFrame = 91;

        var exception = Assert.Throws<System.InvalidOperationException>(() => comboList.ValidateConfiguration());
        Assert.That(exception.Message, Does.Contain("可触发的最后一帧"));
        Assert.That(exception.Message, Does.Contain("严格小于 1"));

        DestroyComboListAssets(comboList);
        Object.DestroyImmediate(comboList);
    }

    [Test]
    public void ReverseHitEventOrderFailsValidation()
    {
        ComboList comboList = BuildValidComboList("ReverseComboList");
        // 第一事件帧号 3，第二事件帧号 2，命中事件未按非递减排列。
        comboList.ComboConfigs[0].AttackDetectionConfig[1].StartFrame = 2;

        var exception = Assert.Throws<System.InvalidOperationException>(() => comboList.ValidateConfiguration());
        Assert.That(exception.Message, Does.Contain("非递减排列"));

        DestroyComboListAssets(comboList);
        Object.DestroyImmediate(comboList);
    }

    [Test]
    public void SameStartFrameMultipleBoxesPassesValidation()
    {
        ComboList comboList = BuildValidComboList("SameFrameComboList");
        // 同一帧允许多个碰撞盒，只需把第二事件的帧号压到与第一事件相同。
        comboList.ComboConfigs[0].AttackDetectionConfig[1].StartFrame = 3;

        Assert.DoesNotThrow(() => comboList.ValidateConfiguration());

        DestroyComboListAssets(comboList);
        Object.DestroyImmediate(comboList);
    }

    [Test]
    public void ZeroScaleComponentFailsValidation()
    {
        ComboList comboList = BuildValidComboList("ZeroScaleComboList");
        comboList.ComboConfigs[0].AttackDetectionConfig[0].Scale = new Vector3(0.5f, 0f, 0.8f);

        var exception = Assert.Throws<System.InvalidOperationException>(() => comboList.ValidateConfiguration());
        Assert.That(exception.Message, Does.Contain("Scale"));
        Assert.That(exception.Message, Does.Contain("大于 0"));

        DestroyComboListAssets(comboList);
        Object.DestroyImmediate(comboList);
    }

    [Test]
    public void NegativeDamageFailsValidation()
    {
        ComboList comboList = BuildValidComboList("NegativeDamageComboList");
        comboList.ComboConfigs[0].InteractionConfig[0].Damage = -1f;

        var exception = Assert.Throws<System.InvalidOperationException>(() => comboList.ValidateConfiguration());
        Assert.That(exception.Message, Does.Contain("Damage"));
        Assert.That(exception.Message, Does.Contain("不小于 0"));

        DestroyComboListAssets(comboList);
        Object.DestroyImmediate(comboList);
    }

    [Test]
    public void UndefinedAttackForceFailsValidation()
    {
        ComboList comboList = BuildValidComboList("BadForceComboList");
        comboList.ComboConfigs[0].InteractionConfig[0].AttackForce = (AttackForce)42;

        var exception = Assert.Throws<System.InvalidOperationException>(() => comboList.ValidateConfiguration());
        Assert.That(exception.Message, Does.Contain("AttackForce"));
        Assert.That(exception.Message, Does.Contain("Easy/Medium/Hard"));

        DestroyComboListAssets(comboList);
        Object.DestroyImmediate(comboList);
    }

    [Test]
    public void BothEmptyArraysIsValidNoDamageCombo()
    {
        ComboList comboList = ScriptableObject.CreateInstance<ComboList>();
        comboList.name = "NoDamageComboList";
        ComboConfig noDamageCombo = BuildNoDamageCombo("NoDamage01");
        // 本测试只验证空命中数组；其余必填配置必须保持合法，避免被恢复帧校验提前拦截。
        noDamageCombo.RecoveryStartFrame = 20;
        SetComboConfigs(comboList, new[] { noDamageCombo });

        // 两个数组都为空表示这一段是无伤害招式，是合法配置。
        Assert.DoesNotThrow(() => comboList.ValidateConfiguration());

        DestroyComboListAssets(comboList);
        Object.DestroyImmediate(comboList);
    }

    [Test]
    public void AMComboListAssetPassesRecoveryFrameConfiguration()
    {
        // 后摇起始帧由原 Transition 事件换算后写入全部五段资产，整份实际连招表必须通过校验。
        ComboList comboList = AssetDatabase.LoadAssetAtPath<ComboList>(
            "Assets/ScriptableObjects/Characters/Player/CombatSO/AM_ComboList.asset");
        Assert.That(comboList, Is.Not.Null);

        Assert.DoesNotThrow(() => comboList.ValidateConfiguration());
    }

    [Test]
    public void EveryComboRequiresRecoveryStartFrame()
    {
        ComboList comboList = BuildValidComboList("MissingChainFrameComboList");
        comboList.ComboConfigs[0].RecoveryStartFrame = 0;

        var exception = Assert.Throws<System.InvalidOperationException>(() => comboList.ValidateConfiguration());
        Assert.That(exception.Message, Does.Contain("RecoveryStartFrame"));

        DestroyComboListAssets(comboList);
        Object.DestroyImmediate(comboList);
    }

    [Test]
    public void LastComboAlsoRequiresRecoveryStartFrame()
    {
        ComboList comboList = BuildValidComboList("LastComboChainFrameList");
        comboList.ComboConfigs[1].RecoveryStartFrame = 0;

        var exception = Assert.Throws<System.InvalidOperationException>(() => comboList.ValidateConfiguration());
        Assert.That(exception.Message, Does.Contain("RecoveryStartFrame"));

        DestroyComboListAssets(comboList);
        Object.DestroyImmediate(comboList);
    }

    [Test]
    public void RecoveryStartCannotPrecedeLastHitFrame()
    {
        ComboList comboList = BuildValidComboList("ChainStartBeforeLastHitComboList");
        comboList.ComboConfigs[0].RecoveryStartFrame = 4;

        var exception = Assert.Throws<System.InvalidOperationException>(() => comboList.ValidateConfiguration());
        Assert.That(exception.Message, Does.Contain("不能早于最后一次命中帧"));

        DestroyComboListAssets(comboList);
        Object.DestroyImmediate(comboList);
    }

    [Test]
    public void LoopingMotionClipFailsValidation()
    {
        ComboList comboList = BuildValidComboList("LoopingAttackClipList");
        AnimationClip clip = comboList.ComboConfigs[0].MotionData.Clip;
        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        var exception = Assert.Throws<System.InvalidOperationException>(() => comboList.ValidateConfiguration());
        Assert.That(exception.Message, Does.Contain("MotionData.Clip 必须关闭循环播放"));

        DestroyComboListAssets(comboList);
        Object.DestroyImmediate(comboList);
    }

    [Test]
    public void AttackStateOwnsTimingWithoutRecoveryOrAttackAnimationEvents()
    {
        string attackStateSource = File.ReadAllText(ProjectPath(
            "Assets/Scrips/Characters/Player/Statemachine/Movement/State/Attack/PlayerAttackState.cs"));
        string stateMachineSource = File.ReadAllText(ProjectPath(
            "Assets/Scrips/Characters/Player/Statemachine/Movement/PlayerMovementStateMachine.cs"));

        Assert.That(attackStateSource, Does.Not.Contain("OnAnimationTransitionEvent"));
        Assert.That(attackStateSource, Does.Not.Contain("OnAnimationExitEvent"));
        Assert.That(attackStateSource, Does.Not.Contain("AttackRecoveryState"));
        Assert.That(stateMachineSource, Does.Not.Contain("AttackRecoveryState"));
        Assert.That(File.Exists(ProjectPath(
            "Assets/Scrips/Characters/Player/Statemachine/Movement/State/Attack/PlayerAttackRecoveryState.cs")), Is.False);
    }

    [Test]
    public void AttackClipsNoLongerContainCombatTimingEvents()
    {
        string[] attackClipPaths =
        {
            "Assets/Animations/Characters/Player/Clip/Attack/AM_Attack01.anim",
            "Assets/Animations/Characters/Player/Clip/Attack/AM_Attack02.anim",
            "Assets/Animations/Characters/Player/Clip/Attack/AM_Attack03.anim",
            "Assets/Animations/Characters/Player/Clip/Attack/AM_Attack04.anim",
            "Assets/Animations/Characters/Player/Clip/Attack/AM_Attack05.anim"
        };

        foreach (string path in attackClipPaths)
        {
            string clipSource = File.ReadAllText(ProjectPath(path));
            Assert.That(clipSource, Does.Not.Contain("TriggerOnMovementStateAnimationTransitionEvent"), path);
            Assert.That(clipSource, Does.Not.Contain("TriggerOnMovementStateAnimationExitEvent"), path);
        }
    }

    [Test]
    public void CombatEventsReadSingleAnimancerTimeSource()
    {
        string attackStateSource = File.ReadAllText(ProjectPath(
            "Assets/Scrips/Characters/Player/Statemachine/Movement/State/Attack/PlayerAttackState.cs"));
        int updateStart = attackStateSource.IndexOf("public override void Update()");
        int updateEnd = attackStateSource.IndexOf("public override void PhysicsUpdate()");
        Assert.That(updateStart, Is.GreaterThanOrEqualTo(0));
        Assert.That(updateEnd, Is.GreaterThan(updateStart));
        string updateSource = attackStateSource.Substring(updateStart, updateEnd - updateStart);

        int timeSourceIndex = updateSource.IndexOf(
            "float normalizedTime = currentComboAnimationState.NormalizedTime");
        int motionTimeIndex = updateSource.IndexOf("motionDriver.SetNormalizedTime(normalizedTime)");
        int combatUpdateIndex = updateSource.IndexOf("combatExecutor.Update(normalizedTime)");

        // 位移曲线与命中判定必须取自同一份播放进度，避免两个时间源在同一帧产生偏差。
        Assert.That(timeSourceIndex, Is.GreaterThanOrEqualTo(0));
        Assert.That(motionTimeIndex, Is.GreaterThan(timeSourceIndex));
        Assert.That(combatUpdateIndex, Is.GreaterThan(timeSourceIndex));
    }

    [Test]
    public void AttackUsesOneRecoveryFrameForComboMovementAndDashCancel()
    {
        string comboConfigSource = File.ReadAllText(ProjectPath(
            "Assets/Scrips/Characters/Player/Data/ScriptableObject/Combo/ComboConfig.cs"));
        string attackStateSource = File.ReadAllText(ProjectPath(
            "Assets/Scrips/Characters/Player/Statemachine/Movement/State/Attack/PlayerAttackState.cs"));

        Assert.That(comboConfigSource, Does.Not.Contain("DashCancelStartFrame"));
        Assert.That(attackStateSource, Does.Not.Contain("GetDashCancelStartNormalizedTime"));
        Assert.That(attackStateSource, Does.Contain("TryCancelToMovement"));
        Assert.That(attackStateSource, Does.Contain("GetRecoveryStartNormalizedTime(currentComboIndex)"));
    }

    [Test]
    public void ComboConfigOwnsRecoveryFrameValidation()
    {
        string comboConfigSource = File.ReadAllText(ProjectPath(
            "Assets/Scrips/Characters/Player/Data/ScriptableObject/Combo/ComboConfig.cs"));
        string comboListSource = File.ReadAllText(ProjectPath(
            "Assets/Scrips/Characters/Player/Data/ScriptableObject/Combo/ComboList.cs"));

        Assert.That(comboConfigSource, Does.Contain("ValidateRecoveryStart"));
        Assert.That(comboListSource, Does.Not.Contain("ValidateRecoveryStart"));
    }

    [Test]
    public void CombatExecutorConsumesAllDueFXEventsInOneUpdate()
    {
        string executorSource = File.ReadAllText(ProjectPath(
            "Assets/Scrips/Characters/Player/AttackSystem/CombatExecutor.cs"));
        int fxMethodStart = executorSource.IndexOf("private void RunFXEvent");
        int resetIndexesStart = executorSource.IndexOf("private void ResetEventIndexes");
        string fxMethodSource = executorSource.Substring(fxMethodStart, resetIndexesStart - fxMethodStart);

        Assert.That(fxMethodSource, Does.Contain("while (true)"));
        Assert.That(fxMethodSource, Does.Contain("fxEventIndex++"));
    }

    [Test]
    public void ComboListRuntimeAccessorsDropRedundantTryFallbacks()
    {
        // 已校验数据的直接读取不再暴露静默兜底接口，缺失应暴露为错误而非静默失效。
        Assert.That(typeof(ComboList).GetMethod("TryGetComboInteractionConfig"), Is.Null);
        Assert.That(typeof(ComboList).GetMethod("TryGetComboConfigsCount"), Is.Null);
        Assert.That(typeof(ComboList).GetMethod("GetComboInteractionConfig"), Is.Not.Null);
        Assert.That(typeof(ComboList).GetProperty("ComboCount"), Is.Not.Null);

        // Animancer 直接播放 MotionData.Animation，Animator 状态路径不再是连招数据的一部分，
        // 因此段名访问器整体移除，而不是保留一个没有消费者的只读接口。
        Assert.That(typeof(ComboList).GetMethod("TryGetComboName"), Is.Null);
        Assert.That(typeof(ComboList).GetMethod("GetComboName"), Is.Null);
        Assert.That(typeof(ComboConfig).GetField("ComboName"), Is.Null);
        Assert.That(typeof(ComboConfig).GetField("AttackClip"), Is.Null);

        // 事件游标走到数组末尾返回 null 是正常结束信号，这些遍历接口必须保留。
        Assert.That(typeof(ComboList).GetMethod("TryGetAttackDetectionConfig"), Is.Not.Null);
        Assert.That(typeof(ComboList).GetMethod("TryGetFXConfig"), Is.Not.Null);
    }

    [Test]
    public void AttackStatePlaysComboThroughAnimancerFromStart()
    {
        string attackStateSource = File.ReadAllText(ProjectPath(
            "Assets/Scrips/Characters/Player/Statemachine/Movement/State/Attack/PlayerAttackState.cs"));

        // 攻击段改由 Animancer 播放 MotionData.Animation，Animator 播放与状态查询接口不得回流。
        Assert.That(attackStateSource, Does.Contain("stateMachine.Player.Animancer.Play("));
        Assert.That(attackStateSource, Does.Contain("comboConfig.MotionData.Animation"));
        Assert.That(attackStateSource, Does.Not.Contain("CrossFadeInFixedTime"));
        Assert.That(attackStateSource, Does.Not.Contain("GetCurrentAnimatorStateInfo"));
        Assert.That(attackStateSource, Does.Not.Contain("GetNextAnimatorStateInfo"));

        // 同一段可被打断后重新起手，必须强制从头播放而不是接续上次保存的进度。
        Assert.That(attackStateSource, Does.Contain("FadeMode.FromStart"));
    }

    [Test]
    public void AttackStateEndsOnExplicitProgressCheckInsteadOfAnimancerEndEvent()
    {
        string attackStateSource = File.ReadAllText(ProjectPath(
            "Assets/Scrips/Characters/Player/Statemachine/Movement/State/Attack/PlayerAttackState.cs"));

        // 动画结束回调可能早于本帧 Update 触发，会跳过最后一帧应结算的命中与 FX，
        // 因此攻击段结束固定走 Update 内的显式进度判定，与 Dash、Landing 的处理方式不同。
        Assert.That(attackStateSource, Does.Not.Contain("Events(this)"));
        Assert.That(attackStateSource, Does.Contain("if (normalizedTime >= 1f)"));
    }

    [Test]
    public void AttackStateRunsDataLayerValidationBeforePlayingCombo()
    {
        string attackStateSource = File.ReadAllText(ProjectPath(
            "Assets/Scrips/Characters/Player/Statemachine/Movement/State/Attack/PlayerAttackState.cs"));
        int enterStart = attackStateSource.IndexOf("public override void Enter()");
        int enterEnd = attackStateSource.IndexOf("public override void Exit()");
        Assert.That(enterStart, Is.GreaterThanOrEqualTo(0));
        Assert.That(enterEnd, Is.GreaterThan(enterStart));
        string enterSource = attackStateSource.Substring(enterStart, enterEnd - enterStart);

        // 配置错误必须在注册输入和起手表现之前暴露，不能留下半初始化的攻击状态。
        int validationIndex = enterSource.IndexOf("ValidateConfiguration()");
        int executeComboIndex = enterSource.IndexOf("ExecuteCombo(0)");
        Assert.That(validationIndex, Is.GreaterThanOrEqualTo(0));
        Assert.That(executeComboIndex, Is.GreaterThan(validationIndex));

        Assert.That(attackStateSource, Does.Contain("comboList.ValidateConfiguration()"));
    }

    [Test]
    public void CombatExecutorStaysOutOfInputBuffering()
    {
        string executorSource = File.ReadAllText(ProjectPath(
            "Assets/Scrips/Characters/Player/AttackSystem/CombatExecutor.cs"));

        // 命中执行器只消费动画进度，不得读取预输入或决定下一段招式。
        Assert.That(executorSource, Does.Not.Contain("PlayerActionBuffer"));
        Assert.That(executorSource, Does.Not.Contain("PlayerActionType"));
        Assert.That(executorSource, Does.Not.Contain("ExecuteCombo"));
    }

    [Test]
    public void AttackStateOwnsAttackInputBufferingAndConsumption()
    {
        string attackStateSource = File.ReadAllText(ProjectPath(
            "Assets/Scrips/Characters/Player/Statemachine/Movement/State/Attack/PlayerAttackState.cs"));
        string stateMachineSource = File.ReadAllText(ProjectPath(
            "Assets/Scrips/Characters/Player/Statemachine/Movement/PlayerMovementStateMachine.cs"));

        // 缓冲器由状态机持有，攻击状态负责记录、消费与退出清理。
        Assert.That(stateMachineSource, Does.Contain("public PlayerActionBuffer ActionBuffer"));
        Assert.That(attackStateSource, Does.Contain("actionBuffer = stateMachine.ActionBuffer"));
        Assert.That(attackStateSource, Does.Contain("actionBuffer.Record(PlayerActionType.Attack"));
        Assert.That(attackStateSource, Does.Contain("actionBuffer.TryConsume("));
        Assert.That(attackStateSource, Does.Contain("actionBuffer.Clear(PlayerActionType.Attack)"));
    }

    [Test]
    public void AttackCallbackOnlyRecordsIntentWithoutExecutingCombo()
    {
        string attackStateSource = File.ReadAllText(ProjectPath(
            "Assets/Scrips/Characters/Player/Statemachine/Movement/State/Attack/PlayerAttackState.cs"));
        int callbackStart = attackStateSource.IndexOf("protected override void OnAttackStarted");
        int callbackEnd = attackStateSource.IndexOf("protected override void OnDashStarted");
        Assert.That(callbackStart, Is.GreaterThanOrEqualTo(0));
        Assert.That(callbackEnd, Is.GreaterThan(callbackStart));
        string callbackSource = attackStateSource.Substring(callbackStart, callbackEnd - callbackStart);

        // 输入回调不再读取动画进度或直接换段，衔接时机完全交给 Update。
        Assert.That(callbackSource, Does.Contain("actionBuffer.Record"));
        Assert.That(callbackSource, Does.Not.Contain("ExecuteCombo"));
        Assert.That(callbackSource, Does.Not.Contain("TryGetCurrentComboNormalizedTime"));
        Assert.That(callbackSource, Does.Not.Contain("GetRecoveryStartNormalizedTime"));
    }

    [Test]
    public void ComboContinuationIsArbitratedBeforeMovementCancel()
    {
        string attackStateSource = File.ReadAllText(ProjectPath(
            "Assets/Scrips/Characters/Player/Statemachine/Movement/State/Attack/PlayerAttackState.cs"));
        int updateStart = attackStateSource.IndexOf("public override void Update()");
        int updateEnd = attackStateSource.IndexOf("public override void PhysicsUpdate()");
        Assert.That(updateStart, Is.GreaterThanOrEqualTo(0));
        Assert.That(updateEnd, Is.GreaterThan(updateStart));
        string updateSource = attackStateSource.Substring(updateStart, updateEnd - updateStart);

        int combatUpdateIndex = updateSource.IndexOf("combatExecutor.Update(normalizedTime)");
        int continueComboIndex = updateSource.IndexOf("TryContinueCombo(normalizedTime)");
        int cancelIndex = updateSource.IndexOf("TryCancelToMovement(normalizedTime)");
        int finishIndex = updateSource.IndexOf("HandleAttackFinished()");

        // 同帧顺序固定为：到时命中与 FX、攻击衔接、移动取消、动画自然结束。
        Assert.That(combatUpdateIndex, Is.GreaterThanOrEqualTo(0));
        Assert.That(continueComboIndex, Is.GreaterThan(combatUpdateIndex));
        Assert.That(cancelIndex, Is.GreaterThan(continueComboIndex));
        Assert.That(finishIndex, Is.GreaterThan(cancelIndex));
    }

    [Test]
    public void DashCancelKeepsImmediateStateChangeWithoutBuffering()
    {
        string attackStateSource = File.ReadAllText(ProjectPath(
            "Assets/Scrips/Characters/Player/Statemachine/Movement/State/Attack/PlayerAttackState.cs"));
        int callbackStart = attackStateSource.IndexOf("protected override void OnDashStarted");
        int callbackEnd = attackStateSource.IndexOf("protected override void OnJumpStarted");
        Assert.That(callbackStart, Is.GreaterThanOrEqualTo(0));
        Assert.That(callbackEnd, Is.GreaterThan(callbackStart));
        string callbackSource = attackStateSource.Substring(callbackStart, callbackEnd - callbackStart);

        // 冲刺不进入缓冲，仍在回调中即时切换状态。
        Assert.That(callbackSource, Does.Contain("ChangeState(stateMachine.DashingState)"));
        Assert.That(callbackSource, Does.Not.Contain("actionBuffer"));
    }

    [Test]
    public void PlayerActionBufferStaysIndependentFromUnityAndStateMachine()
    {
        string bufferSource = File.ReadAllText(ProjectPath(
            "Assets/Scrips/Characters/Player/Utilities/Input/PlayerActionBuffer.cs"));

        // 缓冲器是纯数据服务：不引用 UnityEngine、输入系统、动画或状态机。
        Assert.That(bufferSource, Does.Not.Contain("using UnityEngine"));
        Assert.That(bufferSource, Does.Not.Contain("Time.time"));
        Assert.That(bufferSource, Does.Not.Contain("Animator"));
        Assert.That(bufferSource, Does.Not.Contain("stateMachine"));
        Assert.That(bufferSource, Does.Not.Contain("InputAction"));
    }

    [Test]
    public void PlayerAssetConfiguresPositiveAttackBufferDuration()
    {
        PlayerSO playerData = AssetDatabase.LoadAssetAtPath<PlayerSO>(
            "Assets/ScriptableObjects/Characters/Player/Player.asset");
        Assert.That(playerData, Is.Not.Null);

        // 新增字段必须已写入实际资产：旧资产反序列化会得到 0，从而在进入攻击时抛异常。
        Assert.That(playerData.AttackData.AttackBufferDuration, Is.GreaterThan(0f));
    }

    // 构造一份完全合法的两事件 ComboList，供各失败用例按需破坏单项字段。
    private static ComboList BuildValidComboList(string listName)
    {
        ComboList comboList = ScriptableObject.CreateInstance<ComboList>();
        comboList.name = listName;
        ComboConfig firstCombo = BuildValidCombo("Combo01", 3, 5);
        firstCombo.RecoveryStartFrame = 20;
        ComboConfig lastCombo = BuildValidCombo("Combo02", 7, 10);
        lastCombo.RecoveryStartFrame = 20;
        SetComboConfigs(comboList, new[] { firstCombo, lastCombo });
        return comboList;
    }

    private static ComboConfig BuildValidCombo(string comboName, int firstFrame, int secondFrame)
    {
        ComboConfig comboConfig = ScriptableObject.CreateInstance<ComboConfig>();
        comboConfig.name = comboName;
        // 30 FPS、3 秒动画，共 90 帧，合法帧号为 1..90。
        SetComboMotionClip(comboConfig, BuildAttackClip(30f, 3f));
        comboConfig.AttackDetectionConfig = new[]
        {
            BuildValidDetection(firstFrame),
            BuildValidDetection(secondFrame)
        };
        comboConfig.InteractionConfig = new[]
        {
            BuildValidInteraction(),
            BuildValidInteraction()
        };
        comboConfig.AttackFeedbackConfig = new AttackFeedbackConfig[0];
        comboConfig.FXConfig = new FXConfig[0];
        comboConfig.SFXConfig = new SFXConfig[0];
        return comboConfig;
    }

    private static ComboConfig BuildNoDamageCombo(string comboName)
    {
        ComboConfig comboConfig = ScriptableObject.CreateInstance<ComboConfig>();
        comboConfig.name = comboName;
        SetComboMotionClip(comboConfig, BuildAttackClip(30f, 3f));
        comboConfig.AttackDetectionConfig = new AttackDetectionConfig[0];
        comboConfig.InteractionConfig = new ComboInteractionConfig[0];
        comboConfig.AttackFeedbackConfig = new AttackFeedbackConfig[0];
        comboConfig.FXConfig = new FXConfig[0];
        comboConfig.SFXConfig = new SFXConfig[0];
        return comboConfig;
    }

    private static AnimationClip BuildAttackClip(float frameRate, float length)
    {
        // AnimationClip 不是 ScriptableObject，需要直接构造；用一条覆盖目标长度的曲线写入 length。
        AnimationClip clip = new AnimationClip { frameRate = frameRate };
        clip.SetCurve(
            string.Empty,
            typeof(Transform),
            "localPosition.x",
            AnimationCurve.Linear(0f, 0f, length, 0f));
        return clip;
    }

    // MotionData 的运行时访问器是只读的，测试只能按真实序列化结构写入内嵌 ClipTransition 的 Clip 字段。
    private static void SetComboMotionClip(ComboConfig comboConfig, AnimationClip clip)
    {
        SerializedObject serializedCombo = new SerializedObject(comboConfig);
        serializedCombo
            .FindProperty("motionData")
            .FindPropertyRelative("animation")
            .FindPropertyRelative(ClipTransition.ClipFieldName)
            .objectReferenceValue = clip;
        serializedCombo.ApplyModifiedPropertiesWithoutUndo();
    }

    private static AttackDetectionConfig BuildValidDetection(int startFrame)
    {
        return new AttackDetectionConfig
        {
            StartFrame = startFrame,
            Position = Vector3.zero,
            Rotation = Vector3.zero,
            Scale = new Vector3(0.5f, 0.5f, 0.5f)
        };
    }

    private static ComboInteractionConfig BuildValidInteraction()
    {
        return new ComboInteractionConfig
        {
            AttackForce = AttackForce.Medium,
            Damage = 1f
        };
    }

    private static T[] AppendElement<T>(T[] source, T element)
    {
        T[] extended = new T[source.Length + 1];
        System.Array.Copy(source, extended, source.Length);
        extended[source.Length] = element;
        return extended;
    }

    // ComboList 的 ComboConfigs 为 private set，运行时由 SerializedField 写入；
    // 编辑器测试需要直接装配数据，通过反射一次性写入，避免改动数据类的可见性。
    private static void SetComboConfigs(ComboList comboList, ComboConfig[] comboConfigs)
    {
        typeof(ComboList)
            .GetProperty(nameof(ComboList.ComboConfigs))
            .SetValue(comboList, comboConfigs);
    }

    // ComboList 与其内联 ComboConfig、AnimationClip 都是编辑器临时对象，统一释放避免泄漏。
    private static void DestroyComboListAssets(ComboList comboList)
    {
        if (comboList == null || comboList.ComboConfigs == null)
        {
            return;
        }

        foreach (ComboConfig comboConfig in comboList.ComboConfigs)
        {
            if (comboConfig == null)
            {
                continue;
            }
            if (comboConfig.MotionData.Clip != null)
            {
                Object.DestroyImmediate(comboConfig.MotionData.Clip);
            }
            Object.DestroyImmediate(comboConfig);
        }
    }

    private static string ProjectPath(string relativePath)
    {
        return Path.Combine(Directory.GetParent(Application.dataPath).FullName, relativePath);
    }
}
