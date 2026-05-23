using AwesomeAssertions;
using MiniatR;

namespace MiniatR.Tests;

public sealed class NothingTests
{
    [Fact]
    public void Equality_AllInstancesAreEqual()
    {
        var a = Nothing.Value;
        var b = new Nothing();

        a.Equals(b).Should().BeTrue();
        (a == b).Should().BeTrue();
        (a != b).Should().BeFalse();
    }

    [Fact]
    public void GetHashCode_ReturnsZero()
        => Nothing.Value.GetHashCode().Should().Be(0);

    [Fact]
    public void ToString_ReturnsEmpty()
        => Nothing.Value.ToString().Should().BeEmpty();

    [Fact]
    public async Task Task_ReturnsCachedInstance()
    {
        var task1 = Nothing.Task;
        var task2 = Nothing.Task;

        task1.Should().BeSameAs(task2);
        (await task1).Should().Be(Nothing.Value);
    }
}
