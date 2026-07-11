using System.Collections;
using System.IO;
using System.Linq;
using MagicaCloth;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class Unity6MigrationBehaviorTests
{
    private const string SampleScenePath = "Assets/Scenes/SampleScene.unity";

    [UnityTearDown]
    public IEnumerator ExitPlayModeAfterTest()
    {
        if (EditorApplication.isPlaying)
        {
            yield return new ExitPlayMode();
        }
    }

    [Test]
    [Category("Unity6Migration")]
    public void PlayerInputActionsFollowComponentLifecycle()
    {
        GameObject owner = new GameObject("PlayerInput Lifecycle Test");
        owner.SetActive(false);

        try
        {
            PlayerInput playerInput = owner.AddComponent<PlayerInput>();
            playerInput.EnsureInitialized();

            Assert.That(playerInput.InputAction, Is.Not.Null);
            Assert.That(playerInput.PlayerActions.enabled, Is.False);

            owner.SetActive(true);
            Assert.That(playerInput.PlayerActions.enabled, Is.True);

            owner.SetActive(false);
            Assert.That(playerInput.PlayerActions.enabled, Is.False);
        }
        finally
        {
            Object.DestroyImmediate(owner);
        }
    }

    [Test]
    [Category("Unity6Migration")]
    public void SampleSceneMaterialsUseSupportedShadersWithoutCompileErrors()
    {
        string[] materialPaths = AssetDatabase.GetDependencies(SampleScenePath, true)
            .Where(path => path.EndsWith(".mat"))
            .ToArray();

        Assert.That(materialPaths, Is.Not.Empty);

        foreach (string materialPath in materialPaths)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            Assert.That(material, Is.Not.Null, materialPath);
            Assert.That(material.shader, Is.Not.Null, materialPath);
            Assert.That(material.shader.isSupported, Is.True,
                $"Unsupported shader '{material.shader.name}' on {materialPath}");
            Assert.That(ShaderUtil.ShaderHasError(material.shader), Is.False,
                $"Shader '{material.shader.name}' has compile errors on {materialPath}");
        }
    }

    [UnityTest]
    [Category("Unity6Migration")]
    public IEnumerator SampleSceneSupportsPlayerInputPhysicsAndMagicaClothLifecycle()
    {
        yield return new EnterPlayMode();

        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(SampleScenePath, LoadSceneMode.Single);
        while (!loadOperation.isDone)
        {
            yield return null;
        }

        for (int i = 0; i < 10; i++)
        {
            yield return null;
        }

        Player player = Object.FindAnyObjectByType<Player>();
        Assert.That(player, Is.Not.Null);
        Assert.That(player.Input.PlayerActions.enabled, Is.True);
        Assert.That(player.Rigidbody, Is.Not.Null);

        BaseCloth[] clothComponents = Object.FindObjectsByType<BaseCloth>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None)
            .Where(cloth => cloth.isActiveAndEnabled)
            .ToArray();
        Assert.That(clothComponents, Is.Not.Empty);

        for (int i = 0; i < 30 && clothComponents.Any(cloth => !cloth.Status.IsInitComplete); i++)
        {
            yield return null;
        }

        foreach (BaseCloth cloth in clothComponents)
        {
            Assert.That(cloth.Status.IsInitSuccess, Is.True, cloth.name);
            cloth.enabled = false;
        }

        yield return null;

        foreach (BaseCloth cloth in clothComponents)
        {
            Assert.That(cloth.Status.IsActive, Is.False, cloth.name);
            cloth.enabled = true;
        }

        yield return null;
        yield return null;

        foreach (BaseCloth cloth in clothComponents)
        {
            Assert.That(cloth.Status.IsInitSuccess, Is.True, cloth.name);
        }

        MagicaPhysicsManager manager = MagicaPhysicsManager.Instance;
        Assert.That(manager, Is.Not.Null);
        Assert.That(manager.IsActive, Is.True);

        manager.enabled = false;
        yield return null;
        Assert.That(manager.IsActive, Is.False);

        manager.enabled = true;
        yield return null;
        Assert.That(manager.IsActive, Is.True);

        VerifyDistinctEntityIdsRemainDistinct(manager);

        Keyboard keyboard = InputSystem.AddDevice<Keyboard>("Unity6MigrationTestKeyboard");
        Vector3 movementStart = player.Rigidbody.position;

        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W));
        InputSystem.Update();
        yield return null;
        for (int i = 0; i < 5; i++)
        {
            yield return new WaitForFixedUpdate();
        }

        float movedDistance = Vector3.ProjectOnPlane(
            player.Rigidbody.position - movementStart,
            Vector3.up).magnitude;

        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.LeftShift));
        InputSystem.Update();
        yield return new WaitForFixedUpdate();
        float dashSpeed = Vector3.ProjectOnPlane(player.Rigidbody.linearVelocity, Vector3.up).magnitude;

        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.Space));
        InputSystem.Update();
        yield return new WaitForFixedUpdate();
        float jumpSpeed = player.Rigidbody.linearVelocity.y;

        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        InputSystem.Update();
        InputSystem.RemoveDevice(keyboard);

        Assert.That(movedDistance, Is.GreaterThan(0.01f), "W did not move the player.");
        Assert.That(dashSpeed, Is.GreaterThan(1f), "Shift did not produce dash velocity.");
        Assert.That(jumpSpeed, Is.GreaterThan(0.1f), "Space did not produce upward velocity.");

        foreach (BaseCloth cloth in clothComponents)
        {
            cloth.enabled = false;
        }

        Object.Destroy(manager.gameObject);
        yield return null;
        Assert.That(MagicaPhysicsManager.IsInstance(), Is.False);

        LogAssert.NoUnexpectedReceived();
        yield return new ExitPlayMode();
    }

    [Test]
    [Explicit("Run for Unity 6 migration validation or CI release checks.")]
    [Category("Unity6MigrationBuild")]
    public void WindowsPlayerBuildSucceeds()
    {
        string outputDirectory = Path.GetFullPath(Path.Combine("Temp", "Unity6MigrationBuild"));
        string executablePath = Path.Combine(outputDirectory, "ARPGDemo.exe");

        try
        {
            Directory.CreateDirectory(outputDirectory);
            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes
                    .Where(scene => scene.enabled)
                    .Select(scene => scene.path)
                    .ToArray(),
                locationPathName = executablePath,
                target = BuildTarget.StandaloneWindows64,
                options = UnityEditor.BuildOptions.Development
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);

            Assert.That(
                report.summary.result,
                Is.EqualTo(UnityEditor.Build.Reporting.BuildResult.Succeeded));
            Assert.That(File.Exists(executablePath), Is.True);
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, true);
            }
        }
    }

    private static void VerifyDistinctEntityIdsRemainDistinct(MagicaPhysicsManager manager)
    {
        Mesh firstMesh = new Mesh { name = "Migration EntityId A" };
        Mesh secondMesh = new Mesh { name = "Migration EntityId B" };
        EntityId firstId = firstMesh.GetEntityId();
        EntityId secondId = secondMesh.GetEntityId();
        int firstIndex = -1;
        int secondIndex = -1;

        try
        {
            Assert.That(firstId, Is.Not.EqualTo(secondId));
            Assert.That(manager.Mesh.IsEmptySharedRenderMesh(firstId), Is.True);
            Assert.That(manager.Mesh.IsEmptySharedRenderMesh(secondId), Is.True);

            firstIndex = manager.Mesh.AddRenderMesh(firstId, false, Vector3.one, 3, -1, 0);
            Assert.That(manager.Mesh.IsEmptySharedRenderMesh(firstId), Is.False);
            Assert.That(manager.Mesh.IsEmptySharedRenderMesh(secondId), Is.True);

            secondIndex = manager.Mesh.AddRenderMesh(secondId, false, Vector3.one, 3, -1, 0);
            Assert.That(secondIndex, Is.Not.EqualTo(firstIndex));
            Assert.That(manager.Mesh.IsEmptySharedRenderMesh(secondId), Is.False);
        }
        finally
        {
            manager.Mesh.RemoveRenderMesh(firstIndex);
            manager.Mesh.RemoveRenderMesh(secondIndex);
            Object.Destroy(firstMesh);
            Object.Destroy(secondMesh);
        }
    }
}
