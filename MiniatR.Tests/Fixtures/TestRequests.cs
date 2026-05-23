using MiniatR;

namespace MiniatR.Tests.Fixtures;

public sealed record GetUserQuery(Guid Id) : IRequest<UserResponse>;

public sealed record UserResponse(Guid Id, string Name);

public sealed record DeleteUserCommand(Guid Id) : IRequest;

public sealed record SlowQuery(int DelayMs) : IRequest<string>;

public sealed record NullableQuery() : IRequest<string?>;

public sealed record ThrowingQuery() : IRequest<string>;

public sealed record NestedQuery(int Depth) : IRequest<int>;

public interface ICacheableQuery { }

public sealed record CacheableQuery(string Key) : IRequest<string>, ICacheableQuery;

public sealed record NonCacheableQuery(string Key) : IRequest<string>;

public sealed record DependencyQuery(Guid Id) : IRequest<DependencyResponse>;

public sealed record DependencyResponse(Guid Id, string Value);
