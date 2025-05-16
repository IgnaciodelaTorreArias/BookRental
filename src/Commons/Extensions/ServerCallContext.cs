using Grpc.Core;
using System.Security.Claims;

namespace Commons.Extensions;

public static class ServerCallContextExtension
{
    public static int GetUserId(this ServerCallContext context)
    {
        Claim? id = context.GetHttpContext().User.FindFirst(ClaimTypes.NameIdentifier);
        if (id == null)
            throw new RpcException(new Status(StatusCode.Unauthenticated, "User is not authenticated"));
        return int.Parse(id.Value);
    }
    public static string GetUserRole(this ServerCallContext context)
    {
        Claim? role = context.GetHttpContext().User.FindFirst(ClaimTypes.Role);
        if (role == null)
            throw new RpcException(new Status(StatusCode.Unauthenticated, "User is not authenticated"));
        return role.Value;
    }
}
