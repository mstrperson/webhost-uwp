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
            State.log.Write("Import Exception:  {0}", Message);
            while (innerException != null)
            {
                State.log.Write("Inner Exception:  {0}" + innerException.Message);
                innerException = innerException.InnerException;
            }
        }
    }
}
