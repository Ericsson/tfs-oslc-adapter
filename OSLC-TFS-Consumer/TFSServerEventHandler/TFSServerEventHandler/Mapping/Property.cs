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

using Microsoft.TeamFoundation.WorkItemTracking.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TFSServerEventHandler.Mapping
{
    public class Property {
    
        private String key;
        private String value;
        private Direction direction;
        private bool notifyChange;
        private String fwConstrainedBy;
        private String invConstrainedBy;
        private BiDirectionalMap valueMap;
        private String useMapping;
        private String useKey;
        private List<String> dependencies;

        public Property(String key, String value, Direction direction, bool notifyChange,
            String fwConstrainedBy, String invConstrainedBy)
        {
            this.key = key;
            this.value = value;
            this.direction = direction;
            this.notifyChange = notifyChange;
            this.fwConstrainedBy = fwConstrainedBy;
            this.invConstrainedBy = invConstrainedBy;

            this.valueMap = new BiDirectionalMap();
            this.dependencies = new List<String>();
        }
    
        public void addEntry(
                String value, 
                String to, 
                Direction direction,
                String fwConstrainedBy,
                String invConstrainedBy)
        {
            valueMap.addEntry(value, to, direction, fwConstrainedBy, invConstrainedBy);
        }

        public List<String> getForward(String key, WorkItem workItem)
        {
            List<String> values;
            if (fwConstrainedBy != null && fwConstrainedBy.Length > 0)
            {
                if (workItem.Fields.Contains(fwConstrainedBy))
                {
                    String constraintValue = workItem.Fields[fwConstrainedBy].Value.ToString();
                    values = valueMap.getForward(key, constraintValue);
                    return replaceWildcardValues(values, key);
                }
            }

            values = valueMap.getForward(key, null);
            return replaceWildcardValues(values, key);
        }

        public List<String> getInverse(String key, WorkItem workItem)
        {
            List<String> values;
            if (invConstrainedBy != null && invConstrainedBy.Length > 0)
            {
                if (workItem.Fields.Contains(invConstrainedBy))
                {
                    String constraintValue = workItem.Fields[invConstrainedBy].Value.ToString();
                    values = valueMap.getInverse(key, constraintValue);
                    return replaceWildcardValues(values, key);
                }
            }

            values = valueMap.getInverse(key, null);
            return replaceWildcardValues(values, key);
        }

        // Semantics of value "*" from map is that any value passed in is allowed.
        // So replace any "*" entries from mapping with the key (now value) passed in.
        private List<String> replaceWildcardValues(List<String> values, String value)
        {
            if (values != null)
            {
                for (int i = 0; i < values.Count; i++)
                {
                    if (values[i].Equals("*"))
                    {
                        values[i] = value;
                    }
                }
            }
            return values;
        }

        override public String ToString()
        {
            return "[" + key + " => " + value + 
                    " (" + direction + ", " + notifyChange + ") - valueMap: " + valueMap + "]";
        }
    
        public String getKey()
        {
            return key;
        }

        public String getValue()
        {
            return value;
        }
    
        public Direction getDirection()
        {
            return direction;
        }

        public bool getNotifyChange()
        {
            return notifyChange;
        }
    
        public String getUseMapping()
        {
            return useMapping;
        }

        public String getUseKey()
        {
            return useKey;
        }

        public List<String> getDependencies()
        {
            return dependencies;
        }

        public void setDefault(String forward, String inverse)
        {
            valueMap.setForwardDefault(forward);
            valueMap.setInverseDefault(inverse);
        }

        public void setUseMapping(String mapping)
        {
            this.useMapping = mapping;
        }

        public void setUseKey(String useKey)
        {
            this.useKey = useKey;
        }

        public void setDependencies(List<String> dependencies)
        {
            this.dependencies = dependencies;
        }

        public void addDependency(String dependency)
        {
            this.dependencies.Add(dependency);
        }  
    }
}
