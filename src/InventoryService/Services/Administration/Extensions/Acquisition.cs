using Grpc.Core;

namespace InventoryService.Services.Administration;

public partial class Acquisition
{
    public void Validate()
    {
        if (HasAcquisitionId && AcquisitionId < 1)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "`AcquisitionId` cannot be less than 1"));
        if (HasBookId && BookId < 1)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "`BookId` cannot be less than 1"));
        if (HasQuantity && Quantity < 1)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "`Quantity` cannot be less than 1"));
        if (HasAcquisitionDate && (AcquisitionDate < DateOnly.MinValue.DayNumber || AcquisitionDate > DateOnly.MaxValue.DayNumber))
            throw new RpcException(new Status(StatusCode.InvalidArgument, $"`AcquisitionDate` has an invalid value, valid range is [{DateOnly.MinValue.DayNumber}, {DateOnly.MaxValue.DayNumber}]"));
    }
}
