namespace MiniatR;

public interface IRequest<out TResponse> { }

public interface IRequest : IRequest<Nothing> { }
