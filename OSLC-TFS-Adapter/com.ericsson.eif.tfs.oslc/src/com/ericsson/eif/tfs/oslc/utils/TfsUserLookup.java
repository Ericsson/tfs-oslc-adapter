package com.ericsson.eif.tfs.oslc.utils;

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

import org.apache.log4j.Logger;

import com.microsoft.tfs.core.TFSTeamProjectCollection;
import com.microsoft.tfs.core.clients.webservices.IIdentityManagementService;
import com.microsoft.tfs.core.clients.webservices.IdentityManagementService;
import com.microsoft.tfs.core.clients.webservices.IdentitySearchFactor;
import com.microsoft.tfs.core.clients.webservices.MembershipQuery;
import com.microsoft.tfs.core.clients.webservices.ReadIdentityOptions;
import com.microsoft.tfs.core.clients.webservices.TeamFoundationIdentity;

public class TfsUserLookup {

    private final IIdentityManagementService ims;
    private Logger logger = Logger.getLogger(TfsUserLookup.class);

    public TfsUserLookup(TFSTeamProjectCollection tpc) {
        ims = new IdentityManagementService(tpc);
    }

    public String getUserName(String signum) {
        if (signum.isEmpty()) {
            return null; // no signum -> null user
        }
        try {
            TeamFoundationIdentity tfsUser = ims.readIdentity(
                    IdentitySearchFactor.ACCOUNT_NAME, signum,
                    MembershipQuery.NONE, ReadIdentityOptions.NONE);
            if (tfsUser != null) {
                return tfsUser.getDisplayName();
            }
        } catch (Exception e) {
            logger.error("Exception while looking up display name for: " + signum, e);
        }
        return null;
    }

    public String getSignum(String displayName) {
        if (displayName == null || displayName.isEmpty()) {
            return null; // no name -> null signum
        }
        try {
            TeamFoundationIdentity tfsUser = ims.readIdentity(
                    IdentitySearchFactor.DISPLAY_NAME, 
                    displayName,
                    MembershipQuery.NONE, 
                    ReadIdentityOptions.NONE);
            if (tfsUser != null) {
                String qualifiedSignum = tfsUser.getUniqueName();
                // Strip of the domain name:
                return qualifiedSignum.substring(qualifiedSignum.indexOf("\\") + 1);
            }
        } catch (Exception e) {
            logger.error("Exception while looking up signum for: " + displayName, e);
        }
        return null;
    }
}
