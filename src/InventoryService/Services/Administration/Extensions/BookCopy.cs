﻿using Grpc.Core;

namespace InventoryService.Services.Administration;

public partial class BookCopy
{
    public void Validate()
    {
        if (CopyId < 1)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "`CopyId` cannot be less than 1"));
    }
}