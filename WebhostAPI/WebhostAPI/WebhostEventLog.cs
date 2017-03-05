using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.IO;

namespace WebhostMySQLConnection
{
    /// <summary>
    /// Database Interaction Event Log
    /// </summary>
    public class WebhostEventLog : EventLog
    {
        public static WebhostEventLog Syslog = new WebhostEventLog();
        public static WebhostEventLog GoogleLog = new WebhostEventLog("Webhost GoogleAPI");
        public static WebhostEventLog SchoologyLog = new WebhostEventLog("Webhost SchoologyAPI");
        public static WebhostEventLog CommentLog = new WebhostEventLog("Webhost Comment Editor");

        public WebhostEventLog(String SourceName = "Webhost MySQL Connection") : base("Application")
        {
            if (!EventLog.SourceExists(SourceName))
            {
                String oldSource = SourceName;
                SourceName = "Windows Error Reporting";
                LogError("Could not locate Source:  {0}.  Using Windows Error Reporting.", oldSource);
            }
            Source = SourceName;
        }

        public String AddInfoTag(String message, Type serializableType=null, object data=null)
        {
            if (data == null)
                return message;
            else if (data.GetType().Equals(serializableType))
            {
                // get Json Data Contract info from the ADUser logged in currently.
                DataContractJsonSerializer json = new DataContractJsonSerializer(serializableType);
                MemoryStream mstr = new MemoryStream();
                json.WriteObject(mstr, data);
                mstr.Position = 0;
                StreamReader sr = new StreamReader(mstr);
                String info = sr.ReadToEnd();
                sr.Close();
                mstr.Close();
                sr.Dispose();
                mstr.Dispose();
                return String.Format("{0}{1}____________________________{1}{1}{2}", message, Environment.NewLine, info);
            }
            else
                return String.Format("{0}{1}***Unable to serialize data {2}.", message, Environment.NewLine, data);
        }

        public void LogError(String message, Type serializableType=null, object data=null)
        {
            this.WriteEntry(AddInfoTag(TimeStampMessage(message), serializableType, data), EventLogEntryType.Error);
        }

        public void LogError(String format, params object[] arg)
        {
            this.WriteEntry(AddInfoTag(TimeStampMessage(format, arg)), EventLogEntryType.Error);
        }

        public void LogWarning(String message, Type serializableType = null, object data = null)
        {
            this.WriteEntry(AddInfoTag(TimeStampMessage(message), serializableType, data), EventLogEntryType.Warning);
        }

        public void LogWarning(String format, params object[] arg)
        {
            this.WriteEntry(AddInfoTag(TimeStampMessage(format, arg)), EventLogEntryType.Warning);
        }

        public void LogInformation(String message, Type serializableType = null, object data = null)
        {
            this.WriteEntry(AddInfoTag(TimeStampMessage(message), serializableType, data), EventLogEntryType.Information);
        }

        public void LogInformation(String format, params object[] arg)
        {
            this.WriteEntry(AddInfoTag(TimeStampMessage(format, arg)), EventLogEntryType.Information);
        }

        public static String TimeStampMessage(String value)
        {
            return String.Format("{0}{2}{1}", WebhostMySQLConnection.Log.TimeStamp(), value, Environment.NewLine);
        }

        private static String TimeStampMessage(String format, params object[] list)
        {
            return TimeStampMessage(String.Format(format, list));
        }
    }
}
