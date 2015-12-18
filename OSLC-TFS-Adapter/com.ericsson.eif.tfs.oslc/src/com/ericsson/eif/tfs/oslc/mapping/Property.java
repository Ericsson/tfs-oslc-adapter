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
import java.util.Collection;
import java.util.List;

import com.ericsson.eif.tfs.oslc.mapping.BiDirectionalMap.Direction;

public class Property {
    
    private String key;
    private String value;
    private Direction direction;
    private boolean notifyChange;
    private String forwardConstrainedBy;
    private String inverseConstrainedBy;
    private BiDirectionalMap valueMap;
    private String useMapping;
    private String useKey;
    private List<String> dependencies;
    
    public Property(String key, String value, Direction direction,
            boolean notifyChange, String forwardConstrainedBy, String inverseConstrainedBy) {
        this.key = key;
        this.value = value;
        this.direction = direction;
        this.notifyChange = notifyChange;
        this.forwardConstrainedBy = forwardConstrainedBy;
        this.inverseConstrainedBy = inverseConstrainedBy;
        this.valueMap = new BiDirectionalMap();
        this.dependencies = new ArrayList<>();
    }
    
    /**
     * Adds a value to 
     * @param propertyKey
     * @param value
     * @param direction
     * @param forwardConstraintValue
     * @param inverseConstraintValue
     */
    public void addEntry(
            String value, 
            String to, 
            Direction direction,
            String forwardConstraintValue,
            String inverseConstraintValue) {
        valueMap.addEntry(value, to, direction, forwardConstraintValue, inverseConstraintValue);
    }

    public Collection<String> getForward(String key) {
        return valueMap.getForward(key, null);
    }
    
    public Collection<String> getForward(String key, String constraintValue) {
    	List<String> values = valueMap.getForward(key, constraintValue);
    	return replaceWildcardValues(values, key);
    }
    
    public Collection<String> getInverse(String key) {
        return valueMap.getInverse(key, null);
    }
    
    public Collection<String> getInverse(String key, String constraintValue) {
    	List<String> values = valueMap.getInverse(key, constraintValue);
    	return replaceWildcardValues(values, key);
    }
    
    // Semantics of value "*" from map is that any value passed in is allowed.
    // So replace any "*" entries from mapping with the key (now value) passed in.
    private List<String> replaceWildcardValues(List<String> values, String value) {
    	if (values != null) {
    		for (int i = 0; i < values.size(); i++) {
    			if (values.get(i).equals("*")) {
    				values.set(i, value);
    			}
			}
    	}
    	return values;   	
    }
    
    @Override
    public String toString() {
        return "[" + key + " => " + value + 
                " (" + direction + ", " + notifyChange + 
                (forwardConstrainedBy == null? "" : ", forwardConstrainedBy=" + forwardConstrainedBy) +
                (inverseConstrainedBy == null? "" : ", inverseConstrainedBy=" + inverseConstrainedBy) +
                ") - valueMap: " + valueMap + "]";
    }
    
    public String getKey() {
        return key;
    }

    public String getValue() {
        return value;
    }
    
    public Direction getDirection() {
        return direction;
    }
    
    public boolean isNotifyChange() {
        return notifyChange;
    }
    
    public String getForwardConstrainedBy() {
        return forwardConstrainedBy;
    }
    
    public String getInverseConstrainedBy() {
        return inverseConstrainedBy;
    }
    
    public String getUseMapping() {
        return useMapping;
    }
    
    public String getUseKey() {
        return useKey;
    }
    
    public List<String> getDependencies() {
        return dependencies;
    }

    /**
     * Set the default values
     * @param forward default value if != null
     * @param inverse default value if != null
     */
    public void setDefault(String forward, String inverse) {
    	if (forward != null) {
    		valueMap.setForwardDefault(forward);
    	}
    	if (inverse != null) {
    		valueMap.setInverseDefault(inverse);
    	}
    }

    public void setUseMapping(String mapping) {
        this.useMapping = mapping;
    }
    
    public void setUseKey(String useKey) {
        this.useKey = useKey;
    }
    
    public void setDependencies(List<String> dependencies) {
        this.dependencies = dependencies;
    }
    
    public void addDependency(String dependency) {
        this.dependencies.add(dependency);
    }
}
