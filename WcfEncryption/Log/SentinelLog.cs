using System;
using System.Globalization;
using System.Diagnostics;
using System.Net;
using System.IO;
using System.Xml;

namespace WcfEncryption.Log
{
    /// <summary>
    /// Logger is used for creating a customized error log files or an error can be registered as
    /// a log entry in the Windows Event Log on the administrator's machine.
    /// </summary>
    internal class Logger
    {
        protected static string strLogFilePath = string.Empty;
        protected static bool p_bLoggingEnabled = false;
        protected static string strMsgInput = string.Empty;
        private static StreamWriter sw = null;
        /// <summary>
        /// Setting LogFile path. If the logfile path is null then it will update error info into LogFile.txt under
        /// application directory.
        /// </summary>
        public static string LogFilePath
        {
            set
            {
                strLogFilePath = value;
            }
            get
            {
                return strLogFilePath;
            }
        }
        public static bool LogStatus
        {
            set
            {
                p_bLoggingEnabled = value;
            }
            get
            {
                return p_bLoggingEnabled;
            }
        }
        public static string MsgInput
        {
            set { strMsgInput = value; }
            get { return strMsgInput; }
        }

        /// <summary>
        /// Empty Constructor
        /// </summary>
        public Logger() { }
        /// <summary>
        /// Write error log entry for window event if the bLogType is true. Otherwise, write the log entry to
        /// customized text-based text file
        /// </summary>
        /// <param name="bLogType"></param>
        /// <param name="objException"></param>
        /// <returns>false if the problem persists</returns>
        public static bool ErrorRoutine(bool bLogType, Exception objException)
        {
            try
            {
                //Check whether logging is enabled or not
                //bool bLoggingEnabled =	false;
                //bLoggingEnabled = CheckLoggingEnabled();

                //Don't process more if the logging 
                //if (false == p_bLoggingEnabled)
                //	return true;

                //Write to Windows event log
                if (true == bLogType)
                {
                    string EventLogName = "ErrorEBGateway";

                    if (!EventLog.SourceExists(EventLogName))
                        EventLog.CreateEventSource(objException.Message, EventLogName);

                    // Inserting into event log
                    EventLog Log = new EventLog();
                    Log.Source = EventLogName;
                    Log.WriteEntry(objException.Message, EventLogEntryType.Error);
                    Log = null;
                }
                //Custom text-based event log
                else
                {
                    if (true != CustomErrorRoutine(objException))
                        return false;
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }


        /// <summary>
        /// Write error log entry for window event if the bLogType is true. Otherwise, write the log entry to
        /// customized text-based text file
        /// </summary>
        /// <param name="bLogType"></param>
        /// <param name="objException"></param>
        /// <returns>false if the problem persists</returns>
        public static bool WriteTrace(bool bLogType, string strMessage)
        {
            bool bRet = true;
            try
            {
                if (bLogType)
                {
                    string EventLogName = "ErrorSample";

                    if (!EventLog.SourceExists(EventLogName))
                        EventLog.CreateEventSource(strMessage, EventLogName);

                    // Inserting into event log
                    EventLog Log = new EventLog();
                    Log.Source = EventLogName;
                    Log.WriteEntry(strMessage, EventLogEntryType.Error);
                    Log = null;
                }
                //Custom text-based event log
                else
                {
                    //Don't process more if the logging 
                    if (false != p_bLoggingEnabled)
                        bRet = false;
                    if (true != CustomErrorRoutine(strMessage))
                        bRet = false;
                }
            }
            catch (Exception)
            {
                bRet = false;
            }
            return bRet;
        }
        /// <summary>
        /// Check Logginstatus config file is exist. If exist read the value set the loggig status
        /// </summary>
        private static bool CheckLoggingEnabled()
        {
            string strLoggingStatusConfig = string.Empty;

            strLoggingStatusConfig = GetLoggingStatusConfigFileName();

            //If it's empty then enable the logging status 
            if (strLoggingStatusConfig.Equals(string.Empty))
            {
                return true;
            }
            else
            {

                //Read the value from xml and set the logging status
                bool bTemp = GetValueFromXml(strLoggingStatusConfig);
                return bTemp;
            }
        }
        /// <summary>
        /// Check the Logginstatus config under debug or release folder. If not exist, check under 
        /// project folder. If not exist again, return empty string
        /// </summary>
        /// <returns>empty string if file not exists</returns>
        private static string GetLoggingStatusConfigFileName()
        {
            string strCheckinBaseDirecotry = AppDomain.CurrentDomain.BaseDirectory + "LoggingStatus.Config";

            if (File.Exists(strCheckinBaseDirecotry))
                return strCheckinBaseDirecotry;
            else
            {
                string strCheckinApplicationDirecotry = GetApplicationPath() + "LoggingStatus.Config";

                if (File.Exists(strCheckinApplicationDirecotry))
                    return strCheckinApplicationDirecotry;
                else
                    return string.Empty;
            }
        }
        /// <summary>
        /// Read the xml file and getthe logging status
        /// </summary>
        /// <param name="strXmlPath"></param>
        /// <returns></returns>
        private static bool GetValueFromXml(string strXmlPath)
        {
            try
            {
                //Open a FileStream on the Xml file
                FileStream docIn = new FileStream(strXmlPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

                XmlDocument contactDoc = new XmlDocument();
                //Load the Xml Document
                contactDoc.Load(docIn);

                //Get a node
                XmlNodeList UserList = contactDoc.GetElementsByTagName("LoggingEnabled");

                //get the value
                string strGetValue = UserList.Item(0).InnerText.ToString();
                docIn = null;
                contactDoc = null;
                if (strGetValue.Equals("0"))
                    return false;
                else if (strGetValue.Equals("1"))
                    return true;
                else
                    return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// If the LogFile path is empty then, it will write the log entry to LogFile.txt under application directory.
        /// If the LogFile.txt is not availble it will create it
        /// If the Log File path is not empty but the file is not availble it will create it.
        /// </summary>
        /// <param name="objException"></param>
        /// <returns>false if the problem persists</returns>
        private static bool CustomErrorRoutine(Exception objException)
        {
            string strPathName = string.Empty;
            CultureInfo ci = new CultureInfo("en-US");
            string strToDayFileName = String.Format("\\Log\\{0}Log.log", DateTime.Today.ToString("yyyyMMdd", ci));
            ci = null;
            if (strLogFilePath.Equals(string.Empty))
            {
                //Get Default log file path "FtpyyyyMMdd.log"
                strPathName = GetLogFilePath();
            }
            else
            {
                strPathName = (strLogFilePath.EndsWith(@"\") || strLogFilePath.EndsWith(@"/")) ?
                    strLogFilePath + strToDayFileName : strLogFilePath + "\\" + strToDayFileName;
                //If the log file path is not empty but the file is not available it will create it
                if (false == File.Exists(strPathName))
                {
                    if (false == CheckDirectory(strPathName))
                        return false;

                    FileStream fs = new FileStream(strPathName, FileMode.OpenOrCreate, FileAccess.ReadWrite);
                    fs.Close();
                    fs = null;
                }
                //strPathName	= strLogFilePath;

            }

            bool bReturn = true;
            // write the error log to that text file
            if (true != WriteErrorLog(strPathName, objException))
            {
                bReturn = false;
            }
            return bReturn;
        }

        /// <summary>
        /// If the LogFile path is empty then, it will write the log entry to LogFile.txt under application directory.
        /// If the LogFile.txt is not availble it will create it
        /// If the Log File path is not empty but the file is not availble it will create it.
        /// </summary>
        /// <param name="objException"></param>
        /// <returns>false if the problem persists</returns>
        private static bool CustomErrorRoutine(string strMessage)
        {
            string strPathName = string.Empty;
            CultureInfo ci = new CultureInfo("en-US");
            string strToDayFileName = String.Format("Event{0}.log", System.DateTime.Today.ToString("yyyyMMdd", ci));
            ci = null;
            if (strLogFilePath.Equals(string.Empty))
            {
                //Get Default log file path "LogFile.txt"
                strPathName = GetLogFilePath();
            }
            else
            {
                strPathName = (strLogFilePath.EndsWith(@"\") || strLogFilePath.EndsWith(@"/")) ?
                    strLogFilePath + strToDayFileName : strLogFilePath + "\\" + strToDayFileName;
                //If the log file path is not empty but the file is not available it will create it
                if (false == File.Exists(strPathName))
                {
                    if (false == CheckDirectory(strPathName))
                        return false;

                    FileStream fs = new FileStream(strPathName, FileMode.OpenOrCreate, FileAccess.ReadWrite);
                    fs.Close();
                    fs = null;
                }
                //strPathName	= strLogFilePath;

            }

            bool bReturn = true;
            // write the error log to that text file
            if (true != WriteLog(strPathName, strMessage))
            {
                bReturn = false;
            }
            return bReturn;
        }


        /// <summary>
        /// Write Source,method,date,time,computer,error and stack trace information to the text file
        /// </summary>
        /// <param name="strPathName"></param>
        /// <param name="objException"></param>
        /// <returns>false if the problem persists</returns>
        private static bool WriteErrorLog(string strPathName, Exception objException)
        {
            bool bReturn = false;
            string strException = string.Empty;
            try
            {
                sw = new StreamWriter(strPathName, true);
                sw.WriteLine("^^------------------------Runtime Error-------------------------------^^");
                sw.WriteLine("" + strMsgInput);
                sw.WriteLine("Source		: " + objException.Source.ToString().Trim());
                sw.WriteLine("Method		: " + objException.TargetSite.Name.ToString());
                sw.WriteLine("Date		: " + DateTime.Now.ToShortDateString());
                sw.WriteLine("Time		: " + DateTime.Now.ToLongTimeString());
                sw.WriteLine("Computer	: " + Dns.GetHostName().ToString());
                sw.WriteLine("Error		: " + objException.Message.ToString().Trim());
                sw.WriteLine("Stack Trace	: " + objException.StackTrace.ToString().Trim());
                sw.WriteLine("^^-------------------------------------------------------------------^^");
                sw.Flush();
                sw.Close();
                sw = null;
                bReturn = true;
            }
            catch (Exception)
            {
                bReturn = false;
            }
            return bReturn;
        }

        /// <summary>
        /// Write Source,method,date,time,computer,error and stack trace information to the text file
        /// </summary>
        /// <param name="strPathName"></param>
        /// <param name="objException"></param>
        /// <returns>false if the problem persists</returns>
        private static bool WriteLog(string strPathName, string strMessage)
        {
            bool bReturn = false;
            string strException = string.Empty;
            try
            {
                sw = new StreamWriter(strPathName, true);
                CultureInfo ci = new CultureInfo("en-US");
                sw.WriteLine("{0} =>{1}", System.DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss", ci), strMessage);
                sw.Flush();
                sw.Close();
                sw = null;
                bReturn = true;
            }
            catch (Exception)
            {
                bReturn = false;
            }
            return bReturn;
        }

        /// <summary>
        /// Check the log file in applcation directory. If it is not available, creae it
        /// </summary>
        /// <returns>Log file path</returns>
        private static string GetLogFilePath()
        {
            try
            {
                // get the base directory
                string baseDir = AppDomain.CurrentDomain.BaseDirectory + AppDomain.CurrentDomain.RelativeSearchPath;

                // search the file below the current directory
                System.Globalization.CultureInfo ci = new System.Globalization.CultureInfo("en-US");
                string retFilePath = String.Format("{0}\\Log\\{1}Log.log", baseDir, System.DateTime.Today.ToString("yyyyMMdd", ci));
                ci = null;
                // if exists, return the path
                if (File.Exists(retFilePath) == true)
                    return retFilePath;
                //create a text file
                else
                {
                    if (false == CheckDirectory(retFilePath))
                        return string.Empty;

                    FileStream fs = new FileStream(retFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite);
                    fs.Close();
                    fs = null;
                }

                return retFilePath;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }
        /// <summary>
        /// Create a directory if not exists
        /// </summary>
        /// <param name="strLogPath"></param>
        /// <returns></returns>
        private static bool CheckDirectory(string strLogPath)
        {
            try
            {
                int nFindSlashPos = strLogPath.Trim().LastIndexOf("\\");
                string strDirectoryname = strLogPath.Trim().Substring(0, nFindSlashPos);

                if (false == Directory.Exists(strDirectoryname))
                    Directory.CreateDirectory(strDirectoryname);

                return true;
            }
            catch (Exception)
            {
                return false;

            }
        }

        private static string GetApplicationPath()
        {
            try
            {
                string strBaseDirectory = AppDomain.CurrentDomain.BaseDirectory.ToString();
                int nFirstSlashPos = strBaseDirectory.LastIndexOf("\\");
                string strTemp = string.Empty;

                if (0 < nFirstSlashPos)
                    strTemp = strBaseDirectory.Substring(0, nFirstSlashPos);

                int nSecondSlashPos = strTemp.LastIndexOf("\\");
                string strTempAppPath = string.Empty;
                if (0 < nSecondSlashPos)
                    strTempAppPath = strTemp.Substring(0, nSecondSlashPos);

                string strAppPath = strTempAppPath.Replace("bin", "");
                return strAppPath;
            }
            catch (Exception)
            {

                return string.Empty;
            }
        }
    }
}