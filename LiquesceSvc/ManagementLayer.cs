﻿#region Copyright (C)

// ---------------------------------------------------------------------------------------------------------------
//  <copyright file="ManagementLayer.cs" company="Smurf-IV">
//
//  Copyright (C) 2010-2014 Simon Coghlan (Aka Smurf-IV)
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 2 of the License, or
//   any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program. If not, see http://www.gnu.org/licenses/.
//  </copyright>
//  <summary>
//  Url: http://Liquesce.codeplex.com/
//  Email: http://www.codeplex.com/site/users/view/smurfiv
//  </summary>
// --------------------------------------------------------------------------------------------------------------------

#endregion Copyright (C)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.ServiceModel;
using System.Threading;

using CallbackFS;
using CBFS;
using LiquesceFacade;
using Microsoft.Win32;
using NLog;
using NLog.Config;

namespace LiquesceSvc
{
   internal class ManagementLayer
   {
      static private readonly Logger Log = LogManager.GetCurrentClassLogger();
      private static ManagementLayer instance;
      private ConfigDetails currentConfigDetails;
      private readonly DateTime startTime;
      private LiquesceSvcState state = LiquesceSvcState.Stopped;
      private static readonly Dictionary<Client, IStateChange> subscribers = new Dictionary<Client, IStateChange>();
      private static readonly ReaderWriterLockSlim subscribersLock = new ReaderWriterLockSlim();

      /// <summary>
      /// Returns "The single instance" of this singleton class.
      /// </summary>
      public static ManagementLayer Instance
      {
         get { return instance ?? (instance = new ManagementLayer()); }
      }

      /// <summary>
      /// Private constructor to prevent multiple instances
      /// </summary>
      private ManagementLayer()
      {
         try
         {
            Log.Debug("New ManagementLayer created.");
            startTime = DateTime.UtcNow;
         }
         catch (Exception ex)
         {
            Log.ErrorException("Constructor blew: ", ex);
         }
      }

      public void Subscribe(Client id)
      {
         try
         {
            IStateChange callback = OperationContext.Current.GetCallbackChannel<IStateChange>();
            using (subscribersLock.WriteLock())
               subscribers.Add(id, callback);
         }
         catch (Exception ex)
         {
            Log.ErrorException("Subscribe", ex);
         }
      }

      public void Unsubscribe(Client id)
      {
         try
         {
            using (subscribersLock.UpgradableReadLock())
            {
               IEnumerable<Client> query = from c in subscribers.Keys
                                           where c.id == id.id
                                           select c;
               using (subscribersLock.WriteLock())
                  subscribers.Remove(query.First());
            }
         }
         catch (Exception ex)
         {
            Log.ErrorException("Unsubscribe", ex);
         }
      }

      private readonly List<LiquesceOps> liquesceOperations = new List<LiquesceOps>();

      /// <summary>
      /// Mount a drive.
      /// </summary>
      /// <returns></returns>
      public void Start(object obj)
      {
         try
         {
            TimeSpan delayStart = DateTime.UtcNow - startTime;
            int repeatWait = 0;
            while (IsRunning
               && (repeatWait++ < 100)
               )
            {
               Log.Warn("Last CBFS is still running");
               Thread.Sleep(250);
            }
            if (!IsRunning)
            {
               if (currentConfigDetails == null)
               {
                  new DealWithTheCfgChanging().ReadConfigDetails(ref currentConfigDetails);
               }
               FireStateChange(LiquesceSvcState.Unknown, "Starting up");
               if (currentConfigDetails == null)
               {
                  Log.Fatal("Unable to read the config details to allow this service to run. Will now exit");
                  Environment.Exit(-1);
                  // return;
               }
               string friendlyName = string.Empty;
               try
               {
                  RegistryKey rk = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
                  if (rk != null)
                  {
                     friendlyName = rk.GetValue("ProductName").ToString();
                  }
               }
               catch { }
               Log.Info("OSVersion [{0}], OSFriendlyName [{1}] Is64BitOperatingSystem [{2}]", 
                  Environment.OSVersion.VersionString, friendlyName, Environment.Is64BitOperatingSystem);
               Log.Info("ProcessorCount [{0}] Is64BitProcess [{1}] CLR version [{2}]",
                  Environment.ProcessorCount, Environment.Is64BitProcess, Environment.Version);
               Log.Fatal(currentConfigDetails.ToString());
               SetNLogLevel(currentConfigDetails.ServiceLogLevel);

               // Sometimes the math gets all confused due to the casting !!
               int delayStartMilliseconds = (int)(currentConfigDetails.DelayStartMilliSec - delayStart.Milliseconds);
               if ((delayStartMilliseconds > 0)
                   && (delayStartMilliseconds < UInt16.MaxValue)
                  )
               {
                  Log.Info("Delay Start needs to be obeyed");
                  Thread.Sleep(delayStartMilliseconds);
               }

               if (!CBFSHandlers.CheckStatus(ConfigDetails.ProductNameCBFS))
               {
                  FireStateChange(LiquesceSvcState.InError, "Driver is not mounted - Driver Something is wrong");
#if DEBUG
                  Log.Fatal("Build the mounter application and change it's private string mGuid = Guid to be LiquesceSvc");
                  Log.Fatal("Run it as \"Administrator\" and install the driver");
                  Log.Fatal("Press \"Install\" -> find the cab file -> answer the trust question");
                  Log.Fatal("Wait about 30 seconds and it will then confirm the version number of the cab file selected");
#endif
                  Environment.Exit(-4); // Driver something wrong
                  // return;
               }

               foreach (MountDetail mountDetail in currentConfigDetails.MountDetails)
               {
                  LiquesceOps liquesceOps = new LiquesceOps(mountDetail, currentConfigDetails.CacheLifetimeSeconds);
                  liquesceOperations.Add(liquesceOps);

                  liquesceOps.RegisterAndInit(Properties.Settings.Default.Salt, ConfigDetails.ProductNameCBFS, currentConfigDetails.ThreadCount,
                      CbFsStorageType.stDisk, false);
                  try
                  {
                     // Attempt to remove a drive that may have been zombied by a crash etc.
                     // https://www.eldos.com/forum/read.php?FID=8&TID=747
                     // If the following blows, it means that you might be using the vshost to debug
                     liquesceOps.AddMountingPoint(mountDetail.DriveLetter, CallbackFileSystem.CBFS_SYMLINK_MOUNT_MANAGER, 0);
                     liquesceOps.DeleteMountingPoint();
                     liquesceOps.DeleteStorage(true);
                     liquesceOps.CreateStorage(CbFsStorageType.stDisk, currentConfigDetails.ThreadCount, currentConfigDetails.UseInternalDriverCaches, "Liquesce.ico");
                  }
                  catch (Exception ex)
                  {
                     Log.FatalException("Attempt to remove a drive that may have been zombied by a crash etc.", ex);
                  }

                  // Now get the drive letter ready
                  liquesceOps.AddMountingPoint(mountDetail.DriveLetter, CallbackFileSystem.CBFS_SYMLINK_MOUNT_MANAGER, 0);
                  ulong freeBytesAvailable;
                  ulong totalBytes;
                  ulong totalFreeBytes;
                  liquesceOps.GetDiskFreeSpace(out freeBytesAvailable, out totalBytes, out totalFreeBytes);

                  DirectoryInfo dir = new DirectoryInfo(mountDetail.DriveLetter);
                  // TODO: Search all usages of the DriveLetter and make sure they become MountPoint compatible
                  if (mountDetail.DriveLetter.Length > 1)
                  {
                     if (dir.Exists)
                     {
                        Log.Warn("Removing directory [{0}]", dir.FullName);
                        dir.Delete(true);
                     }
                     Log.Warn("Recreate the directory [{0}]", dir.FullName);
                     dir.Create();
                  }

                  ThreadPool.QueueUserWorkItem(liquesceOps.InitialiseShares);

                  FireStateChange(LiquesceSvcState.Running, "Liquesce initialised");
                  IsRunning = true;

                  // now mount and this will launch the callbacks
#if DEBUG
                  const int ApiTimeout = 0; // This means no timeout, usefull for debugging
#else
               const int ApiTimeout = 32000; // Default to TCP timout of 32 seconds
#endif
                  liquesceOps.MountMedia(ApiTimeout);
               }
            }
            else
            {
               FireStateChange(LiquesceSvcState.InError, "Seems like the last exit request did not exit in time");
               Environment.Exit(-7);
            }
         }
         catch (Exception ex)
         {
            Log.ErrorException("Start has failed in an uncontrolled way: ", ex);
            Environment.Exit(-8);
         }
      }

      private void SetNLogLevel(string serviceLogLevel)
      {
         LoggingConfiguration currentConfig = LogManager.Configuration;
         foreach (LoggingRule rule in currentConfig.LoggingRules)
         {
            // Turn on in order
            switch (serviceLogLevel)
            {
               case "Trace":
                  rule.EnableLoggingForLevel(LogLevel.Trace);
                  goto case "Debug"; // Drop through
               default:
               case "Debug":
                  rule.EnableLoggingForLevel(LogLevel.Debug);
                  goto case "Info"; // Drop through
               case "Info":
                  rule.EnableLoggingForLevel(LogLevel.Info);
                  goto case "Warn"; // Drop through
               case "Warn":
                  rule.EnableLoggingForLevel(LogLevel.Warn);
                  goto case "Error"; // Drop through
               case "Error":
                  rule.EnableLoggingForLevel(LogLevel.Error);
                  goto case "Fatal"; // Drop through
               case "Fatal":
                  rule.EnableLoggingForLevel(LogLevel.Fatal);
                  break;
               //case "Off":
               //   rule.EnableLoggingForLevel(LogLevel.Off);
               //   break;
            }
            // Turn off the rest
            switch (serviceLogLevel)
            {
               // rule.DisableLoggingForLevel(LogLevel.Off);
               case "Off":
                  rule.DisableLoggingForLevel(LogLevel.Fatal);
                  goto case "Fatal";
               case "Fatal":
                  rule.DisableLoggingForLevel(LogLevel.Error);
                  goto case "Error";
               case "Error":
                  rule.DisableLoggingForLevel(LogLevel.Warn);
                  goto case "Warn";
               case "Warn":
                  rule.DisableLoggingForLevel(LogLevel.Info);
                  goto case "Info";
               case "Info":
                  rule.DisableLoggingForLevel(LogLevel.Debug);
                  goto case "Debug";
               case "Debug":
                  rule.DisableLoggingForLevel(LogLevel.Trace);
                  break;

               case "Trace":
                  // Prevent turning off again !
                  break;
            }
         }
         LogManager.ReconfigExistingLoggers();
         Log.Fatal("Test @ [{0}]", serviceLogLevel);
         Log.Error("Test @ [{0}]", serviceLogLevel);
         Log.Warn("Test @ [{0}]", serviceLogLevel);
         Log.Info("Test @ [{0}]", serviceLogLevel);
         Log.Debug("Test @ [{0}]", serviceLogLevel);
         Log.Trace("Test @ [{0}]", serviceLogLevel);
      }

      private bool IsRunning { get; set; }

      public ConfigDetails CurrentConfigDetails
      {
         get { return currentConfigDetails; }
         set
         {
            currentConfigDetails = value;
            // I know.. Bad form calling a function in a setter !
            currentConfigDetails.WriteOutConfigDetails();
         }
      }

      internal void FireStateChange(LiquesceSvcState newState, string message)
      {
         try
         {
            state = newState;
            Log.Info("Changing newState to [{0}]:[{1}]", newState, message);
            using (subscribersLock.ReadLock())
            {
               // Get all the clients in dictionary
               IStateChange[] query = (from c in subscribers
                                       select c.Value).ToArray();
               // Create the callback action
               Type type = typeof(IStateChange);
               MethodInfo methodInfo = type.GetMethod("Update");

               // For each connected client, invoke the callback
               foreach (IStateChange stateChange in query)
               {
                  try
                  {
                     methodInfo.Invoke(stateChange, new object[] { newState, message });
                  }
                  catch
                  {
                  }
               }
            }
         }
         catch (Exception ex)
         {
            Log.ErrorException("Unable to fire state change", ex);
         }
      }

      public LiquesceSvcState State
      {
         get { return state; }
      }

      public void Stop()
      {
         foreach (LiquesceOps liquesceOps in liquesceOperations)
         {
            try
            {
               FireStateChange(LiquesceSvcState.Unknown, "Stop has been requested");
               liquesceOps.DeleteMountingPoint();
               liquesceOps.UnmountMedia();
            }
            finally
            {
               liquesceOps.DeleteStorage(true);
            }
         }
         FireStateChange(LiquesceSvcState.Stopped, "Stop has exited");

         Log.Info("Stopped OUT");
      }
   }
}