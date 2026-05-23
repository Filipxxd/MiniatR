using AwesomeAssertions;
using MiniatR;

namespace MiniatR.Tests.Core;

public sealed class NothingTests
{
    [Fact]
    public void Nothing_Equality_AllInstancesEqual()
    {
        var void1 = Nothing.Value;
        var void2 = new Nothing();

        void1.Equals(void2).Should().BeTrue();
        (void1 == void2).Should().BeTrue();
        (void1 != void2).Should().BeFalse();
    }

    [Fact]
    public void Nothing_GetHashCode_AlwaysZero()
    {
        var v = Nothing.Value;

        v.GetHashCode().Should().Be(0);
    }

    [Fact]
    public void Nothing_ToString_ReturnsParens()
    {
        var v = Nothing.Value;

        v.ToString().Should().Be("()");
    }

    [Fact]
    public async Task Nothing_Task_ReturnsCachedTask()
    {
        var task1 = Nothing.Task;
        var task2 = Nothing.Task;

        task1.Should().BeSameAs(task2);
        var result = await task1;
        result.Should().Be(Nothing.Value);
    }

    [Fact]
    public void Nothing_CompareTo_ReturnsZero()
    {
        var void1 = Nothing.Value;
        var void2 = new Nothing();

        void1.CompareTo(void2).Should().Be(0);
    }

    [Fact]
    public void Nothing_Equals_Object_ReturnsTrueForNothing()
    {
        var v = Nothing.Value;
        object boxed = new Nothing();

        v.Equals(boxed).Should().BeTrue();
    }

    [Fact]
    public void Nothing_Equals_Object_ReturnsFalseForNonNothing()
    {
        var v = Nothing.Value;
        object notNothing = "not a void";

        v.Equals(notNothing).Should().BeFalse();
    }
}
