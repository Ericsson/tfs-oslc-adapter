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

using OSLC4Net.Core.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TFSServerEventHandler
{
    class Constants
    {
        public const string ENTERPRISE_CHANGE_MANAGEMENT_DOMAIN = "http://mhweb.ericsson.com/rdf#";
        public const string ENTERPRISE_CHANGE_MANAGEMENT_NAMESPACE = "http://mhweb.ericsson.com/rdf#";

        public const string ENTERPRISE_CHANGE_MANAGEMENT_NAMESPACE_PREFIX = "ecm";

        public const string ENTERPRISE_CHANGE_REQUEST = "EnterpriseChangeRequest";
        public const string TYPE_ENTERPRISE_CHANGE_REQUEST = ENTERPRISE_CHANGE_MANAGEMENT_NAMESPACE + ENTERPRISE_CHANGE_REQUEST;

        // Should be in Core
        public const string TYPE_COMMENT = OslcConstants.OSLC_CORE_NAMESPACE + "Comment";
        public const string PATH_COMMENT = "comment";

        public const string FOAF_NAMESPACE = "http://xmlns.com/foaf/0.1/";
        public const string FOAF_NAMESPACE_PREFIX = "foaf";

        public const string PATH_PERSON = "person";

        public const string PATH_DISCUSSION = "discussion";
    }
}
