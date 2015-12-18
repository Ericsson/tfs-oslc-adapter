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
import java.io.IOException;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.Collection;
import java.util.Iterator;
import java.util.List;
import java.util.Map.Entry;

import javax.xml.parsers.ParserConfigurationException;
import javax.xml.xpath.XPathExpressionException;

import org.xml.sax.SAXException;

public class XPathAttributesParser {
    
    public static void main(String[] args) throws ParserConfigurationException,
            SAXException, IOException, XPathExpressionException {
        
        boolean customerTest = false;
        if (customerTest) {
            AttributesMapper cm = AttributesMapper.getInstance();
            File customerMappingFile = new File(".." + File.separator + "com.ericsson.eif.tfs.common" + File.separator + "customer_mapping.xml");
            cm.setTestAttributesMappingFile(customerMappingFile.getAbsolutePath());
            cm.load();
        } else {

        AttributesMapper am = AttributesMapper.getInstance();
        File mappingFile = new File(".." + File.separator + "com.ericsson.eif.tfs.common" + File.separator + "attribute_mapping.xml");
        am.setTestAttributesMappingFile(mappingFile.getAbsolutePath());
        am.load();
        am.load();
        // dump the stored properties:
        System.out.println("=== Forward properties ===");
        int forwardCount = 0;
        List<String> forwardKeys = new ArrayList<>();
        Iterator<Entry<String, Property>> forwardIterator = am.getForwardIterator();
        while (forwardIterator.hasNext()) {
            forwardCount++;
            Entry<String, Property> e = forwardIterator.next();
            System.out.println(e.getKey()); // + ": " + e.getValue());
            forwardKeys.add(e.getKey());
        }
        System.out.println("=== Inverse properties ===");
        int inverseCount = 0;
        List<String> inverseKeys = new ArrayList<>();
        Iterator<Entry<String, Property>> inverseIterator = am.getInverseIterator();
        while (inverseIterator.hasNext()) {
            inverseCount++;
            Entry<String, Property> e = inverseIterator.next();
            System.out.println(e.getKey()); // + ": " + e.getValue());
            inverseKeys.add(e.getKey());
        }
        System.out.println("===");
        System.out.println("Forward count: " + forwardCount);
        System.out.println("Inverse count: " + inverseCount);
        System.out.println("===");
        
        // Some testing
        System.out.println("[[[ TESTING ]]]");
        testKey(am, TFSMapper.ECM_TITLE);
        testKey(am, TFSMapper.ECM_PRIORITY);
        testKey(am, TFSMapper.ECM_ACTIVITY);
        testValue(am, TFSMapper.ECM_PRIORITY, "A", new String[] {"0"});
        testValue(am, TFSMapper.ECM_PRIORITY, "C", new String[] {"2"});
        //testValue(am, TFSMapper.ECM_IMPACT_ON_ISP, "C", "2");
        testValue(am, TFSMapper.ECM_ACTIVITY, "AUT", new String[] {"Watson"});
        testValue(am, TFSMapper.ECM_ACTIVITY, "AAQ", new String[] {"<Should fail>"});
        testInverseValue(am, TFSMapper.TFS_PRIORITY, "2", "C");
        testInverseValue(am, TFSMapper.TFS_PRIORITY, "3", "C");
        testInverseValue(am, TFSMapper.TFS_SEVERITY, "3", "D");
        testInverseValue(am, TFSMapper.TFS_HOW_FOUND_CATEGORY, "Deployment", "MB");
        //testKey(am, TFSMapper.ECM_IMPACT_ON_ISP);
        //testKey(am, TFSMapper.ECM_EXPECTED_IMPACT_ON_ISP);
        //testDefault(am, TFSMapper.ECM_FIRST_TECHNICAL_CONTACT);
        testKey(am, TFSMapper.ECM_STATUS);
        testValue(am, TFSMapper.ECM_STATUS, "RE", new String[] {"RE (Registered)", "Active"});
        }
    }
    
    private static void testDefault(AttributesMapper am, String key) {
        Collection<Property> properties = am.getForwardProperties(key);
        for (Property property : properties) {
            Collection<String> forwardDefaults = property.getForward("");
            Collection<String> inverseDefaults = property.getInverse("");
            System.out.println("(Default) Key: " + key + ", forward defaults="
                    + forwardDefaults + ", inverse defaults=" + inverseDefaults);
        }
    }
    
    /**
     * Test the key by:
     * - printing the forward mapping - "key > value(s)"
     *   Note a key may map to one or more values
     * - printing the inverse mapping - "value(s) > key"
     * - more TBD...
     * @param am
     * @param key
     */
    private static void testKey(AttributesMapper am, String key) {
        System.out.println("Forward: " + key + " = " + am.getForwardProperties(key));
        //Obsolete -> System.out.println("Inverse: " + key + " = " + am.getInverse(key) + " <NOTE: Should be empty>");
        // test the inverse as from the "forward property": 
        //String key2 = am.getForward(key).getValue();
        //System.out.println("Inverse: " + key2 + " = " + am.getInverse(key2));
        Collection<Property> props = am.getForwardProperties(key);
        for (Property p : props) {
            String inverseKey = p.getValue();
            System.out.println("Inverse: " + inverseKey + " = " + p.getInverse(inverseKey));
        }
        //
        // Inverse mock test:
        String ecmProperty = key;
        Collection<Property> props2 = am.getForwardProperties(ecmProperty);
        for (Property p : props2) {
            String fieldName = p.getValue();
            String fieldValue = "1";
            Collection<String> values = p.getInverse(fieldValue);
            for (String mappedValue : values) {
                System.out.println("" + fieldName + "(" + fieldValue + ") --> " + ecmProperty + "(" + mappedValue + ")");
            }
        }
        // Forward mock test:
        String ecrValue = "C";
        Collection<Property> props3 = am.getForwardProperties(ecmProperty);
        for (Property p : props3) {
            Collection<String> values = p.getForward(ecrValue);
            for (String mappedValue : values) {
                System.out.println("" + ecmProperty + "(" + ecrValue + ") --> "
                        + p.getValue() + "(" + mappedValue + ")");
            }}
        
//        // Inverse mock test:
//        Property property = AttributesMapper.getInstance().getForward(ecmProperty);
//        String fieldName = property.getValue();
//        String fieldValue = "1";
//        String mappedValue = property.getInverse(fieldValue);
//        System.out.println("" + fieldName + "(" + fieldValue + ") --> " + ecmProperty + "(" + mappedValue + ")");
//        //
//        // Forward mock test:
//        // ecmProperty as above
//        String ecrValue = "C";
//        Property property2 = AttributesMapper.getInstance().getForward(ecmProperty);
//        String mappedValue2 = property2.getForward(ecrValue);
//        System.out.println("" + ecmProperty + "(" + ecrValue + ") --> " + property2.getValue() + "(" + mappedValue2 + ")");
        System.out.println();
    }
    
    private static void testValue(AttributesMapper am, String key, String ecrValue, String[] expected) {
        List<String> expectedValues = Arrays.asList(expected);
        Collection<Property> ps = am.getForwardProperties(key);
        for (Property p : ps) {
            Collection<String> values = p.getForward(ecrValue);
            for (String v : values) {
                System.out.print(key + ": " + ecrValue + " -> " + v);
                if (v != null && expectedValues.contains(v)) { //v.equals(expected[expectedIx])) {
                    System.out.println(" +++ PASS +++");
                } else {
                    System.out.println(" *** FAIL *** - expected: " + expectedValues);
                }
            }
        }
//        Property p = am.getForward(key);
//        String v = p.getForward(ecrValue);
//        System.out.print(key + ": " + ecrValue + " -> " + v);
//        if (v != null && v.equals(expected)) {
//            System.out.println(" +++ PASS +++");
//        } else {
//            System.out.println(" *** FAIL *** - expected: " + expected);
//        }
    }
    
    private static void testInverseValue(AttributesMapper am, String key, String val, String expected) {
        Collection<Property> ps = am.getInverseProperties(key);
        for (Property p : ps) {
            Collection<String> values = p.getInverse(val);
            for (String v : values) { 
                System.out.print(key + ": " + val + " -> " + v);
                if (v != null && v.equals(expected)) {
                    System.out.println(" +++ PASS +++");
                } else {
                    System.out.println(" +++ FAIL *** - expected: " + expected);
                }
            }
        }
    }
}