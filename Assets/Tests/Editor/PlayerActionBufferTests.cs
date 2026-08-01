using NUnit.Framework;

/// <summary>
/// PlayerActionBuffer 的纯数据测试，全部使用固定时刻传参，不依赖真实 Time.time。
/// </summary>
public class PlayerActionBufferTests
{
    private const float ValidDuration = 0.2f;

    private PlayerActionBuffer actionBuffer;

    [SetUp]
    public void SetUp()
    {
        actionBuffer = new PlayerActionBuffer();
    }

    [Test]
    public void RecordedActionIsConsumableWithinValidDuration()
    {
        actionBuffer.Record(PlayerActionType.Attack, 10f);

        Assert.That(
            actionBuffer.TryConsume(PlayerActionType.Attack, 10.1f, ValidDuration),
            Is.True);
    }

    [Test]
    public void RecordedActionIsConsumableAtExactExpiryBoundary()
    {
        // 10 与 0.25 都能被 float 精确表示，两者之差严格等于有效期，边界不受浮点误差干扰。
        const float exactDuration = 0.25f;
        actionBuffer.Record(PlayerActionType.Attack, 10f);

        // 等待时长恰好等于有效期仍视为有效，只有严格超出才失效。
        Assert.That(
            actionBuffer.TryConsume(PlayerActionType.Attack, 10.25f, exactDuration),
            Is.True);
    }

    [Test]
    public void ConsumedActionCannotBeConsumedTwice()
    {
        actionBuffer.Record(PlayerActionType.Attack, 10f);

        Assert.That(
            actionBuffer.TryConsume(PlayerActionType.Attack, 10.1f, ValidDuration),
            Is.True);
        Assert.That(
            actionBuffer.TryConsume(PlayerActionType.Attack, 10.1f, ValidDuration),
            Is.False);
    }

    [Test]
    public void ExpiredActionFailsAndIsRemoved()
    {
        actionBuffer.Record(PlayerActionType.Attack, 10f);

        Assert.That(
            actionBuffer.TryConsume(PlayerActionType.Attack, 10.5f, ValidDuration),
            Is.False);
        // 过期记录必须已被删除，不能在稍后回到有效区间时重新生效。
        Assert.That(
            actionBuffer.TryConsume(PlayerActionType.Attack, 10.1f, ValidDuration),
            Is.False);
    }

    [Test]
    public void MissingRecordFailsToConsume()
    {
        Assert.That(
            actionBuffer.TryConsume(PlayerActionType.Attack, 10f, ValidDuration),
            Is.False);
    }

    [Test]
    public void RecordOverwritesPreviousTimeForSameAction()
    {
        actionBuffer.Record(PlayerActionType.Attack, 10f);
        actionBuffer.Record(PlayerActionType.Attack, 10.4f);

        // 若仍保留旧的 10f，则相对 10.5f 已过期；覆盖成功才能在此刻消费。
        Assert.That(
            actionBuffer.TryConsume(PlayerActionType.Attack, 10.5f, ValidDuration),
            Is.True);
    }

    [Test]
    public void ClearRemovesOnlyRequestedAction()
    {
        // 首期枚举只有 Attack，用未定义值模拟第二种动作，验证按键分槽而非整体清空。
        PlayerActionType otherAction = (PlayerActionType)1;
        actionBuffer.Record(PlayerActionType.Attack, 10f);
        actionBuffer.Record(otherAction, 10f);

        actionBuffer.Clear(PlayerActionType.Attack);

        Assert.That(
            actionBuffer.TryConsume(PlayerActionType.Attack, 10.1f, ValidDuration),
            Is.False);
        Assert.That(
            actionBuffer.TryConsume(otherAction, 10.1f, ValidDuration),
            Is.True);
    }

    [Test]
    public void ClearAllRemovesEveryAction()
    {
        PlayerActionType otherAction = (PlayerActionType)1;
        actionBuffer.Record(PlayerActionType.Attack, 10f);
        actionBuffer.Record(otherAction, 10f);

        actionBuffer.ClearAll();

        Assert.That(
            actionBuffer.TryConsume(PlayerActionType.Attack, 10.1f, ValidDuration),
            Is.False);
        Assert.That(
            actionBuffer.TryConsume(otherAction, 10.1f, ValidDuration),
            Is.False);
    }
}
