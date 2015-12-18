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

using Microsoft.TeamFoundation.Framework.Client;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Linq;
using TFSServerEventHandler.Mapping;

namespace TFSServerEventHandler
{
    public class AttributesMapper
    {
        private static AttributesMapper instance;

        public static AttributesMapper getInstance()
        {
            if (instance == null)
            {
                instance = new AttributesMapper();
            }
            return instance;
        }

        private MultiMap<Property> forwardMap;
        private MultiMap<Property> inverseMap;
        private MultiMap<Property> inverseMapTFSKey;
        private MultiMap<Property> notifyMap;

        public enum NodeType
        {
            DEFAULT,
            PROPERTY,
            MAP,
            USE,
            UNKNOWN
        }

        public List<Property> getNotifyProperties(String key)
        {
            if (notifyMap.ContainsKey(key))
            {
                return notifyMap[key];
            }
            else
            {
                return null;
            }
        }

        public List<Property> getInversePropertiesUsingTFSKey(String key)
        {
            if (inverseMapTFSKey.ContainsKey(key))
            {
                return inverseMapTFSKey[key];
            }
            else
            {
                return null;
            }
        }    

        public List<Property> getForwardProperties(String key) {
            // direction can be either FORWARD or BIDIRECTIONAL
            if (forwardMap.ContainsKey(key)) {
                List<Property> properties = forwardMap[key];
                Predicate<Property> filterInverse = FilterInverse;
                properties.RemoveAll(filterInverse);              
                return properties;
            }
            return null;
        }

        private bool FilterInverse(Property prop)
        {
            return prop.getDirection().Equals(Direction.INVERSE);
        }
    
        public List<Property> getInverseProperties(String key) {
            // direction can be either INVERSE or BIDIRECTIONAL
            if (inverseMap.ContainsKey(key)) {
                List<Property> properties = inverseMap[key];
                Predicate<Property> filterInverse = FilterForward;
                properties.RemoveAll(filterInverse);
                return properties;
            }
            return null;
        }

        private bool FilterForward(Property prop)
        {
            return prop.getDirection().Equals(Direction.FORWARD);
        }

        // Load attributes from the specified filename. If add is true we append mappings. 
        public bool Load(String fileName, bool add)
        {
            // NOTE: Error if not called once first with add = false.
            if (!add)
            {
                forwardMap = new MultiMap<Property>();
                inverseMap = new MultiMap<Property>();

                // TODO: Adding a map to support case when looking up inverse based on TFS key
                // Should this be needed? Also for forward case?
                inverseMapTFSKey = new MultiMap<Property>();

                notifyMap = new MultiMap<Property>();
            }

            try
            {
                XDocument doc = XDocument.Load(fileName);
                XElement mapping = doc.Element("mapping");

                // Loop over all entries and add to the dictionary 
                foreach (XElement propEntry in mapping.Elements("property"))
                {
                    // In TFS consumer case the serviceProvider (i.e. TFS Project) info is not relevant here
                    // as the workitem (Bug) is in a project context. The serviceProvider info is used when
                    // creating a Bug without the TFS context i.e. through the TFS OSLC Provider.

                    Property property = processProperty(propEntry);

                    foreach (XElement child in propEntry.Elements())
                    {
                        switch (child.Name.LocalName)
                        {
                            case "default":
                                processDefault(child, property);
                                break;
                            case "depends":
                                processDepends(child, property);
                                break;
                            case "map":
                                processMap(child, property);
                                break;
                            case "use":
                                processUse(child, property);
                                break;
                            default:
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Error when reading mapping file - assume fatal
                HandlerSettings.LogMessage(
                    "Error when reading the attribute mapping file: " + fileName +
                    "\nError: " + ex.Message +
                    "\nStack: " + ex.StackTrace,
                    HandlerSettings.LoggingLevel.ERROR);
                return false;
            }

            return true;
        }

        private Property processProperty(XElement node)
        {
            String key = node.Attribute("key").Value;
            String value = node.Attribute("value").Value;

            // If defined as "true" changes to the attribute will be propagated
            XAttribute notifyChangeAttr = node.Attribute("notifyChange");
            bool notifyChange = notifyChangeAttr != null ?
                notifyChangeAttr.Value.ToString().Equals("true") :
                false;

            // Set direction - if both forward and inverse set (odd) then bidirectional
            XAttribute fwAttr = node.Attribute("forward");
            XAttribute invAttr = node.Attribute("inverse");
            Direction direction = Direction.BIDIRECTIONAL;
            direction = fwAttr != null && invAttr == null ? Direction.FORWARD : direction;
            direction = invAttr != null && fwAttr == null ? Direction.INVERSE : direction;

            XAttribute fwConstrainAttr = node.Attribute("forwardConstrainedBy");
            String fwConstrainedBy = fwConstrainAttr != null ? fwConstrainAttr.Value : "";

            XAttribute invConstrainAttr = node.Attribute("inverseConstrainedBy");
            String invConstrainedBy = invConstrainAttr != null ? invConstrainAttr.Value : "";

            Property property = addProperty(
                key, value, direction, notifyChange, fwConstrainedBy, invConstrainedBy);

            // If notifyChange and inverse mapping we should update clients when
            // a value is changed. Hence add to special map for this. And note that
            // the key is "value" i.e. the TFS field.
            if (notifyChange && property.getDirection() != Direction.FORWARD)
            {
                notifyMap.Add(value, property);
            }

            // Adding with TFS key
            if (property.getDirection() != Direction.FORWARD)
            {
                inverseMapTFSKey.Add(value, property);
            }

            return property;
        }

        private Property addProperty(String key, String value, Direction direction, bool notify,
            String fwConstrainedBy, String invConstrainedBy)
        {
            Property newProperty = new Property(key, value, direction, notify, fwConstrainedBy, invConstrainedBy);
            switch (direction)
            {
                case Direction.BIDIRECTIONAL:
                    forwardMap.Add(key, newProperty);
                    inverseMap.Add(key, newProperty);
                    break;
                case Direction.FORWARD:
                    forwardMap.Add(key, newProperty);
                    break;
                case Direction.INVERSE:
                    inverseMap.Add(key, newProperty);
                    break;
            }
            return newProperty;
        }

        private void processDefault(XElement node, Property parent)
        {
            String inverseValue = (node.Attribute("inverseValue") != null)?
                node.Attribute("inverseValue").Value : null;
            String forwardValue = (node.Attribute("forwardValue") != null) ?
                node.Attribute("forwardValue").Value : null;

            parent.setDefault(forwardValue, inverseValue);
        }

        private void processDepends(XElement node, Property parent)
        {
            String onAttr = node.Attribute("on").Value;
            if (onAttr != null)
            {            
                parent.addDependency(onAttr);
            }
        }

        private void processMap(XElement node, Property parent)
        {
            String key = node.Attribute("key").Value;
            String value = node.Attribute("value").Value;

            XAttribute fwAttr = node.Attribute("forward");
            XAttribute invAttr = node.Attribute("inverse");
            XAttribute fwWhenAttr = node.Attribute("forwardWhen");
            XAttribute invWhenAttr = node.Attribute("inverseWhen");

            // Set direction - if both forward and inverse set (odd) then bidirectional
            Direction direction = Direction.BIDIRECTIONAL;
            if ((fwAttr != null || fwWhenAttr != null) && invAttr == null && invWhenAttr == null)
            {
                direction = Direction.FORWARD;
            }
            else if ((invAttr != null || invWhenAttr != null) && fwAttr == null && fwWhenAttr == null)
            {
                direction = Direction.INVERSE;
            }

            String fwConstraintValue = fwWhenAttr != null ? fwWhenAttr.Value : null;
            String invConstraintValue = invWhenAttr != null ? invWhenAttr.Value : null;

            parent.addEntry(key, value, direction, fwConstraintValue, invConstraintValue);
        }

        private void processUse(XElement node, Property parent)
        {
            String name = node.Attribute("name").Value;
            parent.setUseMapping(name);

            XAttribute keyAttr = node.Attribute("key");
            if (keyAttr != null)
            {
                parent.setUseKey(keyAttr.Value);
            }
        }

        // ===============================================

        static public String GetTfsValueForEcrKey(String propertyName, WorkItem workItem)
        {
            List<String> values = TFSMapper.getInstance().mapToEcr(propertyName, workItem);
            if (values.Count == 0)
            {
                HandlerSettings.LogMessage(
                    "Missing map entry for 'key': " + propertyName,
                    HandlerSettings.LoggingLevel.INFO);
                return "";
            }
            else if (values.Count > 1)
            {
                HandlerSettings.LogMessage(
                    "More than one value for 'key': " + propertyName,
                    HandlerSettings.LoggingLevel.WARN);
            }

            return values[0];
        }
    }
}
