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

using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Framework.Client;
using Microsoft.TeamFoundation.Framework.Common;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using Microsoft.TeamFoundation.WorkItemTracking.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TFSServerEventHandler.Mapping;

namespace TFSServerEventHandler
{
    public class ECRMapper
    {
        // Bug states
        public const String BUG_STATE_ACTIVE = "Active";
        public const String BUG_STATE_RESOLVED = "Resolved";
        public const String BUG_STATE_CLOSED = "Closed";

        // Action values used in communication with OSLC Provider
        public const String ACTION_CREATE = "Create";
        public const String ACTION_REGISTER_ROUTE = "Register Route";
        public const String ACTION_ASSIGN = "Assign";
        public const String ACTION_PROPOSE = "Propose";
        public const String ACTION_APPROVE = "Approve";
        public const String ACTION_VERIFY = "Verify";
        public const String ACTION_ANSWER = "Answer";
        public const String ACTION_ACCEPT = "Accept";
        public const String ACTION_REJECT = "Reject";
        public const String ACTION_DUPLICATE = "Duplicate";
        public const String ACTION_UNDUPLICATE = "Unduplicate";
        public const String ACTION_CHANGE_PRODUCT = "ChangeProduct";
        public const String ACTION_DISCONNECT = "Disconnect";
        public const String ACTION_REACTIVATE = "Reactivate";
        public const String ACTION_FINISH = "Finish";

        // TR states
        public const String TR_STATE_PRIVATE = "PR (Private)";
        public const String TR_STATE_PRIVATE_S = "PR";
        public const String TR_STATE_REGISTERED = "RE (Registered)";
        public const String TR_STATE_REGISTERED_S = "RE";
        public const String TR_STATE_ASSIGNED = "AS (Assigned)";
        public const String TR_STATE_ASSIGNED_S = "AS";
        public const String TR_STATE_PROPOSED = "PP (Proposed)";
        public const String TR_STATE_PROPOSED_S = "PP";
        public const String TR_STATE_PROP_APPROV = "PA (Proposal Approved)";
        public const String TR_STATE_PROP_APPROV_S = "PA";
        public const String TR_STATE_CORR_VER = "CV (Correction Verified)";
        public const String TR_STATE_CORR_VER_S = "CV";
        public const String TR_STATE_NODE_COMP_VER = "NC (Node Component Verified)";
        public const String TR_STATE_NODE_COMP_VER_S = "NC";
        public const String TR_STATE_TECH_ANSW_PROV = "TA (Technical Answer Provided)";
        public const String TR_STATE_TECH_ANSW_PROV_S = "TA";
        public const String TR_STATE_POSTPONED = "PO (Postponed)";
        public const String TR_STATE_POSTPONED_S = "PO";
        public const String TR_STATE_CANCELLED = "CA (Cancelled)";
        public const String TR_STATE_CANCELLED_S = "CA";
        public const String TR_STATE_FINISHED = "FI (Finished)";
        public const String TR_STATE_FINISHED_S = "FI";

        // Internal action value
        public const String ACTION_IGNORE = "Ignore";

        // Provide a human readable name for the MHWeb states - should be factored to config file
        private static Dictionary<String, String> mhStateToFullName = new Dictionary<String, String>()
        {
            { TR_STATE_PRIVATE_S, TR_STATE_PRIVATE},
            { TR_STATE_REGISTERED_S, TR_STATE_REGISTERED},
            { TR_STATE_ASSIGNED_S, TR_STATE_ASSIGNED},
            { TR_STATE_PROPOSED_S, TR_STATE_PROPOSED},
            { TR_STATE_PROP_APPROV_S, TR_STATE_PROP_APPROV},
            { TR_STATE_CORR_VER_S, TR_STATE_CORR_VER},
            { TR_STATE_NODE_COMP_VER_S, TR_STATE_NODE_COMP_VER},
            { TR_STATE_TECH_ANSW_PROV_S, TR_STATE_TECH_ANSW_PROV},
            { TR_STATE_POSTPONED_S, TR_STATE_POSTPONED},
            { TR_STATE_CANCELLED_S, TR_STATE_CANCELLED},
            { TR_STATE_FINISHED_S, TR_STATE_FINISHED}
        };

        // Provide a list of info/errors that can be shown to user in the TR State field
        public enum TRSyncState
        {
            SyncStateNone,      // No Sync. Changes will not be propagated.
            SyncStateOK,        // Sync state OK
            SyncStateWarning,   // State is in sync but with errors reported
            SyncStateError,     // State is out of sync. Changes will not be propagated.
        }

        // Mapping between Bug state and TR state. If Bug state does not match this
        // user needs to update TR to get back in sync. Nothing will be propagated from
        // Bug to TR until back in sync.
        private static string[] activeStates =
            { TR_STATE_PRIVATE, TR_STATE_REGISTERED, TR_STATE_ASSIGNED, TR_STATE_PROPOSED, TR_STATE_PROP_APPROV };
        private static string[] resolvedStates = { TR_STATE_TECH_ANSW_PROV };
        private static string[] closedStates = { TR_STATE_FINISHED, TR_STATE_POSTPONED };
        private static Dictionary<String, String[]> bugStateToTrState = new Dictionary<string, string[]>()
        {
            { BUG_STATE_ACTIVE, activeStates},
            { BUG_STATE_RESOLVED, resolvedStates},
            { BUG_STATE_CLOSED, closedStates}
        };

        // Added {0} to allow setting direction to e.g. "(in)" or "(out)" depending on if latest change is
        // done by the TFS user or by functional user - i.e. incoming from MHWeb change.
        //
        // NOTE: To be able to recognize from the string what sync state we are in, the following rule apply:
        // For SyncStateOK: The message must contain "OK" and must not contain "Warning" or "Error".
        // For SyncStateWarning: The message must contain "Warning" and must not contain "OK" or "Error".
        // For SyncStateError: The message must contain "Error" and must not contain "OK" or "Warning".
        private static Dictionary<TRSyncState, String> trSyncStateToString = new Dictionary<TRSyncState, String>()
        {
            { TRSyncState.SyncStateNone, "{0} No sync, see History"},
            { TRSyncState.SyncStateOK, "{0} Latest sync OK"},
            { TRSyncState.SyncStateWarning, "{0} Warnings, see History"},     
            { TRSyncState.SyncStateError, "{0} Error, see History"},
        };

        public static bool IsSyncError(Microsoft.TeamFoundation.WorkItemTracking.Client.WorkItem workItem)
        {
            String currentSyncState = workItem.Fields[TFSMapper.ERICSSON_DEFECT_SYNCSTATE].Value.ToString();
            return currentSyncState.Contains("Error");
        }

        // Provide a list of info/errors that can be shown to user in the TR State field
        public enum TRStateInfo
        {
            InfoPendingUpdate,    // Indicating value is being updated
            ErrorMissingTR,       // Indicating that the link field is empty for a maintenance Bug
            ErrorHasTR,           // Indicating that the link filed has value though not a maintenance Bug
        }

        private static Dictionary<TRStateInfo, String> trStateToString = new Dictionary<TRStateInfo, String>()
        {
            { TRStateInfo.InfoPendingUpdate, "Pending update ..."},
            { TRStateInfo.ErrorMissingTR, "Missing a linked TR."},
            { TRStateInfo.ErrorHasTR, "Should not have a linked TR."}
        };


        // =======================================================================
        // Mapping from workItem to ECR

        // Return the Bug and TR state change if part of update
        public static void getStatusChange(WorkItemChangedEvent notification,
            Microsoft.TeamFoundation.WorkItemTracking.Client.WorkItem workItem,
            out String oldState,
            out String newState,
            out String action)
        {
            newState = null;
            oldState = null;
            action = null;

            // If the Bug and TR are in different states, block update from Bug to TR.
            if (ECRMapper.IsSyncError(workItem))
            {
                action = ACTION_IGNORE;
                return;
            }

            bool changedAssignedTo = false;

            // If Bug marked as Duplicate, the only activity to be propagated to TR is Unduplicate.
            String substate = workItem.Fields[TFSMapper.TFS_SUBSTATE].Value.ToString();
            bool isDuplicate = substate.Equals(TFSMapper.TFS_SUBSTATE_DUPLICATE);
            bool isDuplicateChanged = false;

            if (notification.ChangedFields == null || notification.ChangedFields.StringFields == null)
            {
                // No fields of interest are changed
                return; 
            }

            StringField[] changedFields = notification.ChangedFields.StringFields;
            for (int i = 0; i < changedFields.Length; i++)
            {
                String name = changedFields[i].ReferenceName;
                if (name.Equals(TFSMapper.TFS_STATE, StringComparison.OrdinalIgnoreCase))
                {
                    oldState = changedFields[i].OldValue;
                    newState = changedFields[i].NewValue;
                }
                else if (name.Equals(TFSMapper.TFS_OWNER, StringComparison.OrdinalIgnoreCase))
                {
                    changedAssignedTo = true;
                }

                // If TFS_SUBSTATE is changed to/from Duplicate the TR should be set to
                // corresponding state. If marked as Duplicate, the only activity that
                // should be propagated to TR is Unduplicate.
                else if (name.Equals(TFSMapper.TFS_SUBSTATE))
                {
                    if (changedFields[i].NewValue.Equals(TFSMapper.TFS_SUBSTATE_DUPLICATE) ||
                        changedFields[i].OldValue.Equals(TFSMapper.TFS_SUBSTATE_DUPLICATE))
                    {
                        isDuplicateChanged = true;
                    }
                }

                // If the release is changed, we need to update the TR product
                // Note: In case we also set to Duplicate, we will disregard product change
                else if (name.Equals(TFSMapper.TFS_FAULTY_PRODUCT))
                {
                    action = ACTION_CHANGE_PRODUCT;
                }
            }
          
            if (isDuplicateChanged)
            {
                // If the Duplicate flag is changed we need to act on this. This is a substate flag, so
                // it can happen also during a state transition.
                action = isDuplicate ? ACTION_DUPLICATE : ACTION_UNDUPLICATE;
                return;
            }
            else if (isDuplicate)
            {
                // If Bug is duplicate and there is no change for this state, we should send no
                // updates to TR until unduplicated.
                action = ACTION_IGNORE;
                return;
            }

            // If we have a changed of "Assigned To" this will only be possible to update in specific
            // TR states. So if Assigned To changed at same time as state - likely need to prevent.
            if (changedAssignedTo)
            {
                if (newState == null || oldState == null)
                {
                    // No state change for Bug, but for TR
                    oldState = BUG_STATE_ACTIVE;
                    newState = BUG_STATE_ACTIVE;
                }
                else
                {
                    HandlerSettings.LogMessage(
                        String.Format(
                            "The 'Assigned To' change can not be done for TR: {0} at same time as state change",
                            workItem.Title),
                        HandlerSettings.LoggingLevel.WARN);
                }
            }
        }

        // Return an EnterpriseChangeRequest with mapped values for the update
        public static EnterpriseChangeRequest mapFromUpdatedWorkitem(
            Microsoft.TeamFoundation.WorkItemTracking.Client.WorkItem workItem,
            Uri about,
            WorkItemChangedEvent notification)
        {
            EnterpriseChangeRequest newEcr = new EnterpriseChangeRequest();
            newEcr.SetAbout(about);

            // The notification contain all changed fields. To understand what to
            // propagate to the client, we need to check which ecm fields that are
            // affected by the changes and are configured for notifyChange

            int noOfMappedChanged = 0;

            if (notification.ChangedFields != null)
            {
                    // Most fields are String fields
                    StringField[] changedStrFields = notification.ChangedFields.StringFields;
                    if (changedStrFields != null)
                    {
                        for (int i = 0; i < changedStrFields.Length; i++)
                        {
                            String fieldName = changedStrFields[i].ReferenceName;
                            noOfMappedChanged += mapFromUpdated(workItem, newEcr, fieldName);
                        }
                    }

                    // For example Priority is an Integer field
                    IntegerField[] changedIntFields = notification.ChangedFields.IntegerFields;
                    if (changedIntFields != null)
                    {
                        for (int i = 0; i < changedIntFields.Length; i++)
                        {
                            String fieldName = changedIntFields[i].ReferenceName;
                            noOfMappedChanged += mapFromUpdated(workItem, newEcr, fieldName);
                        }
                    }
            }

            // For example the Description is a Text field
            TextField[] changedTextFields = notification.TextFields;
            if (changedTextFields != null)
            {
                for (int i = 0; i < changedTextFields.Length; i++)
                {
                    String fieldName = changedTextFields[i].ReferenceName;
                    noOfMappedChanged += mapFromUpdated(workItem, newEcr, fieldName);
                }
            }
            
            // To find a change in the Comment/History one need to look at revision - 1.
            noOfMappedChanged += mapFromUpdated(workItem, newEcr, TFSMapper.TFS_HISTORY);

            // Need to send list of attachments in all cases when we have another update. So if we already have
            // an update (noOfMappedChanged > 0), send - otherwise, check if changed then send.
            if ((noOfMappedChanged > 0 || TFSMapper.getInstance().hasLinksChanged(workItem)))
            {
                noOfMappedChanged += mapFromUpdated(workItem, newEcr, TFSMapper.ERICSSON_DEFECT_HYPERLINK);
            }

            if (noOfMappedChanged > 0)
            {
                // More than 1 field that was mapped changed
                return newEcr;
            }
            else
            {
                // No field of interest was changed
                HandlerSettings.LogMessage(
                    String.Format("No mapped fields was updated for: {0}", workItem.Title),
                    HandlerSettings.LoggingLevel.INFO);
                return null;
            }
        }

        private static int mapFromUpdated(
            Microsoft.TeamFoundation.WorkItemTracking.Client.WorkItem workItem,
            EnterpriseChangeRequest newEcr, String fieldName)
        {
            int noOfMappedChanged = 0;

            // TODO: For the "calculated" fields we have defined ourselves we know the
            // dependencies, but should be expressed more elegant than this. Also the
            // Ericsson.Defect.User.DisplayName is dependent on System.AssignedTo but
            // will not be updated - we "know" that ...
            if (fieldName.Equals(TFSMapper.TFS_OWNER))
            {
                fieldName = TFSMapper.ERICSSON_DEFECT_USER_SIGNUM;
            }

            // Can be multiple ECR properties updated by one TFS field
            List<Property> props = props = AttributesMapper.getInstance().getNotifyProperties(fieldName);
            if (props != null && props.Count > 0)
            {
                foreach (Property prop in props)
                {
                    if (prop.getNotifyChange() && TFSMapper.getInstance().setEcrValues(newEcr, prop.getKey(), workItem))
                    {
                        noOfMappedChanged++;
                    }
                }
            }

            return noOfMappedChanged;
        }

        // Return an EnterpriseChangeRequest with mapped values needed for the state transiton
        // If there is a fail with mapping some values, this will be captured in event log.
        // TODO: This could likely be described in a xml mapping file for configuration
        public static EnterpriseChangeRequest mapFromWorkitem(
            Microsoft.TeamFoundation.WorkItemTracking.Client.WorkItem workItem,
            Uri about,
            String action)
        {
            EnterpriseChangeRequest newEcr = new EnterpriseChangeRequest();
            newEcr.SetAbout(about);

            // Create a mapped ECR based on suggested action
            
            switch (action)
            {
                case ACTION_CREATE:
                    newEcr.SetTitle(AttributesMapper.GetTfsValueForEcrKey(TFSMapper.ECM_TITLE, workItem));
                    newEcr.SetDescription(AttributesMapper.GetTfsValueForEcrKey(TFSMapper.ECM_DESCRIPTION, workItem));
                    newEcr.SetCurrentMho(AttributesMapper.GetTfsValueForEcrKey(TFSMapper.ECM_CURRENT_MHO, workItem));
                    newEcr.SetCustomer(AttributesMapper.GetTfsValueForEcrKey(TFSMapper.ECM_CUSTOMER, workItem)); 
                    newEcr.SetProduct(AttributesMapper.GetTfsValueForEcrKey(TFSMapper.ECM_PRODUCT, workItem));
                    newEcr.SetProductRevision(AttributesMapper.GetTfsValueForEcrKey(TFSMapper.ECM_PRODUCT_REVISION, workItem));
                    newEcr.SetNodeProduct(AttributesMapper.GetTfsValueForEcrKey(TFSMapper.ECM_NODE_PRODUCT, workItem));
                    newEcr.SetNodeProductRevision(AttributesMapper.GetTfsValueForEcrKey(TFSMapper.ECM_NODE_PRODUCT_REVISION, workItem));

                    // Note: The field firstTechnicalContact also is needed for TR creation. This we retrieve from
                    // the user in the create notification, and put in the REST call header as required by MHWeb.
                    // Formally we have mapping specified in ERICSSON_DEFECT_CREATOR_SIGNUM, but not used.

                    // Add the connected Bug as related link
                    Uri relatedBug = new Uri(HandlerSettings.GetUriForBug(workItem.Id.ToString()));
                    String label = workItem.Id.ToString() + ": " + workItem.Title;
                    OSLC4Net.Core.Model.Link link = new OSLC4Net.Core.Model.Link(relatedBug, label);
                    newEcr.AddRelatedChangeRequest(link);

                    break;

                case ACTION_REGISTER_ROUTE:
                    newEcr.SetAction(ACTION_REGISTER_ROUTE);
                    newEcr.SetImpactOnISP(AttributesMapper.GetTfsValueForEcrKey(TFSMapper.ECM_IMPACT_ON_ISP, workItem));                    
                    newEcr.SetPriority(AttributesMapper.GetTfsValueForEcrKey(TFSMapper.ECM_PRIORITY, workItem));
                    newEcr.SetDiddet(AttributesMapper.GetTfsValueForEcrKey(TFSMapper.ECM_DIDDET, workItem));
                    newEcr.SetActivity(AttributesMapper.GetTfsValueForEcrKey(TFSMapper.ECM_ACTIVITY, workItem));
                    newEcr.SetFirstTechContactInfo(AttributesMapper.GetTfsValueForEcrKey(TFSMapper.ECM_FIRST_TECHNICAL_CONTACT_INFO, workItem));
                    newEcr.SetCountry(AttributesMapper.GetTfsValueForEcrKey(TFSMapper.ECM_COUNTRY, workItem));
                    newEcr.SetSite(AttributesMapper.GetTfsValueForEcrKey(TFSMapper.ECM_SITE, workItem));

                    break;

                case ACTION_ASSIGN:
                    newEcr.SetAction(ACTION_ASSIGN);
                    newEcr.SetOwner(AttributesMapper.GetTfsValueForEcrKey(TFSMapper.ECM_OWNER, workItem));
                    break;

                case ACTION_PROPOSE:
                    newEcr.SetAction(ACTION_PROPOSE);
                    newEcr.SetDiddet(AttributesMapper.GetTfsValueForEcrKey(TFSMapper.ECM_DIDDET, workItem));
                    newEcr.SetActivity(AttributesMapper.GetTfsValueForEcrKey(TFSMapper.ECM_ACTIVITY, workItem));
                    newEcr.SetFirstTechContactInfo(AttributesMapper.GetTfsValueForEcrKey(TFSMapper.ECM_FIRST_TECHNICAL_CONTACT_INFO, workItem));
                    newEcr.SetExpectedImpactOnISP(AttributesMapper.GetTfsValueForEcrKey(TFSMapper.ECM_EXPECTED_IMPACT_ON_ISP, workItem));
                    newEcr.SetAnswer(AttributesMapper.GetTfsValueForEcrKey(TFSMapper.ECM_ANSWER, workItem));
                    newEcr.SetFaultCode(AttributesMapper.GetTfsValueForEcrKey(TFSMapper.ECM_FAULTCODE, workItem));
                    newEcr.SetAnswerCode(AttributesMapper.GetTfsValueForEcrKey(TFSMapper.ECM_ANSWER_CODE, workItem));

                    // Corrected Product info is mandatory for some  answerCodes and optional for others. Here
                    // we pass in all cases and let the bug.xml handle mandatoryness and mhweb complain if not
                    // present.

                    newEcr.SetCorrectedProduct(
                        AttributesMapper.GetTfsValueForEcrKey(TFSMapper.ECM_CORRECTED_PRODUCT, workItem));
                    newEcr.SetCorrectedProductRevision(
                        AttributesMapper.GetTfsValueForEcrKey(TFSMapper.ECM_CORRECTED_PRODUCT_REVISION, workItem));
                    newEcr.SetCorrectedNodeProduct(
                        AttributesMapper.GetTfsValueForEcrKey(TFSMapper.ECM_CORRECTED_NODE_PRODUCT, workItem));
                    newEcr.SetCorrectedNodeProductRevision(
                        AttributesMapper.GetTfsValueForEcrKey(TFSMapper.ECM_CORRECTED_NODE_PRODUCT_REVISION, workItem));
                    break;

                case ACTION_APPROVE:
                    newEcr.SetAction(ACTION_APPROVE); 
                    break;

                case ACTION_VERIFY:
                    newEcr.SetAction(ACTION_VERIFY);
                    newEcr.SetCorrectedNodeProduct(
                        AttributesMapper.GetTfsValueForEcrKey(TFSMapper.ECM_CORRECTED_NODE_PRODUCT, workItem));
                    newEcr.SetCorrectedNodeProductRevision(
                        AttributesMapper.GetTfsValueForEcrKey(TFSMapper.ECM_CORRECTED_NODE_PRODUCT_REVISION, workItem));
                    break;

                case ACTION_ANSWER:
                    newEcr.SetAction(ACTION_ANSWER);
                    newEcr.SetCorrectedNodeProduct(
                        AttributesMapper.GetTfsValueForEcrKey(TFSMapper.ECM_CORRECTED_NODE_PRODUCT, workItem));
                    newEcr.SetCorrectedNodeProductRevision(
                        AttributesMapper.GetTfsValueForEcrKey(TFSMapper.ECM_CORRECTED_NODE_PRODUCT_REVISION, workItem));
                    break;

                case ACTION_ACCEPT:
                    newEcr.SetAction(ACTION_ACCEPT);
                    break;

                case ACTION_REJECT:
                    newEcr.SetAction(ACTION_REJECT);
                    newEcr.SetNotebook(AttributesMapper.GetTfsValueForEcrKey(TFSMapper.ECM_NOTEBOOK, workItem));
                    break;

                case ACTION_DUPLICATE:
                    newEcr.SetAction(ACTION_DUPLICATE);
                    newEcr.SetPrimaryTR(AttributesMapper.GetTfsValueForEcrKey(TFSMapper.ECM_PRIMARYTR, workItem));
                    break;

                case ACTION_UNDUPLICATE:
                    newEcr.SetAction(ACTION_UNDUPLICATE);
                    break;

                case ACTION_CHANGE_PRODUCT:
                    newEcr.SetAction(ACTION_CHANGE_PRODUCT);
                    newEcr.SetProduct(AttributesMapper.GetTfsValueForEcrKey(TFSMapper.ECM_PRODUCT, workItem));
                    newEcr.SetProductRevision(AttributesMapper.GetTfsValueForEcrKey(TFSMapper.ECM_PRODUCT_REVISION, workItem));
                    newEcr.SetNodeProduct(AttributesMapper.GetTfsValueForEcrKey(TFSMapper.ECM_NODE_PRODUCT, workItem));
                    newEcr.SetNodeProductRevision(AttributesMapper.GetTfsValueForEcrKey(TFSMapper.ECM_NODE_PRODUCT_REVISION, workItem));
                    break;

                case ACTION_REACTIVATE:
                    newEcr.SetAction(ACTION_REACTIVATE);
                    break;

                case ACTION_FINISH:
                    newEcr.SetAction(ACTION_FINISH);
                    break;

                case ACTION_DISCONNECT:
                    newEcr.SetAction(ACTION_DISCONNECT);
                    break;
            }

            return newEcr;       
        }

        // If set, clear the TR fields conneting the Bug with a TR.
        static public Boolean disconnectBug(Microsoft.TeamFoundation.WorkItemTracking.Client.WorkItem workItem)
        {
            Boolean updated = false;

            if (workItem.Fields[TFSMapper.ERICSSON_DEFECT_LINK_FIELD].Value != null &&
                workItem.Fields[TFSMapper.ERICSSON_DEFECT_LINK_FIELD].Value.ToString().Length > 0)
            {
                String trId = workItem.Fields[TFSMapper.ERICSSON_DEFECT_LINK_FIELD].Value.ToString();
                workItem.Fields[TFSMapper.TFS_HISTORY].Value = "Workitem disconnected from TR: " + trId;

                workItem.Fields[TFSMapper.ERICSSON_DEFECT_LINK_FIELD].Value = null;
                workItem.Fields[TFSMapper.ERICSSON_DEFECT_STATE_FIELD].Value = null;
                workItem.Fields[TFSMapper.ERICSSON_DEFECT_SYNCSTATE].Value = null;

                updated = true;

                HandlerSettings.LogMessage(
                    String.Format(
                        "Cleared TR connection fields for WorkItem with id {0}.",
                        "" + workItem.Id),
                        HandlerSettings.LoggingLevel.INFO);
            }

            return updated;
        }

        // Get the Uri for the TR before disconnected assuming disconnect was done in
        // previous save. Preconditions should be checked by caller.
        static public Uri getDisconnectedTRLink(
            Microsoft.TeamFoundation.WorkItemTracking.Client.WorkItem workItem,
            WorkItemChangedEvent notification)
        {
            // Apparently we need to check on second last revision to get the correct
            // value for the previous value. This does not show in UI, but is clear when
            // debugging. So check we have > 2 revisions, and then compare.

            if (workItem.Revision < 3)
            {
                return null;
            }

            Revision lastRev = workItem.Revisions[workItem.Revision - 2];
            String oldTrLink = lastRev.Fields[TFSMapper.ERICSSON_DEFECT_LINK_FIELD].Value.ToString();
            if (oldTrLink == null || oldTrLink.Length == 0)
            {
                return null;               
            }

            HandlerSettings.LogMessage(
                String.Format(
                    "Disconnected TR '{0}' from WorkItem with id {1}.",
                    oldTrLink, "" + workItem.Id), HandlerSettings.LoggingLevel.INFO);

            return new Uri(HandlerSettings.GetUriForTR(oldTrLink));         
        }

        // Update the TR State field for the bug based either on incoming ecr, or if this is null
        // get the current TR State from the bug. If missing link for a maintenance product or if
        // having a link for a non-maintenance product, also show this.
        static public void updateBug(Status status, String user,
            Microsoft.TeamFoundation.WorkItemTracking.Client.WorkItem workItem)
        {
            // Check if we are connected when we should be 
            bool isProdInMaint = ProductMapper.getInstance().IsProductInMaintenance(workItem);
            String currentLink = HandlerSettings.GetIDFromLink(workItem);
            bool hasLink = currentLink.Length > 0;

            if (isProdInMaint && (!hasLink && status.TRAbout == null))
            {
                // Error - should have link and nothing in the status.TRAbout so have not created.
                workItem.Fields[TFSMapper.ERICSSON_DEFECT_STATE_FIELD].Value = trStateToString[TRStateInfo.ErrorMissingTR];
            }
            else if (!isProdInMaint)
            {
                // OK - clear any status fields and return
                ECRMapper.disconnectBug(workItem);
                return;
            }
            else
            {
                // OK - prod is in maint and has link - normal case

                // Check if state is updated
                String newTrState = null;
                if (status.TRState != null)
                {
                    String currentTrState = getTRState(workItem);
                    String updatedTrState = status.TRState;
                    if (!updatedTrState.Equals(currentTrState))
                    {
                        newTrState = updatedTrState;
                        workItem.Fields[TFSMapper.ERICSSON_DEFECT_STATE_FIELD].Value = newTrState;
                    }
                }

                if (status.TRAbout != null)
                {
                    // We will store the relative path of the "Edit TR" link in the bug, reason being that the Link
                    // that we need to define for the UI to allow click to navigate require a non blank UriRoot value,
                    // see http://msdn.microsoft.com/en-us/library/dd936107.aspx.

                    String updatedLink = HandlerSettings.GetIDFromUri(status.TRAbout.ToString());
                    if (updatedLink.Length > 0)
                    {
                        if (!currentLink.Equals(updatedLink))
                        {
                            workItem.Fields[TFSMapper.ERICSSON_DEFECT_LINK_FIELD].Value = updatedLink;
                        }
                    }
                }
            }

            // Put the error and expected state string into system history field
            String message = status.GetMessage();

            // Append any defined guiding message
            TRSyncState newSyncState = getSyncState(status, workItem);
            String guidingMessage = "";
            switch (newSyncState)
            {
                case TRSyncState.SyncStateNone:
                    guidingMessage = HandlerSettings.SyncNoSyncMessage;
                    break;
                case TRSyncState.SyncStateOK:
                    guidingMessage = HandlerSettings.SyncSuccessMessage;
                    break;
                case TRSyncState.SyncStateWarning:
                    guidingMessage = HandlerSettings.SyncWarningMessage;
                    break;
                case TRSyncState.SyncStateError:
                    guidingMessage = HandlerSettings.SyncErrorMessage;
                    break;
            }

            if (message.Length > 0 || guidingMessage.Length > 0)
            {
                workItem.History = message + ((guidingMessage.Length > 0) ? " " + guidingMessage : "");
            }

            // Put the sync state in the Sync State field
            String currentSyncStateStr = workItem.Fields[TFSMapper.ERICSSON_DEFECT_SYNCSTATE].Value.ToString();
            String newSyncStateStr = trSyncStateToString[newSyncState];

            bool incomingChange = user.Equals(HandlerSettings.TFSProviderUser, StringComparison.OrdinalIgnoreCase);
            newSyncStateStr = String.Format(newSyncStateStr, incomingChange ? "(in)" : "(out)");

            if (!currentSyncStateStr.Equals(trSyncStateToString))
            {
                workItem.Fields[TFSMapper.ERICSSON_DEFECT_SYNCSTATE].Value = newSyncStateStr; 
            }
        }

        // =======================================================================
        // Getters

        // Get long TR state name e.g. AS (Assigned) from short e.g. AS.
        public static String getLongTRState(String shortTRState)
        {
            if (mhStateToFullName.ContainsKey(shortTRState))
            {
                return mhStateToFullName[shortTRState]; 
            }
            else
            {
                // Should not happen - unknown state to translate
                HandlerSettings.LogMessage(
                    String.Format("State {0} is not recognized.", shortTRState),
                    HandlerSettings.LoggingLevel.WARN);
                return shortTRState;
            }
        }

        public static TRSyncState getSyncState(Status status,
            Microsoft.TeamFoundation.WorkItemTracking.Client.WorkItem workItem)
        {
            String trState = getTRState(workItem);
            String bugState = workItem.State;

            TRSyncState syncState = TRSyncState.SyncStateOK;

            // If Bug marked as Duplicate, we should notify that No Sync will be done.
            String substate = workItem.Fields[TFSMapper.TFS_SUBSTATE].Value.ToString();
            if (substate.Equals(TFSMapper.TFS_SUBSTATE_DUPLICATE))
            {
                return TRSyncState.SyncStateNone;
            }

            // Check if we are in sync error state. Get the TR states corresponding to the Bug state.
            // If Bug state is that list - OK, if not we have a sync "error state".
            if (bugStateToTrState.ContainsKey(bugState))
            {
                string[] trStatesForBug = bugStateToTrState[bugState];
                syncState = Array.IndexOf(trStatesForBug, trState) < 0 ? TRSyncState.SyncStateError : syncState; 
            }

            // If not error - check if warn state
            if (syncState != TRSyncState.SyncStateError)
            {
                syncState = (status.GetMessage().Length > 0) ? TRSyncState.SyncStateWarning : syncState;
            }

            return syncState;
        }

        public static String getTRState(Microsoft.TeamFoundation.WorkItemTracking.Client.WorkItem workItem)
        {
            return workItem.Fields[TFSMapper.ERICSSON_DEFECT_STATE_FIELD].Value.ToString();
        }
    }
}