using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebhostMySQLConnection.Web
{
    public class WebhostException:Exception
    {
        public WebhostException(String message, Exception innerException = null)
            : base(message, innerException)
        {
            WebhostEventLog.Syslog.LogInformation("Import Exception:  {0}", Message);
            while (innerException != null)
            {
                WebhostEventLog.Syslog.LogInformation("Inner Exception:  {0}" + innerException.Message);
                innerException = innerException.InnerException;
            }
        }
    }
}
