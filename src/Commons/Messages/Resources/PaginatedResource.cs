using Grpc.Core;

namespace Commons.Messages.Resources;

public partial class PaginatedResource
{
    public int Offset => (int)(Page * Size);
    public int Limit => (int)Size;

    public void Validate()
    {
        if (Size < 5 || Size > 50)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "`Size` should be in a a range [5,50]"));
        if (HasIdentifier && Identifier < 1)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "`Id` cannot be less than 1"));
    }
}
