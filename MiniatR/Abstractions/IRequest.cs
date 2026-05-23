namespace MiniatR.Abstractions;

public interface IRequest<out TResponse> { }

public interface IRequest : IRequest<Nothing> { }
