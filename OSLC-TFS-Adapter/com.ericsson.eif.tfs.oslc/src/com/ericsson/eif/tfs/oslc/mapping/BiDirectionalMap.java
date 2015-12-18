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

import java.util.ArrayList;
import java.util.Arrays;
import java.util.Collection;
import java.util.List;
import java.util.Map;

import com.google.common.collect.HashMultimap;
import com.google.common.collect.Multimap;

/**
 * A bi-directional "map", i.e. <b>Map&lt;String, Value&gt;</b>, (however, not
 * properly implemented as a {@link Map} but rather just as to {@link Map}s, one
 * for the "forward" direction and one for the "inverse" direction.
 * 
 */
public class BiDirectionalMap {
    
    private class Value {
        private String value;
        private String constraintValue;
        
        public Value(String value, String constraint) {
            this.value = value;
            this.constraintValue = constraint;
        }
        
        public String getValue() {
            return value;
        }
        
        public boolean matches(String constraintValue) {
            if (this.constraintValue == null) {
                // no constraints
                return true;
            }
            for (String constraint : this.constraintValue.split(",")) {
                if (constraint.equals(constraintValue)) {
                    return true;
                }
            }
            return false;
        }
    }
    
    private Multimap<String, Value> forwardMap; 
    private Multimap<String, Value> inverseMap; 
    private String forwardDefaultValue;
    private String inverseDefaultValue;

    public enum Direction {
        FORWARD, INVERSE, BIDIRECTIONAL
    }
    
    public BiDirectionalMap() {
        forwardMap = HashMultimap.create();
        inverseMap = HashMultimap.create();
    }
    
    public static Direction getDirection(String directive) {
        switch (directive) {
        case "forward":
            return Direction.FORWARD;
        case "inverse":
            return Direction.INVERSE;
        case "bidirectional":
            return Direction.BIDIRECTIONAL;
        default:
            return Direction.BIDIRECTIONAL;
        }
    }

    /**
     * Adds an entry to the map
     * 
     * @param key
     * @param value
     * @param direction
     * @param when
     * @param matches
     * @throws InvalidKeyValueException 
     */
    public void addEntry(String key, String value, Direction direction, 
                         String forwardConstraintValue, String inverseConstraintValue) {
        //         <map key="PR" value="Active" forward="true"/>
        //         <map key="PR" value="Active" forward="true" forwardWhen="Active,Resolved"/>
        switch (direction) {
        case FORWARD:
            Value forwardValue = new Value(value, forwardConstraintValue);
            forwardMap.put(key, forwardValue);
            break;
        case INVERSE:
            Value inverseValue = new Value(key, inverseConstraintValue);
            inverseMap.put(value, inverseValue);
            break;
        case BIDIRECTIONAL:
        default:
            Value forwardValue1 = new Value(value, forwardConstraintValue);
            Value inverseValue1 = new Value(key, inverseConstraintValue);
            forwardMap.put(key, forwardValue1);
            inverseMap.put(value, inverseValue1);
            break;
        }
    }
    
    /**
     * Clears the map - intended to be used when reloading from file
     * 
     */
    public void clear() {
        forwardMap.clear();
        inverseMap.clear();
    }
    
    /**
     * Map the "key" forward to a "value" <br>
     * If the "key" is null, return the default fw value if defined <br>
     * If no key/value mappings: map directly, key -> value <br>
     * If no "value" is found for "key", return the default fw value if defined <br>
     * Returns <b>null</b> if there's no mapping
     * 
     * @param key
     * @param constraintValue
     *            to be compared with the value of the contrainedBy of the
     *            containing {@link Property} to determine which mapping to use
     *            The constraintValue can consist of multiple comma separated
     *            values, e.g. "Active,Resolved"
     * @return the mapped value or <b>null</b> if no mapping found
     */
    public List<String> getForward(String key, String constraintValue) {
    	// If null key use the default value if this is defined
        if (key == null) {
            if (forwardDefaultValue != null) {
            	return Arrays.asList(forwardDefaultValue);
            }
            else {
                return null;
            }
        }
    	
        if (forwardMap.keys().isEmpty()) {
            // no translation - this is a one-one mapping:
            return Arrays.asList(key);
        } else if (forwardMap.containsKey(key)) {
            Collection<Value> values = forwardMap.get(key);
            List<String> stringValues = new ArrayList<>();
            for (Value v : values) {
                if (constraintValue == null) {
                    stringValues.add(v.getValue());
                } else {
                    if (v.matches(constraintValue)) {
                        stringValues.add(v.getValue());
                    }
                }
            }
            return stringValues;
        } else {
          if (forwardDefaultValue != null) {
              return Arrays.asList(forwardDefaultValue);
          }
        }
        return null; // mapping failed
    }

    /**
     * As {@link BiDirectionalMap#getForward(String)}
     * but in the inverse direction
     * Returns <b>null</b> if there's no mapping
     * @param key
     * @param constraintValue
     *            to be compared with the value of the contrainedBy of the
     *            containing {@link Property} to determine which mapping to use
     * @return the mapped value or <b>null</b> if no mapping found
     */
    public List<String> getInverse(String key, String constraintValue) {
    	// If null key use the default value if this is defined
        if (key == null) {
            if (inverseDefaultValue != null) {
            	return Arrays.asList(inverseDefaultValue);
            }
            else {
                return null;
            }
        }
        
        if (inverseMap.keys().isEmpty()) {
            // no translation - this is a one-one mapping:
            return Arrays.asList(key);
        } else if (inverseMap.containsKey(key)) {
            Collection<Value> values = inverseMap.get(key);
            List<String> stringValues = new ArrayList<>();
            for (Value v : values) {
                if (constraintValue == null) {
                    stringValues.add(v.getValue());
                } else {
                    if (v.matches(constraintValue)) {
                        stringValues.add(v.getValue());
                    }
                }
            }
            return stringValues;
        } else {
            if (inverseDefaultValue != null) {
                return Arrays.asList(inverseDefaultValue);
            }
        }
        return null; // mapping failed
    }
    
    @Override
    public String toString() {
        return "Forward: " + forwardMap.toString() + ", Inverse: " + inverseMap.toString();
    }
    
    public void setForwardDefault(String value) {
        forwardDefaultValue = value;
    }

    public void setInverseDefault(String value) {
        inverseDefaultValue = value;
    }

    /**
     * True if both maps are empty - used to determine that a 1-1 mapping should
     * be done. 
     * NOTE: If a default value is set, either forward or inverse, then one-to-one
     * mapping is NOT done.
     * @return
     * 
     * This is currently use, thus marked as {@link Deprecated} and will be removed
     */
    @Deprecated
    public boolean mapOneToOne() {
        return forwardMap.isEmpty()
                && inverseMap.isEmpty()
                && forwardDefaultValue == null && inverseDefaultValue == null;
    }
    
}
