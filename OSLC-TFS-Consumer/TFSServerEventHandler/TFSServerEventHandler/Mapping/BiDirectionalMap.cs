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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TFSServerEventHandler.Mapping
{

    public enum Direction
    {
        FORWARD, INVERSE, BIDIRECTIONAL
    }

    class BiDirectionalMap
    {
        class Value
        {
            private String value;
            private String constraintValue;

            public Value(String value, String constraintValue)
            {
                this.value = value;
                this.constraintValue = constraintValue;
            }

            public String getValue()
            {
                return value;
            }

            public bool constraintMatches(String parameter)
            {
                if (parameter == null || constraintValue == null)
                {
                    return true;
                }

                String[] constraintValues = constraintValue.Split(',');
                return constraintValues.Contains(parameter);
            }
        }

        private MultiMap<Value> forwardMap;
        private MultiMap<Value> inverseMap;
        private String forwardDefaultValue;
        private String inverseDefaultValue;

        public BiDirectionalMap()
        {
            forwardMap = new MultiMap<Value>();
            inverseMap = new MultiMap<Value>();
        }

        // Adds an entry to the map. If the key is empty, the value is used
        // as default for the given direction
        public void addEntry(String key, String value, Direction direction,
            String fwConstrainedBy, String invConstrainedBy)
        {
            switch (direction)
            {
                case Direction.FORWARD:
                    Value forwardValue = new Value(value, fwConstrainedBy);
                    forwardMap.Add(key, forwardValue);
                    break;
                case Direction.INVERSE:
                    Value inverseValue = new Value(key, invConstrainedBy);
                    inverseMap.Add(value, inverseValue);
                    break;
                case Direction.BIDIRECTIONAL:
                default:
                    Value forwardValue1 = new Value(value, fwConstrainedBy);
                    Value inverseValue1 = new Value(key, invConstrainedBy);
                    forwardMap.Add(key, forwardValue1);
                    inverseMap.Add(value, inverseValue1);
                    break;
            }
        }

        // Clears the map - intended to be used when reloading from file
        public void clear()
        {
            forwardMap = new MultiMap<Value>();
            inverseMap = new MultiMap<Value>();
        }

        public List<String> getForward(String key, String constraintValue)
        {
            return getBasic(key, constraintValue, forwardDefaultValue, forwardMap);
        }

        public List<String> getInverse(String key, String constraintValue)
        {
            return getBasic(key, constraintValue, inverseDefaultValue, inverseMap);
        }

        private List<String> getBasic(String key, String constraintValue, String defaultValue, MultiMap<Value> map)
        {
            List<String> mappedValues = new List<String>();

            // If null key use the default value if this is defined
            if (key == null)
            {
                if (defaultValue != null)
                {
                    mappedValues.Add(defaultValue);
                    return mappedValues;
                }
                else
                {
                    return null;
                }
            }

            if (map.IsEmpty())
            {
                // no translation - this is a one-one mapping:
                mappedValues.Add(key);
                return mappedValues;
            }
            else if (map.ContainsKey(key))
            {
                List<Value> values = map[key];
                List<String> stringValues = new List<String>();
                foreach (Value v in values)
                {
                    if (v.constraintMatches(constraintValue))
                    {
                        stringValues.Add(v.getValue());
                    }
                }
                return stringValues;
            }
            else if (defaultValue != null)
            {
                mappedValues.Add(defaultValue);
                return mappedValues;
            }

            return null; // mapping failed
        }

        public void setForwardDefault(String value)
        {
            forwardDefaultValue = value;
        }

        public void setInverseDefault(String value)
        {
            inverseDefaultValue = value;
        }
    }
}
