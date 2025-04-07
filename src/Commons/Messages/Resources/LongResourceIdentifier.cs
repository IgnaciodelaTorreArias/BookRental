using Grpc.Core;

namespace Commons.Messages.Resources;

public partial class LongResourceIdentifier
{
    public void Validate()
    {
        if (Identifier < 1)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "`Id` cannot be less than 1"));
    }
}
