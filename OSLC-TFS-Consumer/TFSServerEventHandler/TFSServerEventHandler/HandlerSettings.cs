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
using System.Net;
using Microsoft.TeamFoundation.Framework.Client;
using Microsoft.TeamFoundation.Framework.Common;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using System.IO;
using System.Reflection;
using System.Xml.Linq;
using System.Diagnostics;
using System.Security;
using TFSServerEventHandler.OAuth;
using System.Net.Http;
using System.Xml;
using Microsoft.TeamFoundation.WorkItemTracking.Server;
using TFSServerEventHandler.Mapping;
using Microsoft.TeamFoundation.Server;
using System.Threading;

namespace TFSServerEventHandler
{
    public static class HandlerSettings
    {
        // TODO: Implement similar as TFSAggregatorSettings i.e. logic to get settings from file etc
        // See http://stackoverflow.com/questions/4764680/how-to-get-the-location-of-the-dll-currently-executing
        // See http://tfsaggregator.codeplex.com/SourceControl/latest#Main/TFSAggregatorSettings.cs
        // For now - hardcoded settings for tests.

        // Adapter home directory
        private static String adapterHome;

        // Directory with file watchers
        private static Dictionary<ConfigFile, FileSystemWatcher> configFileWatchers =
            new Dictionary<ConfigFile, FileSystemWatcher>();
        private static Dictionary<FileSystemWatcher, DateTime> configFileChangeTimes =
            new Dictionary<FileSystemWatcher, DateTime>();

        // Flags for keeping track if init has run and if completed OK
        private static bool initiated = false;
        private static bool initiatedOK = false;

        // Credentials for access to TFS
        private static ICredentials credentials;

        private static String tfsUri;
        private static String clientUri;

        private static String tfsProviderUri;
        private static String tfsProviderUser;

        private static String creationFactoryUri;
        private static String rootservicesUri;

        // Url pattern for the path part of the about url for an item
        // The full uri = clientUri + baseAccessUri and with [id] replaced with id from item.
        private static String baseAccessUri;

        // Mapping files
        private static String attributeMappingFile;
        private static String customerMappingFile;
        private static String productMappingFile;

        // OAuth
        private static Dictionary<String, FriendInfo> friends;

        // REST call Headers
        private static Dictionary<String, String> headers;
        private static String headerUserKey;

        // Logging
        private static Boolean logToEventLog;
        private static LoggingLevel logToEventLevel = LoggingLevel.ERROR;
        private const String EventLogSource = "TFS EventHandler";
        private const String EventLogApp = "Application";

        private static Boolean logToFileLog;
        private static String logToFileLocation;
        private static LoggingLevel logToFileLevel = LoggingLevel.ERROR;
        private static TextWriterTraceListener logListener;

        // Sync status messages
        private static String noSyncMessage;
        private static String successMessage;
        private static String warningMessage;
        private static String errorMessage;

        // Logging level where NONE is least verbose (...) and INFO most.
        // Inclusive of previous level, i.e. all ERROR are included in WARN etc.
        public enum LoggingLevel
        {
            NONE,
            ERROR,
            WARN,
            DEBUG,
            INFO
        }

        private enum ConfigFile
        {
            PROPERTIES,
            ATTRIBUTE_MAPPINGS,
            PRODUCT_MAPPINGS,
            CUSTOMER_MAPPINGS
        }

        /// <summary>
        /// Initiatie application - if fatal error, return false.
        /// </summary>
        static public Boolean initFromFile() {
            if (initiated)
            {
                return initiatedOK;
            }
            initiated = true;
            initiatedOK = false;

            // =====================================================================
            // Get adapterHome and config file - fatal error if not found, report and exit

            adapterHome = Environment.GetEnvironmentVariable("ADAPTER_HOME_TFS");
            if (adapterHome == null || !Directory.Exists(adapterHome))
            {
                // TODO: Is there a reasonable deafult to use here? For now; C:\ + "tfs_adapter".
                // Note: Can't use the user home as the TFS process normally can't read there
                adapterHome = "C:\\adapter_home_tfs";
            }
            if (!Directory.Exists(adapterHome))
            {
                throw new DirectoryNotFoundException(
                    String.Format("Failed to find adapter home at: {0}. Exit.", adapterHome));
            }

            // Note: Using the recommended file construct Path.Combine(adapterHome + "properties.xml")
            // resulted in "C:\\adapter_home_tfsproperties.xml" i.e. not correct. So resort to basic.
            String configFile = adapterHome + "\\properties.xml";

            // Handle re-read of files without need of server reboot. So if missing file is logged, if
            // adding a file at that location it should be picked up.
            handleConfigFileChange(ConfigFile.PROPERTIES, configFile);

            if (!File.Exists(configFile))
            {
                throw new FileNotFoundException(
                    String.Format("Failed to find properties.xml at: {0}. Exit.", configFile));
            }


            // =====================================================================
            // Read the configuration file

            XElement config = null;
            try
            {
                XDocument doc = XDocument.Load(configFile);
                config = doc.Element("configuration");
            }
            catch (Exception e)
            {
                throw new Exception(
                        String.Format("Exception when loading property file {0}. ", configFile), e);   
            }

            // =====================================================================
            // Initiate logging

            // For logging, log4net is recommended, available e.g. at http://www.nuget.org/packages/log4net/2.0.3
            // BUT introduces another dependency, and found no clear examples of using this on server side. So
            // will use a simpler approach for now.

            initLogging(config, adapterHome);

            LogMessage(String.Format("Reading properties from file: {0}", configFile), LoggingLevel.INFO);

            // =====================================================================
            // Get connection settings

            initiatedOK = initConnectionSettings(config);
            if (!initiatedOK)
            {
                return initiatedOK;
            }

            // =====================================================================
            // Read list of Friends

            initiatedOK = initFriends(config);
            if (!initiatedOK)
            {
                return initiatedOK;
            }

            // =====================================================================
            // Read list of Headers for REST call

            initiatedOK = initHeaders(config);
            if (!initiatedOK)
            {
                return initiatedOK;
            }

            // =====================================================================
            // Configure mapping - if mapping not found, fatal error

            initiatedOK = TFSMapper.initTFSFieldNames(config);
            if (!initiatedOK)
            {
                return initiatedOK;
            }

            initiatedOK = initMapping(config, adapterHome);
            if (!initiatedOK)
            {
                return initiatedOK;
            }

            // ======================================
            // Read optional sync status messages

            XElement noSyncMessageAttr = config.Element("noSyncMessage");
            if (noSyncMessageAttr != null) {
                noSyncMessage = noSyncMessageAttr.Value;
            }
            XElement successMessageAttr = config.Element("successMessage");
            if (successMessageAttr != null) {
                successMessage = successMessageAttr.Value;
            }
            XElement warningMessageAttr = config.Element("warningMessage");
            if (warningMessageAttr != null) {
                warningMessage = warningMessageAttr.Value;
            }
            XElement errorMessageAttr = config.Element("errorMessage");
            if (errorMessageAttr != null) {
                errorMessage = errorMessageAttr.Value;
            }
            LogMessage("Read optional sync status messages.", LoggingLevel.INFO);

            LogMessage(
                String.Format("Read of properties from file {0} successful.", configFile),
                LoggingLevel.INFO);

            initiatedOK = true;
            return initiatedOK;
        }

        private static bool initMapping(XElement config, String adapterHome)
        {
            try
            {
                attributeMappingFile = config.Element("attributeMappingFile").Attribute("name").Value;
                attributeMappingFile = replaceRelativePath(adapterHome, attributeMappingFile);
                handleConfigFileChange(ConfigFile.ATTRIBUTE_MAPPINGS, attributeMappingFile);
                if (!File.Exists(attributeMappingFile))
                {
                    LogMessage(
                        String.Format("Failed to find attribute mapping file at: {0}. Exit.", attributeMappingFile),
                        LoggingLevel.ERROR);
                    return false;
                }
                else if (!AttributesMapper.getInstance().Load(attributeMappingFile, false))
                {
                    // Logging of error in Load method
                    return false;
                }
                LogMessage("Attribute mapping file read: " + attributeMappingFile, LoggingLevel.INFO);

                customerMappingFile = config.Element("customerMappingFile").Attribute("name").Value;
                customerMappingFile = replaceRelativePath(adapterHome, customerMappingFile);
                handleConfigFileChange(ConfigFile.CUSTOMER_MAPPINGS, customerMappingFile);
                if (!File.Exists(customerMappingFile))
                {
                    LogMessage(
                        String.Format("Failed to find customer mapping file at: {0}. Exit.", customerMappingFile),
                        LoggingLevel.ERROR);
                    return false;
                }
                else if (!AttributesMapper.getInstance().Load(customerMappingFile, true))
                {
                    // Logging of error in Load method
                    return false;
                }
                LogMessage("Customer mapping file read: " + customerMappingFile, LoggingLevel.INFO);

                productMappingFile = config.Element("productMappingFile").Attribute("name").Value;
                productMappingFile = replaceRelativePath(adapterHome, productMappingFile);
                handleConfigFileChange(ConfigFile.PRODUCT_MAPPINGS, productMappingFile);
                if (!File.Exists(productMappingFile))
                {
                    LogMessage(
                        String.Format("Failed to find product mapping file at: {0}. Exit.", productMappingFile),
                        LoggingLevel.ERROR);
                    return false;
                }
                else if (!ProductMapper.getInstance().Load(productMappingFile))
                {
                    // Logging of error in Load method
                    return false;
                }
                LogMessage("Product mapping file read: " + productMappingFile, LoggingLevel.INFO);
            }
            catch (Exception e)
            {
                LogMessage(
                    String.Format("Exception when loading a mapping file: {0}. Exit.", e.Message),
                    LoggingLevel.ERROR);
                return false;
            }

            return true;
        }

        private static void initLogging(XElement config, String adapterHome)
        {
            logToEventLog = true;
            String readProperties = "";
            try
            {
                // Get settings for Event logging
                logToEventLog = Boolean.Parse(config.Element("logToEventLog").Attribute("enabled").Value);
                logToEventLevel = (LoggingLevel)Enum.Parse(
                    typeof(LoggingLevel),
                    config.Element("logToEventLog").Attribute("level").Value,
                    true);
                readProperties += "\n\tlogToEventLog: enabled=" + logToEventLog + " level=" + logToEventLevel;

                // When EventLogSource was not defined - I got SecurityException as application tried
                // accessing the Security log in search of EventLogSource. When manually adding the key
                // it's found and is working fine.

                if (logToEventLog)
                {
                    if (!EventLog.SourceExists(HandlerSettings.EventLogSource))
                    {
                        try
                        {
                            // Note: Need admin permission to create entry
                            EventLog.CreateEventSource(
                                HandlerSettings.EventLogSource,
                                HandlerSettings.EventLogApp);
                        }
                        catch (Exception)
                        {
                            // Will log to Trace/Output - but not event log
                            LogMessage(
                                "Failed to initiate Event Log. No event log entries will be created.",
                                LoggingLevel.WARN);
                            logToEventLog = false;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                LogMessage(
                    String.Format(
                        "Exception on init event log or getting a logging property: {0}." +
                        "\nProperties read before exception: {1}", e.Message, readProperties),
                    LoggingLevel.WARN);
            }

            logToFileLog = true;
            try
            {
                // Get settings for file logging
                logToFileLog = Boolean.Parse(config.Element("logToFile").Attribute("enabled").Value);
                logToFileLevel = (LoggingLevel)Enum.Parse(
                    typeof(LoggingLevel),
                    config.Element("logToFile").Attribute("level").Value,
                    true);
                logToFileLocation = config.Element("logToFile").Attribute("location").Value;
                logToFileLocation = replaceRelativePath(adapterHome, logToFileLocation);
                readProperties += "\n\tlogToFileLog: enabled=" + logToFileLog +
                    " level=" + logToFileLevel +
                    " location=" + logToFileLocation;

                // Configure file logging 
                Directory.CreateDirectory(logToFileLocation);

                // If removing logToFileLog 
                if (!logToFileLog && logListener != null)
                {
                    Trace.Listeners.Remove(logListener);
                    logListener.Close();
                    logListener.Dispose();
                    logListener = null;
                }
            }
            catch (Exception e)
            {
                LogMessage(
                    String.Format(
                        "Exception on init file log or getting a logging property: {0}." +
                        "\nProperties read before exception: {1}", e.Message, readProperties),
                    LoggingLevel.WARN);
            }

            LogMessage(
                    String.Format("Logging properties read: {0}", readProperties),
                    LoggingLevel.INFO);
        }

        // Read connection settings. Report if fatal error or not.
        private static bool initConnectionSettings(XElement config)
        {
            String readProperties = "";
            try
            {
                tfsUri = config.Element("tfsUri").Value;
                readProperties += "\n\ttfsUri: " + tfsUri;

                String user = config.Element("user").Value;
                readProperties += "\n\tuser: " + user;
                String pw = config.Element("password").Value;
                credentials = new System.Net.NetworkCredential(user, pw);
                readProperties += "\n\tpw: ***";

                tfsProviderUri = config.Element("tfsProviderUri").Value;
                readProperties += "\n\ttfsProviderUri: " + tfsProviderUri;
                tfsProviderUser = config.Element("tfsProviderUser").Value;
                readProperties += "\n\ttfsProviderUser: " + tfsProviderUser;

                clientUri = config.Element("clientUri").Value;
                readProperties += "\n\tclientUri: " + clientUri;
                rootservicesUri = config.Element("rootservicesUri").Value;
                readProperties += "\n\trootservicesUri: " + rootservicesUri;
                baseAccessUri = config.Element("baseAccessUri").Value;
                readProperties += "\n\tbaseAccessUri: " + baseAccessUri;
            }
            catch (Exception e)
            {
                LogMessage(
                    String.Format(
                        "Exception when getting a connection property: {0}. Exit." +
                        "\nProperties read before exception: {1}", e.Message, readProperties),
                    LoggingLevel.ERROR);
                return false;
            }

            LogMessage(
                    String.Format("Connection properties read: {0}", readProperties),
                    LoggingLevel.INFO);

            return true;
        }

        // Read friend settings. Report if fatal error or not.
        private static bool initFriends(XElement config)
        {
            String readProperties = "";
            try
            {
                friends = new Dictionary<string, FriendInfo>();
                XElement friendsEntry = config.Element("friends");
                if (friendsEntry == null) {
                    LogMessage(
                        "No 'friends' properties found - needed for connection. Exit.",
                        LoggingLevel.ERROR);
                    return false;
                }

                foreach (XElement friendEntry in friendsEntry.Elements("friend"))
                {
                    String name = friendEntry.Attribute("name").Value.ToLower();
                    FriendInfo fi = new FriendInfo(name);
                    friends.Add(name, fi);
                    readProperties += "\n\tfriend: " + name;

                    // Set what access method to use
                    String useAccess = friendEntry.Attribute("useAccess").Value.ToLower();
                    try
                    {
                        FriendInfo.UseAccessType value = (FriendInfo.UseAccessType)
                            Enum.Parse(typeof(FriendInfo.UseAccessType), useAccess);
                        fi.UseAccess = value;
                    }
                    catch (ArgumentException)
                    {
                        // Default to basic
                        fi.UseAccess = FriendInfo.UseAccessType.basic;
                    }
                    readProperties += "\n\t\tuseAccess: " + useAccess;

                    XElement oauthEntry = friendEntry.Element("oauth");
                    if (oauthEntry != null)
                    {
                        fi.ConsumerKey = oauthEntry.Element("consumerKey").Value;
                        fi.ConsumerSecret = oauthEntry.Element("consumerSecret").Value;
                        fi.AccessToken = oauthEntry.Element("accessToken").Value;
                        fi.AccessTokenSecret = oauthEntry.Element("accessTokenSecret").Value;

                        readProperties += "\n\t\toauth: " +
                            "\n\t\t\tconsumerKey: " + fi.ConsumerKey +
                             "\n\t\t\tconsumerSecret: ***" +
                             "\n\t\t\taccessToken: " + fi.AccessToken +
                             "\n\t\t\taccessTokenSecret: ***";
                    }

                    XElement basicEntry = friendEntry.Element("basic");
                    if (basicEntry != null)
                    {
                        readProperties += "\n\t\tbasic:";
                        XElement userAttr = basicEntry.Element("user");
                        XElement passwordAttr = basicEntry.Element("password");
                        XElement encodedCredentialsAttr = basicEntry.Element("encodedCredentials");
                        if (userAttr != null && passwordAttr != null)
                        {
                            Byte[] byteArray = Encoding.ASCII.GetBytes(userAttr.Value + ":" + passwordAttr.Value);
                            fi.EncodedCredentials = Convert.ToBase64String(byteArray);
                            readProperties += "\n\t\t\tuser: " + userAttr.Value + "\n\t\t\tpw: ***";
                        }
                        else if (encodedCredentialsAttr != null)
                        {
                            fi.EncodedCredentials = encodedCredentialsAttr.Value;
                            readProperties += "\n\t\t\tencodedCredentials: " + fi.EncodedCredentials;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                LogMessage(
                    String.Format(
                        "Exception when getting a friend property: {0}. Exit." +
                        "\nFriend properties read before exception: {1}", e.Message, readProperties),
                    LoggingLevel.ERROR);
                return false;
            }

            LogMessage("Friend properties read: " + readProperties,
                    LoggingLevel.INFO);

            return true;
        }

        private static bool initHeaders(XElement config)
        {
            String readProperties = "";
            try
            {
                headers = new Dictionary<String, String>();
                XElement headerEntries = config.Element("headers");
                if (headerEntries == null)
                {
                    LogMessage(
                        "No 'headers' properties found - needed for connection. Exit.",
                        LoggingLevel.ERROR);
                    return false;
                }

                foreach (XElement headerEntry in headerEntries.Elements("header"))
                {
                    String key = headerEntry.Attribute("key").Value.ToLower();
                    String value = headerEntry.Attribute("value").Value.ToLower();
                    if (value.Equals("[tfs_user]"))
                    {
                        headerUserKey = key;
                    }
                    headers.Add(key, value);
                    readProperties += "\n\theader key: " + key + " value: " + value;
                }
            }
            catch (Exception e)
            {
                LogMessage(
                    String.Format(
                        "Exception when getting a header property: {0}. Exit." +
                        "\nHeader properties read before exception: {1}", e.Message, readProperties),
                    LoggingLevel.ERROR);
                return false;
            }

            LogMessage("Header properties read: " + readProperties,
                    LoggingLevel.INFO);

            return true;
        }

        private static String replaceRelativePath(String configFilePath, String propFile)
        {
            if (propFile.StartsWith("."))
            {
                return configFilePath + propFile.Substring(1);
            }
            else
            {
                return propFile;
            }
        }

        // Add watcher for config file change so if a config file is updated it's re-read
        // and the event handler is re-initialized with any cached objects released. 
        private static void handleConfigFileChange(ConfigFile file, String fileName)
        {
            if (configFileWatchers.ContainsKey(file))
            {
                String watchedFileDir = configFileWatchers[file].Path;
                String watchedFileName = configFileWatchers[file].Filter;
                String watchedFile = Path.Combine(watchedFileDir, watchedFileName);
                
                if (fileName.Equals(watchedFile, StringComparison.OrdinalIgnoreCase))
                {
                    // Already watching this
                    return;
                }
                else
                {
                    // Change the path and filter of the watched file
                    configFileWatchers[file].Path = Path.GetDirectoryName(fileName);
                    configFileWatchers[file].Filter = Path.GetFileName(fileName);
                    return;
                }
            }

            // Add a new watcher
            FileSystemWatcher watcher = new FileSystemWatcher();
            configFileWatchers.Add(file, watcher);

            watcher.Path = Path.GetDirectoryName(fileName);
            watcher.Filter = Path.GetFileName(fileName);
            watcher.NotifyFilter = NotifyFilters.LastWrite;
            watcher.Changed += new FileSystemEventHandler(OnChanged);

            // Begin watching.
            watcher.EnableRaisingEvents = true;
        }

        // Define the event handlers. 
        private static void OnChanged(object source, FileSystemEventArgs e)
        {
            // The file is changed or being changed - lets make sure we can read before proceeding
            while (true)
            {
                try
                {
                    using (Stream stream = System.IO.File.Open(
                        e.FullPath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
                    {
                        if (stream != null)
                        {
                            LogMessage(
                                String.Format("File {0} changed, will re-read.", e.FullPath),
                                LoggingLevel.INFO);
                            break;
                        }
                    }
                }
                catch (FileNotFoundException ex)
                {
                    LogMessage(
                        String.Format("File {0} not ready for re-read yet: {1}", e.FullPath, ex.Message),
                        LoggingLevel.INFO);
                }
                catch (IOException ex)
                {
                    LogMessage(
                        String.Format("File {0} not ready for re-read yet: {1}", e.FullPath, ex.Message),
                        LoggingLevel.INFO);
                }
                catch (UnauthorizedAccessException ex)
                {
                    LogMessage(
                        String.Format("File {0} not ready for re-read yet: {1}", e.FullPath, ex.Message),
                        LoggingLevel.INFO);
                }
                Thread.Sleep(500);
            }

            try
            {
                FileSystemWatcher watcher = (FileSystemWatcher)source;
                if (configFileChangeTimes.ContainsKey(watcher))
                {
                    DateTime lastKnownChange = configFileChangeTimes[watcher];
                    DateTime lastChange = File.GetLastWriteTime(e.FullPath);
                    if (lastKnownChange == lastChange)
                    {
                        // Event originating because of same change - ignore
                        return;
                    }
                }

                // Set new last file write time
                configFileChangeTimes[watcher] = File.GetLastWriteTime(e.FullPath);

                // Clear cached values
                WorkItemChangedEventHandler.clearConnection();
                RESTCallHandler.clearHandler();

                // Re-initialize by re-reading all config files
                initiated = false;
                initFromFile();
            }
            catch (Exception ex)
            {
                LogMessage(
                        String.Format("Failed to re-read the property file: {1}", ex.Message),
                        LoggingLevel.ERROR);
            }
        }

        // =================================================================
        // Simple logging

        public static void LogMessage(String message, LoggingLevel level)
        {
            // Write to log if the message is of equal or lower verboseness 
            // (0 lowest = NONE) than what is specified in the configuration file. 
            if (logToEventLog && level <= logToEventLevel)
            {
                LogToEventLog(message, level);
            }

            if (logToFileLog && level <= logToFileLevel)
            {
                LogToFileLog(message, level);
            }
        }

        private static void LogToEventLog(String message, LoggingLevel level)
        {
            try
            {
                String logMessage = EventLogSource + ": " + message;
                switch (level)
                {
                    case LoggingLevel.ERROR:
                        EventLog.WriteEntry(EventLogSource, logMessage, EventLogEntryType.Error);
                        break;
                    case LoggingLevel.WARN:
                        EventLog.WriteEntry(EventLogSource, logMessage, EventLogEntryType.Warning);
                        break;
                    case LoggingLevel.DEBUG:
                    case LoggingLevel.INFO:
                        EventLog.WriteEntry(EventLogSource, logMessage, EventLogEntryType.Information);
                        break;
                }
            } catch (Exception)
            {
                // Logging should not throw exception
            }
        }

        private static void LogToFileLog(String message, LoggingLevel level)
        {
            try
            {
                UpdateLogFile();

                // Write to file and Output (Default listener) if Debug
                String dateAndTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                String logMessage = String.Format("{0} [{1}] {2}", dateAndTime, level.ToString(), message);

                if (System.Diagnostics.Debugger.IsAttached)
                {
                    Trace.WriteLine(logMessage);
                }
                if (logListener != null)
                {
                    logListener.WriteLine(logMessage);
                    logListener.Flush();
                }
            }
            catch (Exception)
            {
                // Logging should not throw exception
            }
        }

        private static void UpdateLogFile()
        {
            String fileName = logToFileLocation + "\\tfs_oslc_consumer.log";
            if (File.Exists(fileName))
            {
                String modDate = File.GetLastWriteTime(fileName).ToString("yyyyMMdd");
                String date = DateTime.Now.ToString("yyyyMMdd");
                if (!modDate.Equals(date))
                {
                    if (logListener != null)
                    {
                        logListener.Close();
                        logListener.Dispose();
                        logListener = null;
                    }

                    String oldLogFile = logToFileLocation + "\\tfs_oslc_consumer_" + modDate + ".log";
                    File.Move(fileName, oldLogFile);
                }
            }

            if (logListener == null)
            {
                FileStream traceLog = new FileStream(fileName, FileMode.Append);
                logListener = new TextWriterTraceListener(traceLog, EventLogSource);
            }
        }

        // The content is written async - hence declare as async task
        public static async Task LogMessage(String message, ObjectContent content, String url, LoggingLevel level)
        {
            if ((logToEventLog && level <= logToEventLevel) || (logToFileLog && level <= logToFileLevel))
            {
                MemoryStream stream = new MemoryStream();
                await content.CopyToAsync(stream);
                stream.Position = 0;
                StreamReader reader = new StreamReader(stream);
                String contentString = reader.ReadToEnd();

                LogMessage(message + url + "\nContent:\n" + contentString, level);
            }
        }

        // =================================================================
        // Access to values from config file

        public static Dictionary<String, String> getRESTHeaders(String user)
        {
            if (headerUserKey != null && headers.ContainsKey(headerUserKey)) {
                headers[headerUserKey] = user;
            }
            return headers;
        }

        public static String SyncNoSyncMessage { get { initFromFile(); return noSyncMessage; } }
        public static String SyncSuccessMessage { get { initFromFile(); return successMessage; } }
        public static String SyncWarningMessage { get { initFromFile(); return warningMessage; } }
        public static String SyncErrorMessage { get { initFromFile(); return errorMessage; } }

        public static FriendInfo GetFriend(String name)
        {
            initFromFile();
            if (friends.ContainsKey(name.ToLower()))
            {
                return friends[name.ToLower()];
            }
            else
            {
                return null;
            }
        }

        // Return the MHWeb TR EriRef (e.g. TB12345) from a about uri
        public static String GetIDFromUri(String about)
        {
            int inx = about.LastIndexOf("/");
            if (inx != -1)
            {
                return about.Substring(inx + 1);
            }
            else
            {
                return "";
            }
        }

        // Return the MHWeb TR EriRef (e.g. TB12345) from the field in the BUG
        public static String GetIDFromLink(Microsoft.TeamFoundation.WorkItemTracking.Client.WorkItem workItem)
        {
            String linkField = workItem.Fields[TFSMapper.ERICSSON_DEFECT_LINK_FIELD].Value.ToString();
            if (linkField.Length == 0)
            {
                return "";
            }

            // New syntax, the value is the id 
            String id = linkField;

            // Old syntax where the LINK field had syntax /TREditWeb/faces/tredit/tredit.xhtml?eriref=[id]
            int inx = linkField.LastIndexOf("?eriref=");
            if (inx != -1)
            {
                id = linkField.Substring(inx + 8);
            }

            return id;
        }

        public static String GetUriForBug(String id)
        {
            if (id.Length == 0)
            {
                return "";
            }

            initFromFile();
            return tfsProviderUri.Replace("[id]", id);
        }

        public static String GetLinkUriForBug(String id)
        {
            if (id.Length == 0)
            {
                return "";
            }

            return id;
        }

        public static String GetUriForTR(String id)
        {
            if (id.Length == 0)
            {
                return "";
            }

            initFromFile();
            return clientUri + baseAccessUri.Replace("[id]", id);
        }

        /// <summary>
        /// The configuration directory from where settings are read
        /// </summary>
        public static String AdapterHome { get { initFromFile(); return adapterHome; } }

        /// <summary>
        /// The signum (no domain) of the functional user used to connect with TFS from the TFS OSLC Provider
        /// </summary>
        public static String TFSProviderUser { get { initFromFile(); return tfsProviderUser; } }

        /// <summary>
        /// The location of the tfs server that we are connecting to
        /// </summary>
        public static string TFSUri { get { initFromFile(); return tfsUri; } }

        /// <summary>
        /// The credentials for the TFS functional user
        /// </summary>
        static public ICredentials GetCredentials()
        {
            initFromFile();

            return credentials;
        }

        // Check if we should ignore event by the user with Guid changerUid.
        public static Boolean IgnoreEventByUser(TfsTeamProjectCollection tpc, String changerUid)
        {
            String userUid = tpc.AuthorizedIdentity.TeamFoundationId.ToString();
            return changerUid.Equals(userUid, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// The server that we are connecting to (i.e. MHWeb)
        /// </summary>
        public static string ClientUri { get { initFromFile(); return clientUri; } }

        /// <summary>
        /// The creation factory for the provider (i.e. MHWeb)
        /// </summary>
        public static string CreationFactoryUri { get { return creationFactoryUri; } set { creationFactoryUri = value; } }

        /// <summary>
        /// The rootservices for the provider (i.e. MHWeb)
        /// </summary>
        public static string RootservicesUri { get { initFromFile(); return clientUri + rootservicesUri; } }

        // ===============================================================
        // Utility methods - could be moved elsewhere

        // For call to create TR we need Ericsson user id (also domain?) - seems like we have what we want here.
        // See http://stackoverflow.com/questions/19911368/using-the-tfs-2012-api-how-do-i-get-the-email-address-of-a-user
        static public TeamFoundationIdentity GetSignumForAssignedTo(
            Microsoft.TeamFoundation.WorkItemTracking.Client.WorkItem workItem)
        {
            TfsTeamProjectCollection tpc = workItem.Store.TeamProjectCollection;
            String userName = workItem.Fields[TFSMapper.TFS_OWNER].Value.ToString();

            IIdentityManagementService mgmntService = tpc.GetService<IIdentityManagementService>();

            TeamFoundationIdentity member = mgmntService.ReadIdentity(
                    IdentitySearchFactor.DisplayName,
                    userName,
                    MembershipQuery.Direct,
                    ReadIdentityOptions.ExtendedProperties);

            if (member == null)
            {
                HandlerSettings.LogMessage(
                String.Format("Failed to get user identity for user: {0}", userName),
                HandlerSettings.LoggingLevel.WARN);
            } 

            return member;
        }

        // For call to create TR we need Ericsson user id (also domain?) - seems like we have what we want here.
        // TDOD: Try and make sure we are getting the correct attribute.
        // See http://stackoverflow.com/questions/19911368/using-the-tfs-2012-api-how-do-i-get-the-email-address-of-a-user
        static public String GetSignumForChangeNotification(
            Microsoft.TeamFoundation.WorkItemTracking.Client.WorkItem workItem,
            WorkItemChangedEvent notification)
        {
            TfsTeamProjectCollection tpc = workItem.Store.TeamProjectCollection;
            String userId = notification.ChangerTeamFoundationId;

            IIdentityManagementService mgmntService = tpc.GetService<IIdentityManagementService>();

            Guid[] ids = new Guid[1];
            ids[0] = new Guid(notification.ChangerTeamFoundationId);

            TeamFoundationIdentity[] members = mgmntService.ReadIdentities(ids, MembershipQuery.Direct);

            return GetUserFromSignum(members[0].UniqueName);
        }

        // Strip the domain name from the signum
        static public String GetUserFromSignum(String signum)
        {
            int inx = signum.LastIndexOf('\\');
            if (inx > 0)
            {
                return signum.Substring(inx + 1);
            }
            else
            {
                return signum;
            }
        }
    }
}
