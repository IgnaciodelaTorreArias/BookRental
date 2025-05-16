using APIGateway.Extensions;
using Grpc.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace APIGateway.Filters;

public class ExceptionFilter(ILogger<ExceptionFilter> logger) : IExceptionFilter
{
    private readonly ILogger<ExceptionFilter> _logger = logger;

    public void OnException(ExceptionContext context)
    {
        context.ExceptionHandled = true;
        if (context.Exception is RpcException exception)
        {
            context.Result = exception.ToActionResult();
            return;
        }
        _logger.LogError("An  error occurred: {@Error}", new
        {
            Event = context.Exception.GetType().Name,
            context.HttpContext,
            context.RouteData,
            context.Exception.Message
        });
        context.Result = new ObjectResult("An unexpected error occurred") { StatusCode = 500 };
    }
}
