namespace MiniatR;

/// <summary>
/// Marker interface for a request that returns a response.
/// </summary>
/// <typeparam name="TResponse">The type of response returned by the handler.</typeparam>
public interface IRequest<out TResponse> { }

/// <summary>
/// Marker interface for a request that does not return a response.
/// </summary>
public interface IRequest : IRequest<Nothing> { }
