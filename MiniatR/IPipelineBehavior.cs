namespace MiniatR;

public delegate Task<TResponse> PipelineDelegate<TResponse>(CancellationToken cancellationToken);

public interface IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    Task<TResponse> Handle(TRequest request, PipelineDelegate<TResponse> next, CancellationToken cancellationToken);
}
