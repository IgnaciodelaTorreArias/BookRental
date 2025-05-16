using Grpc.Core;

using Inventory.DBContext.Models;

using AServices = Inventory.Public.Services.Administration;

namespace Inventory.Public.DBContext;

public static class AcquisitionExtension
{
    public static Acquisition.AcquisitionStatus ConvertMessageStatus(AServices.AcquisitionStatus status) => status switch
    {
        AServices.AcquisitionStatus.Unconfirmed => Acquisition.AcquisitionStatus.Unconfirmed,
        AServices.AcquisitionStatus.Confirmed => Acquisition.AcquisitionStatus.Confirmed,
        AServices.AcquisitionStatus.Error => Acquisition.AcquisitionStatus.Error,
        _ => throw new NotImplementedException()
    };

    public static AServices.AcquisitionStatus ConvertToMessageStatus(Acquisition.AcquisitionStatus status) => status switch
    {
        Acquisition.AcquisitionStatus.Unconfirmed => AServices.AcquisitionStatus.Unconfirmed,
        Acquisition.AcquisitionStatus.Confirmed => AServices.AcquisitionStatus.Confirmed,
        Acquisition.AcquisitionStatus.Error => AServices.AcquisitionStatus.Error,
        _ => throw new NotImplementedException()
    };

    public static AServices.Acquisition Message(this Acquisition acquisition) => new()
    {
        AcquisitionId = (uint)acquisition.AcquisitionId,
        BookId = (uint)acquisition.BookId,
        AcquisitionPrice = (uint)acquisition.AcquisitionPrice,
        Quantity = (uint)acquisition.Quantity,
        AcquisitionDate = (uint)acquisition.AcquisitionDate.DayNumber,
        Status = ConvertToMessageStatus(acquisition.Status),
        CopyIdStart = (uint)acquisition.CopyIdStart
    };

    public static void FromMessage(this Acquisition acquisition, AServices.Acquisition source)
    {
        if (!source.HasBookId)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "`BookId` is required for this operation"));
        if (!source.HasAcquisitionPrice)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "`AcquisitionPrice` is required for this operation"));
        if (!source.HasQuantity)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "`Quantity` is required for this operation"));
        acquisition.BookId = (int)source.BookId;
        acquisition.AcquisitionPrice = (int)source.AcquisitionPrice;
        acquisition.Quantity = (int)source.Quantity;
        if (source.HasAcquisitionDate)
            acquisition.AcquisitionDate = DateOnly.FromDayNumber((int)source.AcquisitionDate);
    }

    public static void Update(this Acquisition acquisition, AServices.Acquisition source)
    {
        if (source.HasAcquisitionPrice && source.AcquisitionPrice == acquisition.AcquisitionPrice)
            source.ClearAcquisitionPrice();
        if (source.HasQuantity && source.Quantity == acquisition.Quantity)
            source.ClearQuantity();
        if (source.HasAcquisitionDate && source.AcquisitionDate == acquisition.AcquisitionDate.DayNumber)
            source.ClearAcquisitionDate();
        if (source.HasStatus && source.Status == ConvertToMessageStatus(acquisition.Status))
            source.ClearStatus();

        if (source.HasAcquisitionPrice)
            acquisition.AcquisitionPrice = (int)source.AcquisitionPrice;
        if (source.HasAcquisitionDate)
            acquisition.AcquisitionDate = DateOnly.FromDayNumber((int)source.AcquisitionDate);
        if (source.HasStatus)
        {
            if (source.Status == AServices.AcquisitionStatus.Unconfirmed)
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Cannot change acquisition status to `Unconfirmed`"));
            if (acquisition.Status == Acquisition.AcquisitionStatus.Confirmed || acquisition.Status == Acquisition.AcquisitionStatus.Error)
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Cannot change acquisition status, status is already `Confirmed` or `Error`"));
            acquisition.Status = ConvertMessageStatus(source.Status);
        }
    }
}