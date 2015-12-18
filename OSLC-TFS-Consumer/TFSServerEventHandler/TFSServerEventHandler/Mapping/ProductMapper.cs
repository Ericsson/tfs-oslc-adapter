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

using Microsoft.TeamFoundation.WorkItemTracking.Client;
using Microsoft.TeamFoundation.WorkItemTracking.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using TFSServerEventHandler.Mapping;

namespace TFSServerEventHandler
{
    public class ProductMapper
    {
        private String serviceProvider;

        private Dictionary<String, PRIMProduct> releaseToProductMap;
        private static ProductMapper instance;

        // Provide a list of info/errors that can be shown to user in the TR State field
        public enum EventType
        {
            CreateUpdate,      // Event to be handled
            Disconnect,        // Disconnect Bug
            Ignore,            // Ignore
        }

        public static ProductMapper getInstance()
        {
            if (instance == null)
            {
                instance = new ProductMapper();
            }
            return instance;
        }

        private ProductMapper()
        {        
        }

        public bool Load(String fileName)
        {
            releaseToProductMap = new Dictionary<String, PRIMProduct>();     

            try
            {
                XDocument doc = XDocument.Load(fileName);
                XElement mapping = doc.Element("mapping");

                // Loop over all entries and add to the dictionary 
                foreach (XElement spEntry in mapping.Elements("serviceProvider"))
                {
                    // If the serviceProvider (TFS Project) is defined we only 
                    // care about events that are passed to that TFS Project
                    serviceProvider = spEntry.Attribute("name").Value;

                    foreach (XElement productEntry in spEntry.Elements("product"))
                    {
                        // TODO: Check if product info is case-sensitive
                        String primProdNo = productEntry.Attribute("primProdNo").Value;
                        String primRState = productEntry.Attribute("primRState").Value;
                        PRIMProduct product = new PRIMProduct(primProdNo, primRState);

                        String mapTo = productEntry.Attribute("mapTo").Value; 

                        // NOTE: We don't care about the team attribute for now. 
                        // String team = productEntry.Attribute("team").Value; //.ToLower();;

                        releaseToProductMap.Add(mapTo, product);
                    }
                }
            }
            catch (Exception ex)
            {
                // Error when reading mapping file - assume fatal
                HandlerSettings.LogMessage(
                    "Error when reading the product mapping file: " + fileName +
                    "\nError: " + ex.Message,
                    HandlerSettings.LoggingLevel.ERROR);
                return false;
            }

            return true;
        }

        /// <summary>
        /// The Service Provider / Project for which the mapping applies
        /// </summary>
        public string ServiceProvider { get { return serviceProvider; } }

        /// <summary>
        /// Get the Product based on a Release
        /// </summary>
        public PRIMProduct GetProduct(String release)
        {
            if (releaseToProductMap.ContainsKey(release))
            {
                return releaseToProductMap[release];
            }
            else
            {
                return null;
            } 
        }

        // TODO: This is cheating as we have hardcoded knowledge where release field is.
        // Need to make this fast, but to be correct we should use regular TFSMapper way.
        public bool IsProductInMaintenance(Microsoft.TeamFoundation.WorkItemTracking.Client.WorkItem wi)
        {
            String releaseName = wi.Fields[TFSMapper.TFS_FAULTY_PRODUCT].Value.ToString();
            if (releaseName == null || releaseName.Length == 0)
            {
                // Should not happen - Mapping needs to be initialized
                HandlerSettings.LogMessage(
                    "Can not tell if product is in maintenance as the Release name not defined.",
                    HandlerSettings.LoggingLevel.WARN);
                return false;
            }

            return releaseToProductMap.ContainsKey(releaseName);
        }

        // Check the Release is or just changed from a maintenance product.  
        // TODO: This is cheating as we have hardcoded knowledge where release field is.
        // Need to make this fast, but to be correct we should use regular TFSMapper way.
        public EventType GetEventType(
            Microsoft.TeamFoundation.WorkItemTracking.Client.WorkItem wi,
            WorkItemChangedEvent notification,
            String user)
        {
            Field createTR = wi.Fields[TFSMapper.ERICSSON_DEFECT_CREATETR];
            if (createTR == null ||
               createTR.Value.ToString().Length == 0 ||
               createTR.Value.ToString().Equals("No", StringComparison.OrdinalIgnoreCase))
            {
                return EventType.Ignore;
            }


            // Check project name. If not as defined for product mapping, disregard event.
            // This allows other projects on TFS server to be non-connected to TR system.
            if (serviceProvider.Length > 0 &&
                !wi.Project.Name.Equals(serviceProvider, StringComparison.OrdinalIgnoreCase))
            {
                return EventType.Ignore;
            }

            

            bool inMaint = IsProductInMaintenance(wi);
            if (inMaint)
            {
                // Currenty selected release / product is in maintenance
                EventType evType = CheckDisconnect(wi, notification, user);
                if (evType == EventType.Disconnect)
                { 
                    return EventType.Disconnect;
                }
                return EventType.CreateUpdate;
            }

            // ========================================================================
            // Check if we just changed from having a product in maintenance. Rare case,
            // then need to allow severing links and updating state of the workitem.

            if (notification.ChangeType == ChangeTypes.New)
            {
                // Can only happen for a changed WI, not a new.
                return EventType.Ignore;
            }

            if (notification.ChangedFields == null || notification.ChangedFields.StringFields == null)
            {
                // No fields of interest are changed
                return EventType.Ignore;
            }

            StringField[] changedFields = notification.ChangedFields.StringFields;
            for (int i = 0; i < changedFields.Length; i++)
            {
                String name = changedFields[i].ReferenceName;

                // If the release is changed, we need to update the TR product
                // Note: In case we also set to Duplicate, we will disregard product change
                if (name.Equals(TFSMapper.TFS_FAULTY_PRODUCT))
                {
                    String releaseName = changedFields[i].OldValue;
                    if (releaseName == null || releaseName.Length == 0)
                    {
                        // Should not happen - Mapping needs to be initialized
                        HandlerSettings.LogMessage(
                            "Can not tell if product is in maintenance as the Release name not defined.",
                            HandlerSettings.LoggingLevel.WARN);
                        return EventType.Ignore;
                    }

                    if (releaseToProductMap.ContainsKey(releaseName))
                    {
                        return EventType.CreateUpdate;
                    }
                    else
                    {
                        return EventType.Ignore;
                    }
                }
            }

            return EventType.Ignore;
        }

        // Check if we should disconnect a TR. Condition is that an external save
        // (by TFSProviderUser) has caused the TR Link field to transition from a 
        // value to an empty value, This indicates disconnect.
        private EventType CheckDisconnect(
            Microsoft.TeamFoundation.WorkItemTracking.Client.WorkItem wi,
            WorkItemChangedEvent notification,
            String user)
        {
            if (!user.Equals(HandlerSettings.TFSProviderUser, StringComparison.OrdinalIgnoreCase))
            {
                // Only disconnect based on incoming external event. 
                // Disconnect by user is handled by separate code
                return EventType.Ignore;
            }

            if (notification.ChangeType == ChangeTypes.New)
            {
                // Disconnect can only happen for a changed WI, not a new.
                return EventType.Ignore;
            }

            // Get the current field - Disconnect case assume this is ""
            if (!wi.Fields.Contains(TFSMapper.ERICSSON_DEFECT_LINK_FIELD))
            {
                // Odd case where the mandatory field is missing
                return EventType.Ignore;
            }

            Object trLinkValue = wi.Fields[TFSMapper.ERICSSON_DEFECT_LINK_FIELD].Value;
            if (trLinkValue == null)
            {
                return EventType.Ignore;
            }
            String trLink = trLinkValue.ToString();
            if (trLink != null && trLink.Length > 0)
            {
                return EventType.Ignore;
            }

            TextField[] changedTextFields = notification.TextFields;
            if (changedTextFields == null || changedTextFields.Length == 0)
            {
                // The TR Link field is a Text field - if no changes, ignore.
                return EventType.Ignore;
            }

            // Check so also the TR Link field is changed
            for (int i = 0; i < changedTextFields.Length; i++)
            {
                String name = changedTextFields[i].ReferenceName;
                if (name.Equals(TFSMapper.ERICSSON_DEFECT_LINK_FIELD))
                {
                    return EventType.Disconnect;
                }
            }

            return EventType.Ignore;           
        }    
    }
}
