package com.ericsson.eif.tfs.oslc.factories;

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
import javax.ws.rs.core.UriInfo;

import org.apache.log4j.Logger;

import com.ericsson.eif.tfs.oslc.TFSAdapterManager;
import com.ericsson.eif.tfs.oslc.TFSConnector;
import com.ericsson.eif.tfs.oslc.exception.CreateWorkItemException;
import com.ericsson.eif.tfs.oslc.exception.UpdateWorkItemException;
import com.ericsson.eif.tfs.oslc.mapping.TFSMapper;
import com.ericsson.eif.tfs.oslc.resources.EnterpriseChangeRequest;
import com.ericsson.eif.tfs.oslc.utils.ValidationMessages;
import com.microsoft.tfs.core.clients.workitem.WorkItem;
import com.microsoft.tfs.util.GUID;

public class TFSChangeRequestFactory {
    
    static Logger logger = Logger.getLogger(TFSChangeRequestFactory.class);
    
    public static EnterpriseChangeRequest createChangeRequest(
    		EnterpriseChangeRequest ecr,
            HttpServletRequest httpServletRequest, UriInfo uriInfo) throws CreateWorkItemException {

        TFSWorkItemFactory.createWorkItem(
                ecr,
                httpServletRequest);
        
        return ecr;
    }

    public static EnterpriseChangeRequest createChangeRequest(
            EnterpriseChangeRequest ecr,
            String projectId, 
            HttpServletRequest httpServletRequest) throws CreateWorkItemException {

        int id = Integer.parseInt(projectId);
        
        TFSWorkItemFactory.createWorkItem(
                id,
                ecr,
                httpServletRequest);
        
        return ecr;
    }

    public static ValidationMessages updateChangeRequest(EnterpriseChangeRequest ecr,
            String workItemId, HttpServletRequest httpServletRequest) throws UpdateWorkItemException {
        
        return TFSWorkItemFactory.updateWorkItem(
                ecr,
                workItemId,
                httpServletRequest);
    }
    
    public static EnterpriseChangeRequest getChangeRequest(
            String collectionId,
            String workItemId, UriInfo uriInfo) throws URISyntaxException {
        WorkItem workItem = TFSWorkItemFactory.getWorkItem(workItemId);
        EnterpriseChangeRequest ecr = createEnterpriseChangeRequest(workItem, uriInfo);
        return ecr;
    }
    
    public static List<EnterpriseChangeRequest> getChangeRequests(
            String collectionId,
            String projectId,
            UriInfo uriInfo
            ) throws URISyntaxException {
        List<WorkItem> workItems = new ArrayList<>();
        List<EnterpriseChangeRequest> results = new ArrayList<>();
        workItems = TFSWorkItemFactory.getWorkItems(collectionId, projectId);
        for (WorkItem workItem : workItems) {
            results.add(createEnterpriseChangeRequest(workItem, uriInfo));
        }
        
        return results;
    }

    /**
     * Creates an {@link EnterpriseChangeRequest} from the given {@link WorkItem}
     * 
     * @param workItem
     * @param uriInfo
     * @return
     * @throws URISyntaxException
     */
    private static EnterpriseChangeRequest createEnterpriseChangeRequest(
            WorkItem workItem, UriInfo uriInfo) throws URISyntaxException {
        EnterpriseChangeRequest ecr = new EnterpriseChangeRequest();
        TFSMapper mapper = TFSMapper.getInstance();
        mapper.clear();
        mapper.setEcrValues(ecr, TFSMapper.ECM_TITLE, workItem);
        mapper.setEcrValues(ecr, TFSMapper.ECM_IDENTIFIER, workItem);
        mapper.setEcrValues(ecr, TFSMapper.ECM_PRIORITY, workItem);
        mapper.setEcrValues(ecr, TFSMapper.ECM_OWNER, workItem);
        mapper.setEcrValues(ecr, TFSMapper.ECM_COUNTRY, workItem);
        mapper.setEcrValues(ecr, TFSMapper.ECM_SITE, workItem);
        mapper.setEcrValues(ecr, TFSMapper.ECM_CUSTOMER, workItem);
        mapper.setEcrValues(ecr, TFSMapper.ECM_IMPACT_ON_ISP, workItem);
        mapper.setEcrValues(ecr, TFSMapper.ECM_DIDDET, workItem);
        mapper.setEcrValues(ecr, TFSMapper.ECM_DESCRIPTION, workItem);
        mapper.setEcrValues(ecr, TFSMapper.ECM_EXPECTED_IMPACT_ON_ISP, workItem); //
        mapper.setEcrValues(ecr, TFSMapper.ECM_ANSWER_CODE, workItem);
        mapper.setEcrValues(ecr, TFSMapper.ECM_FAULT_CODE, workItem);
        mapper.setEcrValues(ecr, TFSMapper.ECM_ANSWER, workItem);
        mapper.setEcrValues(ecr, TFSMapper.ECM_STATUS, workItem);
        mapper.setEcrValues(ecr, TFSMapper.ECM_ACTIVITY, workItem);
        mapper.setEcrValues(ecr, TFSMapper.ECM_FIRST_TECHNICAL_CONTACT, workItem);
        mapper.setEcrValues(ecr, TFSMapper.ECM_NODE_PRODUCT, workItem);
        mapper.setEcrValues(ecr, TFSMapper.ECM_NODE_PRODUCT_REVISION, workItem);
        mapper.setEcrValues(ecr, TFSMapper.ECM_PRODUCT, workItem);
        mapper.setEcrValues(ecr, TFSMapper.ECM_PRODUCT_REVISION, workItem);
        mapper.setEcrValues(ecr, TFSMapper.ECM_CORRECTED_PRODUCT, workItem);
        mapper.setEcrValues(ecr, TFSMapper.ECM_CORRECTED_PRODUCT_REVISION, workItem);
        mapper.setEcrValues(ecr, TFSMapper.ECM_CORRECTED_NODE_PRODUCT, workItem);
        mapper.setEcrValues(ecr, TFSMapper.ECM_CORRECTED_NODE_PRODUCT_REVISION, workItem);
        mapper.setEcrValues(ecr, TFSMapper.ECM_CURRENT_MHO, workItem);
        mapper.setEcrValues(ecr, TFSMapper.ECM_ATTACHMENT, workItem);
        mapper.setEcrValues(ecr, TFSMapper.ECM_RELATED_CHANGE_REQUEST, workItem);
        
        TFSWorkItemFactory.setAbout(ecr, workItem);
        return ecr;
    }

    /**
     * Returns the URI for the TFS WebUI for the given {@link WorkItem}
     * In context of this provider, it should be used for HTML response
     * but not in RDF/XML
     * @param workItemId
     * @return
     */
    public static URI getWorkItemUri(String  workItemId) {
        //TODO - cleanup, replace use of deprecated method etc
        // http://<TFS server address>:8080/tfs/web/wi.aspx?pcguid=" + GUID + "&id=" + workItemID
        // uri - (collectionName + "/") + "web/wi.aspx?pcguid=" + GUID + "&id=" + workItemID
        GUID guid = TFSConnector.getTpc().getInstanceID();
        String segment1 = "web/wi.aspx?pcguid=";
        String segment2 = "&id=";
        URI uri;
        try {
            uri = TFSConnector.getTpc().getURL().toURI();
            String uriStr = uri.toString();
            String collectionName = TFSAdapterManager.getTfsCollection();
            String baseUri = uriStr.substring(0, uriStr.indexOf(collectionName));
            String workItemUriStr = baseUri + segment1 + guid + segment2 + workItemId;
            return new URI(workItemUriStr);
        } catch (URISyntaxException e) {
            logger.error("Failed to construct URI for workItem ID=" + workItemId, e);
            return null;
        }
    }
}
