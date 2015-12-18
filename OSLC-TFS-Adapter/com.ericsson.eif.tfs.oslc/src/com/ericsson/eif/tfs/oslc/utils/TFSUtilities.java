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

import com.microsoft.tfs.core.clients.workitem.WorkItem;
import com.microsoft.tfs.core.clients.workitem.WorkItemClient;
import com.microsoft.tfs.core.clients.workitem.project.Project;
import com.microsoft.tfs.core.clients.workitem.query.WorkItemCollection;

public class TFSUtilities {
	
	static Logger logger = Logger.getLogger(TFSUtilities.class);

    public static Project getProjectById(int id, WorkItemClient wic) {
        for (Project p : wic.getProjects()) {
            if (p.getID() == id) {
                return p;
            }
        }
        return null;
    }
    
    public static Project getProjectById(String id, WorkItemClient wic) {
        return getProjectById(Integer.parseInt(id), wic);
    }
    
    private static String bugQueryProlog = 
            "Select ID, Title from WorkItems where " +
            "(System.TeamProject = '";
                 
    private static String bugQueryEpilog = "')";

    public static WorkItemCollection getBugs(Project project, WorkItemClient wic) {
        String query = bugQueryProlog + project.getName() + bugQueryEpilog;
        WorkItemCollection workItems = wic.query(query);
        return workItems;
    }
    
 
    /**
     * Get a Bug that is linked to a TR with trId. Should return 0..1
     * work items. Note that only ID is retrieved, so caller need to
     * fetch more fields if needed.
     * 
     * @param wic
     * @param trId
     * @return
     */
    public static WorkItem getBugForTR(WorkItemClient wic, String trId) {
    	String query = 
                "Select ID from WorkItems where (Ericsson.Defect.Link contains '" + 
                trId + "')";
        WorkItemCollection workItems = wic.query(query);
        
        if (workItems.size() == 1) {
        	return workItems.getWorkItem(0);
        } else if (workItems.size() > 1) {
        	// Log error and return null
        	logger.error("Found > 1 Bugs connected to TR with id: " + trId + ". Should be 0..1");
        } 
        
        return null;
    }
    
    public static WorkItem getWorkItem(WorkItemClient wic, String wiID) {
        return wic.getWorkItemByID(Integer.parseInt(wiID));
    }


}
