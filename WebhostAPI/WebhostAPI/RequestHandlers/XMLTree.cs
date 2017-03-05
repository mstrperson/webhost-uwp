using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.IO;

namespace WebhostMySQLConnection
{
    public class XMLTree
    {
        public static readonly Regex header = new Regex(@"<\?.*\?>", RegexOptions.Singleline);
        public static readonly Regex tree = new Regex(@"<(?<root>([a-zA-Z_]+[a-zA-Z_\d]*))( [a-zA-Z_]+[a-zA-Z_\d]*='[a-zA-Z_\d]+')*>.*</\k<root>>", RegexOptions.Singleline);
        public static Regex XMLTagEx = new Regex(@"<(?<tag>([a-zA-Z_]+[a-zA-Z_\d]*))( [a-zA-Z_]+[a-zA-Z_\d]*='[a-zA-Z_\d]+')*>[^<]*</\k<tag>>");
        public static Regex SelfClosingTag = new Regex(@"<([a-zA-Z_]+[a-zA-Z_\d]*)( [a-zA-Z_]+[a-zA-Z_\d]*='[a-zA-Z_\d]+')*\s*/>");

        public static String MakeXMLAttributeValueSafe(String str)
        {
            Regex unsafeChars = new Regex(@"[^a-zA-Z_\d]");
            str = str.Replace(" ", "_");
            foreach(Match match in unsafeChars.Matches(str))
            {
                str = str.Replace(match.Value, "");
            }

            return str;
        }

        /// <summary>
        /// Sub-Trees of this tree.
        /// </summary>
        public List<XMLTree> ChildTrees
        {
            get;
            set;
        }

        /// <summary>
        /// All the leaves at this branch of the tree.
        /// </summary>
        public List<SimpleXMLTag> ChildNodes
        {
            get;
            set;
        }

        public Dictionary<String, String> Attributes
        {
            get;
            set;
        }

        public String TagName
        {
            get;
            set;
        }
        
        public override String ToString()
        {
            String xml = "<" + TagName;
            foreach (string attr in Attributes.Keys)
            {
                xml += String.Format(" {0}='{1}'", attr, Attributes[attr]);
            }
            xml += ">";
            foreach(XMLTree ct in ChildTrees)
            {
                xml += "\r\n" + ct.ToString();
            }

            foreach(SimpleXMLTag tag in ChildNodes)
            {
                xml += "\r\n" + tag.ToString();
            }

            xml += "\r\n</" + TagName + ">";

            return xml;
        }

        public static List<String> GetParallelRoots(String xml)
        {
            Dictionary<String, int> openTagsCounters = new Dictionary<string, int>();
            
            List<String> roots = new List<string>();

            String root = "";
            while (!xml.Equals(""))
            {
                int firstOpenChar = xml.IndexOf('<');
                int firstCloseChar = xml.IndexOf('>');
                String tag = xml.Substring(firstOpenChar, firstCloseChar - firstOpenChar + 1);
                tag = tag.Replace("<","").Replace(">","");
                bool selfClosing = false;
                if(tag.EndsWith("/"))
                {
                    // ignore self closing tag!
                    selfClosing = true;
                }
                tag = tag.Split(' ')[0];

                if(selfClosing)
                {
                    if (!openTagsCounters.ContainsKey(tag))
                        openTagsCounters.Add(tag, 0);
                }
                else if (tag[0] != '/')
                {
                    if (openTagsCounters.ContainsKey(tag))
                    {
                        openTagsCounters[tag]++;
                    }
                    else
                    {
                        openTagsCounters.Add(tag, 1);
                    }
                }
                else
                {
                    tag = tag.Substring(1);
                    if (openTagsCounters.ContainsKey(tag))
                    {
                        openTagsCounters[tag]--;
                    }
                    else
                    {
                        // closed an unopened tag!
                        throw new XMLException("Invalid XML Structure!");
                    }
                }

                bool rootRetuned = true;

                foreach (String key in openTagsCounters.Keys)
                {
                    if (openTagsCounters[key] > 0) rootRetuned = false;
                }

                root += xml.Substring(0, firstCloseChar + 1);
                if (rootRetuned)
                {
                    roots.Add(root);
                    root = "";
                }

                xml = xml.Substring(firstCloseChar + 1);
            }

            foreach (String key in openTagsCounters.Keys)
            {
                if (openTagsCounters[key] != 0)
                    throw new XMLException("Incomplete XML Tree!");
            }

            return roots;
        }
        protected static String StripFormatting(String xml)
        {
            xml = xml.Replace("\r", "").Replace("\n", "").Replace("\t", "");
            return xml;
        }
        public static bool IsTree(String xml)
        {
            if (!tree.IsMatch(xml)) return false;
            Dictionary<String, int> openTagsCounters = new Dictionary<string, int>();

           /* Regex whitespace = new Regex(@"[\s\t]+");
            foreach(Match match in whitespace.Matches(xml))
            {
                xml = xml.Replace(match.Value, "");
            }*/

            foreach(Match match in SelfClosingTag.Matches(xml))
            {
                // ignore self closing tags.
                xml = xml.Replace(match.Value, "");
            }

            while (!xml.Equals(""))
            {
                int firstOpenChar = xml.IndexOf('<');
                int firstCloseChar = xml.IndexOf('>');

                String tag = xml.Substring(firstOpenChar, firstCloseChar - firstOpenChar+1);
                tag = tag.Substring(1, tag.Length - 2);
                tag = tag.Split(' ')[0];

                if(tag[0] != '/')
                {
                    if(openTagsCounters.ContainsKey(tag))
                    {
                        openTagsCounters[tag]++;
                    }
                    else
                    {
                        openTagsCounters.Add(tag, 1);
                    }
                }
                else
                {
                    tag = tag.Substring(1);
                    if (openTagsCounters.ContainsKey(tag))
                    {
                        openTagsCounters[tag]--;
                    }
                    else
                    {
                        // closed an unopened tag!
                        return false;
                    }
                }

                xml = xml.Substring(firstCloseChar + 1);
            }

            foreach(String key in openTagsCounters.Keys)
            {
                if (openTagsCounters[key] != 0) 
                    return false;
            }

            return true;
        }
        
        public XMLTree MergeWith(XMLTree other)
        {
            bool merge = true;
            if(this.TagName.Equals(other.TagName))
            {
                foreach(String attr in this.Attributes.Keys)
                {
                    if(other.Attributes.ContainsKey(attr) && !this.Attributes[attr].Equals(other.Attributes[attr]))
                    {
                        merge = false;
                    }
                }
            }
            else
            {
                merge = false;
            }

            if(merge)
            {
                XMLTree tree = new XMLTree()
                {
                    TagName = this.TagName
                };
                foreach(String attr in this.Attributes.Keys)
                {
                    tree.Attributes.Add(attr, this.Attributes[attr]);
                }
                foreach(string attr in other.Attributes.Keys)
                {
                    if(!tree.Attributes.ContainsKey(attr))
                    {
                        tree.Attributes.Add(attr, other.Attributes[attr]);
                    }
                }

                tree.ChildNodes.AddRange(this.ChildNodes);
                tree.ChildNodes.AddRange(other.ChildNodes);
                tree.ChildTrees.AddRange(this.ChildTrees);
                tree.ChildTrees.AddRange(other.ChildTrees);

                return tree;
            }
            else if(this.TagName.Equals("merge"))
            {
                XMLTree tree = new XMLTree()
                {
                    TagName = "merge"
                };
                tree.ChildTrees.AddRange(this.ChildTrees);
                tree.ChildNodes.AddRange(this.ChildNodes);
                tree.ChildTrees.Add(other);
                return tree;
            }
            else if (other.TagName.Equals("merge"))
            {
                XMLTree tree = new XMLTree()
                {
                    TagName = "merge"
                };
                tree.ChildTrees.AddRange(other.ChildTrees);
                tree.ChildNodes.AddRange(other.ChildNodes);
                tree.ChildTrees.Add(this);
                return tree;
            } 
            else
            {
                XMLTree tree = new XMLTree()
                {
                    TagName = "merge"
                };

                tree.ChildTrees.Add(this);
                tree.ChildTrees.Add(other);
                return tree;
            }

        }

        /// <summary>
        /// Recursive search of the XMLTree for a given tag.
        /// Result:
        /// 
        /// &lt;result&gt; DATA &lt;/result&gt;
        /// 
        /// 
        /// </summary>
        /// <param name="tagname"></param>
        /// <returns></returns>
        public XMLTree Search(String tagname)
        {
            XMLTree tree = new XMLTree();
            tree.TagName = "result";
            foreach(XMLTree subtree in this.ChildTrees)
            {
                if (subtree.TagName.Equals(tagname))
                    tree.ChildTrees.Add(subtree);

                XMLTree result = subtree.Search(tagname);
                tree.ChildTrees.AddRange(result.ChildTrees);
                tree.ChildNodes.AddRange(result.ChildNodes);
            }
            foreach(SimpleXMLTag tag in this.ChildNodes)
            {
                if(tag.TagName.Equals(tagname))
                {
                    tree.ChildNodes.Add(tag);
                }
            }

            return tree;
        }

        /// <summary>
        /// Save the XMLTree to an XML file.
        /// </summary>
        /// <param name="fileName"></param>
        public void Save(String fileName, FileMode mode = FileMode.Create)
        {
            if (mode == FileMode.Create && File.Exists(fileName))
                File.Delete(fileName);

            StreamWriter writer = new StreamWriter(new FileStream(fileName, mode));
            writer.Write(this.ToString());
            writer.Flush();
            writer.Close();
        }

        /// <summary>
        /// Create an empty XMLTree.
        /// </summary>
        public XMLTree()
        {
            ChildNodes = new List<SimpleXMLTag>();
            ChildTrees = new List<XMLTree>();
            Attributes = new Dictionary<string, string>();
        }
        
        /// <summary>
        /// Build an XMLTree from an XML formatted string.
        /// </summary>
        /// <param name="xml"></param>
        public XMLTree(String xml)
        {
            foreach (Match match in header.Matches(xml))
                xml = xml.Replace(match.Value, "");

            xml = StripFormatting(xml);

            /*if(SelfClosingTag.IsMatch(xml))
            {
                ChildNodes = new List<SimpleXMLTag>();
                ChildTrees = new List<XMLTree>();

                TagName = xml.Replace("<", "").Replace("/>", "");
                return;
            }*/

            if (!IsTree(xml)) throw new XMLException(String.Format("Cannot Identify root tag in:  {0}", xml));

            ChildNodes = new List<SimpleXMLTag>();
            ChildTrees = new List<XMLTree>();

            Regex openingTag = new Regex(@"<(?<root>([a-zA-Z_]+[a-zA-Z_\d]*))( [a-zA-Z_]+[a-zA-Z_\d]*='[a-zA-Z_\d]+')*>");

            String opening = openingTag.Match(xml).Value;
            int start = xml.IndexOf(opening);
            opening = opening.Replace("<", "").Replace(">", "");
            String[] parts = Regex.Split(opening, @"[\s]+");
            TagName = parts[0];
            Regex closingTag = new Regex(String.Format("</{0}>", TagName));

            Regex sameTags = new Regex(String.Format("<{0}( [a-zA-Z_]+[a-zA-Z_\\d]*='[a-zA-Z_\\d]+')*>", TagName));
            if (sameTags.Matches(xml).Count > 1)
            {
                List<String> xmlstrings = GetParallelRoots(xml);
                
                if (xmlstrings.Count == 1)
                {
                    xml = xmlstrings[0];
                }
                else
                {
                    throw new UnseparatedChildrenException("XML contains multiple sister tags.", xmlstrings);
                }
            }
            xml = xml.Substring(start + opening.Length + 2);
            int end = xml.LastIndexOf(closingTag.Match(xml).Value);
            xml = xml.Substring(0, end);
            Attributes = new Dictionary<string, string>();
            for (int i = 1; i < parts.Length; i++)
            {
                string[] attr = parts[i].Split('=');
                Attributes.Add(attr[0], attr[1].Replace("'", ""));
            }

            // Get parallel children.
            List<String> children = GetParallelRoots(xml);

            foreach(String child in children)
            {
                if (child.ToCharArray().ToList().Where(c => c == '<').Count() <= 2)
                {
                    ChildNodes.Add(new SimpleXMLTag(child));
                }
                else
                {
                    ChildTrees.Add(new XMLTree(child));
                }
            }
        }
    }
}
