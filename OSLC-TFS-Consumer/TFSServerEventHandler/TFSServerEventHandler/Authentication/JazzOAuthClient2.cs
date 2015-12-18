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

using DotNetOpenAuth.Messaging;
using DotNetOpenAuth.OAuth;
using DotNetOpenAuth.OAuth.ChannelElements;
using DotNetOpenAuth.OAuth.Messages;
using OSLC4Net.Client.Exceptions;
using OSLC4Net.Client.Oslc;
using OSLC4Net.Client.Oslc.Jazz;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using TFSServerEventHandler.Authentication;

// Implementation based on the OSLC4Net class JazzOAuthClient. Main difference is that we would like a non-Jazz
// implementation of the OAuth dance, but this is not yet implemented here. So using provided access tokens from MHWeb.
// See also http://oslc4net.codeplex.com/SourceControl/latest#OSLC4Net_SDK/OSLC4Net.Client/Oslc/Jazz/JazzOAuthClient.cs

namespace TFSServerEventHandler
{
    class JazzOAuthClient2 : OslcClient2
    {
        private const String JAZZ_AUTH_MESSAGE_HEADER = "X-com-ibm-team-repository-web-auth-msg";
        private const String JAZZ_AUTH_FAILED = "authfailed";

        /// <summary>
        /// Initialize an OAuthClient with the required OAuth URLs and an existing access token
        /// </summary>
        /// <param name="requestTokenURL"></param>
        /// <param name="authorizationTokenURL"></param>
        /// <param name="accessTokenURL"></param>
        /// <param name="consumerKey"></param>
        /// <param name="consumerSecret"></param>\
        /// <param name="accessToken"></param>
        /// <param name="accessTokenSecret"></param>
        public JazzOAuthClient2(String requestTokenURL,
                               String authorizationTokenURL,
                               String accessTokenURL,
                               String consumerKey,
                               String consumerSecret,
                               String accessToken,
                               String accessTokenSecret) :
            base(null, OAuthHandlerWithExistingToken(
                requestTokenURL,
                authorizationTokenURL,
                accessTokenURL,
                consumerKey,
                consumerSecret,
                accessToken,
                accessTokenSecret))
        {
        }

        /// <summary>
        /// Initialize an OAuthClient with the required OAuth URLs
        /// </summary>
        /// <param name="requestTokenURL"></param>
        /// <param name="authorizationTokenURL"></param>
        /// <param name="accessTokenURL"></param>
        /// <param name="consumerKey"></param>
        /// <param name="consumerSecret"></param>\
        /// <param name="authUrl"></param>
	    public JazzOAuthClient2(String requestTokenURL,
						       String authorizationTokenURL,
						       String accessTokenURL,
						       String consumerKey,
						       String consumerSecret,
                               String user,
                               String passwd,
                               String authUrl) :
            base(null, OAuthHandler(requestTokenURL,
                authorizationTokenURL,
                accessTokenURL,
                consumerKey,
                consumerSecret,
                user,
                passwd,
                authUrl))
        {
	    }

        /// <summary>
        /// Initialize an OAuthClient with the required OAuth URLs
        /// </summary>
        /// <param name="requestTokenURL"></param>
        /// <param name="authorizationTokenURL"></param>
        /// <param name="accessTokenURL"></param>
        /// <param name="consumerKey"></param>
        /// <param name="consumerSecret"></param>
        /// <param name="oauthRealmName"></param>
        /// <param name="authUrl"></param>
	    public JazzOAuthClient2(String requestTokenURL,
						       String authorizationTokenURL,
						       String accessTokenURL,
						       String consumerKey,
						       String consumerSecret,
                               String oauthRealmName,
                               String user,
                               String passwd,
                               String authUrl) :
            base(null, OAuthHandler(requestTokenURL, authorizationTokenURL, accessTokenURL, consumerKey, consumerSecret,
                                    user, passwd, authUrl))
        {
	    }

        private class TokenManager : IConsumerTokenManager
        {
            public TokenManager(string consumerKey, string consumerSecret)
            {
                this.consumerKey = consumerKey;
                this.consumerSecret = consumerSecret;
            }

            public string ConsumerKey
            {
                get { return consumerKey; }
            }

            public string ConsumerSecret
            {
                get { return consumerSecret; }
            }

            // From IConsumerTokenManager
            public string GetTokenSecret(string token)
            {
                return tokensAndSecrets[token];
            }

            // From IConsumerTokenManager
            public void StoreNewRequestToken(UnauthorizedTokenRequest request,
                                             ITokenSecretContainingMessage response)
            {
                tokensAndSecrets[response.Token] = response.TokenSecret;
            }

            // From IConsumerTokenManager
            public void ExpireRequestTokenAndStoreNewAccessToken(string consumerKey,
                                                                 string requestToken,
                                                                 string accessToken,
                                                                 string accessTokenSecret)
            {
                tokensAndSecrets.Remove(requestToken);
                tokensAndSecrets[accessToken] = accessTokenSecret;
            }

            // From IConsumerTokenManager
            public TokenType GetTokenType(string token)
            {
                throw new NotImplementedException();
            }

            // To allow populating with known accessToken and accessTokenSecret
            public void AddKnownAccessTokens(string accessToken, string accessTokenSecret)
            {
                tokensAndSecrets[accessToken] = accessTokenSecret;
            }

            public string GetRequestToken()
            {
                return tokensAndSecrets.First().Key;
            }

            private readonly IDictionary<string, string> tokensAndSecrets =
                new Dictionary<string, string>();
            private readonly string consumerKey;
            private readonly string consumerSecret;
        }

        // A OAuth handler when accessToken and accessTokenSecrets are known
        private static HttpMessageHandler OAuthHandlerWithExistingToken(String requestTokenURL,
                                               String authorizationTokenURL,
                                               String accessTokenURL,
                                               String consumerKey,
                                               String consumerSecret,
                                               String accessToken,
                                               String accessTokenSecret)
        {
            ServiceProviderDescription serviceDescription = new ServiceProviderDescription();

            serviceDescription.AccessTokenEndpoint = new MessageReceivingEndpoint(new Uri(accessTokenURL), HttpDeliveryMethods.PostRequest);
            serviceDescription.ProtocolVersion = ProtocolVersion.V10a;
            serviceDescription.RequestTokenEndpoint = new MessageReceivingEndpoint(new Uri(requestTokenURL), HttpDeliveryMethods.PostRequest);
            serviceDescription.TamperProtectionElements = new ITamperProtectionChannelBindingElement[] { new HmacSha1SigningBindingElement() };
            serviceDescription.UserAuthorizationEndpoint = new MessageReceivingEndpoint(new Uri(authorizationTokenURL), HttpDeliveryMethods.PostRequest);

            TokenManager tokenManager = new TokenManager(consumerKey, consumerSecret);

            // Here we add the known token info
            tokenManager.AddKnownAccessTokens(accessToken, accessTokenSecret);

            WebConsumer consumer = new WebConsumer(serviceDescription, tokenManager);

            DesktopConsumer desktopConsumer = new DesktopConsumer(serviceDescription, tokenManager);
            return consumer.CreateAuthorizingHandler(accessToken, CreateSSLHandler());
        }

        // A non-jazz specific OAuth handler
        private static HttpMessageHandler OAuthHandlerBasic(String requestTokenURL,
                                                       String authorizationTokenURL,
                                                       String accessTokenURL,
                                                       String consumerKey,
                                                       String consumerSecret,
                                                       String user,
                                                       String passwd,
                                                       String authUrl)
        {
            ServiceProviderDescription serviceDescription = new ServiceProviderDescription();

            serviceDescription.AccessTokenEndpoint = new MessageReceivingEndpoint(new Uri(accessTokenURL), HttpDeliveryMethods.PostRequest);
            serviceDescription.ProtocolVersion = ProtocolVersion.V10a;
            serviceDescription.RequestTokenEndpoint = new MessageReceivingEndpoint(new Uri(requestTokenURL), HttpDeliveryMethods.PostRequest);
            serviceDescription.TamperProtectionElements = new ITamperProtectionChannelBindingElement[] { new HmacSha1SigningBindingElement() };
            serviceDescription.UserAuthorizationEndpoint = new MessageReceivingEndpoint(new Uri(authorizationTokenURL), HttpDeliveryMethods.PostRequest);

            TokenManager tokenManager = new TokenManager(consumerKey, consumerSecret);
            WebConsumer consumer = new WebConsumer(serviceDescription, tokenManager);

            // callback is never called by CLM, but needed to do OAuth based forms login
            // XXX - Dns.GetHostName() alway seems to return simple, uppercased hostname
            string callback = "https://" + Dns.GetHostName() + '.' + IPGlobalProperties.GetIPGlobalProperties().DomainName + ":9443/cb";

            callback = callback.ToLower();

            consumer.PrepareRequestUserAuthorization(new Uri(callback), null, null);
            OslcClient oslcClient = new OslcClient();
            HttpClient client = oslcClient.GetHttpClient();

            HttpStatusCode statusCode = HttpStatusCode.Unused;
            String location = null;
            HttpResponseMessage resp;

            try
            {
                client.DefaultRequestHeaders.Clear();

                resp = client.GetAsync(authorizationTokenURL + "?oauth_token=" + tokenManager.GetRequestToken() +
                                                            "&oauth_callback=" + Uri.EscapeUriString(callback).Replace("#", "%23").Replace("/", "%2F").Replace(":", "%3A")).Result;
                statusCode = resp.StatusCode;

                if (statusCode == HttpStatusCode.Found)
                {
                    location = resp.Headers.Location.AbsoluteUri;
                    resp.ConsumeContent();
                    statusCode = FollowRedirects(client, statusCode, location);
                }

                String securityCheckUrl = "j_username=" + user + "&j_password=" + passwd;
                StringContent content = new StringContent(securityCheckUrl, System.Text.Encoding.UTF8);

                MediaTypeHeaderValue mediaTypeValue = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

                mediaTypeValue.CharSet = "utf-8";

                content.Headers.ContentType = mediaTypeValue;

                resp = client.PostAsync(authUrl + "/j_security_check", content).Result;
                statusCode = resp.StatusCode;

                String jazzAuthMessage = null;
                IEnumerable<string> values = new List<string>();

                if (resp.Headers.TryGetValues(JAZZ_AUTH_MESSAGE_HEADER, out values))
                {
                    jazzAuthMessage = values.Last();
                }

                if (jazzAuthMessage != null && String.Compare(jazzAuthMessage, JAZZ_AUTH_FAILED, true) == 0)
                {
                    resp.ConsumeContent();
                    throw new JazzAuthFailedException(user, authUrl);
                }
                else if (statusCode != HttpStatusCode.OK && statusCode != HttpStatusCode.Found)
                {
                    resp.ConsumeContent();
                    throw new JazzAuthErrorException(statusCode, authUrl);
                }
                else //success
                {
                    Uri callbackUrl = resp.Headers.Location;

                    resp = client.GetAsync(callbackUrl.AbsoluteUri).Result;
                    callbackUrl = resp.Headers.Location;
                    resp = client.GetAsync(callbackUrl.AbsoluteUri).Result;
                    callbackUrl = resp.Headers.Location;

                    NameValueCollection qscoll = callbackUrl.ParseQueryString();

                    if (callbackUrl.OriginalString.StartsWith(callback + '?') && qscoll["oauth_verifier"] != null)
                    {
                        DesktopConsumer desktopConsumer = new DesktopConsumer(serviceDescription, tokenManager);
                        AuthorizedTokenResponse authorizedTokenResponse = desktopConsumer.ProcessUserAuthorization(tokenManager.GetRequestToken(), qscoll["oauth_verifier"]);

                        return consumer.CreateAuthorizingHandler(authorizedTokenResponse.AccessToken, CreateSSLHandler());
                    }

                    throw new JazzAuthErrorException(statusCode, authUrl);
                }
            }
            catch (JazzAuthFailedException jfe)
            {
                throw jfe;
            }
            catch (JazzAuthErrorException jee)
            {
                throw jee;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
            }

            // return consumer.CreateAuthorizingHandler(accessToken);
            return null;
        }

        // A jazz specific OAuth handler
        private static HttpMessageHandler OAuthHandler(String requestTokenURL,
						                               String authorizationTokenURL,
                                                       String accessTokenURL,
						                               String consumerKey,
                                                       String consumerSecret,
                                                       String user,
                                                       String passwd,
                                                       String authUrl)
        {
            ServiceProviderDescription serviceDescription = new ServiceProviderDescription();

            serviceDescription.AccessTokenEndpoint = new MessageReceivingEndpoint(new Uri(accessTokenURL), HttpDeliveryMethods.PostRequest);
            serviceDescription.ProtocolVersion = ProtocolVersion.V10a;
            serviceDescription.RequestTokenEndpoint = new MessageReceivingEndpoint(new Uri(requestTokenURL), HttpDeliveryMethods.PostRequest);
            serviceDescription.TamperProtectionElements = new ITamperProtectionChannelBindingElement[] { new HmacSha1SigningBindingElement() };
            serviceDescription.UserAuthorizationEndpoint = new MessageReceivingEndpoint(new Uri(authorizationTokenURL), HttpDeliveryMethods.PostRequest);

            TokenManager tokenManager = new TokenManager(consumerKey, consumerSecret);
            WebConsumer consumer = new WebConsumer(serviceDescription, tokenManager);

            // callback is never called by CLM, but needed to do OAuth based forms login
            // XXX - Dns.GetHostName() alway seems to return simple, uppercased hostname
            string callback = "https://" + Dns.GetHostName() + '.' + IPGlobalProperties.GetIPGlobalProperties().DomainName +  ":9443/cb";

            callback = callback.ToLower();

            consumer.PrepareRequestUserAuthorization(new Uri(callback), null, null);
            OslcClient oslcClient = new OslcClient();
            HttpClient client = oslcClient.GetHttpClient();

            HttpStatusCode statusCode = HttpStatusCode.Unused;
		    String location = null;
            HttpResponseMessage resp;

		    try 
		    {
                client.DefaultRequestHeaders.Clear();

                resp = client.GetAsync(authorizationTokenURL + "?oauth_token=" + tokenManager.GetRequestToken() +
                                                            "&oauth_callback=" + Uri.EscapeUriString(callback).Replace("#", "%23").Replace("/", "%2F").Replace(":", "%3A")).Result;
                statusCode = resp.StatusCode;

                if (statusCode == HttpStatusCode.Found)
                {
                    location = resp.Headers.Location.AbsoluteUri;
                    resp.ConsumeContent();
                    statusCode = FollowRedirects(client, statusCode, location);
                }

                String securityCheckUrl = "j_username=" + user + "&j_password=" + passwd;
                StringContent content = new StringContent(securityCheckUrl, System.Text.Encoding.UTF8);

                MediaTypeHeaderValue mediaTypeValue = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

                mediaTypeValue.CharSet = "utf-8";

                content.Headers.ContentType = mediaTypeValue;

                resp = client.PostAsync(authUrl + "/j_security_check", content).Result;
		        statusCode = resp.StatusCode;
		    
		        String jazzAuthMessage = null;
                IEnumerable<string> values = new List<string>();

		        if (resp.Headers.TryGetValues(JAZZ_AUTH_MESSAGE_HEADER, out values)) {
		    	    jazzAuthMessage = values.Last();
		        }
		    
		        if (jazzAuthMessage != null && String.Compare(jazzAuthMessage, JAZZ_AUTH_FAILED, true) == 0)
		        {
                    resp.ConsumeContent();
                    throw new JazzAuthFailedException(user, authUrl);
		        }
                else if (statusCode != HttpStatusCode.OK && statusCode != HttpStatusCode.Found)
		        {
                    resp.ConsumeContent();
                    throw new JazzAuthErrorException(statusCode, authUrl);
		        }
		        else //success
		        {
		    	    Uri callbackUrl = resp.Headers.Location;

                    resp = client.GetAsync(callbackUrl.AbsoluteUri).Result;
                    callbackUrl = resp.Headers.Location;
                    resp = client.GetAsync(callbackUrl.AbsoluteUri).Result;
                    callbackUrl = resp.Headers.Location;

                    NameValueCollection qscoll = callbackUrl.ParseQueryString();

                    if (callbackUrl.OriginalString.StartsWith(callback + '?') && qscoll["oauth_verifier"] != null)
                    {
                        DesktopConsumer desktopConsumer = new DesktopConsumer(serviceDescription, tokenManager);
                        AuthorizedTokenResponse authorizedTokenResponse = desktopConsumer.ProcessUserAuthorization(tokenManager.GetRequestToken(), qscoll["oauth_verifier"]);
                        
                        return consumer.CreateAuthorizingHandler(authorizedTokenResponse.AccessToken, CreateSSLHandler());
                    }

                    throw new JazzAuthErrorException(statusCode, authUrl);
                }
		    } catch (JazzAuthFailedException jfe) {
			    throw jfe;
	        } catch (JazzAuthErrorException jee) {
	    	    throw jee;
	        } catch (Exception e) {
                Console.WriteLine(e.StackTrace);
            }

            // return consumer.CreateAuthorizingHandler(accessToken);
            return null;
	    }

        private static HttpStatusCode FollowRedirects(HttpClient client, HttpStatusCode statusCode, String location)
	    {

            while ((statusCode == HttpStatusCode.Found) && (location != null))
		    {
			    try {
                    HttpResponseMessage newResp = client.GetAsync(location).Result;
				    statusCode = newResp.StatusCode;
				    location = (newResp.Headers.Location != null) ? newResp.Headers.Location.AbsoluteUri : null;
                    newResp.ConsumeContent();
			    } catch (Exception e) {
				    Console.WriteLine(e.StackTrace);
			    }

		    }
		    return statusCode;
	    }
    }
}
