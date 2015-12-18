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
using OSLC4Net.Client.Oslc.Jazz;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using VDS.RDF;
using VDS.RDF.Parsing;

// Implementation based on the OSLC4Net class JazzRootServicesHelper. Main difference is addition of a client init method
// where the access tokens and secret are provided and not using the OAuth dance for getting them.
// See also http://oslc4net.codeplex.com/SourceControl/latest#OSLC4Net_SDK/OSLC4Net.Client/Oslc/Jazz/JazzRootServicesHelper.cs

namespace TFSServerEventHandler.OAuth
{
    class JazzRootServicesHelper2
    {
        private String baseUrl;
        private String rootServicesUrl;
        private String catalogUrl;

        // Hardcode CM domain
        private String catalogNamespace = OSLCConstants.OSLC_CM;
        private String catalogProperty = JazzRootServicesConstants.CM_ROOTSERVICES_CATALOG_PROP;

        // OAuth URLs
        String requestTokenUrl;
        String authorizationTokenUrl;
        String accessTokenUrl;

        private const String JFS_NAMESPACE = "http://jazz.net/xmlns/prod/jazz/jfs/1.0/";
        private const String JD_NAMESPACE = "http://jazz.net/xmlns/prod/jazz/discovery/1.0/";

//        private static ILog logger = LogManager.GetLogger(typeof(JazzRootServicesHelper));

        /// <summary>
        /// Initialize Jazz rootservices-related URLs such as the catalog location and OAuth URLs
        /// 
        /// rootservices is unprotected and access does not require authentication
        /// </summary>
        /// <param name="url">base URL of the Jazz server, no including /rootservices.  Example:  https://example.com:9443/ccm</param>
        /// <param name="catalogDomain">Namespace of the OSLC domain to find the catalog for.  Example:  OSLCConstants.OSLC_CM</param>
	    public JazzRootServicesHelper2(String url, String rootServicesUrl)
        {
		    this.baseUrl = url;
            this.rootServicesUrl = rootServicesUrl;
	
		    ProcessRootServices();
	    }
	
        /// <summary>
        /// Get the OSLC Catalog URL
        /// </summary>
        /// <returns></returns>
	    public String GetCatalogUrl()
	    {
		    return catalogUrl;
	    }

        public JazzOAuthClient2 InitOAuthClient2(String consumerKey, String secret, String accessToken, String accessTokenSecret)
        {
		    return new JazzOAuthClient2(requestTokenUrl,
                authorizationTokenUrl,
                accessTokenUrl,
                consumerKey,
                secret,
                accessToken,
                accessTokenSecret);		
	    }

        public JazzFormAuthClient InitFormClient(String userid, String password)
        {
            return new JazzFormAuthClient(baseUrl, userid, password);
        }

	    private void ProcessRootServices()
	    {
		    try {
			    OslcClient rootServicesClient = new OslcClient();
			    HttpResponseMessage response = rootServicesClient.GetResource(rootServicesUrl, OSLCConstants.CT_RDF);
			    Stream stream = response.Content.ReadAsStreamAsync().Result;
                IGraph rdfGraph = new Graph();
                IRdfReader parser = new RdfXmlParser();
                StreamReader streamReader = new StreamReader(stream);

                using (streamReader)
                {
                    parser.Load(rdfGraph, streamReader);
 
			        //get the catalog URL
			        this.catalogUrl = GetRootServicesProperty(rdfGraph, this.catalogNamespace, this.catalogProperty);
						
			        //get the OAuth URLs
			        this.requestTokenUrl = GetRootServicesProperty(rdfGraph, JFS_NAMESPACE, JazzRootServicesConstants.OAUTH_REQUEST_TOKEN_URL);
			        this.authorizationTokenUrl = GetRootServicesProperty(rdfGraph, JFS_NAMESPACE, JazzRootServicesConstants.OAUTH_USER_AUTH_URL);
			        this.accessTokenUrl = GetRootServicesProperty(rdfGraph, JFS_NAMESPACE, JazzRootServicesConstants.OAUTH_ACCESS_TOKEN_URL);
                    //try { // Following field is optional, try to get it, if not found ignore exception because it will use the default
                    //    this.authorizationRealm = GetRootServicesProperty(rdfGraph, JFS_NAMESPACE, JazzRootServicesConstants.OAUTH_REALM_NAME);
                    //} catch (ResourceNotFoundException e) {
                    //    // Ignore
                    //}
                }
		    } catch (Exception e) {
			    throw new RootServicesException(this.baseUrl, e);
		    }	
	    }
	
	    private String GetRootServicesProperty(IGraph rdfGraph, String ns, String predicate)
        {
		    String returnVal = null;
				
		    IUriNode prop = rdfGraph.CreateUriNode(new Uri(ns + predicate));
		    IEnumerable<Triple> triples = rdfGraph.GetTriplesWithPredicate(prop);

		    if (triples.Count() == 1)
            {
                IUriNode obj = triples.First().Object as IUriNode;

                if (obj != null)
                {
			        returnVal = obj.Uri.ToString();
                }
            }

		    if (returnVal == null)
		    {
			    throw new ResourceNotFoundException(baseUrl, ns + predicate);
		    }

		    return returnVal;
	    }
    }
}
