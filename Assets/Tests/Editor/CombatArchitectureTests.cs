using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

public class CombatArchitectureTests
{
    [Test]
    public void HitMovementEvaluatesConfiguredScale()
    {
        HitMovement movement = new HitMovement(
            AnimationCurve.Linear(0f, 0f, 1f, 1f),
            Vector3.forward,
            0f,
            1f,
            2f);

        Assert.That(movement.IsValid, Is.True);
        Assert.That(movement.Evaluate(0.25f), Is.EqualTo(0.5f).Within(0.0001f));
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
