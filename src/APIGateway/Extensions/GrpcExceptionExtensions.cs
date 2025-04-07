using Grpc.Core;
using Microsoft.AspNetCore.Mvc;

namespace APIGateway.Extensions;

public static class GrpcExceptionExtensions
{
    public static ActionResult ToActionResult(this RpcException ex)
    {
        return ex.StatusCode switch
        {
            StatusCode.NotFound => new NotFoundObjectResult(ex.Status.Detail),
            StatusCode.Unavailable => new ObjectResult(ex.Status.Detail) { StatusCode = 503 },
            StatusCode.InvalidArgument => new ObjectResult(ex.Status.Detail) { StatusCode = 400 },
            StatusCode.FailedPrecondition => new ObjectResult(ex.Status.Detail) { StatusCode = 409 },
            _ => new ObjectResult(ex.Status.Detail) { StatusCode = 500 }
        };
    }
}
