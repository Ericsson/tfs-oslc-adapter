/*
* Copyright (C) 2015 Ericsson AB. All rights reserved.
*
* Redistribution and use in source and binary forms, with or without
* modification, are permitted provided that the following conditions
* are met:
*
* 1. Redistributions of source code must retain the above copyright
*    notice, this list of conditions and the following disclaimer.
* 2. Redistributions in binary form must reproduce the above copyright
*    notice, this list of conditions and the following disclaimer
*    in the documentation and/or other materials provided with the
*    distribution.
*
* THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
* "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
* LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
* A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT
* OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
* SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
* LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
* DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
* THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
* (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
* OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.TeamFoundation.Common;
using Microsoft.TeamFoundation.Framework.Server;
using Microsoft.TeamFoundation.WorkItemTracking.Server;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using TFSServerEventHandler.Mapping;

namespace TFSServerEventHandler
{
    public class WorkItemChangedEventHandler : ISubscriber
    {

        // Keep the TPC connection and WI Store in static variables.
        private static TfsTeamProjectCollection tpc = null;
        private static WorkItemStore store = null;

        public static TfsTeamProjectCollection getTPC()
        {
            if (tpc == null)
            {
                tpc = new TfsTeamProjectCollection(
                            new Uri(HandlerSettings.TFSUri),
                            HandlerSettings.GetCredentials()); 
            }
            return tpc;
        }

        public static WorkItemStore getStore()
        {
            if (store == null)
            {
                store = getTPC().GetService<WorkItemStore>(); 
            }
            return store;
        }

        public static void clearConnection()
        {
            tpc = null;
            store = null;
        }

        // =============================================================
        // Ignore all changes done by ourselves - i.e. the functional user used to handle save events.
        // Note that IF functional user would be logged in as regular user, events will not be handled.

        private bool ignoreSaveEvent(WorkItemChangedEvent ev)
        {
            bool ignore = HandlerSettings.IgnoreEventByUser(
                getStore().TeamProjectCollection,
                ev.ChangerTeamFoundationId);
            return ignore;
        }

        // =============================================================
        // Implementation of ISubscribe

        public Type[] SubscribedTypes()
        {
            return new Type[1] { typeof(WorkItemChangedEvent) };
        }

        public string Name
        {
            get { return "WorkItemChangedEventHandler"; }
        }

        public SubscriberPriority Priority
        {
            get { return SubscriberPriority.Normal; }
        }

        public EventNotificationStatus ProcessEvent(TeamFoundationRequestContext requestContext,
            NotificationType notificationType,
            object notificationEventArgs,
            out int statusCode,
            out string statusMessage,
            out ExceptionPropertyCollection properties)
        {
            statusCode = 0;
            properties = null;
            statusMessage = String.Empty;

            // TODO: Make impact on performance minimal by optimize logic for understanding if we are interested
            // in this event or not. Also put longer running task in Jobs. For now - do basic checks.

            if (notificationType != NotificationType.Notification)
            {
                return EventNotificationStatus.ActionPermitted;
            }
            var notification = notificationEventArgs as WorkItemChangedEvent;
            if (notification == null)
            {
                return EventNotificationStatus.ActionPermitted;
            }

            // Should not thow exception - if so, the code will be disabled by TFS.
            try
            {
                // Init adapter - will only be done once per deployment of plugin
                if (!HandlerSettings.initFromFile())
                {
                    // Fatal error when trying to init - return
                    // TODO: How allow retry without server restart or re-deploy of plugin?
                    return EventNotificationStatus.ActionPermitted;
                }

                // Handle case where event is fired because of Save we initiated.
                if (ignoreSaveEvent(notification))
                {
                    return EventNotificationStatus.ActionPermitted;
                }

                int workItemId = notification.CoreFields.IntegerFields[0].NewValue;
                HandlerSettings.LogMessage(
                    String.Format(
                        "WorkItem with id {0} named '{1}' was saved.",
                        "" + workItemId,
                        notification.WorkItemTitle),
                    HandlerSettings.LoggingLevel.INFO);

                // TODO: In examples I have seen you get actual WI by process below, i.e. opening a new connection to TFS.
                // I would expect as we already are in the context of a TFS you somehow could use this. Note: There is a
                // WorkItem class in the namespace Microsoft.TeamFoundation.WorkItemTracking.Server - no documentation found.
               
                Microsoft.TeamFoundation.WorkItemTracking.Client.WorkItem workItem = getStore().GetWorkItem(workItemId);

                // If a new item is based on an existing (copied) we need to reset the connection to any existing TR.
                // Note exception if item is created by the functional user. This to allow test case where TR is set
                // by test case code.
                String user = HandlerSettings.GetSignumForChangeNotification(workItem, notification);
                if (notification.ChangeType == ChangeTypes.New)
                {
                    if (!user.Equals(HandlerSettings.TFSProviderUser, StringComparison.OrdinalIgnoreCase))
                    {
                        // Note: Will not save - done in event handling or at end.
                        ECRMapper.disconnectBug(workItem);
                    }              
                }

                ProductMapper.EventType eventType =
                    ProductMapper.getInstance().GetEventType(workItem, notification, user);

                if (eventType == ProductMapper.EventType.CreateUpdate)
                {
                    // Handle the event as the release is for a maintenance product
                    RESTCallHandler.getHandler().handleEvent(workItem, user, notification);
                }
                else if (eventType == ProductMapper.EventType.Disconnect)
                {
                    // Handle the event as the release is for a maintenance product
                    RESTCallHandler.getHandler().handleDisconnectEvent(workItem, user, notification);
                }
                else
                {
                    HandlerSettings.LogMessage(
                        String.Format(
                            "WorkItem with id {0} save event was ignored by integration.",
                            "" + workItemId),
                        HandlerSettings.LoggingLevel.INFO);
                }

                // If updated but not saved, we need to explicitly call save. Very unusual case but will happen
                // e.g. if item is copied with a product value that before was in maintenance but now not. Then
                // code to disconnect bug is called, but event not handled or Bug saved, hence save here.
                if (workItem.IsDirty)
                {
                    RESTCallHandler.getHandler().saveBug(workItem);
                }

            }
            catch (Exception ex)
            {
                // Error when reading mapping file - assume fatal
                HandlerSettings.LogMessage(
                    "Error when handling the change of workitem."  +
                    "\nError: " + ex.Message +
                    "\nStack: " + ex.StackTrace,
                    HandlerSettings.LoggingLevel.ERROR);
            }
            return EventNotificationStatus.ActionPermitted;
        }
    }
}
