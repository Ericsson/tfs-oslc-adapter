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

using Microsoft.TeamFoundation.Framework.Client;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using OSLC4Net.Core.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Linq;

namespace TFSServerEventHandler.Mapping
{
    public class TFSMapper
    {
        private static TFSMapper instance;

        private TFSMapper()
        {
        }

        public static TFSMapper getInstance()
        {
            if (instance == null)
            {
                instance = new TFSMapper();
            }
            return instance;
        }

        /**
         *  Define field constants - if possible, replace with TFS SDK constants
         *  Fields are named according to the ECM Names or TFS if no ECM name exists
         *  but the field is required by the bug
         *  See Bug-TR mapping.xls
         */

        // Fields referenced in code. Definitions in properties.xml to insulate
        public static String TFS_STATE; //CoreFieldReferenceNames.State;
        public static String TFS_OWNER; //CoreFieldReferenceNames.AssignedTo;
        public static String TFS_HISTORY; //CoreFieldReferenceNames.History;
        public static String TFS_DESCRIPTION; //CoreFieldReferenceNames.Description;
        public static String TFS_CUSTOMERS_AFFECTED; 
        public static String TFS_SUBSTATE; 
        public static String TFS_FAULTY_PRODUCT;
        public static String TFS_DUPLICATEID;

        // TODO: Move into properties.xml
        public static String TFS_TITLE = "System.Title";

        // List values referenced in code. Definitions in properties.xml to insulate
        public static String TFS_SUBSTATE_DUPLICATE;

        // Fields added and needed by the integration. Definitions in properties.xml to insulate
        public static String ERICSSON_DEFECT_STATE_FIELD; 
        public static String ERICSSON_DEFECT_LINK_FIELD; 
        public static String ERICSSON_DEFECT_SYNCSTATE;
        public static String ERICSSON_DEFECT_CREATETR;

        // Not used as regular fields in Bug, but mapped in code to be resolved from TFS fields
        public static String ERICSSON_DEFECT_USER_SIGNUM = "Ericsson.Defect.User.Signum";
        public static String ERICSSON_DEFECT_USER_DISPLAYNAME = "Ericsson.Defect.User.DisplayName";
        public static String ERICSSON_DEFECT_CREATOR_SIGNUM = "Ericsson.Defect.Creator.Signum";
        public static String ERICSSON_DEFECT_HYPERLINK = "Ericsson.Defect.Hyperlink";


        // ECM Constants
        public const String ECM_TITLE = "dcterms:title";
        public const String ECM_IDENTIFIER = "dcterms:identifier";
        public const String ECM_ABOUT = "dcterms:about";
        public const String ECM_PRIORITY = "ecm:priority";
        public const String ECM_STATUS = "oslc_cm:status";
        public const String ECM_OWNER = "ecm:owner";
        public const String ECM_COUNTRY = "ecm:country";
        public const String ECM_CUSTOMER = "ecm:customer";
        public const String ECM_SITE = "ecm:site";
        public const String ECM_IMPACT_ON_ISP = "ecm:impactOnISP";
        public const String ECM_DIDDET = "ecm:diddet";
        public const String ECM_DESCRIPTION = "dcterms:description";
        public const String ECM_EXPECTED_IMPACT_ON_ISP = "ecm:expectedImpactOnISP";
        public const String ECM_FAULTCODE = "ecm:faultCode";
        public const String ECM_ACTIVITY = "ecm:activity";
        public const String ECM_ANSWER = "ecm:answer";
        public const String ECM_ANSWER_CODE = "ecm:answerCode";
        public const String ECM_FIRST_TECHNICAL_CONTACT = "ecm:firstTechnicalContact";
        public const String ECM_FIRST_TECHNICAL_CONTACT_INFO = "ecm:firstTechContactInfo";
        public const String ECM_NODE_PRODUCT = "ecm:nodeProduct";
        public const String ECM_NODE_PRODUCT_REVISION = "ecm:nodeProductRevision";
        public const String ECM_CORRECTED_PRODUCT = "ecm:correctedProduct";
        public const String ECM_CORRECTED_PRODUCT_REVISION = "ecm:correctedProductRevision";
        public const String ECM_CORRECTED_NODE_PRODUCT = "ecm:correctedNodeProduct";
        public const String ECM_CORRECTED_NODE_PRODUCT_REVISION = "ecm:correctedNodeProductRevision";
        public const String ECM_PRODUCT = "ecm:product";
        public const String ECM_PRODUCT_REVISION = "ecm:productRevision";
        public const String ECM_CURRENT_MHO = "ecm:currentMho";
        public const String ECM_NOTEBOOK = "ecm:notebook";
        public const String ECM_PROGRESSINFO = "ecm:progressInfo";
        public const String ECM_ATTACHMENT = "ecm:attachment";
        public const String ECM_RELEATED_CHANGE_REQUEST = "oslc_cm:relatedChangeRequest";
        public const String ECM_PRIMARYTR = "ecm:primaryTR";


        // Read TFS Filed names from config file
        public static bool initTFSFieldNames(XElement config)
        {
            String readProperties = "";
            try
            {
                XElement fieldEntries = config.Element("tfsFields");
                if (fieldEntries == null)
                {
                    HandlerSettings.LogMessage(
                        "No 'tfsFields' properties found - needed for connection. Exit.",
                        HandlerSettings.LoggingLevel.ERROR);
                    return false;
                }

                TFS_STATE = fieldEntries.Element("state").Attribute("key").Value;
                TFS_SUBSTATE = fieldEntries.Element("subState").Attribute("key").Value;
                TFS_OWNER = fieldEntries.Element("owner").Attribute("key").Value;
                TFS_FAULTY_PRODUCT = fieldEntries.Element("faultyProduct").Attribute("key").Value;

                TFS_CUSTOMERS_AFFECTED = fieldEntries.Element("customersAffected").Attribute("key").Value;
                TFS_DUPLICATEID = fieldEntries.Element("duplicate").Attribute("key").Value;
                TFS_SUBSTATE_DUPLICATE = fieldEntries.Element("duplicateValue").Attribute("key").Value;
                TFS_HISTORY = fieldEntries.Element("history").Attribute("key").Value;
                TFS_DESCRIPTION = fieldEntries.Element("description").Attribute("key").Value;

                ERICSSON_DEFECT_STATE_FIELD = fieldEntries.Element("trState").Attribute("key").Value;
                ERICSSON_DEFECT_LINK_FIELD = fieldEntries.Element("trLink").Attribute("key").Value;
                ERICSSON_DEFECT_SYNCSTATE = fieldEntries.Element("trSyncState").Attribute("key").Value;
                ERICSSON_DEFECT_CREATETR = fieldEntries.Element("trCreate").Attribute("key").Value;
            }
            catch (Exception e)
            {
                HandlerSettings.LogMessage(
                    String.Format(
                        "Exception when getting a tfsField property: {0}. Exit." +
                        "\nThe tfsField properties read before exception: {1}", e.Message, readProperties),
                    HandlerSettings.LoggingLevel.ERROR);
                return false;
            }

            HandlerSettings.LogMessage("The tfsField properties read: " + readProperties,
                    HandlerSettings.LoggingLevel.INFO);

            return true;
        }

        public List<String> mapToEcr(String ecmProperty, WorkItem workItem)
        {
            List<String> mappedValues = new List<String>();

            List<Property> properties = AttributesMapper.getInstance().getInverseProperties(ecmProperty);
            if (properties == null)
            {
                // Property has no inverse entry in mapping - inconsistent state
                HandlerSettings.LogMessage(
                    String.Format("Property: {0} has no inverse entry in mapping file", ecmProperty),
                    HandlerSettings.LoggingLevel.WARN);
                return mappedValues;
            }

            foreach (Property property in properties)
            {
                // Get the TFS fieldName mapped to the ecmProperty
                String fieldName = property.getValue();

                // Get the TFS fieldValue for the fieldName
                String fieldValue = getFieldValue(workItem, fieldName);

                // Handle "use" case
                String useMapping = property.getUseMapping();
                if (useMapping != null && useMapping.Equals("ProductMapping"))
                {
                    PRIMProduct primProduct = ProductMapper.getInstance().GetProduct(fieldValue);
                    if (primProduct == null)
                    {
                        // No product found for release, so this is not a maintenance Bug.
                        // Should likely have been caught before this, but return empty.
                        return mappedValues;
                    }

                    String useKey = property.getUseKey();
                    if (useKey != null)
                    {
                        if (useKey.Equals("primProdNo"))
                        {
                            mappedValues.Add(primProduct.getPrimProdNo());
                        }
                        else if (useKey.Equals("primRState"))
                        {
                            mappedValues.Add(primProduct.getPrimRState());
                        }
                    }

                    return mappedValues;
                }

                // Get or adjust the TFS value in case of complex mapping
                fieldValue = getTFSFieldValue(workItem, fieldName, fieldValue);

                // Lookup value in the property value map table. If mapped value is found,
                // this is added. Else if defaultValue is defined, this is used. Else the
                // fieldValue itself is returned.

                List<String> values = property.getInverse(fieldValue, workItem);
                if (values != null)
                {
                    foreach (String mappedValue in values)
                    {
                        String adjustedValue = adjustTFSFieldValue(workItem, fieldName, mappedValue);
                        mappedValues.Add(adjustedValue);
                    }
                }
            }

            return mappedValues;
        }

        private String getFieldValue(WorkItem workItem, String fieldName)
        {
            String fieldValue = null;
            if (workItem.Fields.Contains(fieldName) && workItem.Fields[fieldName].Value != null)
            {
                fieldValue = workItem.Fields[fieldName].Value.ToString();
            }
            return fieldValue;
        }

        // Return a String value if anything mapped or null if no value 
        private String getTFSFieldValue(WorkItem workItem, String fieldName, String fieldValue)
        {
            TeamFoundationIdentity member;

            if (fieldName.Equals(TFS_CUSTOMERS_AFFECTED))
            {
                // This is a multivalue field. We will return only the first entry.
                // Format: [val1];[val2]; - should return val1

                if (fieldValue != null && fieldValue.Length > 0 && fieldValue.StartsWith("["))
                {
                    return fieldValue.Substring(1, fieldValue.IndexOf(']') - 1);
                }
                return fieldValue;

            }
            else if (fieldName.Equals(TFS_TITLE))
            {
                // Format of System.Title is String and type of the title field in the OSLC resource is XMLLiteral 
                // so will not be encoded by rdf formatter. So it needs to be explicitly encoded to pass e.g. &
                // and > and < over correct.
                if (fieldValue != null)
                {
                    fieldValue = HttpUtility.HtmlEncode(fieldValue);
                }
                return fieldValue;

            }
            else if (fieldName.Equals(TFS_DESCRIPTION))
            {
                // The format of System.Description is HTML and type of the title field in the OSLC resource 
                // is XMLLiteraland OSLC resource is XMLLiteral. Single & < and > will be correctly escaped from
                // the System.Description, but correct html content like lists etc has to be escaped.
                if (fieldValue != null)
                {
                    fieldValue = HttpUtility.HtmlEncode(fieldValue);
                }
                return fieldValue;

            }
            else if (fieldName.Equals(TFS_HISTORY))
            {
                // The System.History field is always empty after save, but it's value is stored in the
                // latest revision of the History field given something is added there - else null.

                // No need for encode as for Title and Description as OSLC resource format is not XMLLiteral.

                Revision lastRev = workItem.Revisions[workItem.Revision - 1];
                fieldValue = lastRev.Fields[TFS_HISTORY].Value.ToString();
                return (fieldValue == null || fieldValue.Length == 0) ? null : fieldValue;

            }
            else if (fieldName.Equals(TFS_DUPLICATEID))
            {
                // The field value is the id of the duplicate Bug. We need to find the corresponding TR id.

                String wiql = "SELECT [System.Id] FROM WorkItems WHERE [System.Id] = '" + fieldValue + "'";
                WorkItemCollection wic = workItem.Store.Query(wiql);
                if (wic.Count != 1)
                {
                    return null;
                }
                WorkItem targetWorkItem = workItem.Store.GetWorkItem(wic[0].Id);
                return HandlerSettings.GetIDFromLink(targetWorkItem);
            }
            else if (fieldName.Equals(ERICSSON_DEFECT_USER_SIGNUM))
            {
                // Also check so member is not a container (group) - needs to be a real user
                member = HandlerSettings.GetSignumForAssignedTo(workItem);
                return (member != null && !member.IsContainer) ?
                    HandlerSettings.GetUserFromSignum(member.UniqueName) :
                    null;
            }
            else if (fieldName.Equals(ERICSSON_DEFECT_USER_DISPLAYNAME))
            {
                // Also check so member is not a container (group) - needs to be a real user
                member = HandlerSettings.GetSignumForAssignedTo(workItem);
                return (member != null && !member.IsContainer) ?
                    member.DisplayName :
                    null;

            }
            else if (fieldName.Equals(ERICSSON_DEFECT_HYPERLINK))
            {
                foreach (Microsoft.TeamFoundation.WorkItemTracking.Client.Link link in workItem.Links)
                {
                    if (link is Hyperlink)
                    {
                        String urlString = ((Hyperlink)link).Location;
                        fieldValue = (fieldValue == null || fieldValue.Length == 0) ? urlString : fieldValue + ";" + urlString;
                    }    
                }
                return fieldValue;
            }
            else if (fieldName.Equals(ERICSSON_DEFECT_LINK_FIELD))
            {
                // Get the uri for the TR based on value in Bug field.
                String id = HandlerSettings.GetIDFromLink(workItem);
                return HandlerSettings.GetUriForTR(id);
            }
                
            return fieldValue;
        }

        private String adjustTFSFieldValue(WorkItem workItem, String fieldName, String fieldValue)
        {
            if (fieldName.Equals(TFS_HISTORY))
            {
                Revision lastRev = workItem.Revisions[workItem.Revision - 1];

                if (fieldValue != null && fieldValue.Length > 0)
                {
                    // Add timestamp to allow the TFS OSLC Provider to recognize the entry as being
                    // added by TFS. Could move into config file to make more customizable.
                    // NOTE: Syntax needs to be coordinated with the TFS OSLC Provider.

                    DateTime dateTime = (DateTime)lastRev.Fields["Changed Date"].Value;
                    String date = dateTime.ToString("yyyy-MM-dd HH:mm:ss");
                    String tag = "[TFS " + date + "] ";
                    fieldValue = tag + fieldValue;
                }
            }

            return fieldValue;
        }

        // Return if there is a change of hyperlinks since last revision
        public bool hasLinksChanged(WorkItem workItem)
        {
            // Apparently we need to check on second last revision to get the correct
            // value for the previous links. This does not show in UI, but is clear when
            // debugging. So check we have > 2 revisions, and then compare.

            if (workItem.Revision < 3)
            {
                return true;
            }

            Revision lastRev = workItem.Revisions[workItem.Revision - 2];
            List<Hyperlink> oldLinks = getHyperlinks(lastRev.Links);
            List<Hyperlink> newLinks = getHyperlinks(workItem.Links);

            // If number of links has changed, there has been a change.
            if (oldLinks.Count != newLinks.Count)
            {
                return true;
            }

            // If same number of links - check that each link in the new collection has one identical
            // link in the old collection by removing equals as we find them. If oldLinks.Count != 0
            // when we are done, the link list has changed.
            foreach (Hyperlink newLink in newLinks)
            {
                for (int i = 0; i < oldLinks.Count; i++)
                {
                    Hyperlink oldLink = oldLinks[i];
                    if (newLink.Location.Equals(oldLink.Location) && newLink.Comment.Equals(oldLink.Comment))
                    {
                        oldLinks.RemoveAt(i);
                    }
                }
            }
            return (oldLinks.Count != 0);
        }

        private List<Hyperlink> getHyperlinks(LinkCollection links)
        {
            List<Hyperlink> hyperlinks = new List<Hyperlink>();
            foreach (Microsoft.TeamFoundation.WorkItemTracking.Client.Link link in links)
            {
                if (link is Hyperlink)
                {
                    hyperlinks.Add((Hyperlink)link);
                }
            }
            return hyperlinks;
        }

        // Return true if we have an updated value, otherwise false
        public bool setEcrValues(EnterpriseChangeRequest ecr, String ecmProperty, WorkItem workItem)
        {
            return setEcrValue(ecr, ecmProperty, mapToEcr(ecmProperty, workItem));
        }

        // Set the value of the ecmProperty. Note that currently there *might* be > 1 value and than we
        // will overwrite so last value wins. Return true if the ecr is updated
        public bool setEcrValue(EnterpriseChangeRequest ecr, String ecmProperty, List<String> values) {
            if (values.Count == 0)
            {
                return false;
            }

            bool updated = true;
            foreach (String value in values) {
                switch (ecmProperty) {
                    case ECM_ABOUT:
                        ecr.SetAbout(new Uri(value));
                        break;
                    case ECM_TITLE:
                        ecr.SetTitle(value);
                        break;
                    case ECM_IDENTIFIER:
                        ecr.SetIdentifier(value);
                        break;
                    case ECM_PRIORITY:
                        ecr.SetPriority(value);
                        break;
                    case ECM_OWNER:
                        ecr.SetOwner(value);
                        break;
                    case ECM_COUNTRY:
                        ecr.SetCountry(value);
                        break;
                    case ECM_CUSTOMER:
                        ecr.SetCustomer(value);
                        break;
                    case ECM_SITE:
                        ecr.SetSite(value);
                        break;
                    case ECM_DIDDET:
                        ecr.SetDiddet(value);
                        break;
                    case ECM_DESCRIPTION:
                        ecr.SetDescription(value);
                        break;
                    case ECM_ANSWER:
                        ecr.SetAnswer(value);
                        break;
                    case ECM_ANSWER_CODE:
                        ecr.SetAnswerCode(value);
                        break;
                    case ECM_FAULTCODE:
                        ecr.SetFaultCode(value);
                        break;
                    case ECM_ACTIVITY:
                        ecr.SetActivity(value);
                        break;
                    case ECM_FIRST_TECHNICAL_CONTACT:
                        // TODO: No immediate need to add this
                        // ecr.SetFirstTechnicalContact(value);
                        updated = false;
                        break;
                    case ECM_FIRST_TECHNICAL_CONTACT_INFO:
                        ecr.SetFirstTechContactInfo(value);
                        break;
                    case ECM_STATUS:
                        ecr.SetStatus(value);
                        break;
                    case ECM_CURRENT_MHO:
                        ecr.SetCurrentMho(value);
                        break;
                    case ECM_PRODUCT:
                        ecr.SetProduct(value);
                        break;
                    case ECM_PRODUCT_REVISION:
                        ecr.SetProductRevision(value);
                        break;
                    case ECM_NODE_PRODUCT:
                        ecr.SetNodeProduct(value);
                        break;
                    case ECM_NODE_PRODUCT_REVISION:
                        ecr.SetNodeProductRevision(value);
                        break;
                    case ECM_CORRECTED_PRODUCT:
                        ecr.SetCorrectedProduct(value);
                        break;
                    case ECM_CORRECTED_PRODUCT_REVISION:
                        ecr.SetCorrectedProductRevision(value);
                        break;
                    case ECM_CORRECTED_NODE_PRODUCT:
                        ecr.SetCorrectedNodeProduct(value);
                        break;
                    case ECM_CORRECTED_NODE_PRODUCT_REVISION:
                        ecr.SetCorrectedNodeProductRevision(value);
                        break;
                    case ECM_ATTACHMENT:
                        if (value.Length > 0)
                        {
                            String[] urls = value.Split(';');
                            foreach (String url in urls)
                            {
                                OSLC4Net.Core.Model.Link link = new OSLC4Net.Core.Model.Link(new Uri(url));
                                link.SetLabel(url);
                                ecr.AddAttachment(link);
                            }
                        }
                      
                        break;
                    case ECM_NOTEBOOK:
                        ecr.SetNotebook(value);
                        break;
                    case ECM_PROGRESSINFO:
                        ecr.SetProgressInfo(value);
                        break;
                    case ECM_EXPECTED_IMPACT_ON_ISP:
                        ecr.SetExpectedImpactOnISP(value);
                        break;
                    case ECM_IMPACT_ON_ISP:
                        ecr.SetImpactOnISP(value);
                        break;
                    case ECM_RELEATED_CHANGE_REQUEST:
                        Uri resource = new Uri(value);
                        String label = HandlerSettings.GetIDFromUri(value);
                        OSLC4Net.Core.Model.Link relatedChangeRequest = new OSLC4Net.Core.Model.Link(resource, label);
                        ecr.AddRelatedChangeRequest(relatedChangeRequest);
                        break;
                    default:
                        updated = false;
                        break;
                    }
            }
            return updated;
        }

        /**
         * Gets the ECR value for the given ecmProperty
         * 
         * @param ecr
         * @param ecmProperty
         * @return
         */
        private static String getEcrValue(EnterpriseChangeRequest ecr, String ecmProperty)
        {
            switch (ecmProperty)
            {
                case ECM_TITLE:
                    return ecr.GetTitle();
                case ECM_IDENTIFIER:
                    return ecr.GetIdentifier();
                case ECM_PRIORITY:
                    return ecr.GetPriority();
                case ECM_OWNER:
                    return ecr.GetOwner();
                case ECM_COUNTRY:
                    return ecr.GetCountry();
                case ECM_CUSTOMER:
                    return ecr.GetCustomer();
                case ECM_SITE:
                    return ecr.GetSite();
                case ECM_DIDDET:
                    return ecr.GetDiddet();
                case ECM_DESCRIPTION:
                    return ecr.GetDescription();
                case ECM_ANSWER:
                    return ecr.GetAnswer();
                case ECM_ANSWER_CODE:
                    return ecr.GetAnswerCode();
                case ECM_FAULTCODE:
                    return ecr.GetFaultCode();
                case ECM_ACTIVITY:
                    return ecr.GetActivity();
                case ECM_FIRST_TECHNICAL_CONTACT:
                    return ecr.GetFirstTechnicalContact();
                case ECM_FIRST_TECHNICAL_CONTACT_INFO:
                    return ecr.GetFirstTechContactInfo();
                case ECM_STATUS:
                    return ecr.GetStatus();
                case ECM_CURRENT_MHO:
                    return ecr.GetCurrentMho();
                case ECM_PRODUCT:
                    return ecr.GetProduct();
                case ECM_PRODUCT_REVISION:
                    return ecr.GetProductRevision();
                case ECM_NODE_PRODUCT:
                    return ecr.GetNodeProduct();
                case ECM_NODE_PRODUCT_REVISION:
                    return ecr.GetNodeProductRevision();
                case ECM_CORRECTED_PRODUCT:
                    return ecr.GetCorrectedProduct();
                case ECM_CORRECTED_PRODUCT_REVISION:
                    return ecr.GetCorrectedProductRevision();
                case ECM_CORRECTED_NODE_PRODUCT:
                    return ecr.GetCorrectedNodeProduct();
                case ECM_CORRECTED_NODE_PRODUCT_REVISION:
                    return ecr.GetCorrectedNodeProductRevision();
                case ECM_ATTACHMENT:
                    return null;
                case ECM_NOTEBOOK:
                    return ecr.GetNotebook();
                case ECM_PROGRESSINFO:
                    return ecr.GetProgressInfo();
                case ECM_EXPECTED_IMPACT_ON_ISP:
                    return ecr.GetExpectedImpactOnISP();
                case ECM_IMPACT_ON_ISP:
                    return ecr.GetImpactOnISP();
                case ECM_RELEATED_CHANGE_REQUEST:
                    OSLC4Net.Core.Model.Link[] related = ecr.GetRelatedChangeRequests();
                    return related[0].GetValue().ToString();
            }
            return "";
        }
    }
}
