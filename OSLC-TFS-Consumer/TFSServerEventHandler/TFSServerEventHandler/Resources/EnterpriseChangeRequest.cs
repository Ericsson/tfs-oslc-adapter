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

using OSLC4Net.Client.Oslc.Resources;
using OSLC4Net.Core.Attribute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OSLC4Net.Core.Model;
using TFSServerEventHandler.Resources;

namespace TFSServerEventHandler
{
    /// <summary>
    /// OSLC Change Management resource
    /// </summary>
    [OslcNamespace(Constants.ENTERPRISE_CHANGE_MANAGEMENT_NAMESPACE)]
    [OslcResourceShape(title = "Enterprise Change Request Resource Shape", describes = new string[] { CmConstants.TYPE_CHANGE_REQUEST })]
    public class EnterpriseChangeRequest : ChangeRequest
    {
        // Note: It seems like the attribute name needs to be same as OSLC property definition
        // See code at http://oslc4net.codeplex.com/SourceControl/latest#OSLC4Net_SDK/JsonProvider/JsonHelper.cs

        // From OSLC CM v3 spec
//        private Severity severity;
//        private Priority priority;

        // From ecm domain.
        private String currentMho;
        private String diddet;
        private String product;
        private String productRevision;
        private String nodeProduct;
        private String nodeProductRevision;
        private String correctedProduct;
        private String correctedProductRevision;
        private String correctedNodeProduct;
        private String correctedNodeProductRevision;
        private String firstTechnicalContact;
        private String firstTechContactInfo;
        private String activity;
        private String country;
        private String site;
        private String customer; 
        private String impactOnISP;
        private String expectedImpactOnISP;
        private String answerCode;
        private String answer;
        private String faultCode;
        private String progressInfo;
        private String notebook;
        private String primaryTR;

        // TEST
        //private Discussion progressInfo2;
        //private Discussion notebook2;

        private ISet<Link> attachments = new HashSet<Link>();

        // Action attribute for state change 
        private String action;

        // Temporary ecm domain.
        private String priority;
        private String owner;

        // ==========================================================
        // Note: From cm spec v3. 
/*
        [OslcAllowedValue(new string[] { "Unclassified", "Minor", "Normal", "Major", "Critical", "Blocker" })]
        [OslcDescription("Note: From cm spec v3. Severity of the item.")]
        [OslcPropertyDefinition(CmConstants.CHANGE_MANAGEMENT_NAMESPACE + "severity")]
        [OslcOccurs(Occurs.ExactlyOne)]
        [OslcTitle("Severity")]
        public String GetSeverity()
        {
            return SeverityExtension.ToString(severity);
        }


        [OslcAllowedValue(new string[] { "PriorityUnassigned", "Low", "Medium", "High" })]
        [OslcDescription("Note: From cm spec v3. Priority of the item.")]
        [OslcPropertyDefinition(CmConstants.CHANGE_MANAGEMENT_NAMESPACE + "priority")]
        [OslcOccurs(Occurs.ExactlyOne)]
        [OslcTitle("Priority")]
        public String GetPriority()
        {
            return PriorityExtension.ToString(priority);
        }
*/
        // ==========================================================
        // To be replaced with corresponding OSLC CM constructs

        // TEST

        //[OslcDescription("The notebook info.")]
        //[OslcName("notebook2")]
        //[OslcPropertyDefinition(Constants.ENTERPRISE_CHANGE_MANAGEMENT_NAMESPACE + "notebook2")]
        //[OslcRange(CmConstants.TYPE_DISCUSSION)]
        //[OslcOccurs(Occurs.ZeroOrOne)]
        //[OslcRepresentation(Representation.Inline)]
        //[OslcTitle("Notebook2")]
        //[OslcValueShape(OslcConstants.PATH_RESOURCE_SHAPES + "/" + Constants.PATH_DISCUSSION)]
        //public Discussion GetNotebook2()
        //{
        //    return notebook2;
        //}

        //public void SetNotebook2(Discussion notebook2)
        //{
        //    this.notebook2 = notebook2;
        //}

        //[OslcDescription("The Progress Info.")]
        //[OslcName("progressInfo2")]
        //[OslcPropertyDefinition(Constants.ENTERPRISE_CHANGE_MANAGEMENT_NAMESPACE + "progressInfo2")]
        //[OslcRange(CmConstants.TYPE_DISCUSSION)]
        //[OslcOccurs(Occurs.ZeroOrOne)]
        //[OslcRepresentation(Representation.Inline)]
        //[OslcTitle("Progress Info")]
        //[OslcValueShape(OslcConstants.PATH_RESOURCE_SHAPES + "/" + Constants.PATH_DISCUSSION)]
        //public Discussion GetProgressInfo2()
        //{
        //    return progressInfo2;
        //}

        //public void SetProgressInfo2(Discussion progressInfo2)
        //{
        //    this.progressInfo2 = progressInfo2;
        //}

        // ===

        [OslcDescription("The EriRef of the primary TR in case this is set as a duplicate.")]
        [OslcName("primaryTR")]
        [OslcPropertyDefinition(Constants.ENTERPRISE_CHANGE_MANAGEMENT_NAMESPACE + "primaryTR")]
        [OslcRange(CmConstants.TYPE_CHANGE_REQUEST)]
        [OslcTitle("Primary TR")]
        public String GetPrimaryTR()
        {
            return primaryTR;
        }

        [OslcDescription("Attachments to the ECR.")]
        [OslcName("attachment")]
        [OslcPropertyDefinition(Constants.ENTERPRISE_CHANGE_MANAGEMENT_NAMESPACE + "attachment")]
        [OslcRange(CmConstants.TYPE_CHANGE_REQUEST)]
        [OslcTitle("Attachments")]
        public Link[] GetAttachments()
        {
            return attachments.ToArray();
        }

        [OslcDescription("The owner.")]
        [OslcPropertyDefinition(Constants.ENTERPRISE_CHANGE_MANAGEMENT_NAMESPACE + "owner")]
        [OslcTitle("Owner")]
        [OslcValueType(OSLC4Net.Core.Model.ValueType.XMLLiteral)]
        public String GetOwner()
        {
            return owner;
        }

        [OslcDescription("TBD Description")]
        [OslcPropertyDefinition(Constants.ENTERPRISE_CHANGE_MANAGEMENT_NAMESPACE + "firstTechnicalContact")]
        [OslcTitle("FirstTechnicalContact")]
        [OslcValueType(OSLC4Net.Core.Model.ValueType.XMLLiteral)]
        public String GetFirstTechnicalContact()
        {
            return firstTechnicalContact;
        }

        [OslcDescription("The MHWeb priority.")]
        [OslcPropertyDefinition(Constants.ENTERPRISE_CHANGE_MANAGEMENT_NAMESPACE + "priority")]
        [OslcTitle("MH Priority")]
        [OslcValueType(OSLC4Net.Core.Model.ValueType.XMLLiteral)]
        public String GetPriority()
        {
            return priority;
        }

        // ==========================================================

        [OslcDescription("The progress info.")]
        [OslcPropertyDefinition(Constants.ENTERPRISE_CHANGE_MANAGEMENT_NAMESPACE + "progressInfo")]
        [OslcOccurs(Occurs.ZeroOrOne)]
        [OslcTitle("Progress info")]
        public String GetProgressInfo()
        {
            return progressInfo;
        }

        [OslcDescription("The notebook info.")]
        [OslcPropertyDefinition(Constants.ENTERPRISE_CHANGE_MANAGEMENT_NAMESPACE + "notebook")]
        [OslcOccurs(Occurs.ZeroOrOne)]
        [OslcTitle("Notebook")]
        public String GetNotebook()
        {
            return notebook;
        }

        [OslcDescription("The fault code.")]
        [OslcPropertyDefinition(Constants.ENTERPRISE_CHANGE_MANAGEMENT_NAMESPACE + "faultCode")]
        [OslcOccurs(Occurs.ZeroOrOne)]
        [OslcTitle("Fault Code")]
        public String GetFaultCode()
        {
            return faultCode;
        }

        [OslcDescription("The experienced customer ISP (In Service Performance) code and the explanation.")]
        [OslcPropertyDefinition(Constants.ENTERPRISE_CHANGE_MANAGEMENT_NAMESPACE + "impactOnISP")]
        [OslcOccurs(Occurs.ZeroOrOne)]
        [OslcTitle("Experienced Impact On ISP")]
        public String GetImpactOnISP()
        {
            return impactOnISP;
        }

        [OslcDescription("The expected customer ISP (In Service Performance) code and the explanation.")]
        [OslcPropertyDefinition(Constants.ENTERPRISE_CHANGE_MANAGEMENT_NAMESPACE + "expectedImpactOnISP")]
        [OslcOccurs(Occurs.ZeroOrOne)]
        [OslcTitle("Expected Impact On ISP")]
        public String GetExpectedImpactOnISP()
        {
            return expectedImpactOnISP;
        }

        [OslcDescription("At what customer site was the issue reported.")]
        [OslcPropertyDefinition(Constants.ENTERPRISE_CHANGE_MANAGEMENT_NAMESPACE + "site")]
        [OslcOccurs(Occurs.ZeroOrOne)]
        [OslcTitle("Site")]
        public String GetSite()
        {
            return site;
        }

        [OslcDescription("Where was the issue reported.")]
        [OslcPropertyDefinition(Constants.ENTERPRISE_CHANGE_MANAGEMENT_NAMESPACE + "country")]
        [OslcOccurs(Occurs.ZeroOrOne)]
        [OslcTitle("Country")]
        public String GetCountry()
        {
            return country;
        }

        [OslcDescription("By what customer was the issue reported.")]
        [OslcPropertyDefinition(Constants.ENTERPRISE_CHANGE_MANAGEMENT_NAMESPACE + "customer")]
        [OslcOccurs(Occurs.ZeroOrOne)]
        [OslcTitle("Customer")]
        public String GetCustomer()
        {
            return customer;
        }

        [OslcDescription("When the defect was detected - DID DETECT.")]
        [OslcPropertyDefinition(Constants.ENTERPRISE_CHANGE_MANAGEMENT_NAMESPACE + "diddet")]
        [OslcOccurs(Occurs.ZeroOrOne)]
        [OslcTitle("DIDDET")]
        public String GetDiddet()
        {
            return diddet;
        }

        [OslcDescription("The activity (?)")]
        [OslcPropertyDefinition(Constants.ENTERPRISE_CHANGE_MANAGEMENT_NAMESPACE + "activity")]
        [OslcOccurs(Occurs.ZeroOrOne)]
        [OslcTitle("Activity")]
        public String GetActivity()
        {
            return activity;
        }

        [OslcDescription("Who the first technical contact is.")]
        [OslcPropertyDefinition(Constants.ENTERPRISE_CHANGE_MANAGEMENT_NAMESPACE + "firstTechContactInfo")]
        [OslcOccurs(Occurs.ZeroOrOne)]
        [OslcTitle("First Technical Contact")]
        public String GetFirstTechContactInfo()
        {
            return firstTechContactInfo;
        }

        [OslcDescription("The MHO.")]
        [OslcPropertyDefinition(Constants.ENTERPRISE_CHANGE_MANAGEMENT_NAMESPACE + "currentMho")]
        [OslcOccurs(Occurs.ExactlyOne)]
        [OslcTitle("Current MHO")]
        public String GetCurrentMho()
        {
            return currentMho;
        }

        [OslcDescription("The Product.")]
        [OslcPropertyDefinition(Constants.ENTERPRISE_CHANGE_MANAGEMENT_NAMESPACE + "product")]
        [OslcOccurs(Occurs.ExactlyOne)]
        [OslcTitle("Product")]
        public String GetProduct()
        {
            return product;
        }

        [OslcDescription("The Product Revision.")]
        [OslcPropertyDefinition(Constants.ENTERPRISE_CHANGE_MANAGEMENT_NAMESPACE + "productRevision")]
        [OslcOccurs(Occurs.ExactlyOne)]
        [OslcTitle("Product Revision")]
        public String GetProductRevision()
        {
            return productRevision;
        }

        [OslcDescription("The Node Product.")]
        [OslcPropertyDefinition(Constants.ENTERPRISE_CHANGE_MANAGEMENT_NAMESPACE + "nodeProduct")]
        [OslcOccurs(Occurs.ZeroOrOne)]
        [OslcTitle("Node Product")]
        public String GetNodeProduct()
        {
            return nodeProduct;
        }

        [OslcDescription("The Node Product Revision.")]
        [OslcPropertyDefinition(Constants.ENTERPRISE_CHANGE_MANAGEMENT_NAMESPACE + "nodeProductRevision")]
        [OslcOccurs(Occurs.ZeroOrOne)]
        [OslcTitle("Node Product Revision")]
        public String GetNodeProductRevision()
        {
            return nodeProductRevision;
        }

        [OslcDescription("The Corrected Product.")]
        [OslcPropertyDefinition(Constants.ENTERPRISE_CHANGE_MANAGEMENT_NAMESPACE + "correctedProduct")]
        [OslcOccurs(Occurs.ZeroOrOne)]
        [OslcTitle("Corrected Product")]
        public String GetCorrectedProduct()
        {
            return correctedProduct;
        }

        [OslcDescription("The Corrected Product Revision.")]
        [OslcPropertyDefinition(Constants.ENTERPRISE_CHANGE_MANAGEMENT_NAMESPACE + "correctedProductRevision")]
        [OslcOccurs(Occurs.ZeroOrOne)]
        [OslcTitle("Corrected Product Revision")]
        public String GetCorrectedProductRevision()
        {
            return correctedProductRevision;
        }

        [OslcDescription("The Corrected Node Product.")]
        [OslcPropertyDefinition(Constants.ENTERPRISE_CHANGE_MANAGEMENT_NAMESPACE + "correctedNodeProduct")]
        [OslcOccurs(Occurs.ZeroOrOne)]
        [OslcTitle("Corrected Node Product")]
        public String GetCorrectedNodeProduct()
        {
            return correctedNodeProduct;
        }

        [OslcDescription("The Corrected Node Product Revision.")]
        [OslcPropertyDefinition(Constants.ENTERPRISE_CHANGE_MANAGEMENT_NAMESPACE + "correctedNodeProductRevision")]
        [OslcOccurs(Occurs.ZeroOrOne)]
        [OslcTitle("Corrected Node Product Revision")]
        public String GetCorrectedNodeProductRevision()
        {
            return correctedNodeProductRevision;
        }

        [OslcDescription("The Answer Code.")]
        [OslcPropertyDefinition(Constants.ENTERPRISE_CHANGE_MANAGEMENT_NAMESPACE + "answerCode")]
        [OslcOccurs(Occurs.ZeroOrOne)]
        [OslcTitle("Answer Code")]
        public String GetAnswerCode()
        {
            return answerCode;
        }

        [OslcDescription("The Answer.")]
        [OslcPropertyDefinition(Constants.ENTERPRISE_CHANGE_MANAGEMENT_NAMESPACE + "answer")]
        [OslcOccurs(Occurs.ZeroOrOne)]
        [OslcTitle("Answer")]
        public String GetAnswer()
        {
            return answer;
        }

        [OslcDescription("An action that can change the state of the reciever.")]
        [OslcPropertyDefinition(Constants.ENTERPRISE_CHANGE_MANAGEMENT_NAMESPACE + "action")]
        [OslcOccurs(Occurs.ZeroOrOne)]
        [OslcTitle("Action")]
        public String GetAction()
        {
            return action;
        }

        /*
        public void SetSeverity(String severity)
        {
            try
            {
                this.severity = SeverityExtension.FromString(severity);
            }
            catch (ArgumentException e)
            {
                HandlerSettings.LogMessage(
                    String.Format("Failed to update Change Request severity to: {0}. Error: {1}", severity, e.Message),
                    HandlerSettings.LoggingLevel.ERROR);
            }
        }

        
        public void SetPriority(String priority)
        {
            try
            {
                this.priority = PriorityExtension.FromString(priority);
            }
            catch (ArgumentException e)
            {
                HandlerSettings.LogMessage(
                    String.Format("Failed to update Change Request priority to: {0}. Error: {1}", priority, e.Message),
                    HandlerSettings.LoggingLevel.ERROR);
            }
        }
        */

        public void SetPrimaryTR(String primaryTR)
        {
            this.primaryTR = primaryTR;
        }

        public void AddAttachment(Link attachment)
        {
            this.attachments.Add(attachment);
        }

        public void SetAttachments(Link[] attachments)
        {
            this.attachments.Clear();

            if (attachments != null)
            {
                this.attachments.AddAll(attachments);
            }
        }

        public void SetProduct(String product)
        {
            this.product = product;
        }

        public void SetProductRevision(String productRevision)
        {
            this.productRevision = productRevision;
        }

        public void SetNodeProduct(String nodeProduct)
        {
            this.nodeProduct = nodeProduct;
        }

        public void SetNodeProductRevision(String nodeProductRevision)
        {
            this.nodeProductRevision = nodeProductRevision;
        }

        public void SetCorrectedProduct(String correctedProduct)
        {
            this.correctedProduct = correctedProduct;
        }

        public void SetCorrectedProductRevision(String correctedProductRevision)
        {
            this.correctedProductRevision = correctedProductRevision;
        }

        public void SetCorrectedNodeProduct(String correctedNodeProduct)
        {
            this.correctedNodeProduct = correctedNodeProduct;
        }

        public void SetCorrectedNodeProductRevision(String correctedNodeProductRevision)
        {
            this.correctedNodeProductRevision = correctedNodeProductRevision;
        }

        public void SetCurrentMho(String currentMho)
        {
            this.currentMho = currentMho;
        }

        public void SetDiddet(String diddet)
        {
            this.diddet = diddet;
        }

        public void SetFirstTechContactInfo(String firstTechContactInfo)
        {
            this.firstTechContactInfo = firstTechContactInfo;
        }

        public void SetActivity(String activity)
        {
            this.activity = activity;
        }

        public void SetPriority(String priority)
        {
            this.priority = priority;
        }

        public void SetFirstTechnicalContact(String firstTechnicalContact)
        {
            this.firstTechnicalContact = firstTechnicalContact;
        }

        public void SetOwner(String owner)
        {
            this.owner = owner;
        }

        public void SetCountry(String country)
        {
            this.country = country;
        }

        public void SetSite(String site)
        {
            this.site = site;
        }

        public void SetCustomer(String customer)
        {
            this.customer = customer;
        }

        public void SetFaultCode(String faultCode)
        {
            this.faultCode = faultCode;
        }

        public void SetProgressInfo(String progressInfo)
        {
            this.progressInfo = progressInfo;
        }

        public void SetNotebook(String notebook)
        {
            this.notebook = notebook;
        }

        public void SetImpactOnISP(String impactOnISP)
        {
            this.impactOnISP = impactOnISP;
        }

        public void SetExpectedImpactOnISP(String expectedImpactOnISP)
        {
            this.expectedImpactOnISP = expectedImpactOnISP;
        }

        public void SetAnswerCode(String answerCode)
        {
            this.answerCode = answerCode;
        }

        public void SetAnswer(String answer)
        {
            this.answer = answer;
        }

        public void SetAction(String action)
        {
            this.action = action;
        }
    }
}
