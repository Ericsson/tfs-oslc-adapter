package com.ericsson.eif.tfs.oslc;

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

import java.net.URI;
import java.net.URISyntaxException;
import java.util.ArrayList;
import java.util.List;

import javax.servlet.http.HttpServletRequest;

import org.apache.log4j.Logger;

import com.ericsson.eif.tfs.oslc.exception.UnauthorizedException;
import com.microsoft.tfs.core.TFSTeamProjectCollection;
import com.microsoft.tfs.core.clients.workitem.WorkItemClient;
import com.microsoft.tfs.core.clients.workitem.project.ProjectCollection;
import com.microsoft.tfs.core.config.ConnectionAdvisor;
import com.microsoft.tfs.core.httpclient.UsernamePasswordCredentials;
import com.microsoft.tfs.core.util.URIUtils;

/**
 * Connects to TFS using the SDK credentials supplied by the adapter configuration
 * A "functional user" must exist for TFS
 *
 */
public class TFSConnector implements IConnector {
    
    private static List<TFSConnector> connectors = new ArrayList<>();
    // Special connector for the functional user:
    private static TFSConnector connector;
    private TFSTeamProjectCollection tpc;
    
    private static final Logger logger = Logger
            .getLogger(TFSConnector.class.getName());

    public void setTpc(TFSTeamProjectCollection tpc) {
        this.tpc = tpc;
    }
    
    public static TFSConnector createAuthorized(Credentials credentials)
            throws UnauthorizedException {
    	TFSConnector tfsConnector = new TFSConnector();
    	try {
    	    tfsConnector.connectToTFS(credentials);
        } catch (Exception e) {
            logger.error("Error connecting: " + e.getMessage());
            return null;
        }
    	tfsConnector.validate();
        return tfsConnector;
    }
    
    private void validate() throws UnauthorizedException {
    	
    	WorkItemClient wic = null;
        try {
            wic = tpc.getWorkItemClient();
        } catch (Exception e) {
            logger.error("Error getting the work item client: " + e.getMessage());
            throw new UnauthorizedException("Failed to get work item client.");
        }
        
        try {
            @SuppressWarnings("unused")
            ProjectCollection projects = wic.getProjects();
        } catch (Exception e) {
            logger.error("Error getting the TFS Projects: " + e.getMessage());
            throw new UnauthorizedException("Failed to get the TFS Projects");
        }
    }
    
    protected TFSConnector() {
        synchronized (connectors) {
            connectors.add(this);
        }
    }
    
	public static void initialize(Credentials credentials)
            throws UnauthorizedException, URISyntaxException {
        if (connector == null) {
            connector = new TFSConnector();
            connector.connectToTFS(credentials);
            connector.validate();
        }
    }
    
    public static TFSTeamProjectCollection getTpc() {
        return connector.tpc;
    }
    
    public static WorkItemClient getWorkItemClient() {
        return connector.tpc.getWorkItemClient();
    }
    
    /**
     * Get an authorized TFSConnector from the HttpSession
     * 
     * The connector should be placed in the session by the CredentialsFilter
     * servlet filter.
     * 
     * @param request
     * @return connector
     */
    public static TFSConnector getAuthorized(HttpServletRequest request) {
        // Connector should never be null if CredentialsFilter is doing its job
        TFSConnector connector = (TFSConnector) request.getSession()
                .getAttribute(CredentialsFilter.CONNECTOR_ATTRIBUTE);
        if (connector == null) {
            logger.error("TFS Connector not initialized - check adapter.properties");
        }
        return connector;
    }    

    public boolean isValid() {
        try {
            validate();
            return tpc != null;
        } catch (UnauthorizedException e) {
            return false;
        }
    }

    public boolean isAdmin() {
        // TODO: This is needed for OAuth authentication. For now not used, but likely
    	// want to ensure that user has admin rights to allow granting access.
        return true;
    }

    private  void connectToTFS(Credentials credentials)
            throws URISyntaxException {
        com.microsoft.tfs.core.httpclient.Credentials tfsCredentials;

        tfsCredentials = new UsernamePasswordCredentials(
                credentials.getUsername(), credentials.getPassword());

        URI collectionURI = URIUtils.newURI(TFSAdapterManager.getCollectionUrl()); //new URI(collectionUrl);
        ConnectionAdvisor connectionAdvisor = new TFSConnectionAdvisor();
        
        logger.debug("Creating a TPC collection using: \n" + 
        		"collectionUri: " + collectionURI.toString() + "\n" +
        		"credentials (user): " + credentials.getUsername() + "\n" + 
        		"connectionAdvisor: " + connectionAdvisor.getClass().toString());
        
        tpc = new TFSTeamProjectCollection(collectionURI, tfsCredentials,
                connectionAdvisor);
        
        logger.debug("TPC collection created.");
    }
    
    public static TFSConnector getConnector() {
        return connector;
    }
}
