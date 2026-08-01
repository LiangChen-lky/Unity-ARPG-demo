using NUnit.Framework;
using UnityEditor;
using UnityEngine;

/// <summary>
/// MotionDriver 的纯运行时数据测试，验证曲线时间换算与 Rigidbody 水平速度写入边界。
/// </summary>
public class MotionDriverTests
{
    private GameObject playerObject;
    private Rigidbody rigidbody;
    private ComboConfig motionOwner;
    private MotionDriver motionDriver;

    [SetUp]
    public void SetUp()
    {
        playerObject = new GameObject("Motion Driver Test Player");
        rigidbody = playerObject.AddComponent<Rigidbody>();
        motionOwner = ScriptableObject.CreateInstance<ComboConfig>();
        motionDriver = new MotionDriver(rigidbody);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(motionOwner);
        Object.DestroyImmediate(playerObject);
    }

    [Test]
    public void PhysicsUpdateMapsNormalizedTimeAndPreservesVerticalVelocity()
    {
        AnimationMotionData motionData = ConfigureMotionData(
            AnimationCurve.Linear(0f, 1f, 2f, 5f),
            2f);
        rigidbody.linearVelocity = new Vector3(8f, 4f, 6f);

        motionDriver.Begin(motionData, Vector3.right * 3f);
        motionDriver.SetNormalizedTime(0.5f);
        motionDriver.PhysicsUpdate();

        Assert.That(rigidbody.linearVelocity.x, Is.EqualTo(3f).Within(0.001f));
        Assert.That(rigidbody.linearVelocity.y, Is.EqualTo(4f).Within(0.001f));
        Assert.That(rigidbody.linearVelocity.z, Is.EqualTo(0f).Within(0.001f));
    }

    [Test]
    public void PhysicsUpdateSupportsSignedBackwardSpeed()
    {
        AnimationMotionData motionData = ConfigureMotionData(
            AnimationCurve.Constant(0f, 1f, -2f),
            1f);

        motionDriver.Begin(motionData, Vector3.forward);
        motionDriver.SetNormalizedTime(0.5f);
        motionDriver.PhysicsUpdate();

        Assert.That(rigidbody.linearVelocity.z, Is.EqualTo(-2f).Within(0.001f));
    }

    [Test]
    public void BeginWaitsForFirstNormalizedTime()
    {
        AnimationMotionData motionData = ConfigureMotionData(
            AnimationCurve.Constant(0f, 1f, 5f),
            1f);
        Vector3 originalVelocity = new Vector3(2f, 3f, 4f);
        rigidbody.linearVelocity = originalVelocity;

        motionDriver.Begin(motionData, Vector3.forward);
        motionDriver.PhysicsUpdate();

        Assert.That(rigidbody.linearVelocity, Is.EqualTo(originalVelocity));
    }

    [Test]
    public void StopLeavesCurrentVelocityUntouched()
    {
        AnimationMotionData motionData = ConfigureMotionData(
            AnimationCurve.Constant(0f, 1f, 5f),
            1f);
        Vector3 velocityAfterStop = new Vector3(7f, 3f, 2f);

        motionDriver.Begin(motionData, Vector3.forward);
        motionDriver.SetNormalizedTime(0.5f);
        motionDriver.Stop();
        rigidbody.linearVelocity = velocityAfterStop;
        motionDriver.PhysicsUpdate();

        Assert.That(rigidbody.linearVelocity, Is.EqualTo(velocityAfterStop));
    }

    [Test]
    public void BeginResetsPreviousDataDirectionAndTime()
    {
        AnimationMotionData firstMotion = ConfigureMotionData(
            AnimationCurve.Constant(0f, 1f, 2f),
            1f);
        motionDriver.Begin(firstMotion, Vector3.forward);
        motionDriver.SetNormalizedTime(1f);
        motionDriver.PhysicsUpdate();

        AnimationMotionData secondMotion = ConfigureMotionData(
            AnimationCurve.Linear(0f, 1f, 2f, 3f),
            2f);
        Vector3 velocityBeforeTimeSync = new Vector3(6f, 4f, 5f);
        rigidbody.linearVelocity = velocityBeforeTimeSync;

        motionDriver.Begin(secondMotion, Vector3.left);
        motionDriver.PhysicsUpdate();
        Assert.That(rigidbody.linearVelocity, Is.EqualTo(velocityBeforeTimeSync));

        motionDriver.SetNormalizedTime(0.5f);
        motionDriver.PhysicsUpdate();
        Assert.That(rigidbody.linearVelocity.x, Is.EqualTo(-2f).Within(0.001f));
        Assert.That(rigidbody.linearVelocity.y, Is.EqualTo(4f).Within(0.001f));
        Assert.That(rigidbody.linearVelocity.z, Is.EqualTo(0f).Within(0.001f));
    }

    [Test]
    public void PhysicsUpdateDoesNotApplyRotationCurve()
    {
        AnimationMotionData motionData = ConfigureMotionData(
            AnimationCurve.Constant(0f, 1f, 1f),
            1f,
            AnimationCurve.Linear(0f, 0f, 1f, 180f));
        Quaternion originalRotation = Quaternion.Euler(0f, 35f, 0f);
        rigidbody.rotation = originalRotation;

        motionDriver.Begin(motionData, Vector3.forward);
        motionDriver.SetNormalizedTime(1f);
        motionDriver.PhysicsUpdate();

        Assert.That(
            Quaternion.Angle(rigidbody.rotation, originalRotation),
            Is.EqualTo(0f).Within(0.001f));
    }

    /// <summary>
    /// 通过真实 SerializedProperty 填充内嵌数据，避免为运行时数据类增加仅供测试使用的 setter。
    /// </summary>
    private AnimationMotionData ConfigureMotionData(
        AnimationCurve speedCurve,
        float bakedDuration,
        AnimationCurve rotationCurve = null)
    {
        SerializedObject serializedOwner = new SerializedObject(motionOwner);
        SerializedProperty motionData = serializedOwner.FindProperty("motionData");
        motionData.FindPropertyRelative("speedCurve").animationCurveValue =
            speedCurve;
        motionData.FindPropertyRelative("rotationCurve").animationCurveValue =
            rotationCurve ?? AnimationCurve.Constant(0f, bakedDuration, 0f);
        motionData.FindPropertyRelative("bakedDuration").floatValue =
            bakedDuration;
        serializedOwner.ApplyModifiedPropertiesWithoutUndo();
        return motionOwner.MotionData;
    }
}
