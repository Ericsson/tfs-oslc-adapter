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

using OSLC4Net.Client.Exceptions;
using OSLC4Net.Client.Oslc;
using OSLC4Net.Core.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using TFSServerEventHandler.Authentication;

// Implementation based on the OSLC4Net class OslcClient. Main difference is that we would like a non-Jazz
// implementation of basic authentication, and addition of parameters to header.
// See also http://oslc4net.codeplex.com/SourceControl/latest#OSLC4Net_SDK/OSLC4Net.Client/Oslc/OslcClient.cs

namespace TFSServerEventHandler.OAuth
{
    class BasicAuthClient : OslcClient2
    {

        private String encodedCredentials;

        public BasicAuthClient(String encodedCredentials)
        {
            this.encodedCredentials = encodedCredentials;
        }

        public BasicAuthClient(String user, String password)
        {
            Byte[] byteArray = Encoding.ASCII.GetBytes(user + ":" + password);
            this.encodedCredentials = Convert.ToBase64String(byteArray);
        }

        override protected void AddAuthorizationHeader()
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", encodedCredentials);
        }
    }
}
