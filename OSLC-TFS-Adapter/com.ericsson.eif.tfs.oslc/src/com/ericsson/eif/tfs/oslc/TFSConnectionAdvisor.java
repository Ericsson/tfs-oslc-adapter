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

import java.util.Locale;
import java.util.TimeZone;

import org.apache.log4j.LogManager;
import org.apache.log4j.Logger;

import com.microsoft.tfs.core.config.ConnectionAdvisor;
import com.microsoft.tfs.core.config.ConnectionInstanceData;
import com.microsoft.tfs.core.config.auth.DefaultTransportRequestHandler;
import com.microsoft.tfs.core.config.client.ClientFactory;
import com.microsoft.tfs.core.config.client.DefaultClientFactory;
import com.microsoft.tfs.core.config.httpclient.ConfigurableHTTPClientFactory;
import com.microsoft.tfs.core.config.httpclient.DefaultHTTPClientFactory;
import com.microsoft.tfs.core.config.httpclient.HTTPClientFactory;
import com.microsoft.tfs.core.config.persistence.DefaultPersistenceStoreProvider;
import com.microsoft.tfs.core.config.persistence.PersistenceStoreProvider;
import com.microsoft.tfs.core.config.serveruri.DefaultServerURIProvider;
import com.microsoft.tfs.core.config.serveruri.ServerURIProvider;
import com.microsoft.tfs.core.config.tfproxy.TFProxyServerSettingsFactory;
import com.microsoft.tfs.core.config.webservice.DefaultWebServiceFactory;
import com.microsoft.tfs.core.config.webservice.WebServiceFactory;

public class TFSConnectionAdvisor implements ConnectionAdvisor {

    private static Logger logger = LogManager.getLogger(TFSConnectionAdvisor.class.getName());
	
    @Override
    public ClientFactory getClientFactory(ConnectionInstanceData instanceData) {
        return new DefaultClientFactory();
    }

    @Override
    public HTTPClientFactory getHTTPClientFactory(
            ConnectionInstanceData instanceData) {
        // TODO: See SnippetsSamplesConnectionAdvisor
        // do we need to override the default factory?
    	logger.debug("TFS getting the getHTTPClientFactory.");
    	
        return new DefaultHTTPClientFactory(instanceData);
    }

    @Override
    public Locale getLocale(ConnectionInstanceData instanceData) {
        // TODO Implement proper support for locale? Or is this enough?
        return Locale.getDefault();
    }

    @Override
    public PersistenceStoreProvider getPersistenceStoreProvider(
            ConnectionInstanceData instanceData) {
        // TODO: See SnippetsSamplesConnectionAdvisor
        // do we need to override the default store behavior?
    	logger.debug("TFS getting the getPersistenceStoreProvider.");
    	
        return new TFSPersistenceStoreProvider();
    }

    static class TFSPersistenceStoreProvider extends
            DefaultPersistenceStoreProvider {

        public TFSPersistenceStoreProvider() {
            super();
        }
    }

    @Override
    public ServerURIProvider getServerURIProvider(
            ConnectionInstanceData instanceData) {
    	
    	logger.debug("TFS getting the getServerURIProvider.");
    	
        return new DefaultServerURIProvider(instanceData);
    }

    @Override
    public TFProxyServerSettingsFactory getTFProxyServerSettingsFactory(
            ConnectionInstanceData instanceData) {
        // TODO Auto-generated method stub
    	
    	logger.debug("TFS getting the getTFProxyServerSettingsFactory.");
    	
        return null;
    }

    @Override
    public TimeZone getTimeZone(ConnectionInstanceData instanceData) {
        // TODO Auto-generated method stub
        return null;
    }

    @Override
    public WebServiceFactory getWebServiceFactory(
            ConnectionInstanceData instanceData) {
    	
    	logger.debug("TFS getting the getWebServiceFactory.");

        return new DefaultWebServiceFactory(
                getLocale(instanceData),
                new DefaultTransportRequestHandler(
                        instanceData,
                        (ConfigurableHTTPClientFactory) getHTTPClientFactory(instanceData)));
    }

}
