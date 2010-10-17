﻿using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.Threading;
using LiquesceFaçade;
using NLog;

namespace LiquesceSvc
{
   [ServiceBehavior(
      InstanceContextMode = InstanceContextMode.Single,
           ConcurrencyMode = ConcurrencyMode.Multiple,
           IncludeExceptionDetailInFaults = true
           )
   ]
   public class LiquesceFaçade : ILiquesce
   {
      static private readonly Logger Log = LogManager.GetCurrentClassLogger();

      public LiquesceFaçade()
      {
         Log.Debug("Object Created");
      }

      public void Stop()
      {
         Log.Debug("Calling Stop");
         ManagementLayer.Instance.Stop();
      }

      public void Start()
      {
         Log.Debug("Calling Start");
         // Queue the main work as a thread pool task as we want this method to finish promptly.
         ThreadPool.QueueUserWorkItem(ManagementLayer.Instance.Start);
      }

      public LiquesceSvcState State
      {
         get
         {
            Log.Debug("Calling State");
            return ManagementLayer.Instance.State;
         }
      }

      public List<LanManShareDetails> GetPossibleShares()
      {
          Log.Debug("Calling GetPossibleShares");
          return ManagementLayer.Instance.GetPossibleShares();
      }

      public List<string> GetMirrorDeleteToDo()
      {
          Log.Debug("Calling GetMirrorDeleteToDo");
          return ManagementLayer.Instance.GetMirrorDeleteToDo();
      }

      public ConfigDetails ConfigDetails
      {
         get
         {
            Log.Debug("Calling get_ConfigDetails");
            return ManagementLayer.Instance.CurrentConfigDetails;
         }
         set
         {
            Log.Debug("Calling set_ConfigDetails");
            ManagementLayer.Instance.CurrentConfigDetails = value;
         }
      }

   }
}
