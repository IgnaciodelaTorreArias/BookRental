using Grpc.Core.Interceptors;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace Commons.GrpcInterceptors;

public class ExceptionInterceptor(ILogger<ExceptionInterceptor> logger) : Interceptor
{
    private readonly ILogger<ExceptionInterceptor> _logger = logger;
    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        try
        {
            return await continuation(request, context);
        }
        catch (RpcException)
        {
            throw;
        }
        catch (InvalidOperationException ex)
        {
            // Probably a database down error
            _logger.LogError("An  error occurred: {@Error}", new
            {
                Event = "InvalidOperationException",
                context.Method,
                ex.Message
            });
            throw new RpcException(new Status(StatusCode.Unavailable, "Service Unavailable, try again later, if the problem persists contact an administrator"));
        }
        catch (Exception ex)
        {
            _logger.LogError("An error occurred: {@Error}", new
            {
                Event = "Exception",
                context.Method,
                ex.Message,
                ex.Source,
                ex.StackTrace,
                ex.TargetSite
            });
            throw new RpcException(new Status(StatusCode.Internal, "An unexpected error occurred"));
        }
    }
}
