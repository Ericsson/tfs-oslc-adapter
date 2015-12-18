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
    [OslcNamespace(Constants.FOAF_NAMESPACE)]
    [OslcResourceShape(title = "Person Resource Shape", describes = new string[] { CmConstants.TYPE_PERSON })]
    class Person : AbstractResource {
        private URI uri = null;
        private String name = null;
        private String mbox = null;

        public URI getUri() {
            return uri;
        }

        public void setUri(URI uri) {
            this.uri = uri;
        }

        [OslcDescription("A FOAF name.")]
        [OslcPropertyDefinition(Constants.FOAF_NAMESPACE + "name")]
        [OslcReadOnly]
        [OslcTitle("Name")]
        public String getName() {
            return name;
        }

        public void setName(String name) {
            this.name = name;
        }

        [OslcDescription("A FOAF Email address.")]
        [OslcPropertyDefinition(Constants.FOAF_NAMESPACE + "mbox")]
        [OslcReadOnly]
        [OslcTitle("Email Address")]
        public String getMbox() {
            return mbox;
        }

        public void setMbox(String mbox) {
            this.mbox = mbox;
        }
    }
}
