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
using OSLC4Net.Core.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TFSServerEventHandler.Resources
{
    [OslcNamespace(OslcConstants.OSLC_CORE_NAMESPACE)]
    [OslcResourceShape(title = "Comment Resource Shape", describes = new string[] { Constants.TYPE_COMMENT })]
    class Comment : AbstractResource
    {
        private String identifier;
        private String title;
        private Person creator;
        private DateTime created;
        private String description;
        private URI partOfDiscussion;
        private URI inReplyTo;

        public void SetIdentifier(String identifier)
        {
            this.identifier = identifier;
        }

        public void SetTitle(String title)
        {
            this.title = title;
        }
        public void SetCreator(Person creator)
        {
            this.creator = creator;
        }

        public void SetCreated(DateTime created)
        {
            this.created = created;
        }

        public void SetDescription(String description)
        {
            this.description = description;
        }

        public void SetPartOfDiscussion(URI partOfDiscussion)
        {
            this.partOfDiscussion = partOfDiscussion;
        }

        public void SetInReplyTo(URI inReplyTo)
        {
            this.inReplyTo = inReplyTo;
        }

        [OslcDescription("A unique identifier for a resource. Not intended for end-user display.")]
        [OslcOccurs(Occurs.ExactlyOne)]
        [OslcPropertyDefinition(OslcConstants.DCTERMS_NAMESPACE + "identifier")]
        [OslcReadOnly]
        [OslcTitle("Identifier")]
        public String GetIdentifier()
        {
            return identifier;
        }

        [OslcDescription("Title (reference: Dublin Core) or often a single line summary of the resource represented as rich text in XHTML content.")]
        [OslcOccurs(Occurs.ExactlyOne)]
        [OslcPropertyDefinition(OslcConstants.DCTERMS_NAMESPACE + "title")]
        [OslcTitle("Title")]
        [OslcValueType(OSLC4Net.Core.Model.ValueType.XMLLiteral)]
        public String GetTitle()
        {
            return title;
        }

        [OslcDescription("The foaf:Person who created the comment.")]
        [OslcName("creator")]
        [OslcPropertyDefinition(OslcConstants.DCTERMS_NAMESPACE + "creator")]
        [OslcRange(CmConstants.TYPE_PERSON)]
        [OslcTitle("Creator")]
        public Person GetCreator()
        {
            return creator; ;
        }

        [OslcDescription("Timestamp of resource creation.")]
        [OslcPropertyDefinition(OslcConstants.DCTERMS_NAMESPACE + "created")]
        [OslcReadOnly]
        [OslcTitle("Created")]
        public DateTime GetCreated()
        {
            return created;
        }

        [OslcDescription("Descriptive text (reference: Dublin Core) about resource represented as rich text in XHTML content.")]
        [OslcPropertyDefinition(OslcConstants.DCTERMS_NAMESPACE + "description")]
        [OslcTitle("Description")]
        [OslcValueType(OSLC4Net.Core.Model.ValueType.XMLLiteral)]
        public String GetDescription()
        {
            return description;
        }

        [OslcDescription("Reference to owning Discussion resource.")]
        [OslcPropertyDefinition(OslcConstants.OSLC_CORE_NAMESPACE + "partOfDiscussion")]
        [OslcRange(CmConstants.TYPE_DISCUSSION)]
        [OslcTitle("Part Of Discussion")]
        public URI GetPartOfDiscussion()
        {
            return partOfDiscussion;
        }

        [OslcDescription("Reference to comment this comment is in reply to.")]
        [OslcPropertyDefinition(OslcConstants.OSLC_CORE_NAMESPACE + "inReplyTo")]
        [OslcRange(Constants.TYPE_COMMENT)]
        [OslcTitle("In Reply To")]
        public URI GetInReplyTo()
        {
            return inReplyTo;
        }
    }
}
