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
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Text;
using System.Threading.Tasks;

// Implementation based on the OSLC4Net class OslcClient. Main difference is that we would like a non-Jazz
// implementation of basic authentication, and addition of parameters to header.
// See also http://oslc4net.codeplex.com/SourceControl/latest#OSLC4Net_SDK/OSLC4Net.Client/Oslc/OslcClient.cs

namespace TFSServerEventHandler.Authentication
{
    public class OslcClient2 : OslcClient
    {
        public OslcClient2() : base(null)
        {
        }

        protected OslcClient2(RemoteCertificateValidationCallback certCallback,
                             HttpMessageHandler oauthHandler) :
            base(certCallback, oauthHandler)
        {
        }

        // To allow basic auth client to override and and auth
        virtual protected void AddAuthorizationHeader() {
        }

        // For testing. Will create a String as the artifact will be sent over the wire
        public ObjectContent GetResourceAsMessage(object artifact, string mediaType, string acceptType, Dictionary<String, String> headers)
        {
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(acceptType));
            client.DefaultRequestHeaders.Add(OSLCConstants.OSLC_CORE_VERSION, "2.0");

            AddAuthorizationHeader();

            foreach (KeyValuePair<string, string> entry in headers)
            {
                client.DefaultRequestHeaders.Add(entry.Key, entry.Value);
            }

            MediaTypeHeaderValue mediaTypeValue = new MediaTypeHeaderValue(mediaType);
            MediaTypeFormatter formatter =
                new MediaTypeFormatterCollection(formatters).FindWriter(artifact.GetType(), mediaTypeValue);

            ObjectContent content = new ObjectContent(artifact.GetType(), artifact, formatter);
            content.Headers.ContentType = mediaTypeValue;

            return content;
        }

        /// <summary>
        /// Abstract method get an OSLC resource and return a HttpResponseMessage
        /// </summary>
        /// <param name="url"></param>
        /// <param name="mediaType"></param>
        /// <returns>the HttpResponseMessage</returns>
        public HttpResponseMessage GetResource(string url, string mediaType, Dictionary<String, String> headers)
        {
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(mediaType));
            client.DefaultRequestHeaders.Add(OSLCConstants.OSLC_CORE_VERSION, "2.0");

            AddAuthorizationHeader();

            foreach (KeyValuePair<string, string> entry in headers)
            {
                client.DefaultRequestHeaders.Add(entry.Key, entry.Value);
            }

            HttpResponseMessage response = null;
            bool redirect = false;

            do
            {
                response = client.GetAsync(url).Result;

                if ((response.StatusCode == HttpStatusCode.MovedPermanently) ||
                    (response.StatusCode == HttpStatusCode.Moved))
                {
                    url = response.Headers.Location.AbsoluteUri;
                    response.ConsumeContent();
                    redirect = true;
                }
                else
                {
                    redirect = false;
                }
            } while (redirect);

            return response;
        }

        /// <summary>
        /// Create (POST) an artifact to a URL - usually an OSLC Creation Factory
        /// </summary>
        /// <param name="url"></param>
        /// <param name="artifact"></param>
        /// <param name="mediaType"></param>
        /// <param name="acceptType"></param>
        /// <returns></returns>
        public HttpResponseMessage CreateResource(
            string url, object artifact, string mediaType, string acceptType, Dictionary<String, String> headers)
        {
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(acceptType));
            client.DefaultRequestHeaders.Add(OSLCConstants.OSLC_CORE_VERSION, "2.0");

            AddAuthorizationHeader();

            foreach (KeyValuePair<string, string> entry in headers)
            {
                client.DefaultRequestHeaders.Add(entry.Key, entry.Value);
            }

            MediaTypeHeaderValue mediaTypeValue = new MediaTypeHeaderValue(mediaType);
            MediaTypeFormatter formatter =
                new MediaTypeFormatterCollection(formatters).FindWriter(artifact.GetType(), mediaTypeValue);

            HttpResponseMessage response = null;
            bool redirect = false;

            do
            {
                ObjectContent content = new ObjectContent(artifact.GetType(), artifact, formatter);

                content.Headers.ContentType = mediaTypeValue;

                // Write the content of the object to the log. The writing of the object is an async operation, so
                // will get compiler warning if not await execution. OK to proceed here, so avoid warning by assigning
                // variable. See also http://msdn.microsoft.com/en-us/library/hh873131.aspx.

                Task t = HandlerSettings.LogMessage("Object POST: ",
                    content,
                    url,
                    HandlerSettings.LoggingLevel.INFO);

                response = client.PostAsync(url, content).Result;

                if ((response.StatusCode == HttpStatusCode.MovedPermanently) ||
                    (response.StatusCode == HttpStatusCode.Moved))
                {
                    url = response.Headers.Location.AbsoluteUri;
                    response.ConsumeContent();
                    redirect = true;
                }
                else
                {
                    redirect = false;
                }
            } while (redirect);

            return response;
        }

        /// <summary>
        /// Update (PUT) an artifact to a URL - usually the URL for an existing OSLC artifact
        /// </summary>
        /// <param name="url"></param>
        /// <param name="artifact"></param>
        /// <param name="mediaType"></param>
        /// <param name="acceptType"></param>
        /// <returns></returns>
        public HttpResponseMessage UpdateResource(
            string url, object artifact, string mediaType, string acceptType, Dictionary<String, String> headers)
        {
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(acceptType));
            client.DefaultRequestHeaders.Add(OSLCConstants.OSLC_CORE_VERSION, "2.0");

            AddAuthorizationHeader();

            foreach (KeyValuePair<string, string> entry in headers)
            {
                client.DefaultRequestHeaders.Add(entry.Key, entry.Value);
            }

            MediaTypeHeaderValue mediaTypeValue = new MediaTypeHeaderValue(mediaType);
            MediaTypeFormatter formatter =
                new MediaTypeFormatterCollection(formatters).FindWriter(artifact.GetType(), mediaTypeValue);

            HttpResponseMessage response = null;
            bool redirect = false;

            do
            {
                ObjectContent content = new ObjectContent(artifact.GetType(), artifact, formatter);

                content.Headers.ContentType = mediaTypeValue;

                // Write the content of the object to the log. The writing of the object is an async operation, so
                // will get compiler warning if not await execution. OK to proceed here, so avoid warning by assigning
                // variable. See also http://msdn.microsoft.com/en-us/library/hh873131.aspx.

                Task t = HandlerSettings.LogMessage("Object PUT: ",
                    content,
                    url,
                    HandlerSettings.LoggingLevel.INFO);

                response = client.PutAsync(url, content).Result;

                if ((response.StatusCode == HttpStatusCode.MovedPermanently) ||
                    (response.StatusCode == HttpStatusCode.Moved))
                {
                    url = response.Headers.Location.AbsoluteUri;
                    response.ConsumeContent();
                    redirect = true;
                }
                else
                {
                    redirect = false;
                }
            } while (redirect);

            return response;
        }

        /// <summary>
        /// Lookup the URL of a specific OSLC Service Provider in an OSLC Catalog using the service provider's title
        /// </summary>
        /// <param name="catalogUrl"></param>
        /// <param name="serviceProviderTitle"></param>
        /// <returns></returns>
        public string LookupServiceProviderUrl(string catalogUrl, string serviceProviderTitle, Dictionary<String, String> headers)
        {
            String retval = null;
            HttpResponseMessage response = GetResource(catalogUrl, OSLCConstants.CT_RDF, headers);

            // Checking specifically for Unauthorized 
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                throw new Exception(response.ReasonPhrase);
            }

            // All other errors - resource not found
            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new ResourceNotFoundException(catalogUrl, serviceProviderTitle);
            }

            ServiceProviderCatalog catalog = response.Content.ReadAsAsync<ServiceProviderCatalog>(formatters).Result;

            if (catalog != null)
            {
                foreach (ServiceProvider sp in catalog.GetServiceProviders())
                {
                    if (sp.GetTitle() != null && String.Compare(sp.GetTitle(), serviceProviderTitle, true) == 0)
                    {
                        retval = sp.GetAbout().ToString();
                        break;
                    }
                }
            }

            if (retval == null)
            {
                throw new ResourceNotFoundException(catalogUrl, serviceProviderTitle);
            }

            return retval;
        }

        /// <summary>
        /// Find the OSLC Creation Factory URL for a given OSLC resource type.  If no resource type is given, returns
        /// the default Creation Factory, if it exists.
        /// </summary>
        /// <param name="serviceProviderUrl"></param>
        /// <param name="oslcDomain"></param>
        /// <param name="oslcResourceType">the resource type of the desired query capability.   This may differ from the OSLC artifact type.</param>
        /// <returns>URL of requested Creation Factory or null if not found.</returns>
        public string LookupCreationFactory(string serviceProviderUrl, string oslcDomain, string oslcResourceType,
            Dictionary<String, String> headers)
        {
            CreationFactory defaultCreationFactory = null;
            CreationFactory firstCreationFactory = null;

            HttpResponseMessage response = GetResource(serviceProviderUrl, OSLCConstants.CT_RDF, headers);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new ResourceNotFoundException(serviceProviderUrl, "CreationFactory");
            }

            ServiceProvider serviceProvider = response.Content.ReadAsAsync<ServiceProvider>(formatters).Result;

            if (serviceProvider != null)
            {
                foreach (Service service in serviceProvider.GetServices())
                {
                    Uri domain = service.GetDomain();
                    if (domain != null && domain.ToString().Equals(oslcDomain))
                    {
                        CreationFactory[] creationFactories = service.GetCreationFactories();
                        if (creationFactories != null && creationFactories.Length > 0)
                        {
                            firstCreationFactory = creationFactories[0];
                            foreach (CreationFactory creationFactory in creationFactories)
                            {
                                foreach (Uri resourceType in creationFactory.GetResourceTypes())
                                {

                                    //return as soon as domain + resource type are matched
                                    if (resourceType.ToString() != null && resourceType.ToString().Equals(oslcResourceType))
                                    {
                                        return creationFactory.GetCreation().ToString();
                                    }
                                }
                                //Check if this is the default factory
                                foreach (Uri usage in creationFactory.GetUsages())
                                {
                                    if (usage.ToString() != null && usage.ToString().Equals(OSLCConstants.USAGE_DEFAULT_URI))
                                    {
                                        defaultCreationFactory = creationFactory;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            //If we reached this point, there was no resource type match
            if (defaultCreationFactory != null)
            {
                //return default, if present
                return defaultCreationFactory.GetCreation().ToString();
            }
            else if (firstCreationFactory != null && firstCreationFactory.GetResourceTypes().Length == 0)
            {
                //return the first for the domain, if present
                return firstCreationFactory.GetCreation().ToString();
            }

            throw new ResourceNotFoundException(serviceProviderUrl, "CreationFactory");
        }

        /// <summary>
        /// Find the OSLC Query Capability URL for a given OSLC resource type.  If no resource type is given, returns
        /// the default Query Capability, if it exists.
        /// </summary>
        /// <param name="serviceProviderUrl"></param>
        /// <param name="oslcDomain"></param>
        /// <param name="oslcResourceType">the resource type of the desired query capability.   This may differ from the OSLC artifact type.</param>
        /// <returns>URL of requested Query Capablility or null if not found.</returns>
        public string LookupQueryCapability(string serviceProviderUrl, string oslcDomain, string oslcResourceType,
            Dictionary<String, String> headers)
        {
            QueryCapability defaultQueryCapability = null;
            QueryCapability firstQueryCapability = null;

            HttpResponseMessage response = GetResource(serviceProviderUrl, OSLCConstants.CT_RDF, headers);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new ResourceNotFoundException(serviceProviderUrl, "QueryCapability");
            }

            ServiceProvider serviceProvider = response.Content.ReadAsAsync<ServiceProvider>(formatters).Result;

            if (serviceProvider != null)
            {
                foreach (Service service in serviceProvider.GetServices())
                {
                    Uri domain = service.GetDomain();
                    if (domain != null && domain.ToString().Equals(oslcDomain))
                    {
                        QueryCapability[] queryCapabilities = service.GetQueryCapabilities();
                        if (queryCapabilities != null && queryCapabilities.Length > 0)
                        {
                            firstQueryCapability = queryCapabilities[0];
                            foreach (QueryCapability queryCapability in service.GetQueryCapabilities())
                            {
                                foreach (Uri resourceType in queryCapability.GetResourceTypes())
                                {

                                    //return as soon as domain + resource type are matched
                                    if (resourceType.ToString() != null && resourceType.ToString().Equals(oslcResourceType))
                                    {
                                        return queryCapability.GetQueryBase().OriginalString;
                                    }
                                }
                                //Check if this is the default capability
                                foreach (Uri usage in queryCapability.GetUsages())
                                {
                                    if (usage.ToString() != null && usage.ToString().Equals(OSLCConstants.USAGE_DEFAULT_URI))
                                    {
                                        defaultQueryCapability = queryCapability;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            //If we reached this point, there was no resource type match
            if (defaultQueryCapability != null)
            {
                //return default, if present
                return defaultQueryCapability.GetQueryBase().ToString();
            }
            else if (firstQueryCapability != null && firstQueryCapability.GetResourceTypes().Length == 0)
            {
                //return the first for the domain, if present
                return firstQueryCapability.GetQueryBase().ToString();
            }

            throw new ResourceNotFoundException(serviceProviderUrl, "QueryCapability");
        }
    }
}
