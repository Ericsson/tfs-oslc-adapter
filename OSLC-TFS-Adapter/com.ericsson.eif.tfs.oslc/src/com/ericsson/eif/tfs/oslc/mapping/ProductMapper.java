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
import java.util.HashMap;
import java.util.Iterator;
import java.util.Map;
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

public class ProductMapper {
    
    //TODO - Add support for getting project name from serviceProvider
    //Eg: serviceProviderMap containing a map of projects containing primproduct maps
    // project1
    //    { 
    //      primProductA1,
    //      primProductA2,
    //      primProductA3
    //    }
    // project2
    //    { 
    //      primProductB1,
    //      primProductB2,
    //      primProductB3
    //    }
    

    public class ServiceProvider {
        private String name; // E.g. project in TFS
        private String entity; // E.g. collectionName in TFS

        public ServiceProvider(String name, String entity) {
            this.name = name;
            this.entity = entity;
        }
        
        public String getName() {
            return name;
        }
        
        public String getEntity() {
            return entity;
        }
    }

    ServiceProvider serviceProvider;
    Map<String, Object> serviceProviderMap; //TODO - see above
    Map<PrimProduct, BackendEntity> map;
    private static ProductMapper instance;
    private String productMappingFile;

    private ProductMapper() {
        map = new HashMap<>();
    }

    public static ProductMapper getInstance() {
        if (instance == null) {
            instance = new ProductMapper();
        }
        return instance;
    }

    public void setTestMappingFile(String file) {
        productMappingFile = file;
    }

    public void load(InputStream input) throws ParserConfigurationException,
            SAXException, IOException, XPathExpressionException {
        DocumentBuilderFactory domFactory = DocumentBuilderFactory
                .newInstance();
        domFactory.setNamespaceAware(true);
        DocumentBuilder builder = domFactory.newDocumentBuilder();
        Document doc = builder.parse(input);
        XPath xpath = XPathFactory.newInstance().newXPath();

        // locate all product serviceProvider nodes
        NodeList spNodes = (NodeList) xpath.evaluate(
                "//mapping//serviceProvider", doc, XPathConstants.NODESET);

        for (int i = 0; i < spNodes.getLength(); i++) {
            Node node = spNodes.item(i);
            if (processThis(node)) {
                processServiceProvider(node);
            }
        }
    }

    public void load() {
    	if (productMappingFile == null) {
            productMappingFile = TFSAdapterManager.getProductMappingFile();
    	}
    	
        try {
            File inputFile = new File(productMappingFile);
            if (!inputFile.exists()) {
                // try relative path
                inputFile = new File(TFSAdapterManager.getAdapterServletHome()
                        + File.separator + productMappingFile);
                if (!inputFile.exists()) {
                    TFSAdapterManager
                            .logAndExit("Products mapping file missing: "
                                    + productMappingFile);
                }
            }
            load(new FileInputStream(inputFile));
        } catch (XPathExpressionException | ParserConfigurationException
                | SAXException | IOException e) {
            e.printStackTrace();
        }
    }

    private void processServiceProvider(Node node) {
        NamedNodeMap attributes = node.getAttributes();
        String name = null;
        String collectionName = null;
        for (int i = 0; i < attributes.getLength(); i++) {
            Node attribute = attributes.item(i);
            switch (attribute.getNodeName()) {
            case "name":
                name = attribute.getNodeValue();
                break;
            case "collectionName":
                collectionName = attribute.getNodeValue();
                break;
            default:
                break;
            }
        }
        serviceProvider = new ServiceProvider(name, collectionName);
        NodeList children = node.getChildNodes();
        for (int j = 0; j < children.getLength(); j++) {
            Node child = children.item(j);
            if (processThis(child)) {
                processPrimProduct(child);
            }
        }
    }

    private void processPrimProduct(Node node) {
        NamedNodeMap attributes = node.getAttributes();
        String primProdNo = null;
        String primRState = null;
        String mapTo = null;
        String team = null;
        for (int i = 0; i < attributes.getLength(); i++) {
            Node attribute = attributes.item(i);
            switch (attribute.getNodeName()) {
            case "primProdNo":
                primProdNo = attribute.getNodeValue();
                break;
            case "primRState":
                primRState = attribute.getNodeValue();
                break;
            case "mapTo":
                mapTo = attribute.getNodeValue();
                break;
            case "team":
                team = attribute.getNodeValue();
                break;
            default:
                break;
            }
        }
        PrimProduct product = new PrimProduct(primProdNo, primRState);
        BackendEntity entity = new BackendEntity(mapTo, team);
        map.put(product, entity);
    }

    private boolean processThis(Node node) {
        return node.getAttributes() != null; //
    }
    
    public Iterator<PrimProduct> getProductIterator() {
        return map.keySet().iterator();
    }
    
    public Iterator<Entry<PrimProduct, BackendEntity>> getEntityIterator() {
        return map.entrySet().iterator();
    }

    public BackendEntity getEntity(PrimProduct product) {
        if (map.containsKey(product)) {
            return map.get(product);
        }
        return null;
    }

    public PrimProduct getProduct(BackendEntity entity) {
        if (map.containsValue(entity)) {
            Iterator<Entry<PrimProduct, BackendEntity>> iter = map.entrySet().iterator();
            while (iter.hasNext()) {
                Entry<PrimProduct, BackendEntity> next = iter.next();
                if (next.getValue().equals(entity)) {
                    return next.getKey();
                }
            }
        }
        return null;
    }
    
    public ServiceProvider getServiceProvider() {
        return serviceProvider;
    }
}
