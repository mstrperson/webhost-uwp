using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebhostMySQLConnection
{
    public class XMLException : Exception
    {
        public XMLException(String message, Exception innerException = null)
            : base(message, innerException)
        {

        }
    }

    public class UnseparatedChildrenException : XMLException
    {

        public List<String> BrokenStrings
        {
            get;
            protected set;
        }

        public UnseparatedChildrenException(String message, List<String> separatedTags, Exception innerException = null)
            : base(message, innerException)
        {
            BrokenStrings = separatedTags;
        }
    }
}
