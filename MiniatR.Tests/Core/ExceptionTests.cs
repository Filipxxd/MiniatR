using AwesomeAssertions;
using MiniatR.Abstractions.Exceptions;
using MiniatR.Tests.Fixtures;

namespace MiniatR.Tests.Core;

public sealed class ExceptionTests
{
    [Fact]
    public void HandlerNotFound_ThrowsWithRequestType()
    {
        var exception = new HandlerNotFoundException(typeof(GetUserQuery), typeof(UserResponse));

        exception.RequestType.Should().Be(typeof(GetUserQuery));
    }

    [Fact]
    public void HandlerNotFound_ThrowsWithResponseType()
    {
        var exception = new HandlerNotFoundException(typeof(GetUserQuery), typeof(UserResponse));

        exception.ResponseType.Should().Be(typeof(UserResponse));
    }

    [Fact]
    public void HandlerNotFound_ThrowsWithExpectedHandler()
    {
        var exception = new HandlerNotFoundException(typeof(GetUserQuery), typeof(UserResponse));

        exception.Message.Should().Contain("GetUserQuery");
        exception.Message.Should().Contain("IRequestHandler<GetUserQuery, UserResponse>");
    }

    [Fact]
    public void HandlerNotFound_VoidRequest_ShowsCorrectMessage()
    {
        var exception = new HandlerNotFoundException(typeof(DeleteUserCommand), null);

        exception.Message.Should().Contain("DeleteUserCommand");
        exception.Message.Should().Contain("IRequestHandler<DeleteUserCommand>");
        exception.Message.Should().NotContain("IRequestHandler<DeleteUserCommand,");
    }

    [Fact]
    public void DuplicateHandler_ThrowsOnRegistration()
    {
        var handlerTypes = new[] { typeof(GetUserQueryHandler), typeof(GetUserQueryHandler) };

        var exception = new DuplicateHandlerException(typeof(GetUserQuery), handlerTypes);

        exception.RequestType.Should().Be(typeof(GetUserQuery));
    }

    [Fact]
    public void DuplicateHandler_ShowsBothHandlerTypes()
    {
        var handlerTypes = new[] { typeof(GetUserQueryHandler), typeof(GetUserQueryHandler) };

        var exception = new DuplicateHandlerException(typeof(GetUserQuery), handlerTypes);

        exception.HandlerTypes.Should().HaveCount(2);
        exception.Message.Should().Contain("GetUserQueryHandler");
    }
}
