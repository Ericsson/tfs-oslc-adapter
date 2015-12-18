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

namespace TFSServerEventHandler.OAuth
{
    public class FriendInfo
    {
        private String name;

        // Can be "basic" or "oauth" depending on what to use
        private UseAccessType useAccess;

        public enum UseAccessType
        {
            basic,
            oauth
        }

        // OAuth
        private String consumerKey;
        private String consumerSecret;
        private String accessToken;
        private String accessTokenSecret;

        // Basic
        private String encodedCredentials;

        public FriendInfo(String name)
        {
            this.name = name;
        }

        /// <summary>
        /// The access method specified
        /// </summary>
        public UseAccessType UseAccess { get { return useAccess; } set { this.useAccess = value; } }

        /// <summary>
        /// The Basic Auth credentials for the friend Base64 encoded
        /// </summary>
        public String EncodedCredentials { get { return encodedCredentials; } set { this.encodedCredentials = value; } }

        // Get the username from the encoded credentials
        public String GetBasicAuthUser()
        {
            Byte[] byteArray = Convert.FromBase64String(encodedCredentials);
            String decodedAuth = Encoding.UTF8.GetString(byteArray);
            String[] userPassword = decodedAuth.Split(':');
            return userPassword[0];
        }

        /// <summary>
        /// The Consumer Key for the friend
        /// </summary>
        public String ConsumerKey { get { return consumerKey; } set { this.consumerKey = value; } }

        /// <summary>
        /// The Consumer Secret for the friend
        /// </summary>
        public String ConsumerSecret { get { return consumerSecret; } set { this.consumerSecret = value; } }

        /// <summary>
        /// A valid Access Token for the friend
        /// </summary>
        public String AccessToken { get { return accessToken; } set { this.accessToken = value; } }

        /// <summary>
        /// A valid Access Token Secret for the friend
        /// </summary>
        public String AccessTokenSecret { get { return accessTokenSecret; } set { this.accessTokenSecret = value; } }
    }
}
