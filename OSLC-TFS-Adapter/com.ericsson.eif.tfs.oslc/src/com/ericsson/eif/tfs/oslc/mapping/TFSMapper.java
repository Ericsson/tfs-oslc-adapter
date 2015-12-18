package com.ericsson.eif.tfs.oslc.mapping;

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

import java.io.File;
import java.io.FileInputStream;
import java.io.IOException;
import java.net.URI;
import java.net.URISyntaxException;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.Collection;
import java.util.HashMap;
import java.util.Iterator;
import java.util.List;
import java.util.Map;
import java.util.Properties;
import java.util.Map.Entry;
import java.util.Set;
import java.util.regex.Matcher;
import java.util.regex.Pattern;

import org.apache.log4j.Logger;
import org.eclipse.lyo.oslc4j.core.model.Link;

import com.ericsson.eif.tfs.oslc.TFSAdapterManager;
import com.ericsson.eif.tfs.oslc.TFSConnector;
import com.ericsson.eif.tfs.oslc.exception.CreateWorkItemException;
import com.ericsson.eif.tfs.oslc.exception.CustomerMappingExpection;
import com.ericsson.eif.tfs.oslc.exception.ProductMappingError;
import com.ericsson.eif.tfs.oslc.exception.UpdateWorkItemException;
import com.ericsson.eif.tfs.oslc.mapping.BiDirectionalMap.Direction;
import com.ericsson.eif.tfs.oslc.resources.EnterpriseChangeRequest;
import com.ericsson.eif.tfs.oslc.utils.TFSUtilities;
import com.ericsson.eif.tfs.oslc.utils.TfsUserLookup;
import com.ericsson.eif.tfs.oslc.utils.ValidationMessages;
import com.google.common.base.Splitter;
import com.google.common.collect.Lists;
import com.microsoft.tfs.core.clients.workitem.CoreFieldReferenceNames;
import com.microsoft.tfs.core.clients.workitem.WorkItem;
import com.microsoft.tfs.core.clients.workitem.fields.Field;
import com.microsoft.tfs.core.clients.workitem.fields.FieldCollection;
import com.microsoft.tfs.core.clients.workitem.link.Hyperlink;
import com.microsoft.tfs.core.clients.workitem.link.LinkCollection;
import com.microsoft.tfs.core.clients.workitem.link.LinkFactory;
import com.microsoft.tfs.core.clients.workitem.revision.Revision;
import com.microsoft.tfs.core.clients.workitem.revision.RevisionCollection;
import com.microsoft.tfs.core.clients.workitem.revision.RevisionField;

/**
 * Utility to map between TFS Bugs and EnterpriseChangeRequest values or more
 * specifically MHWeb/TR values. Will read some definitions from file, so need
 * the adapter manager (i.e. servlet context) to be initialized before use and
 * the load() method to be called.
 * 
 * @author qfreatt
 * 
 */
public class TFSMapper {

	private static TFSMapper instance;
	static Logger logger = Logger.getLogger(TFSMapper.class);

	private static AttributesMapper mapper;
	
	private static String fieldMappingFile;

	private TFSMapper() {
		// don't allow clients to instantiate
	}

	public synchronized static TFSMapper getInstance() {
		if (instance == null) {
			instance = new TFSMapper();
			mapper = AttributesMapper.getInstance();
		}
		return instance;
	}
	
    public void setTestFieldMappingFile(String file) {
    	fieldMappingFile = file;
    }

	/**
	 * Define field constants - if possible, replace with TFS SDK constants
	 * Fields are named according to the ECM Names or TFS if no ECM name exists
	 * but the field is required by the bug See
	 * Bug-TR mapping.xls
	 */
	// Fields added and needed by the integration - defined in the adapter.properties
	public static String ERICSSON_DEFECT_STATE_FIELD;
	public static String ERICSSON_DEFECT_LINK_FIELD;
	public static String ERICSSON_DEFECT_SYNCSTATE;
	public static String ERICSSON_DEFECT_CREATETR;
	
	// Fields referenced in code - defined in the adapter.properties
	public static String TFS_STATE;
	public static String TFS_SUBSTATE; 
	public static String TFS_FAULTY_PRODUCT; 
	public static String TFS_HISTORY;
	public static String TFS_ISSUE;
	public static String TFS_OWNER;
	public static String TFS_DUPLICATE_ID;
	public static String TFS_TEAM;

    // List values referenced in code - defined in the adapter.properties
    public static String TFS_SUBSTATE_DUPLICATE;
    
    // List values referenced in code - NOT defined in the adapter.properties
    public static String TFS_STATE_ACTIVE = "Active";
    public static String TFS_STATE_RESOLVED = "Resolved";
    
	// Fields referenced only in test code. 
	public final static String TFS_TITLE = CoreFieldReferenceNames.TITLE;
	public final static String TFS_PRIORITY = "Microsoft.VSTS.Common.Priority";
	public final static String TFS_CUSTOMERS_AFFECTED = "Microsoft.VSTS.Common.CustomersAffected";
	public static final String TFS_SEVERITY = "Microsoft.VSTS.Common.Severity";
	public static final String TFS_HOW_FOUND_CATEGORY = "Microsoft.VSTS.Common.HowFoundCategory";
	public static final String TFS_SOURCE = "Microsoft.VSTS.MPT.Source";
	public static final String TFS_CORRECTED_PRODUCT = "Microsoft.VSTS.Common.Release";

	// Not used as regular fields in Bug, but mapped in code to be resolved from TFS fields
	public static final String ERICSSON_DEFECT_USER_SIGNUM = "Ericsson.Defect.User.Signum";
	public static final String ERICSSON_DEFECT_USER_DISPLAYNAME = "Ericsson.Defect.User.DisplayName";
	public static final String ERICSSON_DEFECT_HYPERLINK = "Ericsson.Defect.Creator.DisplayName";

	// ECM Constants
	public static final String ECM_TITLE = "dcterms:title";
	public static final String ECM_IDENTIFIER = "dcterms:identifier";
	public static final String ECM_ABOUT = "dcterms:about";
	public static final String ECM_PRIORITY = "ecm:priority";
	public static final String ECM_STATUS = "oslc_cm:status";
	public static final String ECM_OWNER = "ecm:owner";
	public static final String ECM_COUNTRY = "ecm:country";
	public static final String ECM_CUSTOMER = "ecm:customer";
	public static final String ECM_SITE = "ecm:site";
	public static final String ECM_IMPACT_ON_ISP = "ecm:impactOnISP";
	public static final String ECM_DIDDET = "ecm:diddet";
	public static final String ECM_DESCRIPTION = "dcterms:description";
	public static final String ECM_EXPECTED_IMPACT_ON_ISP = "ecm:expectedImpactOnISP";
	public static final String ECM_FAULT_CODE = "ecm:faultCode";
	public static final String ECM_ACTIVITY = "ecm:activity";
	public static final String ECM_ANSWER = "ecm:answer";
	public static final String ECM_ANSWER_CODE = "ecm:answerCode";
	public static final String ECM_FIRST_TECHNICAL_CONTACT = "ecm:firstTechnicalContact";
	public static final String ECM_FIRST_TECHNICAL_CONTACT_INFO = "ecm:firstTechContactInfo";
	public static final String ECM_NODE_PRODUCT = "ecm:nodeProduct";
	public static final String ECM_NODE_PRODUCT_REVISION = "ecm:nodeProductRevision";
	public static final String ECM_CORRECTED_PRODUCT = "ecm:correctedProduct";
	public static final String ECM_CORRECTED_PRODUCT_REVISION = "ecm:correctedProductRevision";
	public static final String ECM_CORRECTED_NODE_PRODUCT = "ecm:correctedNodeProduct";
	public static final String ECM_CORRECTED_NODE_PRODUCT_REVISION = "ecm:correctedNodeProductRevision";
	public static final String ECM_PRODUCT = "ecm:product";
	public static final String ECM_PRODUCT_REVISION = "ecm:productRevision";
	public static final String ECM_CURRENT_MHO = "ecm:currentMho";
	public static final String ECM_NOTEBOOK = "ecm:notebook";
	public static final String ECM_PROGRESS_INFO = "ecm:progressInfo";
	public static final String ECM_ATTACHMENT = "ecm:attachment";
	public static final String ECM_RELATED_CHANGE_REQUEST = "oslc_cm:relatedChangeRequest";
	public static final String ECM_DUPLICATE_TRS = "ecm:duplicateTRs";

	// Attribute mapping constants
	private static final String ATTRIBUTE_MAPPING_PRODUCT_MAPPING = "ProductMapping";
	private static final String ATTRIBUTE_MAPPING_PRIM_R_STATE = "primRState";

	
	// Fields below are used in code, so to enable user to change mapping without need
	// to change code, we move the definition of the fields to the properties file.
	public void load() {
		Properties properties = new Properties();
		
		if (fieldMappingFile == null) {
			fieldMappingFile = TFSAdapterManager.getFieldMappingFile();
		}
		
		File propertiesFile = new File(fieldMappingFile);
		if (propertiesFile.exists()) {
			try {
				properties.load(new FileInputStream(propertiesFile.toString()));
			} catch (IOException e) {
				logAndExit("Failed to read the adapter.properties file ["
						+ fieldMappingFile + "] - will exit");
				return;
			}
		} else {
			logAndExit("No adapter.properties file found, will exit ["
					+ fieldMappingFile + "]");
			return;
		}		
		
		TFS_STATE = properties.getProperty("tfs_state");
		if (TFS_STATE == null) {
			logAndExit("Missing property 'tfs_state' in adapter.properties. Will exit.");
			return;
		}
		TFS_SUBSTATE = properties.getProperty("tfs_substate");
		if (TFS_SUBSTATE == null) {
			logAndExit("Missing property 'tfs_substate' in adapter.properties. Will exit.");
			return;
		}
		TFS_OWNER = properties.getProperty("tfs_owner");
		if (TFS_OWNER == null) {
			logAndExit("Missing property 'tfs_owner' in adapter.properties. Will exit.");
			return;
		}
		TFS_FAULTY_PRODUCT = properties.getProperty("tfs_faultyProduct");
		if (TFS_FAULTY_PRODUCT == null) {
			logAndExit("Missing property 'tfs_faultyProduct' in adapter.properties. Will exit.");
			return;
		}		
		TFS_HISTORY = properties.getProperty("tfs_history");
		if (TFS_HISTORY == null) {
			logAndExit("Missing property 'tfs_history' in adapter.properties. Will exit.");
			return;
		}	
		TFS_ISSUE = properties.getProperty("tfs_issue"); 
		if (TFS_ISSUE == null) {
			logAndExit("Missing property 'tfs_issue' in adapter.properties. Will exit.");
			return;
		}	
		TFS_DUPLICATE_ID = properties.getProperty("tfs_duplicate"); 
		if (TFS_DUPLICATE_ID == null) {
			logAndExit("Missing property 'tfs_duplicate' in adapter.properties. Will exit.");
			return;
		}	
		TFS_TEAM = properties.getProperty("tfs_team");
		if (TFS_TEAM == null) {
			logAndExit("Missing property 'tfs_team' in adapter.properties. Will exit.");
			return;
		}	
		
		TFS_SUBSTATE_DUPLICATE = properties.getProperty("tfs_substate_duplicate");
		if (TFS_SUBSTATE_DUPLICATE == null) {
			logAndExit("Missing property 'tfs_substate_duplicate' in adapter.properties. Will exit.");
			return;
		}	
		
		ERICSSON_DEFECT_STATE_FIELD = properties.getProperty("tfs_trState");
		if (ERICSSON_DEFECT_STATE_FIELD == null) {
			logAndExit("Missing property 'tfs_trState' in adapter.properties. Will exit.");
			return;
		}	
		ERICSSON_DEFECT_LINK_FIELD = properties.getProperty("tfs_trLink");
		if (ERICSSON_DEFECT_LINK_FIELD == null) {
			logAndExit("Missing property 'tfs_trLink' in adapter.properties. Will exit.");
			return;
		}	
		ERICSSON_DEFECT_SYNCSTATE = properties.getProperty("tfs_trSyncState");
		if (ERICSSON_DEFECT_SYNCSTATE == null) {
			logAndExit("Missing property 'tfs_trSyncState' in adapter.properties. Will exit.");
			return;
		}
		
		ERICSSON_DEFECT_CREATETR = properties.getProperty("tfs_trCreate");
		if (ERICSSON_DEFECT_CREATETR == null) {
			logAndExit("Missing property 'tfs_trCreate' in adapter.properties. Will exit.");
			return;
		}
	}
	
	private void logAndExit(String message) {
		TFSAdapterManager.logAndExit(message);
	}
	
	
	/**
	 * Maps PRIM product to TFS project
	 * 
	 * @param product
	 * @return
	 */
	public String getProject(String product) {
		ProductMapper mapper = ProductMapper.getInstance();
		return mapper.getServiceProvider().getName();
	}

	// Return TR id from link. Assume format of link: <link>/id, e.g.
	// http://<server>/TREditWeb/faces/tredit/tredit.xhtml?eriref=TB65878
	private String getTfsTrLink(String ecrLink) {
		int ix = ecrLink.lastIndexOf("/");
		if (ix == -1 || ix == ecrLink.length()) {
			// ecrLink doesn't end with ID
			return "";
		}
		String id = ecrLink.substring(ix + 1);
		return id; 
	}


	/**
	 * Maps ECR user to a valid TFS value. If no mapped value
	 * found, it can either be a group (valid) or an unknown
	 * user (invalid). In last case, validation will catch
	 * error and report. 
	 * 
	 * @param user
	 * @return
	 */
	public String getTFSUser(String user) {

		// Map signum to "named user"
		TfsUserLookup lookup = new TfsUserLookup(TFSConnector.getTpc());
		String displayName = lookup.getUserName(user);
		
		// If signum update fails, i.e. no user found for the
		// user passed in it can be because it's a group (ok) or
		// it's a non known user - and then validation will fail
		// and error reported back.
		if (displayName == null) {
			return user;
		} else {
			return displayName;
		}
	}

	/**
	 * Updates to the workitem history. To be cleared before update for a ERC
	 * See {@link TFSMapper#clear()}
	 */
	private List<String> historyUpdates = new ArrayList<>();

	/**
	 * Pop an entry from the updates to the History field
	 * 
	 * @return
	 */
	public String popHistoryUpdate() {
		if (historyUpdates.size() > 0) {
			return historyUpdates.remove(0);
		} else {
			return null;
		}
	}
	
	/**
	 * Push an entry from the updates to the History field
	 * 
	 * @return
	 */	
	public void pushHistoryUpdate(String update) {
		historyUpdates.add(update);
	}

	/**
	 * Gets the forward mapped value(s) for the given ecmProperty and assigns it
	 * to the corresponding TFS Field(s).
	 * 
	 * NOTE: Code very similar to the mapFromEcrIfUpdated - could be refactored
	 * 
	 * @param ecmProperty
	 * @param ecrValue
	 * @throws CreateWorkItemException
	 */
	public void mapFromEcr(WorkItem workItem, String ecmProperty,
			EnterpriseChangeRequest ecr, ValidationMessages messages)
			throws CreateWorkItemException, CustomerMappingExpection {
		try {
			
			// Handle mapping of links. Not (yet) covered by mapping
			// configuration file.
			if (ecmProperty.equals(ECM_ATTACHMENT)) {
				updateLinks(ecmProperty, ecr, workItem);
				return;
			}

			// Get the mapping definition for the ecmProperty when
			// mapping in the forward direction (ecm -> workitem).
			// A ecm field can map to multiple workItem fields, hence
			// a collection of properties.			
			Collection<Property> properties = mapper
					.getForwardProperties(ecmProperty);
			if (properties == null) {
				logger.info("No mapping exists for: " + ecmProperty);
				return; // do nothing
			}
			
			// Get the value from the incoming ecr property
			String ecrValue = getEcrValue(ecr, ecmProperty);
			
			for (Property property : properties) {
				
				String constrainedBy = property.getForwardConstrainedBy();
				String constraintValue = null;
				if (constrainedBy != null) {
					constraintValue = getConstraintValue(ecr, workItem,
							property, Direction.FORWARD);
				}
				Collection<String> values = property.getForward(
						ecrValue, constraintValue);
				if (values == null) {
					String value = ecrValue == null? "null" : ecrValue;
					String message = "";
					if (constraintValue != null) {
						message = " constrained by value: " + constraintValue;
					}
					logger.debug("Property: " +  property.getKey() + " with value: " + value +
							message + " did not map to any value for TFS and will be ignored.");
					return;
				}
				
				// Get the name of the TFS field that is mapped
				String fieldName = property.getValue();
				
		        // TODO: For the "calculated" fields we have defined ourselves we know the
		        // dependencies, but should be expressed more elegant than this. Also the
		        // Ericsson.Defect.User.DisplayName is dependent on System.AssignedTo but
		        // will not be updated - we "know" that ...
		        if (fieldName.equals(TFSMapper.ERICSSON_DEFECT_USER_SIGNUM))
		        {
		            fieldName = TFSMapper.TFS_OWNER;			            
		        }
				
				// Handle cases e.g. where > 1 value can be needed
				switch (ecmProperty) {
				case ECM_COUNTRY:
				case ECM_CUSTOMER:
				case ECM_SITE:
					// handle dependencies - must have all to be able to map
					String mappedValue = handleCustomersAffected(
							ecrValue, values, ecr, workItem, property);
					if (mappedValue != null) {
						setField(workItem.getFields(), fieldName,
								mappedValue, messages);
					}	
					return;
					
				default:
					break;
				}
				
				// Get one mapped value - if none, return
				String mappedValue = getMappedValue(values, ecrValue, property);
				if (mappedValue == null) {
					return;
				}
				
				// handle "use" cases	
				String useMapping = property.getUseMapping();
				if (useMapping != null) {
					boolean mapProduct = false;
					switch (useMapping) {
					case ATTRIBUTE_MAPPING_PRODUCT_MAPPING:
						// ignore empty values
						if (mappedValue.equals("")) {
							return;
						}
						boolean isRevision = property.getUseKey().equals(
								ATTRIBUTE_MAPPING_PRIM_R_STATE);
						mapProduct = productMappingFullFilled(isRevision,
								mappedValue);
						break;
					default:
						break;
					}
					if (mapProduct) {
						BackendEntity entity = mapProduct();
						setField(workItem.getFields(), TFS_FAULTY_PRODUCT,
								entity.getName(), messages);
						setField(workItem.getFields(), TFS_TEAM,
								entity.getTeam(), messages);
						return;
					}
				} 
				
				// Handle case of mapping to TFS user
				if (fieldName.equals(TFS_OWNER)) {
					mappedValue = getTFSUser(mappedValue);
				}
				
				setField(workItem.getFields(), fieldName, mappedValue,
							messages);

			}
		} catch (Exception e) {
			// "catch all" to prevent ugly error message being exposed
			// but ensure that they are logged
			String message = e.getMessage();
			logger.debug(message);
			throw new CreateWorkItemException(-1, message);
		}
	}	
	
	/**
	 * Maps the given ecmProperty to its corresponding TFS field(s), setting
	 * them to the mapped value(s) from the given {@link EnterpriseChangeRequest}
	 * 
	 *  NOTE: Code very similar to the mapFromEcr - could be refactored
	 * 
	 * @param workItem
	 * @param ecmProperty ECM property name
	 * @param ecr
	 * @param messages
	 * @return
	 * @throws UpdateWorkItemException
	 */
	public boolean mapFromEcrIfChanged(WorkItem workItem, String ecmProperty,
			EnterpriseChangeRequest ecr, ValidationMessages messages)
			throws UpdateWorkItemException {
		try {
			
			// Handle mapping of links. Not (yet) covered by mapping
			// configuration file.
			if (ecmProperty.equals(ECM_ATTACHMENT)) {
				return updateLinks(ecmProperty, ecr, workItem);
			}
			
			// Get the mapping definition for the ecmProperty for
			// mapping in the forward direction (ecm -> workitem).
			// A ecm field can map to multiple workItem fields, hence
			// a collection of properties.
			Collection<Property> properties = mapper
					.getForwardProperties(ecmProperty);
			if (properties == null) {
				logger.info("No mapping exists for: " + ecmProperty);
				return false; // do nothing
			}
			
			// Get the value from the incoming ecr property
			String ecrValue = getEcrValue(ecr, ecmProperty);
			
			boolean updated = false;
			for (Property property : properties) {
				
				// Get the name of the TFS field that is mapped
				String fieldName = property.getValue();
				
				// Handle mappings to history. Updates to the History field
				// should be saved as separate entries.			
				if (fieldName.equals(TFS_HISTORY)) {
					boolean isAnswer = ecmProperty.equals(ECM_ANSWER);
					String update = filterHistory(workItem, ecrValue, isAnswer);
					if (update != null) {
						pushHistoryUpdate(update);
					} 
					updated |= (update != null);
					continue;
				}
				
				updated |= mapFromEcrIfChangedValues(workItem, ecmProperty,
						ecr, messages, property, ecrValue);
			}
			
			return updated;
				
		} catch (Exception e) {
			// "catch all" to prevent ugly error message being exposed
			// but ensure that they are logged
			String message = e.getMessage();
			logger.debug(message, e);
			throw new UpdateWorkItemException(-1, message);
		}
	}

	/**
	 * Gets the forward mapped value(s) for the given ecmProperty and assigns it
	 * to the corresponding TFS Field(s). 
	 * 
	 * @param workItem
	 * @param ecmProperty
	 * @param ecr
	 * @param messages
	 * @param property
	 * @param ecrValue
	 * @return If the workItem is changed
	 * 
	 * @throws CustomerMappingExpection
	 * @throws ProductMappingError
	 */
	private boolean mapFromEcrIfChangedValues(WorkItem workItem, String ecmProperty,
			EnterpriseChangeRequest ecr, ValidationMessages messages,
			Property property, String ecrValue)
					throws CustomerMappingExpection, ProductMappingError {	
		
		// The mapping property can define that mapping of a value is
		// constrained by another value. If so, get the constraining value
		// and pass in to mapping matching.
		String constrainedBy = property.getForwardConstrainedBy();
		String constraintValue = null;
		if (constrainedBy != null) {
			constraintValue = getConstraintValue(ecr, workItem,
					property, Direction.FORWARD);
		}
		
		// Mapping of a property might result in multiple values in case
		// when we have a n..1 mapping where setting of one TFS field is 
		// determined by the value of multiple ecr values that need to match.
		// E.g. ecm:customer value "GB" maps to "British Telecom" and
		// "ALU-Vodaphone" so need input from ecm:site and ecm:country to
		// determine what of the two that should be the mapped value.		
		Collection<String> mappedValues = property.getForward(ecrValue,
				constraintValue);
		if (mappedValues == null || mappedValues.size() == 0) {
			String value = ecrValue == null? "null" : ecrValue;			
			String message = "";
			if (constraintValue != null) {
				message = " constrained by value: " + constraintValue;
			}
			logger.debug("Property: " +  property.getKey() + " with value: " + value +
					message + " did not map to any value for TFS and will be ignored.");			
			return false;
		}
		
		// Get the name of the TFS field that is mapped
		String fieldName = property.getValue();
		
        // TODO: For the "calculated" fields we have defined ourselves we know the
        // dependencies, but should be expressed more elegant than this. Also the
        // Ericsson.Defect.User.DisplayName is dependent on System.AssignedTo but
        // will not be updated - we "know" that ...
        if (fieldName.equals(TFSMapper.ERICSSON_DEFECT_USER_SIGNUM))
        {
            fieldName = TFSMapper.TFS_OWNER;
        } 
		
		// Handle cases e.g. where > 1 value can be needed
		switch (ecmProperty) {		
		case ECM_COUNTRY:
		case ECM_CUSTOMER:
		case ECM_SITE:
			// handle dependencies - must have all to be able to map
			String mappedValue = handleCustomersAffected(
					ecrValue, mappedValues, ecr, workItem, property);
			if (mappedValue == null) {
				return false;
			}	
			return updateIfChanged(workItem, fieldName,
					mappedValue, messages);
		default:
			break;
		}
		
		// Get one mapped value - if none, return
		String mappedValue = getMappedValue(mappedValues, ecrValue, property);
		if (mappedValue == null) {
			return false;
		}	
			
		// handle "use" cases
        String useMapping = property.getUseMapping();
		if (useMapping != null) {
			boolean mapProduct = false;	
			switch (useMapping) {
			case ATTRIBUTE_MAPPING_PRODUCT_MAPPING:
				// ignore empty values
				if (mappedValue.equals("")) {
					return false;
				}
				boolean isRevision = property.getUseKey().equals(
						ATTRIBUTE_MAPPING_PRIM_R_STATE);
				mapProduct = productMappingFullFilled(isRevision,
						mappedValue);
				break;
			default:
				break;
			}
			if (mapProduct) {
				BackendEntity entity = mapProduct();
				return updateIfChanged(workItem, fieldName,
						entity.getName(), messages);
			}
			return false;
		}
		
		// handle case of mapping to TFS user
		if (fieldName.equals(TFS_OWNER)) {
			mappedValue = getTFSUser(mappedValue);
		}
		
		// If the TFS field has a value that when mapped to ecrValue
		// (inverse mapping) will be same as when forward mapping of
		// the ecrValue to a TFS value, we should not change the
		// current TFS value.
		String fieldValue = getFieldValue(workItem.getFields(),
				fieldName);
		Collection<String> invValues = property.getInverse(
				fieldValue, constraintValue);
		if (invValues != null && !invValues.isEmpty()) {
			String invValue = invValues.iterator().next();
			if (invValue.equals(ecrValue)) {
				// don't change
				return false;
			}
		}
		
		// Kludge to handle incorrect MHWeb behavior. If a value is not 
		// set it should not be sent -> ecrValue == null. But MHWeb send
		// empty value e.g. <ecm:faultCode></ecm:faultCode> in rdf so we
		// are lead to be believe user has set to "" - not true. And in
		// case of TFS_ISSUE we need a value, so can't omit the default
		// mapping value. Hence hardcode ignore of "" value.
		if (fieldName.equals(TFSMapper.TFS_ISSUE)) {
			if (ecrValue.isEmpty()) {
				return false;
			}
		}

		return updateIfChanged(workItem, fieldName,
				mappedValue, messages);
	}
	
	private String getMappedValue(Collection<String> mappedValues,
			String ecrValue, Property property) {
		// Unexpected to have > 1 value, if so - log and get only 1:st
		if (mappedValues.size() > 1) {
			String value = ecrValue == null? "null" : ecrValue;
			logger.warn("Property: " +  property + " with value: " + value +
					" mapped to > 1 values for TFS. Only 1:st will be used.");
		}
		
		String mappedValue = null;
		if (mappedValues.size() > 0) {
			mappedValue = mappedValues.iterator().next();
		}
		
		// handle null value
		if (mappedValue == null) {
			String value = ecrValue == null? "null" : ecrValue;
			logger.debug("Property: " +  property + " with value: " + value +
					" mapped to null value for TFS and will be ignored.");
			return null;
		}
		
		return mappedValue;
	}
	
	/**
	 * Filters for:<br>
	 * - [TFS yyyy-mm-dd hh:mm:ss] text<br>
	 * - [MH yyyy-mm-dd hh:mm:ss] text<br>
	 * - "answer" as indicated by <b>isAnswer</b><br>
	 * <br>
	 * 1) If input begins with the "TFS" pattern -> drop it (return null), but
	 * allow "TFS" pattern within an input<br> 
	 * 2) If input isAnswer, search history and reject if matching entry exists<br>
	 * 3) If input begins with the "MH" pattern -> scan history for an entry
	 * matching the MH time stamp and if not found, accept it (return it as is).
	 * 
	 * @param workItem
	 * @param input
	 * @param isAnswer
	 *            indicates that this is an ecm:answer
	 * @return
	 */
	public String filterHistory(WorkItem workItem, String input,
			boolean isAnswer) {
		if (input == null || input.isEmpty()) {
			return null;
		}

		// Matching "[TFS yyyy-mm-dd hh:mm:ss] Any message"
		String tfsRegexp = "^\\[TFS (\\d{4}-\\d{2}-\\d{2} \\d{2}:\\d{2}:\\d{2}]).*";
		if (Pattern.matches(tfsRegexp, input)) {
			return null;
		}
		
		// Note: Also ecm:answer should have timestamp i.e. [MH ...]. Could then
		// remove special handling of Answer - would be general MH case. But hard
		// to implement according to MHWeb folks. 
		if (isAnswer) {
			return doFilterHistory(workItem, input);
		}		

		// Matching "[MH yyyy-mm-dd hh:mm:ss] Any message" as using find()
		String mhRegexp = "^\\[MH (\\d{4}-\\d{2}-\\d{2} \\d{2}:\\d{2}:\\d{2}])";
		Pattern pattern = Pattern.compile(mhRegexp);
		Matcher m = pattern.matcher(input);
		if (m.find()) {
			String match = m.group();
			if (historyEntryMatchingStart(workItem, match)) {
				return null;
			}
		}

		// neither [TFS...] nor [MH...]
		return input;
	}

	/**
	 * Scan History to see if an entry starting with string tag is present. If
	 * so, return true.
	 * 
	 * @param workItem
	 * @param tag
	 * @return
	 */
	private boolean historyEntryMatchingStart(WorkItem workItem, String tag) {
		// scan history in reverse order, to match most recent entries first
		RevisionCollection revisions = workItem.getRevisions();
		for (int j = revisions.size() - 1; j >= 0; j--) {

			Revision rev = revisions.get(j);
			RevisionField historyField = rev.getField(TFS_HISTORY);
			if (historyField != null) {
				Object rValue = historyField.getValue();
				if (rValue != null && rValue.toString().startsWith(tag)) {
					// found matching history entry
					return true;
				}
			}
		}
		return false;
	}

	/**
	 * Scan History to see if an entry matching input is found. If so return
	 * input stripped from any trailing newlines.
	 * 
	 * @param workItem
	 * @param tag
	 * @return
	 */
	private String doFilterHistory(WorkItem workItem, String input) {
		// remove any trailing newlines (\n) from the input, since TFS
		// strips them when writing to the System.History
		while (input.endsWith("\n")) {
			input = input.substring(0, input.lastIndexOf("\n"));
		}
		// scan history in reverse order, to match most recent entries first
		RevisionCollection revisions = workItem.getRevisions();
		for (int j = revisions.size() - 1; j >= 0; j--) {

			Revision rev = revisions.get(j);
			RevisionField historyField = rev.getField(TFS_HISTORY);
			if (historyField != null) {
				Object rValue = historyField.getValue();
				if (rValue != null && rValue.equals(input)) {
					// found matching history entry
					return null;
				}
			}
		}
		return input;
	}

	public static String filterDescription(String description) {
		if (description == null || description.startsWith("[TFS ")) {
			return null;
		}
		// Convert newline char to <div></div> tags to preserve newlines
		// when updating the description
		String[] splitted = description.split("\\n");
		if (splitted.length > 1) {
			String newDescription = "";
			for (int i = 0; i < splitted.length; i++) {
				newDescription += "<div>";
				newDescription += splitted[i];
				newDescription += "</div>";
			}
			return newDescription;
		}

		return description;
	}

	/**
	 * Updates the value of the field if the newValue is non null and differs
	 * from the oldValue
	 * 
	 * @param fields
	 * @param field
	 * @param newValue
	 */
	public boolean updateIfChanged(WorkItem workItem, String field,
			String newValue, ValidationMessages messages) {
		FieldCollection fields = workItem.getFields();
		if (newValue == null) {
			// don't change when no value passed in with the ECR
			return false;
		}
		Object oldValue = fields.getField(field).getValue();
		boolean setValue = false;
		if (oldValue == null) {
			if (newValue.isEmpty()) {
				// treat empty and null equally
				return false;
			}
			setValue = true;
		} else {
			String oldValueStr = fields.getField(field).getValue().toString();
			if (oldValueStr == null || !oldValueStr.equals(newValue)) {
				setValue = true;
			}
		}
		if (setValue) {
			setField(fields, field, newValue, messages);
			return true;
		}
		return false;
	}

	/**
	 * Set field named fieldName to value. If value is null, nothing is changed
	 * 
	 * @param fields
	 * @param fieldName
	 * @param value
	 */
	private static void setField(FieldCollection fields, String fieldName,
			String value, ValidationMessages messages) {
		if (value == null) {
			// don't update
			return;
		}
		Field field = fields.getField(fieldName);
		if (field.isEditable()) {
			logger.debug("Before Set [" + fieldName + " = " + field.getValue()
					+ "]");
			field.setValue(value);
			logger.debug("After  Set [" + fieldName + " = " + field.getValue()
					+ "]");
		} else {
			String message = "Field: " + fieldName
					+ " not editable. Skipping writing value: " + value;
			logger.debug(message);
			messages.addMessage(message);
		}
	}

	private static Field getField(FieldCollection fields, String fieldName) {
		if (fields.contains(fieldName)) {
			return fields.getField(fieldName);
		}
		return null;
	}

	/**
	 * Answers the field value as {@link String} or <b>null</b>
	 * 
	 * @param fields
	 * @param fieldName
	 * @return
	 */
	public static String getFieldValue(FieldCollection fields, String fieldName) {
		Field field = getField(fields, fieldName);
		if (field != null) {
			Object value = field.getValue();
			return value == null ? null : value + "";
		}
		return null;
	}

	/**
	 * A cache of mapped constraints - note: it must be cleared before
	 * processing a new incoming or outgoing ECR. See {@link TFSMapper#clear()}
	 */
	private Map<String, String> constraintCache = new HashMap<>();

	/**
	 * Gets the constraintValue for the given {@link Property}<br>
	 * E.g. If constrainedBy="System.State", then this will use the mapping to
	 * retrieve the corresponding ECM-key, e.g. "oslc_cm:status". Then using this
	 * key, it will map get the ECR-value from the given
	 * {@link EnterpriseChangeRequest} to get its mapped value and return it. As
	 * a side effect, the mapping will be cached to speed up next time we hit
	 * this.
	 * 
	 * @param ecr
	 * @param property
	 * @return
	 */
	private String getConstraintValue(EnterpriseChangeRequest ecr,
			WorkItem workItem, Property property, Direction direction) {
		String constrainedBy;
		if (direction == Direction.FORWARD) {
			constrainedBy = property.getForwardConstrainedBy();
		} else {
			constrainedBy = property.getInverseConstrainedBy();
		}
		String constraintValue = null;
		if (constrainedBy == null) {
			return null;
		}

		// try to get from constraint cache
		if (constraintCache.containsKey(constrainedBy)) {
			return constraintCache.get(constrainedBy);
		}

		Iterator<Entry<String, Property>> iterator;
		if (direction == Direction.FORWARD) {
			iterator = mapper.getForwardIterator();
		} else {
			iterator = mapper.getInverseIterator();
		}
		while (iterator.hasNext()) {
			Entry<String, Property> entry = iterator.next();
			Property constrainedByProperty = entry.getValue();
			if (constrainedByProperty.getValue().equals(constrainedBy)) {
				String ecmKey = constrainedByProperty.getKey();
				Collection<String> constraintValues;
				if (direction == Direction.FORWARD) {
					constraintValues = constrainedByProperty
							.getForward(getEcrValue(ecr, ecmKey));
					if (constraintValues == null) {
						return null;
					}
					constraintValue = constraintValues.iterator().next(); // only
																			// first
				} else {
					constraintValue = getFieldValue(workItem.getFields(),
							constrainedBy);
				}
				constraintCache.put(constrainedBy, constraintValue);
				break;
			}
		}
		return constraintValue;
	}

	// TODO: Should be generalized to handle all "depends on" mappings
	/**
	 * Cache for the mapping of ECM_COUNTRY+ECM_CUSTOMER+ECM_SITE to
	 * TFS_CUSTOMERS_AFFECTED
	 */
	Map<String, Collection<String>> dependencyMap = new HashMap<>();

	/**
	 * Clears constraints and dependency mapping - call before mapping a new ECR
	 * to TFS
	 */
	public void clear() {
		constraintCache.clear();
		historyUpdates.clear();
		clearDependencies();
	}

	private void clearDependencies() {
		dependencyMap.clear();
	}

	/**
	 * Mapping of TFS field customersAffected to ecm fields for
	 * country, customer and site. 
	 * 
	 * TODO: Should be generalized to be able to be driven completely
	 * by the configuration file, and no hardcoded dependencies.
	 * 
	 * @param ecmProperty
	 * @param ecr
	 * @param workItem
	 * @param messages
	 * @param property
	 * @return
	 * @throws CustomerMappingExpection
	 */
	private String handleCustomersAffected(String ecrValue,
			Collection<String> mappedValues,
			EnterpriseChangeRequest ecr, WorkItem workItem,
			Property property) throws CustomerMappingExpection {

		String fieldName = property.getValue();
		
		// handle dependencies - must have all to be able to map
		if (!dependenciesFullFilled(property, mappedValues)) {
			return null;
		}
		
		Collection<String> mappedCustomers = mapCustomer(ecrValue);
		// Mapping missing?
		if (mappedCustomers.isEmpty()) {
			// get all 3 bits:
			String message = "Customer mapping missing for: [";
			message += ECM_COUNTRY + ": "
					+ getEcrValue(ecr, ECM_COUNTRY) + ", ";
			message += ECM_CUSTOMER + ": "
					+ getEcrValue(ecr, ECM_CUSTOMER) + ", ";
			message += ECM_SITE + ": "
					+ getEcrValue(ecr, ECM_SITE) + "]";
			logger.info(message);
			throw new CustomerMappingExpection(message);
		}
		
		// Customer mapping MUST be unique - will use first
		// value if more than one match
		String mappedCustomer = mappedCustomers.iterator().next();
		
		// Format:
		//    single entry: [value0]
		//    multiple entries: [value0];[value1];
		Field field = getField(workItem.getFields(), fieldName);
		if (field == null) {
			return null;
		}
		
		String value = (String) field.getValue();
		if (value == null) {
			if (mappedCustomer.isEmpty()) {
				return null; // No old value and no new
			}
			value = "[" + mappedCustomer + "]";
		} else if (value.contains(";")) {
			value += ";[" + mappedCustomer + "]";
		} else {
			value = "[" + mappedCustomer + "]";
		}
		return value;
	}	
	
	private boolean dependenciesFullFilled(Property property, Collection<String> values)
			throws CustomerMappingExpection {
		boolean fullFilled = true;
		for (String dependency : property.getDependencies()) {
			if (!dependencyMap.containsKey(dependency)) {
				// Missing dependency - add property and return
				fullFilled = false;
			}
		}
		String key = property.getKey();
		dependencyMap.put(key, values);
		return fullFilled;
	}

	private Collection<String> mapCustomer(String ecrValue)
			throws CustomerMappingExpection {
		Collection<String> intersection = new ArrayList<>();
		boolean firstTime = true;
		Set<String> keys = dependencyMap.keySet();
		for (String key : keys) {
			Collection<String> mappedValues = dependencyMap.get(key);
			if (mappedValues == null) {
				throw new CustomerMappingExpection("Null mapping for: "
						+ ecrValue);
			}
			if (firstTime) {
				intersection.addAll(mappedValues);
				firstTime = false;
			} else {
				intersection.retainAll(mappedValues);
			}
		}
		clearDependencies();
		return intersection;
	}

	private String product;
	private String productRevision;

	private boolean productMappingFullFilled(boolean isRevision, String value) {
		if (isRevision) {
			productRevision = value;
		} else {
			product = value;
		}
		return product != null && productRevision != null;
	}

	private BackendEntity mapProduct() throws ProductMappingError {
		PrimProduct primProduct = new PrimProduct(product, productRevision);
		clearProductMapping();
		BackendEntity entity = ProductMapper.getInstance().getEntity(
				primProduct);
		if (entity == null) {
			throw new ProductMappingError(primProduct);
		}
		return entity;
	}

	private void clearProductMapping() {
		product = null;
		productRevision = null;
	}

	/**
	 * Updates the {@link Hyperlink}s of the given {@link WorkItem} with the
	 * {@link Link}s (ECM_ATTACHMENT) from the given
	 * {@link EnterpriseChangeRequest}. This will add/remove {@link Hyperlink}s
	 * in the {@link WorkItem}
	 * 
	 * @param ecmProperty
	 * @param ecr
	 * @param workItem
	 * @return
	 */
	private boolean updateLinks(String ecmProperty,
			EnterpriseChangeRequest ecr, WorkItem workItem) {
		List<Link> attachments = Arrays.asList(ecr.getAttachments());
		LinkCollection links = workItem.getLinks();

		List<Link> links2add = new ArrayList<>(); // Arrays.asList(attachments));
		List<com.microsoft.tfs.core.clients.workitem.link.Link> links2remove = new ArrayList<>();

		// find which attachments needs to be added to Hyperlinks
		for (Link attachment : attachments) {
			boolean add = true;
			String attachmentName = attachment.getValue().toString();
			for (com.microsoft.tfs.core.clients.workitem.link.Link link : links) {				
				if (link instanceof Hyperlink) {
					Hyperlink hyperlink = (Hyperlink) link;
					if (hyperlink.getLocation().equals(attachmentName)) {
						add = false;
						break;					
					}
				}
			}
			if (add) {
				links2add.add(attachment);
			}
		}
		// find which links needs to be removed from Hyperlinks
		for (com.microsoft.tfs.core.clients.workitem.link.Link link : links) {
			if (link instanceof Hyperlink) {
				boolean remove = true;
				String linkName = ((Hyperlink) link).getLocation();
				for (Link attachment : attachments) {
					if (linkName.equals(attachment.getValue().toString())) {
						remove = false;
						break;
					}
				}
				if (remove) {
					links2remove.add(link);
				}				
			}
		}

		// remove links
		Iterator<com.microsoft.tfs.core.clients.workitem.link.Link> iterator = workItem
				.getLinks().iterator();
		while (iterator.hasNext()) {
			com.microsoft.tfs.core.clients.workitem.link.Link link = iterator
					.next();
			if (links2remove.contains(link)) {
				workItem.getLinks().remove(link);
			}
		}

		for (Link link : links2add) {
			Hyperlink newLink = LinkFactory.newHyperlink(link.getValue()
					.toString(), link.getLabel(), false);
			workItem.getLinks().add(newLink);
		}
		return links2add.size() > 0 || links2remove.size() > 0;
	}

	/**
	 * Special mapping of ECM_RELATED_CHANGE_REQUEST to Ericsson.Defect.Link
	 * 
	 * @param ecmProperty
	 * @param ecr
	 * @param fields
	 */
	public void mapFromEcrLink(String ecmProperty, EnterpriseChangeRequest ecr,
			FieldCollection fields, ValidationMessages messages) {
		Link[] related = ecr.getRelatedChangeRequests();
		if (related == null || related.length == 0) {
			messages.addMessage("Missing the related change request link refering to the ECR.");
			return;
		} if (related.length > 1) {
			messages.addMessage("More than 1 related change request link, only the first will be used.");
		}
		
		// only process first entry
		Link ecrLink = related[0];
		String value = getTfsTrLink(ecrLink.getValue().toString());
		setField(fields, ERICSSON_DEFECT_LINK_FIELD, value, messages);
	}

	/**
	 * Check if the incoming MHO matches the MHO defined for the workitem.
	 * If not we should disconnect the workitem so it does not provide
	 * updates to a "foreign" TR.
	 * Note: We do not have MHO as field on the workitem, but will get
	 * the default value assigned in the attribute mapping.
	 * 
	 * @param workItem
	 * @param ecr
	 * @param messages
	 * @return
	 */
	public boolean shouldDisconnect(WorkItem workItem, EnterpriseChangeRequest ecr,
			ValidationMessages messages) {
		
		// Create a ecr for the mapping
		EnterpriseChangeRequest newEcr = null;
		try {
			newEcr = new EnterpriseChangeRequest();
		} catch (URISyntaxException e) {
			logger.error("Failed to create a dummy ecr. Should not happen.", e);
		}
		List<String> currentMhos = mapToEcr(newEcr, TFSMapper.ECM_CURRENT_MHO, workItem);
		String currentMho = "";
		if (currentMhos == null || currentMhos.size() == 0 ||
				currentMhos.get(0) == null || currentMhos.get(0).isEmpty()) {
			// Incorrect mapping - we should have a MHO defined.
			messages.addMessage("Missing MHO for workitem.");
			logger.error("Workitem: '" + workItem.getID() + "' has no MHO defined.");
			return false;
		} if (currentMhos.size() > 1) {
			// Incorrect mapping - we should have one MHO defined.
			logger.info("Workitem: '" + workItem.getID() + "' has > 1 MHO defined. Unexpected");	
		} 
		currentMho = currentMhos.get(0);
		
		String incomingMho = ecr.getCurrentMho();
		if (incomingMho == null || incomingMho.isEmpty()) {
			// OK case for some states. Log but no message.
			logger.info("Incoming ecr named: '" + ecr.getTitle() + "' has no MHO defined.");
			return false;
		}
		
		// Return if incoming and current MHO match
		if (currentMho.equalsIgnoreCase(incomingMho)) {
			return false;
		}
		
		// Mismatch - we should disconnect
		String trId = getFieldValue(workItem.getFields(),
				ERICSSON_DEFECT_LINK_FIELD); 
		String message = "Incoming ecr named: '" + ecr.getTitle() + "' has MHO: " +
				incomingMho + " and connected Bug with id: " + workItem.getID() + 
				" has MHO: " + currentMho + ". Will disconnect TR: " + trId;
		logger.info(message);
		messages.addMessage(message);	
		
		// Clear the fields and provide message in History
		FieldCollection fields = workItem.getFields();		
		setField(fields, ERICSSON_DEFECT_STATE_FIELD, "", messages);
		setField(fields, ERICSSON_DEFECT_LINK_FIELD, "", messages);
		setField(fields, ERICSSON_DEFECT_SYNCSTATE, "", messages);
		
		setField(fields, TFS_HISTORY, message, messages);

		return true;
	}
	
	/**
	 * In MHWeb, when a TR A is marked as Duplicate to a TR B - the TR A is
	 * marked as Duplicate to TR B, and only possible change is to Unduplicate.
	 * The TR B is set a "Primary TR" and has a link to TR A. A TR can be
	 * primary to > 1, so can be a list of TRs in a String. Format: TR1;TR2 ...
	 * 
	 * We will get update for TR B, so need to update the Bug(s) associated with
	 * the TRs listed as Duplicated unless they already are marked as Duplicate.
	 * 
	 * @param ecr
	 * @param workItem
	 * @param workItemId
	 * @return
	 */
	public List<WorkItem> updateDuplicate(EnterpriseChangeRequest ecr,
			WorkItem workItem, String workItemId, ValidationMessages messages) {

		List<WorkItem> updated = new ArrayList<WorkItem>();

		String duplicateTRs = ecr.getDuplicateTRs();
		if (duplicateTRs == null || duplicateTRs.isEmpty()) {
			return updated;
		}

		// Unparse list of TRs separated with ";"
		String[] duplicateTRList = duplicateTRs.split(";");
		for (int i = 0; i < duplicateTRList.length; i++) {
			String duplicateTR = duplicateTRList[i];

			WorkItem connectedWi = TFSUtilities.getBugForTR(
					TFSConnector.getWorkItemClient(), duplicateTR);
			if (connectedWi == null) {
				continue;
			}

			// Open to get all fields for workitem
			connectedWi.open();

			// If already set to Duplicate we can ignore this
			Object subState = connectedWi.getFields().getField(TFS_SUBSTATE)
					.getValue();
			if (subState != null
					&& subState.toString().equalsIgnoreCase(TFS_SUBSTATE_DUPLICATE)) {
				continue;
			}

			// Set workitem to Duplicate. When Bug and TR are in sync, the TR
			// that is Duplicate is in a state mapping to Active. And we "know"
			// that the "Duplicate" substate for the Bug only is available when
			// Bug is Resolved or Closed. So if Bug in state Active, try to move
			// to Resolved. If this fail due to other conditions - so be it.
			
			// TODO: Can be done more elegant by introduce an action in the bug.xml
			// so we can check for getNextState("Disconnect"), Then not dependent on
			// knowing state names.
			
			Field stateField = connectedWi.getFields().getField(
					TFSMapper.TFS_STATE);
			String state = stateField.getValue().toString();
			if (state.equalsIgnoreCase(TFS_STATE_ACTIVE)) {
				setField(connectedWi.getFields(), TFS_STATE, TFS_STATE_RESOLVED,
						messages);
			}
			setField(connectedWi.getFields(), TFS_SUBSTATE, TFS_SUBSTATE_DUPLICATE,
					messages);
			setField(connectedWi.getFields(), TFS_DUPLICATE_ID, workItemId,
					messages);

			updated.add(connectedWi);
		}

		return updated;
	}

	/**
	 * Validates the given property - against a set of hard coded rules
	 * 
	 * @param workItem
	 * @param ecmProperty
	 * @param ecr
	 * @throws UpdateWorkItemException
	 */
	public void validate(WorkItem workItem, String ecmProperty,
			EnterpriseChangeRequest ecr) throws UpdateWorkItemException {
		
		switch (ecmProperty) {
		
		case ECM_RELATED_CHANGE_REQUEST:
			
			// The TR Link of the Bug
			String trId = getFieldValue(workItem.getFields(),
					ERICSSON_DEFECT_LINK_FIELD);

			// The incoming related TR
			String relatedLink = getEcrValue(ecr, ECM_RELATED_CHANGE_REQUEST);
			String relatedId = relatedLink != null?
					relatedLink.substring(relatedLink .lastIndexOf("/") + 1) :
					null;
			
			// The trId *should* be set as doing an update
			// The relatedId *should* always be set 
			if (trId == null || trId.isEmpty() ||
					relatedId == null || relatedId.isEmpty() ||
					!trId.equals(relatedId)) {
				
				String message = "Attempt to update Bug with id="
						+ workItem.getID()
						+ " with content from TR with id="
						+ (relatedId == null? "null" : relatedId)
						+ " prevented, sinces the Bug has the related change request (TR): "
						+ (trId == null? "null" : trId);
				throw new UpdateWorkItemException(403, message);
			}

		default:
			break;
		}
	}
	
	/**
	 * Gets the ECR value for the given ecmProperty
	 * 
	 * @param ecr
	 * @param ecmProperty
	 * @return
	 */
	private static String getEcrValue(EnterpriseChangeRequest ecr,
			String ecmProperty) {
		switch (ecmProperty) {
		case ECM_TITLE:
			return ecr.getTitle().trim();
		case ECM_IDENTIFIER:
			return ecr.getIdentifier();
		case ECM_PRIORITY:
			return ecr.getPriority();
		case ECM_OWNER:
			return ecr.getOwner();
		case ECM_COUNTRY:
			return ecr.getCountry();
		case ECM_CUSTOMER:
			return ecr.getCustomer();
		case ECM_SITE:
			return ecr.getSite();
		case ECM_DIDDET:
			return ecr.getDiddet();
		case ECM_DESCRIPTION:
			return filterDescription(ecr.getDescription());
		case ECM_ANSWER:
			return ecr.getAnswer();
		case ECM_ANSWER_CODE:
			return ecr.getAnswerCode();
		case ECM_FAULT_CODE:
			return ecr.getFaultCode();
		case ECM_ACTIVITY:
			return ecr.getActivity();
		case ECM_FIRST_TECHNICAL_CONTACT:
			return ecr.getFirstTechnicalContact();
		case ECM_FIRST_TECHNICAL_CONTACT_INFO:
			return ecr.getFirstTechContactInfo();
		case ECM_STATUS:
			return ecr.getStatus();
		case ECM_CURRENT_MHO:
			return ecr.getCurrentMho();
		case ECM_PRODUCT:
			return ecr.getProduct();
		case ECM_PRODUCT_REVISION:
			return ecr.getProductRevision();
		case ECM_NODE_PRODUCT:
			return ecr.getNodeProduct();
		case ECM_NODE_PRODUCT_REVISION:
			return ecr.getNodeProductRevision();
		case ECM_CORRECTED_PRODUCT:
			return ecr.getCorrectedProduct();
		case ECM_CORRECTED_PRODUCT_REVISION:
			return ecr.getCorrectedProductRevision();
		case ECM_CORRECTED_NODE_PRODUCT:
			return ecr.getCorrectedNodeProduct();
		case ECM_CORRECTED_NODE_PRODUCT_REVISION:
			return ecr.getCorrectedNodeProductRevision();
		case ECM_ATTACHMENT:
			return null; // TODO
		case ECM_NOTEBOOK:
			return ecr.getNotebook();
		case ECM_PROGRESS_INFO:
			return ecr.getProgressInfo();
		case ECM_EXPECTED_IMPACT_ON_ISP:
			return ecr.getExpectedImpactOnISP();
		case "ecm:impactOnISP":
			return ecr.getImpactOnISP();
		case ECM_RELATED_CHANGE_REQUEST:
			Link[] related = ecr.getRelatedChangeRequests();
			if (related.length == 0) {
				return null;
			}
			return related[0].getValue().toString();
		default:
			break;
		}
		return "";
	}
	
	// =========================================================
	// Handle request to create a ECR from workItem (for REST GET call)
	// TODO: Complete implementation. Code can handle request, but more
	// needs to be done to provide a correct ER based on the workItem
	
	public void setEcrValues(EnterpriseChangeRequest ecr, String ecmProperty,
			WorkItem workItem) {
		setEcrValue(ecr, ecmProperty, mapToEcr(ecr, ecmProperty, workItem));
	}

	public void setEcrValue(EnterpriseChangeRequest ecr, String ecmProperty,
			List<String> values) {
		if (values == null) {
			return;
		}
		for (String value : values) {
			switch (ecmProperty) {
			case ECM_ABOUT:
				try {
					ecr.setAbout(new URI(value));
				} catch (URISyntaxException e) {
					logger.debug("Failed to setAbout on: " + ecr + " to: "
							+ value);
				}
				break;
			case ECM_TITLE:
				ecr.setTitle(value);
				break;
			case ECM_IDENTIFIER:
				ecr.setIdentifier(value);
				break;
			case ECM_PRIORITY:
				ecr.setPriority(value);
				break;
			case ECM_OWNER:
				ecr.setOwner(value);
				break;
			case ECM_COUNTRY:
				ecr.setCountry(value);
				break;
			case ECM_CUSTOMER:
				ecr.setCustomer(value);
				break;
			case ECM_SITE:
				ecr.setSite(value);
				break;
			case ECM_DIDDET:
				ecr.setDiddet(value);
				break;
			case ECM_DESCRIPTION:
				ecr.setDescription(value);
				break;
			case ECM_ANSWER:
				ecr.setAnswer(value);
				break;
			case ECM_ANSWER_CODE:
				ecr.setAnswerCode(value);
				break;
			case ECM_FAULT_CODE:
				ecr.setFaultCode(value);
				break;
			case ECM_ACTIVITY:
				ecr.setActivity(value);
				break;
			case ECM_FIRST_TECHNICAL_CONTACT:
				ecr.setFirstTechnicalContact(value);
				break;
			case ECM_FIRST_TECHNICAL_CONTACT_INFO:
				ecr.setFirstTechContactInfo(value);
				break;
			case ECM_STATUS:
				ecr.setStatus(value);
				break;
			case ECM_CURRENT_MHO:
				ecr.setCurrentMho(value);
				break;
			case ECM_PRODUCT:
				ecr.setProduct(value);
				break;
			case ECM_PRODUCT_REVISION:
				ecr.setProductRevision(value);
				break;
			case ECM_NODE_PRODUCT:
				ecr.setNodeProduct(value);
				break;
			case ECM_NODE_PRODUCT_REVISION:
				ecr.setNodeProductRevision(value);
				break;
			case ECM_CORRECTED_PRODUCT:
				ecr.setCorrectedProduct(value);
				break;
			case ECM_CORRECTED_PRODUCT_REVISION:
				ecr.setCorrectedProductRevision(value);
				break;
			case ECM_CORRECTED_NODE_PRODUCT:
				ecr.setCorrectedNodeProduct(value);
				break;
			case ECM_CORRECTED_NODE_PRODUCT_REVISION:
				ecr.setCorrectedNodeProductRevision(value);
				break;
			case ECM_ATTACHMENT:
				setAttachment(ecr, values);
				break;
			case ECM_NOTEBOOK:
				ecr.setNotebook(value);
				break;
			case ECM_PROGRESS_INFO:
				ecr.setProgressInfo(value);
				break;
			case ECM_EXPECTED_IMPACT_ON_ISP:
				ecr.setExpectedImpactOnISP(value);
				break;
			case ECM_DUPLICATE_TRS:
				ecr.setDuplicateTRs(value);
				break;
			case ECM_RELATED_CHANGE_REQUEST:
				URI resource;
				try {
					resource = new URI(value);
					String label = value.substring(value.lastIndexOf("/") + 1);
					Link relatedChangeRequest = new Link(resource, label);
					ecr.addRelatedChangeRequest(relatedChangeRequest);
				} catch (URISyntaxException e) {
					logger.debug("Failed to set: " + ecmProperty + " to: "
							+ value);
				}
				break;
			default:
				break;
			}
		}
	}	
	
	/**
	 * Returns the inverse mapped value(s) for the given ecmProperty and the
	 * corresponding TFS field(s)
	 * 
	 * @param ecmProperty
	 * @param fields
	 * @return
	 */
	private List<String> mapToEcr(EnterpriseChangeRequest ecr,
			String ecmProperty, WorkItem workItem) {
		FieldCollection fields = workItem.getFields();

		// handle special values (not mapped)
		switch (ecmProperty) {
		case ECM_RELATED_CHANGE_REQUEST:
			String trId = getFieldValue(fields,
					ERICSSON_DEFECT_LINK_FIELD);
			String relatedLink = getCrLink(trId);
			return Arrays.asList(relatedLink);
		case ECM_OWNER:
			String displayName = getFieldValue(fields, TFS_OWNER);
			TfsUserLookup lookup = new TfsUserLookup(TFSConnector.getTpc());
			String signum = lookup.getSignum(displayName);
			return Arrays.asList(signum);
		default:
			break;
		}
		List<String> mappedValues = new ArrayList<>();
		Collection<Property> properties = mapper
				.getForwardProperties(ecmProperty);
		if (properties == null) {
			logger.debug("No mapping TFS->ECR for: " + ecmProperty);
			return null; // no mapping
		}
		for (Property property : properties) {
			String fieldName = property.getValue();
			String fieldValue = getFieldValue(fields, fieldName);
			List<String> list2 = new ArrayList<>();
			PrimProduct primProduct;
			switch (ecmProperty) {
			case ECM_ANSWER:
				// Don't include in GET responses
				return null;
			case ECM_ANSWER_CODE:
				// mapping only valid in System.State == "Resolved":
				// String state = getFieldValue(fields, TFS_STATE);
				// if (!state.equals("Resolved")) {
				// return null;
				// }
				break;
			case ECM_ATTACHMENT:
				setAttachment(ecr, workItem);
				return null; // indicates that we're done
			case ECM_COUNTRY:
			case ECM_CUSTOMER:
			case ECM_SITE:
				if (fieldValue != null) {
					// handle stripping of [,] - also multivalues: [v1];[v2]...
					List<String> list = Lists.newArrayList(Splitter.on(";")
							.split(fieldValue));
					for (String entry : list) {
						list2.add(entry.replace("[", "").replace("]", ""));
					}
					fieldValue = list2.get(0); // for now, only first value
				}
				break;
			case ECM_IDENTIFIER:
				String id = getFieldValue(fields,
						ERICSSON_DEFECT_LINK_FIELD);
				return Arrays.asList(id);
			case ECM_PRODUCT:
			case ECM_NODE_PRODUCT:
			case ECM_CORRECTED_NODE_PRODUCT:
			case ECM_CORRECTED_PRODUCT:
				primProduct = getPrimProduct(fields, ecmProperty, fieldName);
				return primProduct != null ? Arrays.asList(primProduct
						.getPrimProdNo()) : null;
			case ECM_PRODUCT_REVISION:
			case ECM_NODE_PRODUCT_REVISION:
			case ECM_CORRECTED_NODE_PRODUCT_REVISION:
			case ECM_CORRECTED_PRODUCT_REVISION:
				primProduct = getPrimProduct(fields, ecmProperty, fieldName);
				return primProduct != null ? Arrays.asList(primProduct
						.getPrimRState()) : null;
			default:
				break;
			}
			// VERIFY CONSTRAINTS:
			String constrainedBy = property.getInverseConstrainedBy();
			String constraintValue = null;
			if (constrainedBy != null) {
				constraintValue = getConstraintValue(ecr, workItem, property,
						Direction.INVERSE);
			}

			Collection<String> values = property.getInverse(fieldValue,
					constraintValue);
			if (values == null) {
				logger.debug("Skipping null value for " + ecmProperty);
				return null;
			}
			for (String mappedValue : values) {
				mappedValues.add(mappedValue);
			}
		}
		return mappedValues;
	}
	
	private PrimProduct getPrimProduct(FieldCollection fields,
			String ecmProperty, String releaseField) {

		String entityName = getFieldValue(fields, releaseField);
		String team = getFieldValue(fields, TFS_TEAM);
		if (entityName == null || entityName.isEmpty()) {
			return null;
		}
		if (team == null || team.isEmpty()) {
			return null;
		}
		BackendEntity entity = new BackendEntity(entityName, team);
		return ProductMapper.getInstance().getProduct(entity);
	}

	private void setAttachment(EnterpriseChangeRequest ecr, List<String> values) {
		// TODO Auto-generated method stub
		System.out.println("TBD");
	}

	/**
	 * Gets the attachments (external hyperlinks) from the {@link WorkItem} and
	 * sets the {@link EnterpriseChangeRequest} attachments property
	 * 
	 * @param ecr
	 * @param values
	 */
	private void setAttachment(EnterpriseChangeRequest ecr, WorkItem workItem) {
		List<Link> links = new ArrayList<>();
		for (com.microsoft.tfs.core.clients.workitem.link.Link link : workItem
				.getLinks()) {
			if (link instanceof Hyperlink) {
				Hyperlink hyperlink = (Hyperlink) link;
				String label = hyperlink.getComment();
				URI uri;
				try {
					uri = new URI(hyperlink.getLocation());
					Link ecrLink = new Link(uri, label);
					links.add(ecrLink);
				} catch (URISyntaxException e) {
					logger.info("Skipping bad hyperlink from workitem["
							+ workItem.getID() + ": " + hyperlink.getLocation());
				}
			}
		}
		if (!links.isEmpty()) {
			ecr.setAttachments(links.toArray(new Link[links.size()]));
		}
	}
	
	/**
	 * Construct a link for ecm:relatedChangeRequest
	 * 
	 * @param trId
	 * @return
	 */
	private String getCrLink(String trId) {
		return TFSAdapterManager.getMhweb_related_cr_url() + trId;
	}

}
