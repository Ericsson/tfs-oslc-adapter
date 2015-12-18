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
import java.io.InputStream;
import java.util.Collection;
import java.util.Iterator;
import java.util.Map.Entry;

import javax.xml.parsers.DocumentBuilder;
import javax.xml.parsers.DocumentBuilderFactory;
import javax.xml.parsers.ParserConfigurationException;
import javax.xml.xpath.XPath;
import javax.xml.xpath.XPathConstants;
import javax.xml.xpath.XPathExpressionException;
import javax.xml.xpath.XPathFactory;

import org.w3c.dom.Document;
import org.w3c.dom.NamedNodeMap;
import org.w3c.dom.Node;
import org.w3c.dom.NodeList;
import org.xml.sax.SAXException;

import com.ericsson.eif.tfs.oslc.TFSAdapterManager;
import com.ericsson.eif.tfs.oslc.mapping.BiDirectionalMap.Direction;
import com.google.common.collect.HashMultimap;
import com.google.common.collect.Multimap;

/** 
 * Parses attributes_mapping.xml for Property entries and stores them
 * in a {@link BiDirectionalMap}
 * See the file attribute_mapping.xml for a detailed description of the 
 * mapping syntax 
 */
public class AttributesMapper {

    private Multimap<String, Property> forwardMap; 
    private Multimap<String, Property> inverseMap;
    private String attributesMappingFile;
    
    private static AttributesMapper instance;
    
    public static AttributesMapper getInstance() {
        if (instance == null) {
            instance = new AttributesMapper();
        }
        return instance;
    }
    
    private AttributesMapper() {
        forwardMap = HashMultimap.create();
        inverseMap = HashMultimap.create();
    }
    
    public void setTestAttributesMappingFile(String file) {
        attributesMappingFile = file;
    }
    
    public enum NodeType {
        DEFAULT,
        DEPENDS,
        PROPERTY,
        MAP,
        USE,
        UNKNOWN
    }

    public NodeType getNodeType(String typeName) {
        try {
            NodeType type = NodeType.valueOf(typeName.toUpperCase());
            return type;
        } catch (IllegalArgumentException e) {
            return NodeType.UNKNOWN;
        }
    }
    
    public Property addProperty(
            String key, 
            String value, 
            Direction direction,
            boolean notify, 
            String forwardConstrainedBy,
            String inverseConstrainedBy) {
        Property newProperty = new Property(key, value, direction, notify,
                forwardConstrainedBy, inverseConstrainedBy);
        switch (direction) {
        case BIDIRECTIONAL:
            forwardMap.put(key, newProperty);
            inverseMap.put(value, newProperty);
            break;
        case FORWARD:
            forwardMap.put(key, newProperty);
            break;
        case INVERSE:
            inverseMap.put(value, newProperty);
            break;
        }
        return newProperty;
    }
    
    /**
     * For testing
     * @return
     */
    public Iterator<Entry<String, Property>> getForwardIterator() {
        return forwardMap.entries().iterator();
    }
    
    /**
     * For testing
     * @return
     */
    public Iterator<Entry<String, Property>> getInverseIterator() {
        return inverseMap.entries().iterator();
    }
    
    public Collection<Property> getForwardProperties(String key) {
        // direction can be either FORWARD or BIDIRECTIONAL
        if (forwardMap.containsKey(key)) {
            Collection<Property> properties = forwardMap.get(key);
            for (Iterator<Property> it = properties.iterator(); it.hasNext();) {
                if (it.next().getDirection().equals(Direction.INVERSE)) {
                    it.remove();
                }
            }
            return properties;
        }
        return null;
    }
    
    public Collection<Property> getInverseProperties(String key) {
        // direction can be either INVERSE or BIDIRECTIONAL
        if (inverseMap.containsKey(key)) {
            Collection<Property> properties = inverseMap.get(key);
            for (Iterator<Property> it = properties.iterator(); it.hasNext();) {
                if (it.next().getDirection().equals(Direction.FORWARD)) {
                    it.remove();
                }
            }
            return properties;
        }
        return null;
    }
    
    /**
     * Reloads mapping from file
     * Will not reload more often than 1/minute
     * Checks file timestamp and reloads if the file has been modified
     */
    public void reload() {
        // TODO - Sprint3?
    }

    /**
     * Load mapping rules from the file configured by calling
     * {@link AttributesMapper#setTestAttributesMappingFile(String)}
     */
    public void load() {
    	if (attributesMappingFile == null) {
    		attributesMappingFile = TFSAdapterManager.getAttributesMappingFile();
    	}
        addMappingRules(attributesMappingFile, false);
    }
    
    /**
     * Add mapping rules from the specified file
     * 
     * @param filename
     */
    public void addMappingRules(String filename, boolean add) {
        try {
            File inputFile = new File(filename);
            if (!inputFile.exists()) {
                // try relative path
                inputFile = new File(TFSAdapterManager.getAdapterServletHome()
                        + File.separator + filename);
                if (!inputFile.exists()) {
                    TFSAdapterManager
                            .logAndExit("Attributes mapping file missing: "
                                    + filename);
                }
            }
            load(new FileInputStream(inputFile), add);
        } catch (XPathExpressionException | ParserConfigurationException
                | SAXException | IOException e) {
            e.printStackTrace();
        }
    }


    /**
     * Configure mapping rules by parsing the input file
     * 
     * @param input
     *            file from which to read the mapping rules
     * @param add
     *            if true, add the rules to the existing ones. Else, reset
     *            before loading
     * @throws ParserConfigurationException
     * @throws SAXException
     * @throws IOException
     * @throws XPathExpressionException
     */
    public void load(InputStream input, boolean add)
            throws ParserConfigurationException, SAXException, IOException,
            XPathExpressionException {
        if (!add) {
            // clear existing entries
            forwardMap.clear();
            inverseMap.clear();
        }        
        // initialize dom and xpath factory
        DocumentBuilderFactory domFactory = DocumentBuilderFactory
                .newInstance();
        domFactory.setNamespaceAware(true);
        DocumentBuilder builder = domFactory.newDocumentBuilder();
        Document doc = builder.parse(input);
        XPath xpath = XPathFactory.newInstance().newXPath();

        // locate all property nodes:
        NodeList propertyNodes = (NodeList) xpath.evaluate(
                "//mapping//property", doc,
                XPathConstants.NODESET);
        
        for (int i = 0; i < propertyNodes.getLength(); i++) {
            Node node = propertyNodes.item(i);
            if (processThis(node)) {
                Property property = processProperty(node);
                NodeList children = node.getChildNodes();
                for (int j = 0; j < children.getLength(); j++) {
                    Node child = children.item(j);
                    switch (getNodeType(child.getNodeName())) {
                    case DEFAULT:
                        processDefault(child, property);
                        break;
                    case DEPENDS:
                        processDepends(child, property);
                        break;
                    case MAP:
                        processMap(child, property);
                        break;
                    case USE:
                        processUse(child, property);
                    default:
                        break;
                    }
                }
            }
        }
    }
    
    private boolean processThis(Node node) {
        return true; //node.getClass().getSimpleName().startsWith("DeferredText");
    }
    
    private Property processProperty(Node node) {
        NamedNodeMap attributes = node.getAttributes();
        String key = null;
        String value = null;
        String forward = "";
        String inverse = "";
        boolean notify = false;
        String forwardConstrainedBy = null;
        String inverseConstrainedBy = null;
        for (int i = 0; i < attributes.getLength(); i++) {
            Node attribute = attributes.item(i);
            switch (attribute.getNodeName()) {
            case "key":
                key = attribute.getNodeValue();
                break;
            case "value":
                value = attribute.getNodeValue();
                break;
            case "forward":
                forward = attribute.getNodeValue();
                break;
            case "inverse":
                inverse = attribute.getNodeValue();
                break;
            case "notifyChange":
                notify = Boolean.parseBoolean(attribute.getNodeValue());
                break;
            case "forwardConstrainedBy":
                forwardConstrainedBy = attribute.getNodeValue();
                break;
            case "inverseConstrainedBy":
                inverseConstrainedBy = attribute.getNodeValue();
                break;
            default:
                System.out.println("TODO->" + attribute.getNodeType());
                break;
            }
        }
        
        Direction direction = Direction.BIDIRECTIONAL;
        if (forward.equals("true") && !inverse.equals("true")) {
            direction = Direction.FORWARD;
        }
        if (inverse.equals("true") && !forward.equals("true")) {
            direction = Direction.INVERSE;
        }
        // store the property
        Property property = addProperty(key, value, direction, notify,
                                    forwardConstrainedBy, inverseConstrainedBy);
        return property;
    }
    
    private void processDefault(Node node, Property parent) {
        NamedNodeMap attributes = node.getAttributes();
        String key = null;
        String value = null;
        for (int i = 0; i < attributes.getLength(); i++) {
            Node attribute = attributes.item(i);
            switch (attribute.getNodeName()) {
            case "inverseValue":
                key = attribute.getNodeValue();
                break;
            case "forwardValue":
                value = attribute.getNodeValue();
                break;
            default:
                break;
            }
        }
        parent.setDefault(value, key);
    }

    private void processDepends(Node node, Property parent) {
        NamedNodeMap attributes = node.getAttributes();
        String dependency = "";
        for (int i = 0; i < attributes.getLength(); i++) {
            Node attribute = attributes.item(i);
            switch (attribute.getNodeName()) {
            case "on":
                dependency = attribute.getNodeValue();
                parent.addDependency(dependency);
                break;
            default:
                break;
            }
        }
    }
    
    private void processMap(Node node, Property parent) {
        NamedNodeMap attributes = node.getAttributes();
        String value = null;
        String to = null;
        String forward = "false";
        String inverse = "false";
        String forwardConstraintValue = null;
        String inverseConstraintValue = null;
        for (int i = 0; i < attributes.getLength(); i++) {
            Node attribute = attributes.item(i);
            switch (attribute.getNodeName()) {
            case "key":
                value = attribute.getNodeValue();
                break;
            case "value":
                to = attribute.getNodeValue();
                break;
            case "forward":
                forward = attribute.getNodeValue();
                break;
            case "inverse":
                inverse = attribute.getNodeValue();
                break;
            case "forwardWhen":
                forwardConstraintValue = attribute.getNodeValue();
                forward = "true";
                break;
            case "inverseWhen":
                inverseConstraintValue = attribute.getNodeValue();
                inverse = "true";
                break;
            case "default":
                System.out.println("Default");
                break;
            default:
                // this case is not handled - print out here:
                System.out.println(attribute.getNodeName() + " : " + attribute.getNodeValue());
                System.out.println();
                break;
            }
        }
        Direction direction = Direction.BIDIRECTIONAL;
        if (forward.equals("true") && inverse.equals("false")) {
            direction = Direction.FORWARD;
        } else if (forward.equals("false") && inverse.equals("true")) {
            direction = Direction.INVERSE;
        }
        parent.addEntry(value, to, direction, forwardConstraintValue, 
                inverseConstraintValue);
    }
    
    private void processUse(Node node, Property parent) {
        NamedNodeMap attributes = node.getAttributes();
        for (int i = 0; i < attributes.getLength(); i++) {
            Node attribute = attributes.item(i);
            switch (attribute.getNodeName()) {
            case "name":
                parent.setUseMapping(attribute.getNodeValue());
                break;
            case "key":
                parent.setUseKey(attribute.getNodeValue());
                break;
            default:
                break;
            }
        }
    }

}
