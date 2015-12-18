/*******************************************************************************
 * Copyright (c) 2012 IBM Corporation.
 *
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * and Eclipse Distribution License v. 1.0 which accompanies this distribution.
 *
 * The Eclipse Public License is available at http://www.eclipse.org/legal/epl-v10.html
 * and the Eclipse Distribution License is available at
 * http://www.eclipse.org/org/documents/edl-v10.php.
 *
 * Contributors:
 *
 *     Michael Fiedler     - initial API and implementation for Bugzilla adapter
 *     
 *******************************************************************************/
package com.ericsson.eif.tfs.oslc;

import java.io.BufferedWriter;
import java.io.File;
import java.io.FileWriter;
import java.io.IOException;
import java.nio.file.FileSystems;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.Paths;
import java.nio.file.attribute.PosixFilePermission;
import java.util.HashSet;
import java.util.Set;

import javax.servlet.Filter;
import javax.servlet.FilterChain;
import javax.servlet.FilterConfig;
import javax.servlet.ServletException;
import javax.servlet.ServletRequest;
import javax.servlet.ServletResponse;
import javax.servlet.http.HttpServletRequest;
import javax.servlet.http.HttpServletResponse;
import javax.servlet.http.HttpSession;

import net.oauth.OAuth;
import net.oauth.OAuthException;
import net.oauth.OAuthProblemException;
import net.oauth.http.HttpMessage;
import net.oauth.server.OAuthServlet;

import org.apache.log4j.Logger;
import org.eclipse.lyo.server.oauth.consumerstore.FileSystemConsumerStore;
import org.eclipse.lyo.server.oauth.core.Application;
import org.eclipse.lyo.server.oauth.core.AuthenticationException;
import org.eclipse.lyo.server.oauth.core.OAuthConfiguration;
import org.eclipse.lyo.server.oauth.core.OAuthRequest;
import org.eclipse.lyo.server.oauth.core.token.LRUCache;
import org.eclipse.lyo.server.oauth.core.token.SimpleTokenStrategy;

import com.ericsson.eif.tfs.oslc.exception.UnauthorizedException;
import com.ericsson.eif.tfs.oslc.utils.HttpUtils;

public class CredentialsFilter implements Filter {

    private static final Logger logger = Logger
            .getLogger(CredentialsFilter.class.getName());

    public static final String CONNECTOR_ATTRIBUTE = "org.eclipse.lyo.oslc4j.bugzilla.IConnector";
    public static final String CREDENTIALS_ATTRIBUTE = "org.eclipse.lyo.oslc4j.bugzilla.Credentials";
    private static final String ADMIN_SESSION_ATTRIBUTE = "org.eclipse.lyo.oslc4j.bugzilla.AdminSession";
    public static final String JAZZ_INVALID_EXPIRED_TOKEN_OAUTH_PROBLEM = "invalid_expired_token";
    public static final String OAUTH_REALM = "TFS";

    private static LRUCache<String, IConnector> keyToConnectorCache = new LRUCache<String, IConnector>(
            200);

    @Override
    public void destroy() {
    }

    /**
     * Check for OAuth or BasicAuth credentials and challenge if not found.
     * 
     * Store the IConnector in the HttpSession for retrieval in the REST
     * services.
     */
    @Override
    public void doFilter(ServletRequest servletRequest,
            ServletResponse servletResponse, FilterChain chain)
            throws IOException, ServletException {

        if (!(servletRequest instanceof HttpServletRequest)
                || !(servletResponse instanceof HttpServletResponse)) {
            chain.doFilter(servletRequest, servletResponse);
            return;
        }
        
        if (TFSAdapterManager.isAuthenticationDisabled()) {
            logger.info("Authentication DISABLED");
            chain.doFilter(servletRequest, servletResponse);
            return;
        }

        HttpServletRequest request = (HttpServletRequest) servletRequest;
        HttpServletResponse response = (HttpServletResponse) servletResponse;

        // Don't protect requests to the admin parts of the oauth service.
        if (request.getPathInfo().startsWith("/oauth")) {
            chain.doFilter(servletRequest, servletResponse);
            return;
        }

        String token = OAuthServlet.getMessage(request, null).getToken();
        if (token != null) {
            // OAuth - check if authenticated
            if (!isOAuthAuthenticated(token, request, response)) {
                return;
            }
        } else {
            // Basic Authentication - check if authenticated
            if (!isBasicAuthenticated(request, response)) {
                return;
            }
        }

        // Authenticated - let call through
        chain.doFilter(servletRequest, servletResponse);
    }

    /**
     * Check if the oauth token is valid, and if so
     * 
     * @param token
     * @param request
     * @param response
     * @return
     * @throws IOException
     * @throws ServletException
     */
    private boolean isOAuthAuthenticated(String token,
            HttpServletRequest request, HttpServletResponse response)
            throws IOException, ServletException {

        try {
            try {
                OAuthRequest oAuthRequest = new OAuthRequest(request);
                oAuthRequest.validate();
                IConnector connector = keyToConnectorCache.get(token);

                // Check so session is still valid - if not, reset
                if (connector != null && !TFSAdapterManager.isConnected()) {
                    TFSAdapterManager.clearSession();
                    connector = null;
                    keyToConnectorCache.remove(token);
                    logger.info("TFS session disconnected - will retry to connect.");
                }

                if (connector == null) {
                    throw new OAuthProblemException(
                            OAuth.Problems.TOKEN_REJECTED);
                }

                request.getSession().setAttribute(CONNECTOR_ATTRIBUTE,
                        connector);

            } catch (OAuthProblemException e) {
                if (OAuth.Problems.TOKEN_REJECTED.equals(e.getProblem()))
                    throwInvalidExpiredException(e);
                else
                    throw e;
            }
        } catch (OAuthException e) {
            OAuthServlet.handleException(response, e, OAUTH_REALM);
            return false;
        }

        return true;
    }

    /**
     * Check if the basic credentials are valid, if so - create and store a
     * TFS connection in the session.
     * 
     * @param request
     * @param response
     * @return
     * @throws IOException
     * @throws ServletException
     */
    private boolean isBasicAuthenticated(HttpServletRequest request,
            HttpServletResponse response) throws IOException, ServletException {

        HttpSession session = request.getSession();
        IConnector connector = (IConnector) session
                .getAttribute(CredentialsFilter.CONNECTOR_ATTRIBUTE);

        if (connector != null) {
            if (!TFSAdapterManager.isConnected()) {
                // Session is not valid - reset
                TFSAdapterManager.clearSession();
                connector = null;
                session.setAttribute(CONNECTOR_ATTRIBUTE, null);
                logger.info("TFS session disconnected - will retry to connect.");
            } else {
                // We have an authenticated connector in session - return
                return true;
            }
        }

        try {
            // Try getting credentials from request and create a connection
            Credentials credentials = (Credentials) session
                    .getAttribute(CREDENTIALS_ATTRIBUTE);
            logger.debug("Credentials from session: " + credentials);
            if (credentials == null) {
                credentials = HttpUtils.getCredentials(request);
                if (credentials == null) {
                    throw new UnauthorizedException();
                }
                logger.debug("Credentials: " + credentials.getUsername());
            }
            if (credentials != null) {
                if (credentials.equals(TFSAdapterManager.getAdapterUserCredentials())) {
                	connector = TFSConnector.getConnector();
                } else {
                	connector = TFSConnector.createAuthorized(credentials);
                }
                
                if (connector == null || !connector.isValid()) {
                    // Failed creating a connector for some other reason than access
                    // Try restart the SDK connection.
                    TFSAdapterManager.clearSession();
                    logger.info("TFS Session not connected - will retry to connect.");
                    return false;
                }

                // We have an authenticated connector in session
                session.setAttribute(CONNECTOR_ATTRIBUTE, connector);
                session.setAttribute(CREDENTIALS_ATTRIBUTE, credentials);
            }

        } catch (UnauthorizedException e) {
            // This will trigger the browser to open a basic login dialog
            logger.info("Sending unauthorized-response to get browser login dialog.");
            HttpUtils.sendUnauthorizedResponse(response, e);
            return false;
        }

        return true;
    }

    @Override
    public void init(FilterConfig arg0) throws ServletException {
        OAuthConfiguration config = OAuthConfiguration.getInstance();

        // Validates a user's ID and password.
        config.setApplication(new Application() {
            @Override
            public void login(HttpServletRequest request, String id,
                    String password) throws AuthenticationException {
                try {
                    Credentials creds = new Credentials();
                    creds.setUsername(id);
                    creds.setPassword(password);

                    IConnector connector = TFSConnector.createAuthorized(creds);
                    request.setAttribute(CONNECTOR_ATTRIBUTE, connector);
                    request.getSession().setAttribute(CREDENTIALS_ATTRIBUTE,
                            creds);
                    request.getSession().setAttribute(ADMIN_SESSION_ATTRIBUTE,
                    		connector.isAdmin());
                } catch (UnauthorizedException e) {
                    throw new AuthenticationException(e.getMessage(), e);
                } 
            }

            @Override
            public String getName() {
                // Display name for this application.
                return "TFS";
            }

            @Override
            public boolean isAdminSession(HttpServletRequest request) {
                return Boolean.TRUE.equals(request.getSession().getAttribute(
                        ADMIN_SESSION_ATTRIBUTE));
            }

            @Override
            public String getRealm(HttpServletRequest request) {
                return TFSAdapterManager.REALM;
            }

            @Override
            public boolean isAuthenticated(HttpServletRequest request) {
                IConnector connector = (IConnector) request.getSession().getAttribute(
                        CONNECTOR_ATTRIBUTE);
                if (connector == null) {
                    return false;
                }
                request.setAttribute(CONNECTOR_ATTRIBUTE, connector);
                return true;
            }
        });

        /*
         * Override some SimpleTokenStrategy methods so that we can keep the
         * connection associated with the OAuth tokens.
         */
        config.setTokenStrategy(new SimpleTokenStrategy() {
            @Override
            public void markRequestTokenAuthorized(
                    HttpServletRequest httpRequest, String requestToken)
                    throws OAuthProblemException {
                IConnector connector = (IConnector) httpRequest
                        .getAttribute(CONNECTOR_ATTRIBUTE);
                keyToConnectorCache.put(requestToken, connector);
                super.markRequestTokenAuthorized(httpRequest, requestToken);
            }

            @Override
            public void generateAccessToken(OAuthRequest oAuthRequest)
                    throws OAuthProblemException, IOException {
                String requestToken = oAuthRequest.getMessage().getToken();
                IConnector connector = keyToConnectorCache.remove(requestToken);
                super.generateAccessToken(oAuthRequest);
                keyToConnectorCache.put(oAuthRequest.getAccessor().accessToken,
                        connector);
            }
        });

        try {
            // For now, keep the consumer info in a file. This is only the
            // credentials for allowing server access - to be able to read/write
            // you also need to log in with a valid TFS user. So assume this
            // info is not that sensitive.
            String root = TFSAdapterManager.getAdapterServletHome();
            String oauthStore = root + File.separator + "adapterOAuthStore.xml";
            makeFileOwnerPrivate(oauthStore);
            config.setConsumerStore(new FileSystemConsumerStore(oauthStore));
        } catch (Throwable t) {
            logger.error("Error initializing the OAuth consumer store.", t);
        }
    }

    /**
     * Makes file read/writable only to the owner
     */
    private void makeFileOwnerPrivate(String absolutePath) {
        // create empty file if it does not already exist
        File file = new File(absolutePath);
        if (!file.exists()) {
            try {
                file.createNewFile();
                
                // An empty file will cause setConsumerStore to report error. So initialize.
                BufferedWriter output = new BufferedWriter(new FileWriter(file));
                String emptyRdf =
            		"<rdf:RDF" + "\n" + 
                		"    xmlns:rdf=\"http://www.w3.org/1999/02/22-rdf-syntax-ns#\"" + "\n" + 
                		"    xmlns:j.0=\"http://eclipse.org/lyo/server/oauth#\">" + "\n" +
                	"</rdf:RDF>";
                output.write(emptyRdf);
                output.close();     
            } catch (IOException e) {
                logger.error("Error creating file: " + absolutePath, e);
            }
        }

        // on a POSIX system - protect the file from "others":
        if (FileSystems.getDefault().supportedFileAttributeViews()
                .contains("posix")) {
            Path path = Paths.get(absolutePath);
            Set<PosixFilePermission> permissions = new HashSet<>();
            permissions.add(PosixFilePermission.OWNER_READ);
            permissions.add(PosixFilePermission.OWNER_WRITE);
            try {
                Files.setPosixFilePermissions(path, permissions);
            } catch (IOException e) {
                logger.error("Error protecting the file: " + absolutePath, e);
            }
        }
    }

    /**
     * Jazz requires a exception with the magic string "invalid_expired_token"
     * to restart OAuth authentication
     * 
     * @param e
     * @return
     * @throws OAuthProblemException
     */
    private void throwInvalidExpiredException(OAuthProblemException e)
            throws OAuthProblemException {
        OAuthProblemException ope = new OAuthProblemException(
                JAZZ_INVALID_EXPIRED_TOKEN_OAUTH_PROBLEM);
        ope.setParameter(HttpMessage.STATUS_CODE, new Integer(
                HttpServletResponse.SC_UNAUTHORIZED));
        throw ope;
    }
}
