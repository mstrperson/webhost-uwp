using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WebhostMySQLConnection
{
    public class SimpleXMLTag
    {
        public static Regex XMLTagEx = new Regex(@"<(?<tag>([a-zA-Z_]+[a-zA-Z_\d]*))( [a-zA-Z_]+[a-zA-Z_\d]*='[a-zA-Z_\d]+')*>[^<]*</\k<tag>>", RegexOptions.Singleline);
        public static Regex SelfClosingTag = new Regex(@"<([a-zA-Z_]+[a-zA-Z_\d]*)( [a-zA-Z_]+[a-zA-Z_\d]*='[a-zA-Z_\d]+')*\s*/>");

        public String TagName
        {
            get;
            set;
        }

        public Dictionary<String, String> attributes
        {
            get;
            set;
        }

        public String Value
        {
            get;
            set;
        }

        public override String ToString()
        {
            String str = "<" + TagName;
            foreach(String key in attributes.Keys)
            {
                str += String.Format(" {0}='{1}'", key, attributes[key]);
            }
            str += String.Format(">{0}</{1}>", Value, TagName);
            return str;
        }

        public SimpleXMLTag()
        {
            attributes = new Dictionary<string, string>();
        }

        public SimpleXMLTag(String xml)
        {
            if (!XMLTagEx.IsMatch(xml) && !SelfClosingTag.IsMatch(xml)) throw new XMLException(String.Format("Invalid XML String:  {0}", xml));

            bool selfclosed = SelfClosingTag.IsMatch(xml);

            attributes = new Dictionary<string, string>();

            Regex tagOpen = new Regex(@"<(?<tag>([a-zA-Z_]+[a-zA-Z_\d]*))(\s+[a-zA-Z_]+[a-zA-Z_\d]*='[a-zA-Z_\d]+')*(\s*/)?>");
            String opening = tagOpen.Match(xml).Value;
            xml = xml.Replace(opening, "");
            opening = opening.Replace("<", "").Replace(">", "");
            if (selfclosed) opening = opening.Replace("/", "");
            String[] parts = Regex.Split(opening, @"[\s]+");
            TagName = parts[0];
            for(int i = 1; i < parts.Length; i++)
            {
                if (parts[i].Equals("")) continue;
                string[] attr = parts[i].Split('=');
                attributes.Add(attr[0], attr[1].Replace("'", ""));
            }
            if (selfclosed) Value = "";
            else Value = xml.Replace(String.Format("</{0}>", TagName), "");
        }
    }
}
