﻿using System;
using System.Collections.Generic;
using System.Text;

namespace DomainModels.Types.Messages
{
    [Flags]
    public enum ResponseHint
    {
        OK = 1,
        Retry = 2,
        InProgress = 4,
        CorrectInput = 8,
        InMaintenance = 16,
        SystemError = 32,
        Deprecated = 64,
        Obsoleted = 128,
        Custom = 256,
        UnAuthorized = 512
    }
}
