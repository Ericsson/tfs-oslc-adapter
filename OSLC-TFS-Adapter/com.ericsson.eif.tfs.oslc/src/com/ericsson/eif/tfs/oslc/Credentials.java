/*******************************************************************************
 * Copyright (c) 2011 IBM Corporation.
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
 *     IBM Corporation - initial API and implementation
 *     FindOut/Ericsson
 *******************************************************************************/
package com.ericsson.eif.tfs.oslc;

/**
 * Encapsulates a TFS username and password.
 * 
 * @author Samuel Padgett <spadgett@us.ibm.com>
 */
public class Credentials {
    private String username;
    private String password;

    public Credentials(String username, String password) {
        this.username = username;
        this.password = password;
    }
    
    public Credentials() {
    }

    public String getUsername() {
        return username;
    }

    public void setUsername(String username) {
        this.username = username;
    }

    public String getPassword() {
        return password;
    }

    public void setPassword(String password) {
        this.password = password;
    }
    
    @Override
    public boolean equals(Object obj) {
        if (obj instanceof Credentials) {
            Credentials c = (Credentials) obj;
            return c.getUsername().equals(username) &&
                    c.getPassword().equals(password);
        }
        return false;
    }
    
    @Override
    public String toString() {
        return "user: " + getUsername();
    }
}