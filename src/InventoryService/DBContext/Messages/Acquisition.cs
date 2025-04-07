using Grpc.Core;

using AServices = InventoryService.Services.Administration;

namespace InventoryService.DBContext.Models;

public partial class Acquisition
{
    public enum AcquisitionStatus
    {
        Unconfirmed,
        Confirmed,
        Error
    }

    public static AcquisitionStatus ConvertMessageStatus(AServices.AcquisitionStatus status) => status switch
    {
        AServices.AcquisitionStatus.Unconfirmed => AcquisitionStatus.Unconfirmed,
        AServices.AcquisitionStatus.Confirmed => AcquisitionStatus.Confirmed,
        AServices.AcquisitionStatus.Error => AcquisitionStatus.Error,
        _ => throw new NotImplementedException()
    };

    public static AServices.AcquisitionStatus ConvertToMessageStatus(AcquisitionStatus status) => status switch
    {
        AcquisitionStatus.Unconfirmed => AServices.AcquisitionStatus.Unconfirmed,
        AcquisitionStatus.Confirmed => AServices.AcquisitionStatus.Confirmed,
        AcquisitionStatus.Error => AServices.AcquisitionStatus.Error,
        _ => throw new NotImplementedException()
    };

    public AServices.Acquisition Message() => new()
    {
        AcquisitionId = (uint)AcquisitionId,
        BookId = (uint)BookId,
        AcquisitionPrice = (uint)AcquisitionPrice,
        Quantity = (uint)Quantity,
        AcquisitionDate = (uint)AcquisitionDate.DayNumber,
        Status = ConvertToMessageStatus(Status),
        CopyIdStart = (uint)CopyIdStart
    };

    public void FromMessage(AServices.Acquisition source)
    {
        if (!source.HasBookId)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "`BookId` is required for this operation"));
        if (!source.HasAcquisitionPrice)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "`AcquisitionPrice` is required for this operation"));
        if (!source.HasQuantity)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "`Quantity` is required for this operation"));
        BookId = (int)source.BookId;
        AcquisitionPrice = (int)source.AcquisitionPrice;
        Quantity = (int)source.Quantity;
        if (source.HasAcquisitionDate)
            AcquisitionDate = DateOnly.FromDayNumber((int)source.AcquisitionDate);
    }

    public void Update(AServices.Acquisition source)
    {
        if (source.HasAcquisitionPrice && source.AcquisitionPrice == AcquisitionPrice)
            source.ClearAcquisitionPrice();
        if (source.HasQuantity && source.Quantity == Quantity)
            source.ClearQuantity();
        if (source.HasAcquisitionDate && source.AcquisitionDate == AcquisitionDate.DayNumber)
            source.ClearAcquisitionDate();
        if (source.HasStatus && source.Status == ConvertToMessageStatus(Status))
            source.ClearStatus();

        if (source.HasAcquisitionPrice)
            AcquisitionPrice = (int)source.AcquisitionPrice;
        if (source.HasAcquisitionDate)
            AcquisitionDate = DateOnly.FromDayNumber((int)source.AcquisitionDate);
        if (source.HasStatus)
        {
            if (source.Status == AServices.AcquisitionStatus.Unconfirmed)
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Cannot change acquisition status to `Unconfirmed`"));
            if (Status == AcquisitionStatus.Confirmed || Status == AcquisitionStatus.Error)
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Cannot change acquisition status, status is already `Confirmed` or `Error`"));
            Status = ConvertMessageStatus(source.Status);
        }
    }
}