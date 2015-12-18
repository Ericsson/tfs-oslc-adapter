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
using System.Net;
using System.Web;
using System.IO;
using System.Diagnostics;
using Microsoft.TeamFoundation.Common;
using Microsoft.TeamFoundation.Framework.Server;
using Microsoft.TeamFoundation.WorkItemTracking.Server;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using OSLC4Net.Client.Oslc;
using OSLC4Net.Core.Model;
using System.Net.Http;
using TFSServerEventHandler.OAuth;
using OSLC4Net.Client.Oslc.Jazz;
using System.Net.Http.Headers;
using TFSServerEventHandler.Authentication;
using TFSServerEventHandler.Mapping;
using Microsoft.TeamFoundation.Framework.Client;


namespace TFSServerEventHandler
{
    // For examples on using the OSLC4Net code, see http://oslc4net.codeplex.com/SourceControl/latest#OSLC4Net_SDK/OSLC4Net.Client/Oslc/Jazz/JazzOAuthClient.cs

    public class RESTCallHandler
    {
        private OslcClient2 oslcClient;
        private static RESTCallHandler instance = null;

        public static RESTCallHandler getHandler()
        {
            if (instance == null)
            {
                instance = new RESTCallHandler();
            }
            return instance;
        }

        public static void clearHandler()
        {
            instance = null;
        }

        // Get and initialize a basic auth client
        private OslcClient2 getBasicAuthClient(FriendInfo friend)
        {
            BasicAuthClient basicClient = null;
            String exMessage = "";

            try
            {
                JazzRootServicesHelper2 helper = new JazzRootServicesHelper2(
                    HandlerSettings.ClientUri,
                    HandlerSettings.RootservicesUri);

                // Specific code - logging in
                basicClient = new BasicAuthClient(friend.EncodedCredentials);
                HttpClient client = basicClient.GetHttpClient();

                // Get the Creation Factory for mhweb
                String user = friend.GetBasicAuthUser();
                String serviceProviderUrl = basicClient.LookupServiceProviderUrl(
                    helper.GetCatalogUrl(), "mhweb", HandlerSettings.getRESTHeaders(user));
                HandlerSettings.CreationFactoryUri = basicClient.LookupCreationFactory(
                                            serviceProviderUrl,
                                            Constants.ENTERPRISE_CHANGE_MANAGEMENT_DOMAIN,
                                            Constants.TYPE_ENTERPRISE_CHANGE_REQUEST,
                                            HandlerSettings.getRESTHeaders(user));

            }
            catch (Exception ex)
            {
                basicClient = null;
                exMessage = ex.Message;
            }

            if (basicClient == null || exMessage.Length > 0)
            {
                HandlerSettings.LogMessage(
                    String.Format(
                        "Failed to get a httpClient for basic auth." + 
                        "\nUsing client uri: {0}" + 
                        "\nUsing rootservices uri: {1}" + 
                        "\nMessage: {2}",
                        HandlerSettings.ClientUri,
                        HandlerSettings.RootservicesUri,
                        exMessage),
                    HandlerSettings.LoggingLevel.ERROR);
            } 

            return basicClient;
        }

        // Get and initialize an oauth client
        private OslcClient2 getOAuthClient(FriendInfo friend)
        {
            JazzOAuthClient2 oauthClient = null;
            String exMessage = "";

            try
            {
                JazzRootServicesHelper2 helper = new JazzRootServicesHelper2(
                HandlerSettings.ClientUri,
                HandlerSettings.RootservicesUri);

                oauthClient = helper.InitOAuthClient2(friend.ConsumerKey,
                    friend.ConsumerSecret,
                    friend.AccessToken,
                    friend.AccessTokenSecret);

                // Get the Creation Factory for mhweb
                String serviceProviderUrl = oauthClient.LookupServiceProviderUrl(helper.GetCatalogUrl(), "mhweb");
                HandlerSettings.CreationFactoryUri = oauthClient.LookupCreationFactory(
                                            serviceProviderUrl, Constants.ENTERPRISE_CHANGE_MANAGEMENT_DOMAIN,
                                            Constants.TYPE_ENTERPRISE_CHANGE_REQUEST);
            }
            catch (Exception ex)
            {
                oauthClient = null;
                exMessage = ex.Message;
            }

            if (oauthClient == null || exMessage.Length > 0)
            {
                HandlerSettings.LogMessage(
                    String.Format(
                        "Failed to get a httpClient for oauth." +
                        "\nUsing client uri: {0}" +
                        "\nUsing rootservices uri: {1}" +
                        "\nMessage: {2}",
                        HandlerSettings.ClientUri,
                        HandlerSettings.RootservicesUri,
                        exMessage),
                    HandlerSettings.LoggingLevel.ERROR);
            }

            return oauthClient;
        }

        // Return an authenticated oslc client. Throws exception in case not found.
        public OslcClient2 getOslcClient()
        {
            if (oslcClient != null)
                return oslcClient;

            // Get the Friend info for mhweb
            FriendInfo friend = HandlerSettings.GetFriend("mhweb");
            if (friend == null)
            {
                throw new Exception(String.Format("Failed to get friend info for: {0}", "mhweb"));
            }

            if (friend.UseAccess == FriendInfo.UseAccessType.oauth)
            {
                oslcClient = getOAuthClient(friend);
            }
            else
            {
                oslcClient = getBasicAuthClient(friend);
            }

            if (oslcClient == null)
            {
                throw new Exception(String.Format("Failed to get OSLC client for: {0}", "mhweb"));
            }

            return oslcClient;
        }

        // =========================================================
        // Handle the WorkItemChangedEvent event

        // Disconnect TR if conditions are matching. Return if workitem has been saved.
        public void handleDisconnectEvent(
            Microsoft.TeamFoundation.WorkItemTracking.Client.WorkItem workItem,
            String user,
            WorkItemChangedEvent notification)
        {
            Uri about = ECRMapper.getDisconnectedTRLink(workItem, notification);
            if (about == null)
            {
                return;
            }
            EnterpriseChangeRequest ecr = ECRMapper.mapFromWorkitem(
                    workItem, about, ECRMapper.ACTION_DISCONNECT);
            Status status = new Status();
            callUpdateTR(ecr, user, null, ref status);
            if (!status.OK)
            {
                HandlerSettings.LogMessage(
                    "Failed to disconnect TR from Bug.",
                    HandlerSettings.LoggingLevel.WARN);
            }
        }

        // Return if the workitem is updated (saved) by the handling code.
        public void handleEvent(
            Microsoft.TeamFoundation.WorkItemTracking.Client.WorkItem workItem,
            String user,
            WorkItemChangedEvent notification)
        {
            Status status = new Status();

            // If the user is the OSLC Provider functional user we will only update status
            // message if so needed. This as changes from the OSLC Provider originates from
            // external source = currently: MHWeb. Need revisit to allow multiple clients.
            String aboutStr = AttributesMapper.GetTfsValueForEcrKey(TFSMapper.ECM_ABOUT, workItem);
            if (!user.Equals(HandlerSettings.TFSProviderUser, StringComparison.OrdinalIgnoreCase))
            {
                if (aboutStr.Length == 0) 
                {
                    // We do not have a linked TR, we need to create
                    createTR(workItem, notification, user, ref status);
                }
                else
                {
                    Uri about = new Uri(aboutStr);
                    Status statusAssign = null;

                    EnterpriseChangeRequest ecr = ECRMapper.mapFromWorkitem(workItem, about, ECRMapper.ACTION_ASSIGN);
                    TeamFoundationIdentity assignedTo = HandlerSettings.GetSignumForAssignedTo(workItem);
                    if (user != null && assignedTo != null && !user.Equals(HandlerSettings.GetUserFromSignum(assignedTo.UniqueName), StringComparison.OrdinalIgnoreCase))
                    {
                        statusAssign = new Status();   
                        ecr.SetOwner(user);
                        callUpdateTR(ecr, user, ECRMapper.TR_STATE_ASSIGNED_S, ref statusAssign);
                     }   

                     // We have a TR linked which might need to be updated
                     updateTR(workItem, notification, user, about, ref status);

                     if (statusAssign != null && statusAssign.OK)
                     {
                         ecr.SetOwner(HandlerSettings.GetUserFromSignum(assignedTo.UniqueName));
                         callUpdateTR(ecr, user, ECRMapper.TR_STATE_ASSIGNED_S, ref statusAssign);
                     }
                }
            }    

            // Handle update of Bug 
            ECRMapper.updateBug(status, user, workItem);
        } 

        // UC 6: Create a TR based on a Bug.
        // Return if the bug is updated or not.
        private void createTR(
            Microsoft.TeamFoundation.WorkItemTracking.Client.WorkItem workItem,
            WorkItemChangedEvent notification,
            String user,
            ref Status status)
        {
            // Create the ECR
            callCreateTR(ECRMapper.mapFromWorkitem(workItem, null, ECRMapper.ACTION_CREATE), user, ref status);
            if (!status.OK)
            {
                return;
            }

            // UC 3 (one case): Update all changed attributes before handling any state change

            // Note: If adding an entry in History on create this should propagate to Progress Info, but
            // this caused Bug to appear dirty after Save, see issue 79. Given almost all fields are used on
            // create, it does not make sense to make extra update as below. TBD if we should re-add in a
            // changed form.

            //EnterpriseChangeRequest updatedEcr = ECRMapper.mapFromUpdatedWorkitem(workItem, status.TRAbout, notification);
            //if (updatedEcr != null)
            //{
            //    // Note: Failure to update will be Warning not Error, so status still OK.
            //    callUpdateTR(updatedEcr, user, null, ref status);
            //}

            // Set the ECR in state Registered
            EnterpriseChangeRequest ecr = ECRMapper.mapFromWorkitem(workItem, status.TRAbout, ECRMapper.ACTION_REGISTER_ROUTE);
            callUpdateTR(ecr, user, ECRMapper.TR_STATE_REGISTERED_S, ref status);
            if (!status.OK)
            {
                return;
            }

            // Set the ECR in state Assigned if we have a defined owner
            ecr = ECRMapper.mapFromWorkitem(workItem, status.TRAbout, ECRMapper.ACTION_ASSIGN);
            if (ecr.GetOwner() != null && ecr.GetOwner().Length > 0)
            {
                callUpdateTR(ecr, user, ECRMapper.TR_STATE_ASSIGNED_S, ref status);
                if (!status.OK)
                {
                    return;
                }
            }

            if (status.OK)
            {
                HandlerSettings.LogMessage(
                    String.Format("Created TR based on workitem named: {0}", workItem.Title),
                    HandlerSettings.LoggingLevel.INFO);
            }
        }

        // Update the connected TR. 
        private void updateTR(
            Microsoft.TeamFoundation.WorkItemTracking.Client.WorkItem workItem,
            WorkItemChangedEvent notification,
            String user,
            Uri about,
            ref Status status)
        {
            String oldState, newState, action;
            ECRMapper.getStatusChange(notification, workItem, out oldState, out newState, out action);

            if (action != null)
            {
                if (action.Equals(ECRMapper.ACTION_IGNORE))
                {
                    // Ignore all changes
                    return;
                }

                if (action.Equals(ECRMapper.ACTION_DUPLICATE))
                {
                    // UC 9.4: Duplicate - Find Bug with duplicate id, get attached TR and send action duplicate

                    // Update needs to be done before setting the Bug as Duplicate as the TR can't change after.
                    updateTRFields(workItem, notification, user, about, ref status);
                    
                    EnterpriseChangeRequest ecr = ECRMapper.mapFromWorkitem(workItem, about, ECRMapper.ACTION_DUPLICATE);

                    if (ecr.GetPrimaryTR() == null || ecr.GetPrimaryTR().Length == 0)
                    {
                        // Failed to find a Bug with a connected TR. Log error and return.
                        String msg = String.Format(
                            "Failed to find duplicate to Bug: {0}, or duplicate Bug had not TR Link set",
                            workItem.Title);
                        HandlerSettings.LogMessage(msg, HandlerSettings.LoggingLevel.ERROR);
                        status.ErrorMessage = msg;
                        return;
                    }

                    callUpdateTR(ecr, user, null, ref status);
                    if (!status.OK)
                    {
                        // Log issue, but continue if e.g. a state change
                        HandlerSettings.LogMessage(
                            String.Format("Failed to set TR as duplicate based on Bug: {0}", workItem.Title),
                            HandlerSettings.LoggingLevel.WARN);
                    }

                    // After setting Bug/TR as Duplicated, no futher changes should be propagated.
                    return;
                }
                
                if (action.Equals(ECRMapper.ACTION_UNDUPLICATE))
                {
                    // UC 9.4: Unduplicate - Move TR back to Registered by sending action unduplicate
                    EnterpriseChangeRequest ecr = ECRMapper.mapFromWorkitem(workItem, about, ECRMapper.ACTION_UNDUPLICATE);
                    callUpdateTR(ecr, user, null, ref status);
                    if (!status.OK)
                    {
                        // Log issue, but continue if e.g. a state change
                        HandlerSettings.LogMessage(
                            String.Format("Failed to unduplicate TR based on Bug ", workItem.Title),
                            HandlerSettings.LoggingLevel.WARN);
                    }

                    // Update needs to be done after "Unduplicating" the Bug as the TR can't change before.
                    updateTRFields(workItem, notification, user, about, ref status);

                    // Return before handling a possible state change as states can be out of sync
                    // (normal case) when Bug has been Duplicate. Bug then Resolved / Closed, and 
                    // TR in an Active state. User needs to set TR state to Active.
                    return;
                }

                else if (action.Equals(ECRMapper.ACTION_CHANGE_PRODUCT))
                {
                    // UC 10.2: Update of the release - propagate info to TR
                    EnterpriseChangeRequest ecr = ECRMapper.mapFromWorkitem(workItem, about, ECRMapper.ACTION_CHANGE_PRODUCT);

                    bool noProduct = ecr.GetProduct().Length == 0;
                    if (noProduct)
                    {
                        // No product found, so value is for internal release - add message in notebook
                        ecr.SetNotebook("The referenced design item was moved to a product internal to Design");
                    }

                    callUpdateTR(ecr, user, null, ref status);
                    if (!status.OK)
                    {
                        // Log issue, but continue if e.g. a state change
                        HandlerSettings.LogMessage(
                            String.Format("Failed to change product on TR based on Bug ", workItem.Title),
                            HandlerSettings.LoggingLevel.WARN);
                    }
                }
            }

            updateTRFields(workItem, notification, user, about, ref status);

            if (newState != null)
            {
                // Handle state change
                updateTRState(workItem, oldState, newState, user, about, ref status); ;
            } 
        }

        // Update the changed fields that are mapped. Return if the bug is updated or not.
        private void updateTRFields(
            Microsoft.TeamFoundation.WorkItemTracking.Client.WorkItem workItem,
            WorkItemChangedEvent notification,
            String user,
            Uri about,
            ref Status status)
        {
            // UC 3 (one case): Update all changed attributes before handling any state change.
            EnterpriseChangeRequest updatedEcr = ECRMapper.mapFromUpdatedWorkitem(workItem, about, notification);
            if (updatedEcr != null)
            {
                // Note: Failure to update will be Warning not Error, so status still OK.
                callUpdateTR(updatedEcr, user, null, ref status);
            }
        }

        // Update the connected TR State. Return if the bug is updated or not. 
        private void updateTRState(
            Microsoft.TeamFoundation.WorkItemTracking.Client.WorkItem workItem,
            String oldState, String newState,
            String user,
            Uri about,
            ref Status status)
        {
            status.TRState = ECRMapper.getTRState(workItem);

            if (oldState.Equals(ECRMapper.BUG_STATE_ACTIVE, StringComparison.OrdinalIgnoreCase) &&
                newState.Equals(ECRMapper.BUG_STATE_RESOLVED, StringComparison.OrdinalIgnoreCase))
            {
                // UC 4: Updated TR based on the Bug's Active -> Resolve state change

                EnterpriseChangeRequest ecr = null;
                if (status.TRState.Equals(ECRMapper.TR_STATE_ASSIGNED))
                {
                    ecr = ECRMapper.mapFromWorkitem(workItem, about, ECRMapper.ACTION_PROPOSE);
                    callUpdateTR(ecr, user, ECRMapper.TR_STATE_PROPOSED_S, ref status);
                    if (!status.OK)
                    {
                        return;
                    }
                }

                if (status.TRState.Equals(ECRMapper.TR_STATE_PROPOSED))
                {
                    String answerCode = "";
                    if (ecr != null)
                    {
                        answerCode = ecr.GetAnswerCode();
                    }
                    else
                    {
                        answerCode = AttributesMapper.GetTfsValueForEcrKey(TFSMapper.ECM_ANSWER_CODE, workItem);
                    }

                    if (answerCode.Contains("A") || answerCode.Contains("D") || answerCode.Contains("B11"))
                    {
                        // In case Answer Code = A*, D*, or B11 go directly to Answer
                        callUpdateTR(ECRMapper.mapFromWorkitem(workItem, about, ECRMapper.ACTION_ANSWER),
                            user, ECRMapper.TR_STATE_TECH_ANSW_PROV_S, ref status);
                        return;
                    }
                    else
                    {
                        callUpdateTR(ECRMapper.mapFromWorkitem(workItem, about, ECRMapper.ACTION_APPROVE),
                            user, ECRMapper.TR_STATE_PROP_APPROV_S, ref status);
                    }
                }

                String expectedState = ECRMapper.TR_STATE_PROP_APPROV;
                if (!status.TRState.Equals(expectedState))
                {
                    // Incorrect pre condition
                    HandlerSettings.LogMessage(
                        String.Format("Expected TR State: {0}, current TR state: {1}", expectedState, status.TRState),
                        HandlerSettings.LoggingLevel.WARN);
                }

                // Note: Here we assume we are in trState PA, as we accepted AS, PP and PA. If we have not failed
                // before, this is where we will fail if any assumption was wrong -> error message.
                if (status.OK)
                {
                    callUpdateTR(ECRMapper.mapFromWorkitem(workItem, about, ECRMapper.ACTION_VERIFY),
                        user, ECRMapper.TR_STATE_TECH_ANSW_PROV_S, ref status);
                }
            }

            else if (oldState.Equals(ECRMapper.BUG_STATE_RESOLVED, StringComparison.OrdinalIgnoreCase) &&
                    newState.Equals(ECRMapper.BUG_STATE_CLOSED, StringComparison.OrdinalIgnoreCase))
            {
                // Update a TR based on Close of the Bug's Resolve -> Close state change

                String expectedState = ECRMapper.TR_STATE_TECH_ANSW_PROV;
                if (!status.TRState.Equals(expectedState))
                {
                    // Incorrect pre condition
                    HandlerSettings.LogMessage(
                        String.Format("Expected TR State: {0}, current TR state: {1}", expectedState, status.TRState),
                        HandlerSettings.LoggingLevel.WARN);
                }

                // TODO: Put the condition in configuration file etc.
                String answerCode = AttributesMapper.GetTfsValueForEcrKey(TFSMapper.ECM_ANSWER_CODE, workItem);
                if (answerCode.Equals("D4"))
                {
                    // UC 9.3 - Bug set as Postponed
                    callUpdateTR(ECRMapper.mapFromWorkitem(workItem, about, ECRMapper.ACTION_ACCEPT),
                        user, ECRMapper.TR_STATE_POSTPONED_S, ref status);
                }
                else if (answerCode.Equals("Duplicate"))
                {
                    // UC 9.4 - Bug set as Duplicate
                    callUpdateTR(ECRMapper.mapFromWorkitem(workItem, about, ECRMapper.ACTION_DUPLICATE), user, null, ref status);
                }
                else
                {
                    // UC 9.1 - Close of Bug from TFS
                    callUpdateTR(ECRMapper.mapFromWorkitem(workItem, about, ECRMapper.ACTION_FINISH), user,
                        ECRMapper.TR_STATE_FINISHED_S, ref status);
                }
            }

            else if (oldState.Equals(ECRMapper.BUG_STATE_RESOLVED, StringComparison.OrdinalIgnoreCase) &&
                    newState.Equals(ECRMapper.BUG_STATE_ACTIVE, StringComparison.OrdinalIgnoreCase))
            {
                // UC 9.2: Update a TR based the Bug's Resolved -> Active state change

                callUpdateTR(ECRMapper.mapFromWorkitem(workItem, about, ECRMapper.ACTION_REJECT), user,
                    ECRMapper.TR_STATE_REGISTERED_S, ref status);
                if (!status.OK)
                {
                    return;
                }

                // If Bug is assigned to a user we need to drive change back to Assigned state
                EnterpriseChangeRequest ecr = ECRMapper.mapFromWorkitem(workItem, about, ECRMapper.ACTION_ASSIGN);
                if (ecr.GetOwner() != null && ecr.GetOwner().Length > 0)
                {
                    callUpdateTR(ecr, user, ECRMapper.TR_STATE_ASSIGNED_S, ref status);
                }
            }
            else if (oldState.Equals(ECRMapper.BUG_STATE_CLOSED, StringComparison.OrdinalIgnoreCase) &&
                    newState.Equals(ECRMapper.BUG_STATE_ACTIVE, StringComparison.OrdinalIgnoreCase))
            {
                // UC 9.2: Update a TR based the Bug's Closed -> Active state change

                // In case of Closed -> Active, this is only allowed for the "no action" answer codes
                // in MHWeb, so OK for this to fail if selecting the incorrect answer code.
                // Could block in Bug.xml if we want to prevent some cases.

                callUpdateTR(ECRMapper.mapFromWorkitem(workItem, about, ECRMapper.ACTION_REACTIVATE), user,
                    ECRMapper.TR_STATE_REGISTERED_S, ref status);
                if (!status.OK)
                {
                    return;
                }

                // If Bug is assigned to a user we need to drive change back to Assigned state
                EnterpriseChangeRequest ecr = ECRMapper.mapFromWorkitem(workItem, about, ECRMapper.ACTION_ASSIGN);
                if (ecr.GetOwner() != null && ecr.GetOwner().Length > 0)
                {
                    callUpdateTR(ecr, user, ECRMapper.TR_STATE_ASSIGNED_S, ref status);
                }
            }

            else if (oldState.Equals(ECRMapper.BUG_STATE_ACTIVE, StringComparison.OrdinalIgnoreCase) &&
                    newState.Equals(ECRMapper.BUG_STATE_ACTIVE, StringComparison.OrdinalIgnoreCase))
            {
                // UC 3 (one case): Handle Assign case - state change for TR, attribute change for Bug

                // Incorrect pre condition
                String expectedState = ECRMapper.TR_STATE_REGISTERED;
                if (!status.TRState.Equals(expectedState))
                {  
                    HandlerSettings.LogMessage(
                        String.Format("Expected TR State: {0}, current TR state: {1}", expectedState, status.TRState),
                        HandlerSettings.LoggingLevel.WARN);
                }

                // Handle case when we are in state PR 
                if (status.TRState.Equals(ECRMapper.TR_STATE_PRIVATE))
                {
                    callUpdateTR(ECRMapper.mapFromWorkitem(
                            workItem, about, ECRMapper.ACTION_REGISTER_ROUTE),
                            user, ECRMapper.TR_STATE_REGISTERED_S, ref status);
                }

                callUpdateTR(ECRMapper.mapFromWorkitem(
                    workItem, about, ECRMapper.ACTION_ASSIGN), user, ECRMapper.TR_STATE_ASSIGNED_S, ref status);
            }
        }

        // Return if Bug is successfully saved or not. Note that Save will cause event that will be
        // triggering our code - but correctly filtered out as done by the functional user.
        public Boolean saveBug(
            Microsoft.TeamFoundation.WorkItemTracking.Client.WorkItem workItem)
        {
            try
            {
                workItem.Save();
                HandlerSettings.LogMessage(
                    String.Format("Saved workitem: {0}", workItem.Title),
                    HandlerSettings.LoggingLevel.INFO);
            }
            catch (Exception e)
            {
                // According to doc there are two exceptions that can be thrown:
                //      Microsoft.TeamFoundation.WorkItemTracking.Client.ValidationException
                //      Microsoft.TeamFoundation.WorkItemTracking.Client.DeniedOrNotExistException
                // but at least ValidationException is not recognized as subclass to Exception so compile error
                // See http://msdn.microsoft.com/en-us/library/microsoft.teamfoundation.workitemtracking.client.workitem.partialopen.aspx

                HandlerSettings.LogMessage(
                    String.Format("Failed to save workitem, error: {0}", e.Message),
                    HandlerSettings.LoggingLevel.ERROR);
                return false;
            }

            return true;
        }


        // ======================================================================================
        // REST calls to create or update 

        public void callCreateTR(EnterpriseChangeRequest ecr, String user, ref Status status)
        {
            HttpResponseMessage creationResponse = null;
            String message;

            try
            {
                creationResponse = getOslcClient().CreateResource(
                                HandlerSettings.CreationFactoryUri, ecr,
                                OslcMediaType.APPLICATION_RDF_XML,
                                OslcMediaType.APPLICATION_RDF_XML,
                                HandlerSettings.getRESTHeaders(user));
                message = (creationResponse != null) ?
                    creationResponse.Content.ReadAsStringAsync().Result :
                    "No result when calling " + HandlerSettings.CreationFactoryUri;
            }
            catch (Exception ex)
            {
                HandlerSettings.LogMessage(
                   String.Format("Failed to create ECR: '{0}'", ex.Message),
                   HandlerSettings.LoggingLevel.ERROR);
                status.ErrorMessage = "Failed to create TR from Bug. Error: '" + ex.Message + "'";
                return;
            }

            if (creationResponse == null || creationResponse.StatusCode != HttpStatusCode.Created)
            {
                HandlerSettings.LogMessage(
                    String.Format("Failed to create ECR. Error from MHWeb: '{0}'", message),
                    HandlerSettings.LoggingLevel.ERROR);
                status.ErrorMessage = "Failed to create TR from Bug. Error from MHWeb: '" + message + "'";
                return;
            }
            else
            {
                // Log the incoming rdf
                HandlerSettings.LogMessage(
                    String.Format("Response from create:\n{0}", message),
                    HandlerSettings.LoggingLevel.INFO);
            }

            EnterpriseChangeRequest newEcr = creationResponse.Content.
                ReadAsAsync<EnterpriseChangeRequest>(getOslcClient().GetFormatters()).Result;
            status.TRAbout = newEcr.GetAbout();
            String shortState = newEcr.GetStatus();
            status.TRState = ECRMapper.getLongTRState(shortState);

            HandlerSettings.LogMessage(
                String.Format("ECR named {0} created a location: {1}", newEcr.GetTitle(), status.TRAbout),
                HandlerSettings.LoggingLevel.INFO);

            // Verify result
            if (shortState == null || shortState != ECRMapper.TR_STATE_PRIVATE_S)
            {
                status.ExpectedStateMessage =
                    String.Format("Created ECR in wrong state: {0} expected: PR", shortState);
                HandlerSettings.LogMessage(status.ExpectedStateMessage, HandlerSettings.LoggingLevel.WARN);
            }

            return;
        }

        public void callUpdateTR(EnterpriseChangeRequest ecr, String user, String expectedState, ref Status status)
        {
            String message = null;
            HttpResponseMessage updateResponse = null;
            try
            {
                String about = ecr.GetAbout().ToString();
                updateResponse = getOslcClient().UpdateResource(
                            about, ecr,
                            OslcMediaType.APPLICATION_RDF_XML,
                            OslcMediaType.APPLICATION_RDF_XML,
                            HandlerSettings.getRESTHeaders(user));
                message = (updateResponse != null) ?
                    updateResponse.Content.ReadAsStringAsync().Result :
                    "No result when calling " + about;
            }
            catch (Exception ex)
            {
                HandlerSettings.LogMessage(
                    String.Format("Failed to update ECR: '{0}'", ex.Message),
                    HandlerSettings.LoggingLevel.ERROR);
                status.ErrorMessage = "Failed to update TR from Bug. Error: '" + ex.Message + "'";
                return;
            }

            if (updateResponse == null || updateResponse.StatusCode != HttpStatusCode.OK)
            {
                HandlerSettings.LogMessage(
                    String.Format("Failed to update ECR. Error from MHWeb: '{0}'", message),
                    HandlerSettings.LoggingLevel.ERROR);

                // If trying a state change, report as error - otherwise as warning
                if (expectedState != null)
                {
                    status.ErrorMessage = "Failed to update TR from Bug. Error from MHWeb: '" + message + "'";
                }
                else
                {
                    status.WarningMessage = "Warnings when update TR from Bug. Message from MHWeb: '" + message + "'";
                }
                return;
            }
            else
            {
                // Log the incoming rdf
                HandlerSettings.LogMessage(
                    String.Format("Response from update:\n{0}", message),
                    HandlerSettings.LoggingLevel.INFO);
            }

            // NOTE: MHWeb return an updated item. This is convenient, but not by spec.
            // Might need an explicit GET here to make more robust if other clients.
            EnterpriseChangeRequest newEcr = updateResponse.Content.
                ReadAsAsync<EnterpriseChangeRequest>(getOslcClient().GetFormatters()).Result;
            String shortState = newEcr.GetStatus();
            status.TRState = ECRMapper.getLongTRState(shortState);

            if (expectedState != null)
            {
                // Verify result if we did a state change

                if (shortState == null || shortState != expectedState)
                {
                    status.ExpectedStateMessage =
                        String.Format("The ECR is in wrong state: {0} expected: {1}", shortState, expectedState);
                    HandlerSettings.LogMessage(status.ExpectedStateMessage, HandlerSettings.LoggingLevel.WARN);
                }
                HandlerSettings.LogMessage(
                    String.Format("The ECR named {0} is updated to state: {1}", newEcr.GetTitle(), status.TRState),
                    HandlerSettings.LoggingLevel.INFO);
            }
            else
            {
                HandlerSettings.LogMessage(
                    String.Format("The ECR named: {0} is updated", newEcr.GetTitle()),
                    HandlerSettings.LoggingLevel.INFO);
            }

            return;
        }

        // For test. Return the Resource as content that would be transmitted over wire.
        public ObjectContent getResourceAsMessage(EnterpriseChangeRequest ecr, String user)
        {
            return getOslcClient().GetResourceAsMessage(
                            ecr,
                            OslcMediaType.APPLICATION_RDF_XML,
                            OslcMediaType.APPLICATION_RDF_XML,
                            HandlerSettings.getRESTHeaders(user));
        }
    }
}
