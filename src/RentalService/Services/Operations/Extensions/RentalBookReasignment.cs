using Grpc.Core;

namespace RentalService.Services.Operations;

public partial class RentalBookReasignment
{
    public void Validate()
    {
        if (RentalId < 1)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "`RentalId` cannot be less than 1"));
        if (HasNewCopy && NewCopy < 1)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "`NewCopy` cannot be less than 1"));
    }
}
