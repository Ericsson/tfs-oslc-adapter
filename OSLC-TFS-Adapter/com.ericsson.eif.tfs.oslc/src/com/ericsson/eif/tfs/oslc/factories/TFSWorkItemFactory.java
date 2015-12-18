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

import org.apache.log4j.Logger;

import com.ericsson.eif.tfs.oslc.TFSAdapterManager;
import com.ericsson.eif.tfs.oslc.TFSConnector;
import com.ericsson.eif.tfs.oslc.exception.CreateWorkItemException;
import com.ericsson.eif.tfs.oslc.exception.CustomerMappingExpection;
import com.ericsson.eif.tfs.oslc.exception.UpdateWorkItemException;
import com.ericsson.eif.tfs.oslc.mapping.TFSMapper;
import com.ericsson.eif.tfs.oslc.resources.EnterpriseChangeRequest;
import com.ericsson.eif.tfs.oslc.servlet.ServletListener;
import com.ericsson.eif.tfs.oslc.utils.TFSUtilities;
import com.ericsson.eif.tfs.oslc.utils.ValidationMessages;
import com.microsoft.tfs.core.TFSTeamProjectCollection;
import com.microsoft.tfs.core.clients.workitem.WorkItem;
import com.microsoft.tfs.core.clients.workitem.WorkItemClient;
import com.microsoft.tfs.core.clients.workitem.exceptions.UnableToSaveException;
import com.microsoft.tfs.core.clients.workitem.exceptions.WorkItemException;
import com.microsoft.tfs.core.clients.workitem.fields.Field;
import com.microsoft.tfs.core.clients.workitem.fields.FieldCollection;
import com.microsoft.tfs.core.clients.workitem.fields.FieldStatus;
import com.microsoft.tfs.core.clients.workitem.project.Project;
import com.microsoft.tfs.core.clients.workitem.query.WorkItemCollection;
import com.microsoft.tfs.core.clients.workitem.wittype.WorkItemType;

public class TFSWorkItemFactory {

	static Logger logger = Logger.getLogger(TFSWorkItemFactory.class);

	/**
	 * Creates a new {@link WorkItem} in project based on the product to project
	 * mapping. See {@link TFSMapper#getProject(String)}
	 * 
	 * @param ecr
	 * @param httpServletRequest
	 * @return
	 * @throws CreateWorkItemException
	 */
	public static WorkItem createWorkItem(EnterpriseChangeRequest ecr,
			HttpServletRequest httpServletRequest)
			throws CreateWorkItemException {
		
		String projectName = TFSMapper.getInstance().getProject(ecr.getProduct());
		if (projectName == null || projectName.isEmpty()) {
			String message = "TFS Project Name to create workitem in not defined.";
			logger.error(message);
			throw new CreateWorkItemException(-1, message);
		}
		Project project = TFSConnector.getWorkItemClient().getProjects()
				.get(projectName);
		if (project == null) {
			String message = "Project Name: " + projectName + " not found in TFS.";
			logger.error(message);
			throw new CreateWorkItemException(-2, message);
		}
		
		return createWorkItem(project.getID(), ecr, httpServletRequest);
	}

	/**
	 * Creates a new {@link WorkItem} in the given project
	 * 
	 * @param projectId
	 * @param ecr
	 * @param httpServletRequest
	 * @return
	 * @throws CreateWorkItemException
	 */
	public static WorkItem createWorkItem(int projectId,
			EnterpriseChangeRequest ecr, HttpServletRequest httpServletRequest)
			throws CreateWorkItemException {

		TFSTeamProjectCollection tpc = TFSConnector.getTpc();
		WorkItemClient wic = tpc.getWorkItemClient();
		Project project = TFSUtilities.getProjectById(projectId, wic);

		// Find the work item type matching the specified name.
		WorkItemType bugWorkItemType = project.getWorkItemTypes().get("Bug");

		// Create a new work item of the specified type.
		WorkItem newWorkItem = project.getWorkItemClient().newWorkItem(
				bugWorkItemType);

		FieldCollection fields = newWorkItem.getFields();
		TFSMapper mapper = TFSMapper.getInstance();
		mapper.clear();

		ValidationMessages messages = new ValidationMessages();
		try {
			mapper.mapFromEcr(newWorkItem, TFSMapper.ECM_TITLE, ecr, messages);
			mapper.mapFromEcr(newWorkItem,TFSMapper.ECM_PRIORITY, ecr, 
					messages);
			mapper.mapFromEcr(newWorkItem, TFSMapper.ECM_COUNTRY, ecr, messages);
			mapper.mapFromEcr(newWorkItem, TFSMapper.ECM_CUSTOMER, ecr,
					messages);
			mapper.mapFromEcr(newWorkItem, TFSMapper.ECM_SITE, ecr, messages);
			mapper.mapFromEcr(newWorkItem, TFSMapper.ECM_IMPACT_ON_ISP, ecr,
					messages);
			mapper.mapFromEcr(newWorkItem, TFSMapper.ECM_DIDDET, ecr, messages);
			mapper.mapFromEcr(newWorkItem,TFSMapper.ECM_DESCRIPTION, ecr, 
					messages);
			mapper.mapFromEcr(newWorkItem,TFSMapper.ECM_ANSWER_CODE, ecr, 
					messages);
			mapper.mapFromEcr(newWorkItem, TFSMapper.ECM_FAULT_CODE, ecr, 
					messages);
			mapper.mapFromEcr(newWorkItem,TFSMapper.ECM_STATUS, ecr, messages);
			mapper.mapFromEcr(newWorkItem,TFSMapper.ECM_ACTIVITY, ecr,
					messages);
			// ignoring TFSMapper.ECM_FIRST_TECHNICAL_CONTACT_INFO
			// ignoring TFSMapper.ECM_FIRST_TECHNICAL_CONTACT
			mapper.mapFromEcr(newWorkItem,TFSMapper.ECM_PRODUCT, ecr, messages);
			mapper.mapFromEcr(newWorkItem,TFSMapper.ECM_PRODUCT_REVISION, ecr,
					messages);
			mapper.mapFromEcr(newWorkItem,TFSMapper.ECM_ATTACHMENT, ecr,
					messages);
			mapper.mapFromEcr(newWorkItem,TFSMapper.ECM_OWNER, ecr,
					messages);
			
			// special mapping rules:
			mapper.mapFromEcrLink(TFSMapper.ECM_RELATED_CHANGE_REQUEST, ecr,
					fields, messages);
			
		} catch (CustomerMappingExpection e) {
			throw new CreateWorkItemException(-1, e.getMessage());
		}

		// print any messages from setting values
		String report = messages.createReport();
		if (!report.isEmpty()) {
			logger.debug("!!! Field mapping message: " + report);
		}
		
		// check the workitem for invalid fields:
		String validationErrors = null;
		for (Field field : newWorkItem.getFields()) {
			if (field.getStatus() != FieldStatus.VALID) {
				validationErrors += " "
						+ field.getStatus().getInvalidMessage(field);
				logger.debug("!!! Field validation error: "
						+ field.getStatus().getInvalidMessage(field));
				logger.debug("!!! Field value           : " + field.getValue());
			}
		}
		
		//Set the flag for creating TR. When the flag is set to YES
		// the changes of the bug will be propagated back to TR
		setField(newWorkItem.getFields(),
				TFSMapper.ERICSSON_DEFECT_CREATETR, "Yes");

		// Save the new work item to the server.
		try {
			newWorkItem.save();
		} catch (WorkItemException e) {
			String message = "Exception while creating TFS work item.";
			if (validationErrors != null) {
				message += " Validation errors: " + validationErrors;
			}
			int statusCode = -1;
			if (e instanceof UnableToSaveException) {
				UnableToSaveException use = (UnableToSaveException) e;
				statusCode = use.getErrorID();
			}
			logger.error(message, e);
			throw new CreateWorkItemException(statusCode, message);
		}

		// Update the about now that we have saved and thus a workitem ID
		setAbout(ecr, newWorkItem);
		ecr.setIdentifier(newWorkItem.getID() + "");
		return newWorkItem;
	}

	/**
	 * Updates the given workitem based on the incoming ecr
	 * 
	 * @param ecr
	 * @param workItemId
	 * @param httpServletRequest
	 * @throws UpdateWorkItemException
	 */
	public static ValidationMessages updateWorkItem(
			EnterpriseChangeRequest ecr, String workItemId,
			HttpServletRequest httpServletRequest)
			throws UpdateWorkItemException {

		logger.debug("TFSWorkItemFactory.updateWorkItem()");

		boolean workItemChanged = false;
		WorkItem workItem = TFSUtilities.getWorkItem(
				TFSConnector.getWorkItemClient(), workItemId);

		ValidationMessages messages = new ValidationMessages();
		
		TFSMapper mapper = TFSMapper.getInstance();
		mapper.clear();

		// Handle case where the related item no longer should be connected
		// to the incoming ecr. If so, we will sever the link and save Bug
		boolean disconnect = mapper.shouldDisconnect(workItem, ecr, messages);
		if (disconnect) {
			save(workItem, messages);
			return messages; 
		}
		
		// Verify that the update is done towards a correctly mapped Bug
		// I.e. that the related Change Request matches the TR of the Bug
		mapper.validate(workItem, TFSMapper.ECM_RELATED_CHANGE_REQUEST, ecr);

		// Process the fields in the ECR and update the WI if needed		
		workItemChanged |= mapper.mapFromEcrIfChanged(workItem,
				TFSMapper.ECM_TITLE, ecr, messages);
		workItemChanged |= mapper.mapFromEcrIfChanged(workItem,
				TFSMapper.ECM_PRIORITY, ecr, messages);
		workItemChanged |= mapper.mapFromEcrIfChanged(workItem,
				TFSMapper.ECM_COUNTRY, ecr, messages);
		workItemChanged |= mapper.mapFromEcrIfChanged(workItem,
				TFSMapper.ECM_CUSTOMER, ecr, messages);
		workItemChanged |= mapper.mapFromEcrIfChanged(workItem, TFSMapper.ECM_SITE,
				ecr, messages);
		workItemChanged |= mapper.mapFromEcrIfChanged(workItem,
				TFSMapper.ECM_IMPACT_ON_ISP, ecr, messages);
		workItemChanged |= mapper.mapFromEcrIfChanged(workItem,
				TFSMapper.ECM_EXPECTED_IMPACT_ON_ISP, ecr, messages);
		workItemChanged |= mapper.mapFromEcrIfChanged(workItem,
				TFSMapper.ECM_DIDDET, ecr, messages);
		workItemChanged |= mapper.mapFromEcrIfChanged(workItem,
				TFSMapper.ECM_DESCRIPTION, ecr, messages);
		workItemChanged |= mapper.mapFromEcrIfChanged(workItem,
				TFSMapper.ECM_ANSWER_CODE, ecr, messages);
		workItemChanged |= mapper.mapFromEcrIfChanged(workItem,
				TFSMapper.ECM_FAULT_CODE, ecr, messages);
		workItemChanged |= mapper.mapFromEcrIfChanged(workItem,
				TFSMapper.ECM_STATUS, ecr, messages);
		workItemChanged |= mapper.mapFromEcrIfChanged(workItem,
				TFSMapper.ECM_ACTIVITY, ecr, messages);
				
		// ignoring TFSMapper.ECM_FIRST_TECHNICAL_CONTACT_INFO
		// ignoring TFSMapper.ECM_FIRST_TECHNICAL_CONTACT
		
		workItemChanged |= mapper.mapFromEcrIfChanged(workItem,
				TFSMapper.ECM_PRODUCT, ecr, messages);
		workItemChanged |= mapper.mapFromEcrIfChanged(workItem,
				TFSMapper.ECM_PRODUCT_REVISION, ecr, messages);
		
		workItemChanged |= mapper.mapFromEcrIfChanged(workItem,
				TFSMapper.ECM_CORRECTED_PRODUCT, ecr, messages);
		workItemChanged |= mapper.mapFromEcrIfChanged(workItem,
				TFSMapper.ECM_CORRECTED_PRODUCT_REVISION, ecr, messages);
		workItemChanged |= mapper.mapFromEcrIfChanged(workItem,
				TFSMapper.ECM_CORRECTED_NODE_PRODUCT, ecr, messages);
		workItemChanged |= mapper.mapFromEcrIfChanged(workItem,
				TFSMapper.ECM_CORRECTED_NODE_PRODUCT_REVISION, ecr, messages);

		workItemChanged |= mapper.mapFromEcrIfChanged(workItem,
				TFSMapper.ECM_OWNER, ecr, messages);
		workItemChanged |= mapper.mapFromEcrIfChanged(workItem,
				TFSMapper.ECM_ATTACHMENT, ecr, messages);

		workItemChanged |= mapper.mapFromEcrIfChanged(workItem,
				TFSMapper.ECM_ANSWER, ecr, messages);
		workItemChanged |= mapper.mapFromEcrIfChanged(workItem,
				TFSMapper.ECM_NOTEBOOK, ecr, messages);
		workItemChanged |= mapper.mapFromEcrIfChanged(workItem,
				TFSMapper.ECM_PROGRESS_INFO, ecr, messages);
		
		// Handle setting of Duplicate
		workItemChanged |= updateDuplicate(workItem, workItemId, ecr,
				messages);

		// There can be multiple fields from the ecr that provide input to the
		// TFS_HISTORY field, and each should be saved as a separate entry.
		// Changes from updating fields is saved in stack, and here we can pop
		// one change to include in the 1:st save.
		workItemChanged |= mapper.updateIfChanged(workItem, TFSMapper.TFS_HISTORY,
				mapper.popHistoryUpdate(), messages);

		// Commit the changes
		if (workItemChanged) {
			// Do NOT save if NOTHING has changed
			save(workItem, messages);
			workItemChanged = false;
		}

		// Handle rest of History updates with a syncToLatest as previous
		// save could have caused TFS server code to update workitem. 
		String historyUpdate = mapper.popHistoryUpdate();
		
		// The general pattern* is for TFS Consumer code (plugin) to ignore
		// content update, but update a field informing if latest change
		// was based on user action, or based on incoming change i.e. from
		// this code. So doing a syncToLatest to get updated workitem, and
		// rely on subsequent changes (history changes) will not cause other
		// updates.
		// *For more info on the architecture, see the design docs.
		if (historyUpdate != null) {
			workItem.syncToLatest();
		}
		while (historyUpdate != null) {			
			if (mapper.updateIfChanged(workItem, TFSMapper.TFS_HISTORY,
					historyUpdate, messages)) {
				save(workItem, messages);
			}
			historyUpdate = mapper.popHistoryUpdate();
		}

		return messages;
	}
	
	// Set the Duplicate state for any connected workitems
	private static boolean updateDuplicate(WorkItem workItem,
			String workItemId, EnterpriseChangeRequest ecr,
			ValidationMessages messages) {

		String updatedBugIds = "";
		TFSMapper mapper = TFSMapper.getInstance();
		List<WorkItem> updatedBugs = mapper.updateDuplicate(ecr, workItem,
				workItemId, messages);

		// Save the changed bugs - if errors, continue
		for (WorkItem bug : updatedBugs) {
			try {
				save(bug, messages);
				updatedBugIds += (updatedBugIds.isEmpty() ? "" : ", ")
						+ bug.getID();
			} catch (UpdateWorkItemException e) {
				logger.error("Failed to update Duplicate state for bug: "
						+ bug.getID());
			}
		}

		// Update History with info that Bugs are set as Duplicate to this
		if (!updatedBugIds.isEmpty()) {
			mapper.pushHistoryUpdate("Bug(s) "
					+ updatedBugIds + " is (are) set as Duplicate to this Bug.");
			return true;
		} else {
			return false;
		}
	}

	/**
	 * Set the rdf:about for the given {@link EnterpriseChangeRequest}
	 * 
	 * @param ecr
	 * @param workItem
	 */
	public static void setAbout(EnterpriseChangeRequest ecr, WorkItem workItem) {
		// URL format:
		// <server:port>/tfs/services/workitems/<collectionName>/<workItemID>
		// Eg:
		// http://<TFS Adapter address>:9090/tfs/services/workitems/DefaultCollection/177
		try {
			URI uri = new URI(ServletListener.getServicesBase() + "/workitems/"
					+ TFSAdapterManager.getTfsCollection() + "/"
					+ workItem.getID());
			ecr.setAbout(uri);
		} catch (URISyntaxException e) {
			Logger.getLogger(TFSAdapterManager.class).error(
					"Failed to set about URI : " + e.getLocalizedMessage());
		}
	}

	/**
	 * Gets all {@link WorkItem}s for a collection and a project If projectId is
	 * <b>null</b>, it returns all workItems in the collection
	 * 
	 * @param collectionId
	 * @param projectId
	 * @return
	 */
	public static List<WorkItem> getWorkItems(String collectionId,
			String projectId) {
		WorkItemClient workItemClient = TFSConnector.getWorkItemClient();
		String wiqlQuery;
		if (projectId != null) {
			Project project = workItemClient.getProjects().getByID(
					Integer.parseInt(projectId));
			wiqlQuery = "Select ID, Title from WorkItems where (System.AreaPath = '"
					+ project.getName() + "') order by ID";
		} else {
			wiqlQuery = "Select ID, Title from WorkItems order by ID";
		}
		WorkItemCollection workItems = workItemClient.query(wiqlQuery);
		List<WorkItem> workItemList = new ArrayList<>();
		for (int i = 0; i < workItems.size(); i++) {
			// get full workitem to ensure we have access to all fields
			WorkItem workItem = workItems.getWorkItem(i);
			WorkItem wi = workItemClient.getWorkItemByID(workItem.getID());
			workItemList.add(wi);
		}
		return workItemList;
	}

	public static WorkItem getWorkItem(String workItemId) {
		return TFSConnector.getWorkItemClient().getWorkItemByID(
				Integer.parseInt(workItemId));
	}

	private static void save(WorkItem workItem, ValidationMessages messages)
			throws UpdateWorkItemException {
		
		int noOfRevisions = workItem.getRevisions().size();
				
		List<String> validationResult = validate(workItem);
		try {
			workItem.save();
			logger.debug("Saved workitem: " + workItem.getID());
			
		} catch (Exception e) {

			int id = workItem.getID();
			workItem = TFSConnector.getWorkItemClient().getWorkItemByID(id);
			
			// Log the error and finally throw exception
			String message = "Unable to save updates of workitem id="
					+ workItem.getID();
			int statusCode = -1;
			if (e instanceof UnableToSaveException) {
				UnableToSaveException use = (UnableToSaveException) e;
				statusCode = use.getErrorID();
			}
			logger.info("Exception while saving workitem: " + id + " status code: " + statusCode);
			
			// If error on save is due to race condition, i.e. somebody (e.g.
			// the TFS Consumer code invoked on Save of workItem) changed and
			// saved a new revision of the workItem. If so, add info to message.
			// TODO: Could retry command that caused failing save.
			int currentNoOfRevisions = workItem.getRevisions().size();
			if (noOfRevisions != currentNoOfRevisions) {
				statusCode = -2; // TODO: Into constants
				message += "\nWorkitem had rev: " + noOfRevisions +
						" before trying to save, and rev: " + currentNoOfRevisions +
						" after so updated by other source.";
			}
			
			// Add validation details to message
			if (!validationResult.isEmpty()) {
				message += "\n[Details: ";
				for (String entry : validationResult) {
					logger.info("Validation message: " + entry);
					message += entry;
				}
				message += "]";
			}
			String exceptionMessage = e.getMessage();
			message += "\nException message: " + exceptionMessage;
			logger.info(message, null);
			
			// When save fails - always update the ERICSSON_DEFECT_STATE_FIELD
			String trState = getFieldValue(workItem.getFields(),
					TFSMapper.ERICSSON_DEFECT_STATE_FIELD);
			setField(workItem.getFields(),
					TFSMapper.ERICSSON_DEFECT_STATE_FIELD, trState);
			if (TFSMapper.getInstance().updateIfChanged(workItem,
					TFSMapper.ERICSSON_DEFECT_STATE_FIELD, trState, messages)) {
				try {
					workItem.save();
				} catch (Exception e1) {
					statusCode = -3; // TODO: Into constants
					message += "\nStatus save exception: " + e1.getMessage();
				}	
			}					
			
			throw new UpdateWorkItemException(statusCode, message);
		}
	}

	private static void setField(FieldCollection fields, String fieldName,
			String value) {
		logger.debug("Before Set [" + fieldName + " = "
				+ fields.getField(fieldName).getValue());
		fields.getField(fieldName).setValue(value);
		logger.debug("After  Set [" + fieldName + " = "
				+ fields.getField(fieldName).getValue());
	}

	public static Field getField(FieldCollection fields, String fieldName) {
		if (fields.contains(fieldName)) {
			return fields.getField(fieldName);
		}
		return null;
	}

	public static String getFieldValue(FieldCollection fields, String fieldName) {
		Field field = getField(fields, fieldName);
		if (field != null) {
			return field.getValue() + "";
		}
		return "";
	}

	public static List<String> validate(WorkItem workItem) {
		List<String> invalidFields = new ArrayList<>();
		// check the workitem for invalid fields:
		for (Field field : workItem.getFields()) {
			if (field.getStatus() != FieldStatus.VALID) {
				String separator = ", ";
				String message = "Field: " + field.getName();
				String invalidMessage = field.getStatus().getInvalidMessage(
						field);
				Object value = field.getValue();
				if (value == null) {
					value = "(null)";
				}
				invalidFields.add(message + separator + invalidMessage
						+ separator + value);
			}
		}
		return invalidFields;
	}
}
