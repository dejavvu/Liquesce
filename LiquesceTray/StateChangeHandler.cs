﻿#region Copyright (C)
// ---------------------------------------------------------------------------------------------------------------
//  <copyright file="StateChangeHandler.cs" company="Smurf-IV">
// 
//  Copyright (C) 2010-2011 Smurf-IV
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
#endregion
using System;
using System.ServiceModel;
using LiquesceFacade;
using NLog;

namespace LiquesceTray
{
   public class StateChangeHandler : LiquesceCallbackSvcRef.ILiquesceCallBackCallback
   {
      private static readonly Logger Log = LogManager.GetCurrentClassLogger();
      private LiquesceCallbackSvcRef.LiquesceCallBackClient client;
      private readonly Guid guid = Guid.NewGuid();

      public delegate void SetStateDelegate(LiquesceSvcState state, string text);
      private SetStateDelegate setStateDelegate;

      public void CreateCallBack( SetStateDelegate newDelegate)
      {
         try
         {
            InstanceContext context = new InstanceContext(this);
            client = new LiquesceCallbackSvcRef.LiquesceCallBackClient(context);
            client.Subscribe(guid);
            setStateDelegate = newDelegate;
         }
         catch (Exception ex)
         {
            Log.ErrorException("CreateCallBack:", ex);
            Update(LiquesceSvcState.InError, ex.Message);
            client = null;
            setStateDelegate = null;
         }
      }

      public void RemoveCallback()
      {
         try
         {
            if (client != null)
            {
               client.Unsubscribe(guid);
            }
         }
         catch (Exception ex)
         {
            Log.ErrorException("RemoveCallback:", ex);
            Update(LiquesceSvcState.InError, ex.Message);
         }
         finally
         {
            client = null;
            setStateDelegate = null;
         }
      }

      #region Implementation of ILiquesceCallback

      public void Update(LiquesceSvcState state, string message)
      {
         SetStateDelegate handler = setStateDelegate;
         if (handler != null)
            handler(state, message);
      }

      #endregion
   }
}
