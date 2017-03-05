using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace WebhostMySQLConnection.Web
{
    public class State
    {
        public static String NeedsUpdating { get { return "NeedsUpdating"; } }
        public static String EditMode { get { return "EditMode"; } }
        public static String AuthUser { get { return "AuthUser"; } }
        public static Log log
        {
            get
            {
                try
                {
                    return (Log)HttpContext.Current.Session["log"];
                }
                catch
                {
                    try
                    {
                        HttpContext.Current.Session["log"] = new Log("unknown_log", HttpContext.Current.Server);
                        return State.log;
                    }
                    catch 
                    {
                        return Log.CommandLineLog;
                    }
                }
            }
        }
    }
}
