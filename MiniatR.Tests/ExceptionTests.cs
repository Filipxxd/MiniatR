using AwesomeAssertions;
using MiniatR.Exceptions;
using MiniatR.Tests.Fixtures;

namespace MiniatR.Tests;

public sealed class ExceptionTests
{
    [Fact]
    public void DuplicateHandlerException_SingleDuplicate_FormatsMessageCorrectly()
    {
        var duplicates = new List<DuplicateRegistration>
        {
            new(typeof(GetUserQuery), [typeof(GetUserQueryHandler), typeof(GetUserQueryHandler)])
        };

        var ex = new DuplicateHandlerException(duplicates);

        ex.Duplicates.Should().HaveCount(1);
        ex.Message.Should().Contain("1 request type(s)");
        ex.Message.Should().Contain("GetUserQuery");
        ex.Message.Should().Contain("GetUserQueryHandler");
    }

    [Fact]
    public void DuplicateHandlerException_MultipleDuplicates_ReportsAll()
    {
        var duplicates = new List<DuplicateRegistration>
        {
            new(typeof(GetUserQuery), [typeof(GetUserQueryHandler), typeof(GetUserQueryHandler)]),
            new(typeof(DeleteUserCommand), [typeof(DeleteUserCommandHandler), typeof(DeleteUserCommandHandler)])
        };

        var ex = new DuplicateHandlerException(duplicates);

        ex.Duplicates.Should().HaveCount(2);
        ex.Message.Should().Contain("2 request type(s)");
        ex.Message.Should().Contain("GetUserQuery");
        ex.Message.Should().Contain("DeleteUserCommand");
    }

    [Fact]
    public void DuplicateRegistration_RecordEquality_WorksCorrectly()
    {
        var handlers = new List<Type> { typeof(GetUserQueryHandler) };
        var reg1 = new DuplicateRegistration(typeof(GetUserQuery), handlers);
        var reg2 = new DuplicateRegistration(typeof(GetUserQuery), handlers);

        reg1.Should().Be(reg2);
        reg1.RequestType.Should().Be(typeof(GetUserQuery));
        reg1.HandlerTypes.Should().ContainSingle();
    }

    [Fact]
    public void HandlerNotFoundException_WithResponseType_FormatsMessageCorrectly()
    {
        var ex = new HandlerNotFoundException(typeof(GetUserQuery), typeof(UserResponse));

        ex.RequestType.Should().Be(typeof(GetUserQuery));
        ex.ResponseType.Should().Be(typeof(UserResponse));
        ex.Message.Should().Contain("GetUserQuery");
        ex.Message.Should().Contain("UserResponse");
        ex.Message.Should().Contain("IRequestHandler<GetUserQuery, UserResponse>");
    }

    [Fact]
    public void HandlerNotFoundException_WithNullResponseType_FormatsVoidMessage()
    {
        var ex = new HandlerNotFoundException(typeof(DeleteUserCommand), null);

        ex.RequestType.Should().Be(typeof(DeleteUserCommand));
        ex.ResponseType.Should().BeNull();
        ex.Message.Should().Contain("IRequestHandler<DeleteUserCommand>");
        ex.Message.Should().NotContain("IRequestHandler<DeleteUserCommand,");
    }

    [Fact]
    public void HandlerNotFoundException_WithVoidResponseType_FormatsVoidMessage()
    {
        var ex = new HandlerNotFoundException(typeof(DeleteUserCommand), typeof(void));

        ex.Message.Should().Contain("IRequestHandler<DeleteUserCommand>");
        ex.Message.Should().NotContain("IRequestHandler<DeleteUserCommand,");
    }

    [Fact]
    public void HandlerNotFoundException_WithGenericRequestType_FormatsGenericName()
    {
        var genericType = typeof(List<string>);
        var ex = new HandlerNotFoundException(genericType, typeof(string));

        ex.Message.Should().Contain("System.Collections.Generic.List<System.String>");
    }

    [Fact]
    public void HandlerNotFoundException_WithNestedGenericType_FormatsCorrectly()
    {
        var nestedGenericType = typeof(Dictionary<string, List<int>>);
        var ex = new HandlerNotFoundException(nestedGenericType, typeof(string));

        ex.Message.Should().Contain("Dictionary<System.String, System.Collections.Generic.List<System.Int32>>");
    }
}
