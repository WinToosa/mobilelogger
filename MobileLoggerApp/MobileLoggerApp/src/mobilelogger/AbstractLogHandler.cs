﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MobileLoggerApp.src.mobilelogger
{

    [Obsolete("Need to refactor this class to produce events that are saved into the DB, sending is handled by the MessagingService")]
    public abstract class AbstractLogHandler
    {
        
        public abstract Boolean SendData();


        public abstract void HandleSendError();

    }
}