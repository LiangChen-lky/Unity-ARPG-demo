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
    public void CombatReceiverUsesHitContextContract()
    {
        Assert.That(typeof(IHitReceiver).IsAssignableFrom(typeof(CombatControllerBase)), Is.True);

        MethodInfo receiveHit = typeof(CombatControllerBase).GetMethod(nameof(IHitReceiver.ReceiveHit));
        Assert.That(receiveHit, Is.Not.Null);
        Assert.That(receiveHit.GetParameters(), Has.Length.EqualTo(1));
        Assert.That(receiveHit.GetParameters()[0].ParameterType, Is.EqualTo(typeof(HitContext)));
        Assert.That(typeof(PlayerAttackData).GetProperty("HitFXList"), Is.Null);
        Assert.That(typeof(PlayerAttackData).GetProperty("FXPositionList"), Is.Null);
    }

    [Test]
    public void CombatExecutorDoesNotSpecifyTargetReactions()
    {
        string executorSource = File.ReadAllText(ProjectPath(
            "Assets/Scrips/Characters/Player/AttackSystem/CombatExecutor.cs"));
        string receiverSource = File.ReadAllText(ProjectPath(
            "Assets/Scrips/Characters/Player/AttackSystem/CombatControllerBase.cs"));

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
            "CombatControllerBase",
            "ComboInteractionConfig",
            "MoveOffsetConfig"
        };

        foreach (string dependency in forbiddenDependencies)
        {
            Assert.That(source, Does.Not.Contain(dependency));
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
        Match guidMatch = Regex.Match(inputMeta, @"(?m)^guid:\s*(\w+)\s*$");

        Assert.That(guidMatch.Success, Is.True);
        int inputCount = Regex.Matches(scene, $@"guid:\s*{Regex.Escape(guidMatch.Groups[1].Value)}").Count;

        Assert.That(inputCount, Is.EqualTo(1));
        Assert.That(scene, Does.Match(@"weaponControllerSource:\s*\{fileID:\s*(?!0\b)\d+\}"));
        Assert.That(scene, Does.Not.Contain("<CurrentComboList>k__BackingField"));
        Assert.That(scene, Does.Not.Contain("<TargetLayer>k__BackingField"));
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
            "Assets/Scrips/Weapons/Data/WeaponReusableData.cs"
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
