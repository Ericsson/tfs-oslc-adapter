<!-- <!DOCTYPE html> -->
<%-- <%-- --%>
<%--  Copyright (c) 2011, 2012 IBM Corporation and others. --%>

<%--  All rights reserved. This program and the accompanying materials --%>
<%--  are made available under the terms of the Eclipse Public License v1.0 --%>
<%--  and Eclipse Distribution License v. 1.0 which accompanies this distribution. --%>
 
<%--  The Eclipse Public License is available at http://www.eclipse.org/legal/epl-v10.html --%>
<%--  and the Eclipse Distribution License is available at --%>
<%--  http://www.eclipse.org/org/documents/edl-v10.php. --%>
 
<%--  Contributors: --%>
 
<%--     Sam Padgett 	 - initial API and implementation --%>
<%--     Michael Fiedler	 - adapted for OSLC4J --%>
<%-- 	Jad El-khoury        - initial implementation of code generator (https://bugs.eclipse.org/bugs/show_bug.cgi?id=422448) --%>

<%--  This file is generated by org.eclipse.lyo.oslc4j.codegenerator --%>
<%-- --%> --%>

<%-- <%@page import="org.eclipse.lyo.oslc4j.core.model.ServiceProvider"%> --%>
<%-- <%@page import="java.util.List" %> --%>
<%-- <%@page import="com.ericsson.eif.tfs.oslc.resources.TFSChangeRequest"%> --%>
<%-- <%-- --%>
<%-- Start of user code imports --%>
<%-- --%> --%>
<%-- <%--  --%>
<%-- End of user code  --%>
<%-- --%> --%>


<%-- <%@ page contentType="text/html" language="java" pageEncoding="UTF-8" %> --%>

<%-- <% --%>
// 	ServiceProvider serviceProvider = (ServiceProvider) request.getAttribute("serviceProvider");
<%-- %> --%>
<%-- <%-- --%>
<%-- Start of user code getRequestAttributes --%>
<%-- --%> --%>
<%-- <%-- --%>
<%-- End of user code --%>
<%-- --%> --%>

<!-- <html> -->
<!-- 	<head> -->
<!-- 		<meta http-equiv="Content-Type" content="text/html;charset=utf-8"> -->
<!-- 		<title>Resource Creator</title> -->
<%-- 		<%--  --%>
<%-- Start of user code (RECOMMENDED) headStuff  --%>
<%-- 		--%> --%>
<%-- 		<%--  --%>
<%-- End of user code  --%>
<%-- 		--%> --%>
<!-- 	</head> -->
<!-- 	<body style="padding: 10px;"> -->
<!-- 		<div id="bugzilla-body"> -->
<!-- 		<form id="Create" method="POST" class="enter_bug_form"> -->
<%-- 		<%--  --%>
<%-- Start of user code (RECOMMENDED) formStuff  --%>
<%-- 		--%> --%>
<%-- 		<%--  --%>
<%-- End of user code  --%>
<%-- 		--%> --%>
<!-- 				<table style="clear: both;"> -->

<!-- 						<tr> -->
<%-- 							<td><%= TFSChangeRequest.discussedByAsHtmlForCreation(request, serviceProvider.getIdentifier())%></td> --%>
<!-- 						</tr> -->
<!-- 						<tr> -->
<%-- 							<td><%= TFSChangeRequest.titleAsHtmlForCreation(request, serviceProvider.getIdentifier())%></td> --%>
<!-- 						</tr> -->
<!-- 						<tr> -->
<%-- 							<td><%= TFSChangeRequest.implementsRequirementsAsHtmlForCreation(request, serviceProvider.getIdentifier())%></td> --%>
<!-- 						</tr> -->
<!-- 						<tr> -->
<%-- 							<td><%= TFSChangeRequest.reviewedAsHtmlForCreation(request, serviceProvider.getIdentifier())%></td> --%>
<!-- 						</tr> -->
<!-- 						<tr> -->
<%-- 							<td><%= TFSChangeRequest.instanceShapeAsHtmlForCreation(request, serviceProvider.getIdentifier())%></td> --%>
<!-- 						</tr> -->
<!-- 						<tr> -->
<%-- 							<td><%= TFSChangeRequest.verifiedAsHtmlForCreation(request, serviceProvider.getIdentifier())%></td> --%>
<!-- 						</tr> -->
<!-- 						<tr> -->
<%-- 							<td><%= TFSChangeRequest.relatedTestScriptsAsHtmlForCreation(request, serviceProvider.getIdentifier())%></td> --%>
<!-- 						</tr> -->
<!-- 						<tr> -->
<%-- 							<td><%= TFSChangeRequest.blocksTestExecutionRecordsAsHtmlForCreation(request, serviceProvider.getIdentifier())%></td> --%>
<!-- 						</tr> -->
<!-- 						<tr> -->
<%-- 							<td><%= TFSChangeRequest.shortTitleAsHtmlForCreation(request, serviceProvider.getIdentifier())%></td> --%>
<!-- 						</tr> -->
<!-- 						<tr> -->
<%-- 							<td><%= TFSChangeRequest.dctermsTypesAsHtmlForCreation(request, serviceProvider.getIdentifier())%></td> --%>
<!-- 						</tr> -->
<!-- 						<tr> -->
<%-- 							<td><%= TFSChangeRequest.serviceProviderAsHtmlForCreation(request, serviceProvider.getIdentifier())%></td> --%>
<!-- 						</tr> -->
<!-- 						<tr> -->
<%-- 							<td><%= TFSChangeRequest.relatedChangeRequestsAsHtmlForCreation(request, serviceProvider.getIdentifier())%></td> --%>
<!-- 						</tr> -->
<!-- 						<tr> -->
<%-- 							<td><%= TFSChangeRequest.tracksRequirementsAsHtmlForCreation(request, serviceProvider.getIdentifier())%></td> --%>
<!-- 						</tr> -->
<!-- 						<tr> -->
<%-- 							<td><%= TFSChangeRequest.teamAsHtmlForCreation(request, serviceProvider.getIdentifier())%></td> --%>
<!-- 						</tr> -->
<!-- 						<tr> -->
<%-- 							<td><%= TFSChangeRequest.subjectsAsHtmlForCreation(request, serviceProvider.getIdentifier())%></td> --%>
<!-- 						</tr> -->
<!-- 						<tr> -->
<%-- 							<td><%= TFSChangeRequest.testedByTestCasesAsHtmlForCreation(request, serviceProvider.getIdentifier())%></td> --%>
<!-- 						</tr> -->
<!-- 						<tr> -->
<%-- 							<td><%= TFSChangeRequest.descriptionAsHtmlForCreation(request, serviceProvider.getIdentifier())%></td> --%>
<!-- 						</tr> -->
<!-- 						<tr> -->
<%-- 							<td><%= TFSChangeRequest.identifierAsHtmlForCreation(request, serviceProvider.getIdentifier())%></td> --%>
<!-- 						</tr> -->
<!-- 						<tr> -->
<%-- 							<td><%= TFSChangeRequest.closedAsHtmlForCreation(request, serviceProvider.getIdentifier())%></td> --%>
<!-- 						</tr> -->
<!-- 						<tr> -->
<%-- 							<td><%= TFSChangeRequest.contributorsAsHtmlForCreation(request, serviceProvider.getIdentifier())%></td> --%>
<!-- 						</tr> -->
<!-- 						<tr> -->
<%-- 							<td><%= TFSChangeRequest.modifiedAsHtmlForCreation(request, serviceProvider.getIdentifier())%></td> --%>
<!-- 						</tr> -->
<!-- 						<tr> -->
<%-- 							<td><%= TFSChangeRequest.closeDateAsHtmlForCreation(request, serviceProvider.getIdentifier())%></td> --%>
<!-- 						</tr> -->
<!-- 						<tr> -->
<%-- 							<td><%= TFSChangeRequest.approvedAsHtmlForCreation(request, serviceProvider.getIdentifier())%></td> --%>
<!-- 						</tr> -->
<!-- 						<tr> -->
<%-- 							<td><%= TFSChangeRequest.affectsPlanItemsAsHtmlForCreation(request, serviceProvider.getIdentifier())%></td> --%>
<!-- 						</tr> -->
<!-- 						<tr> -->
<%-- 							<td><%= TFSChangeRequest.createdAsHtmlForCreation(request, serviceProvider.getIdentifier())%></td> --%>
<!-- 						</tr> -->
<!-- 						<tr> -->
<%-- 							<td><%= TFSChangeRequest.affectsRequirementsAsHtmlForCreation(request, serviceProvider.getIdentifier())%></td> --%>
<!-- 						</tr> -->
<!-- 						<tr> -->
<%-- 							<td><%= TFSChangeRequest.inprogressAsHtmlForCreation(request, serviceProvider.getIdentifier())%></td> --%>
<!-- 						</tr> -->
<!-- 						<tr> -->
<%-- 							<td><%= TFSChangeRequest.affectedByDefectsAsHtmlForCreation(request, serviceProvider.getIdentifier())%></td> --%>
<!-- 						</tr> -->
<!-- 						<tr> -->
<%-- 							<td><%= TFSChangeRequest.relatedTestPlansAsHtmlForCreation(request, serviceProvider.getIdentifier())%></td> --%>
<!-- 						</tr> -->
<!-- 						<tr> -->
<%-- 							<td><%= TFSChangeRequest.fixedAsHtmlForCreation(request, serviceProvider.getIdentifier())%></td> --%>
<!-- 						</tr> -->
<!-- 						<tr> -->
<%-- 							<td><%= TFSChangeRequest.statusAsHtmlForCreation(request, serviceProvider.getIdentifier())%></td> --%>
<!-- 						</tr> -->
<!-- 						<tr> -->
<%-- 							<td><%= TFSChangeRequest.creatorsAsHtmlForCreation(request, serviceProvider.getIdentifier())%></td> --%>
<!-- 						</tr> -->
<!-- 						<tr> -->
<%-- 							<td><%= TFSChangeRequest.relatedTestCasesAsHtmlForCreation(request, serviceProvider.getIdentifier())%></td> --%>
<!-- 						</tr> -->
<!-- 						<tr> -->
<%-- 							<td><%= TFSChangeRequest.affectsTestResultsAsHtmlForCreation(request, serviceProvider.getIdentifier())%></td> --%>
<!-- 						</tr> -->
<!-- 						<tr> -->
<%-- 							<td><%= TFSChangeRequest.relatedTestExecutionRecordsAsHtmlForCreation(request, serviceProvider.getIdentifier())%></td> --%>
<!-- 						</tr> -->
<!-- 						<tr> -->
<%-- 							<td><%= TFSChangeRequest.rdfTypesAsHtmlForCreation(request, serviceProvider.getIdentifier())%></td> --%>
<!-- 						</tr> -->
<!-- 						<tr> -->
<%-- 							<td><%= TFSChangeRequest.tracksChangeSetsAsHtmlForCreation(request, serviceProvider.getIdentifier())%></td> --%>
<!-- 						</tr> -->

					
<!-- 					<tr> -->
<!-- 						<td></td> -->
<!-- 						<td> -->
<!-- 							<input type="submit" value="Submit"> -->
<!-- 							<input type="reset"> -->
<!-- 						</td> -->
<!-- 					</tr> -->
<!-- 				</table> -->
				

<!-- 				<div style="width: 500px;"> -->
					
<!-- 				</div> -->
				
<!-- 			</form> -->

<!-- 			<div style="clear: both;"></div> -->
<!-- 		</div> -->
<!-- 	</body> -->
<!-- </html> -->



