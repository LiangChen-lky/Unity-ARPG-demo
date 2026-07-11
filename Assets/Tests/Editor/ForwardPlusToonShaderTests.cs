using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;

public class ForwardPlusToonShaderTests
{
    private const string ToonShaderName = "SimpleURPToonLitExample(With Outline)";
    private const int RenderSize = 64;

    [UnityTest]
    [Category("Unity6Migration")]
    public IEnumerator ToonShaderReceivesAdditionalPointLightInForwardPlus()
    {
        RenderPipelineAsset originalDefaultPipeline = GraphicsSettings.defaultRenderPipeline;
        RenderPipelineAsset originalQualityPipeline = QualitySettings.renderPipeline;
        UniversalRendererData rendererData = null;
        UniversalRenderPipelineAsset pipelineAsset = null;
        Material material = null;
        RenderTexture target = null;
        Texture2D readback = null;
        GameObject cameraObject = null;
        GameObject sphere = null;
        GameObject lightObject = null;

        try
        {
            Shader shader = Shader.Find(ToonShaderName);
            Assert.That(shader, Is.Not.Null, $"Shader '{ToonShaderName}' was not found.");

            rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
            rendererData.renderingMode = RenderingMode.ForwardPlus;
            pipelineAsset = UniversalRenderPipelineAsset.Create(rendererData);
            GraphicsSettings.defaultRenderPipeline = pipelineAsset;
            QualitySettings.renderPipeline = pipelineAsset;

            material = new Material(shader);
            material.SetColor("_BaseColor", Color.white);
            material.SetColor("_IndirectLightMinColor", Color.black);
            material.SetFloat("_IndirectLightMultiplier", 0f);
            material.SetFloat("_DirectLightMultiplier", 1f);
            material.SetFloat("_AdditionalLightIgnoreCelShade", 1f);
            material.SetFloat("_OutlineWidth", 0f);

            sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "Forward+ Toon Test Target";
            sphere.GetComponent<Renderer>().sharedMaterial = material;

            cameraObject = new GameObject("Forward+ Toon Test Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            cameraObject.transform.position = new Vector3(0f, 0f, -3f);
            cameraObject.transform.LookAt(Vector3.zero);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.fieldOfView = 35f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 10f;
            camera.allowHDR = false;
            camera.allowMSAA = false;

            target = new RenderTexture(RenderSize, RenderSize, 24, RenderTextureFormat.ARGB32);
            target.Create();
            camera.targetTexture = target;
            readback = new Texture2D(RenderSize, RenderSize, TextureFormat.RGBA32, false, true);

            lightObject = new GameObject("Forward+ Toon Test Point Light");
            Light pointLight = lightObject.AddComponent<Light>();
            pointLight.type = LightType.Point;
            pointLight.color = Color.white;
            pointLight.intensity = 8f;
            pointLight.range = 5f;
            pointLight.shadows = LightShadows.None;
            lightObject.transform.position = new Vector3(0f, 0f, -1.5f);

            pointLight.enabled = false;
            yield return null;
            float unlitLuminance = RenderCenterLuminance(camera, target, readback);

            pointLight.enabled = true;
            yield return null;
            float litLuminance = RenderCenterLuminance(camera, target, readback);

            Assert.That(rendererData.renderingMode, Is.EqualTo(RenderingMode.ForwardPlus));
            Assert.That(ShaderUtil.ShaderHasError(shader), Is.False,
                $"Shader '{shader.name}' has errors after compiling its Forward+ variant.");
            Assert.That(litLuminance, Is.GreaterThan(unlitLuminance + 0.1f),
                $"Forward+ point light did not brighten the toon material. Unlit={unlitLuminance:F3}, lit={litLuminance:F3}");
        }
        finally
        {
            GraphicsSettings.defaultRenderPipeline = originalDefaultPipeline;
            QualitySettings.renderPipeline = originalQualityPipeline;
            RenderTexture.active = null;

            Object.DestroyImmediate(lightObject);
            Object.DestroyImmediate(sphere);
            Object.DestroyImmediate(cameraObject);
            Object.DestroyImmediate(readback);
            if (target != null)
            {
                target.Release();
            }
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(material);
            Object.DestroyImmediate(pipelineAsset);
            Object.DestroyImmediate(rendererData);
        }
    }

    private static float RenderCenterLuminance(Camera camera, RenderTexture target, Texture2D readback)
    {
        camera.Render();
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = target;
        readback.ReadPixels(new Rect(0, 0, RenderSize, RenderSize), 0, 0, false);
        readback.Apply(false, false);
        RenderTexture.active = previous;

        Color[] pixels = readback.GetPixels(
            RenderSize / 2 - 2,
            RenderSize / 2 - 2,
            4,
            4);
        return pixels.Average(pixel => pixel.linear.grayscale);
    }
}
