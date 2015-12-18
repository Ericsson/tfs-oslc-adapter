/*******************************************************************************
 * Copyright (c) 2011, 2012 IBM Corporation and others.
 *
 *  All rights reserved. This program and the accompanying materials
 *  are made available under the terms of the Eclipse Public License v1.0
 *  and Eclipse Distribution License v. 1.0 which accompanies this distribution.
 *  
 *  The Eclipse Public License is available at http://www.eclipse.org/legal/epl-v10.html
 *  and the Eclipse Distribution License is available at
 *  http://www.eclipse.org/org/documents/edl-v10.php.
 *  
 *  Contributors:
 *  
 *	   Sam Padgett	       - initial API and implementation
 *     Michael Fiedler     - adapted for OSLC4J
 *     Jad El-khoury        - initial implementation of code generator (https://bugs.eclipse.org/bugs/show_bug.cgi?id=422448)
 *
 * This file is generated by org.eclipse.lyo.oslc4j.codegenerator
 *******************************************************************************/

package com.ericsson.eif.tfs.oslc.servlet;

import java.net.InetAddress;
import java.net.UnknownHostException;

import javax.servlet.ServletContext;
import javax.servlet.ServletContextEvent;
import javax.servlet.ServletContextListener;

import com.ericsson.eif.tfs.oslc.TFSAdapterManager;


import org.apache.log4j.Logger;
// Start of user code imports
import org.eclipse.lyo.oslc4j.client.ServiceProviderRegistryURIs;
// End of user code


public class ServletListener implements ServletContextListener  {

	private static String servletBase = null;
	private static String servicesBase = null;

	private static final String SERVICES_PATH = "/services";

	// Public keys to allow setting of properties
	public static final String PROPERTY_SCHEME = ServletListener.class.getPackage().getName() + ".scheme";
    public static final String PROPERTY_PORT   = ServletListener.class.getPackage().getName() + ".port";
    public static final String PROPERTY_HOST   = ServletListener.class.getPackage().getName() + ".host";
    
    private static Logger logger = Logger.getLogger(ServletListener.class);

	// Start of user code class_attributes
    private static final String CATALOG_PATH_SEGMENT = "catalog";
    private static final String SYSTEM_PROPERTY_NAME_REGISTRY_URI = 
            ServiceProviderRegistryURIs.class.getPackage().getName() + 
            ".registryuri";
	// End of user code
	
	// Start of user code class_methods
	// End of user code

    public ServletListener() {
        super();
    }

	private static String getHost() {
        try {
            String hostname = InetAddress.getLocalHost().getCanonicalHostName();
            return hostname.toLowerCase();
        } catch (final UnknownHostException exception) {
            return "localhost";
        }
	}

	public static String getServletBase() {
		return servletBase;
	}

	public static String getServicesBase() {
		return servicesBase;
	}

    private static String generateBasePath(final ServletContextEvent servletContextEvent)
    {
        final ServletContext servletContext = servletContextEvent.getServletContext();

        String scheme = System.getProperty(PROPERTY_SCHEME);
        if (scheme == null)
        {
            scheme = servletContext.getInitParameter(PROPERTY_SCHEME);
        }

        String port = System.getProperty(PROPERTY_PORT);
        if (port == null)
        {
            port = servletContext.getInitParameter(PROPERTY_PORT);
        }
        
        String host = System.getProperty(PROPERTY_HOST);
        if (host == null)
        {
        	host = getHost();
        }

        String path = scheme + "://" + host;
        path += (port == null || port.isEmpty() ? "" : ":") + port;
        path += servletContext.getContextPath();
        
        logger.debug("Base path: " + path);

        return path;
    }

    @Override
    public void contextInitialized(final ServletContextEvent servletContextEvent)
    {
		// Start of user code contextInitialized_init
    	
		// Establish connection to data backbone etc ...
		TFSAdapterManager.contextInitializeServletListener(servletContextEvent);		
		// End of user code

    	String basePath=generateBasePath(servletContextEvent);
    	servletBase = basePath;
    	servicesBase = basePath + SERVICES_PATH;

		logger.info("servletListner contextInitialized.");

		// Start of user code contextInitialized_final
		System.setProperty(SYSTEM_PROPERTY_NAME_REGISTRY_URI, servicesBase + "/" + CATALOG_PATH_SEGMENT);
		// End of user code
    }

	@Override
	public void contextDestroyed(ServletContextEvent servletContextEvent) 
	{
		// Start of user code contextDestroyed_init
		// End of user code

		// Shutdown connections to data backbone etc...
		TFSAdapterManager.contextDestroyServletListener(servletContextEvent);	

		// Start of user code contextDestroyed_final
		// End of user code
	}

}
