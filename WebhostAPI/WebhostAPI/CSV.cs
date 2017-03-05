using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;
using System.Text.RegularExpressions;

/// <summary>
/// Summary description for CSV
/// </summary>
namespace WebhostMySQLConnection
{
public class CSV
{

    public String Heading
    { get; set; }

    private List<Dictionary<String, String>> _Data;
    public List<Dictionary<String, String>> Data
    {
        get { return _Data; }
    }

    private static Regex Quoted = new Regex("^\"[^\"]*\"$");

    public CSV(String heading = "")
    {
        Heading = heading;
        _Data = new List<Dictionary<string, string>>();
    }

	public CSV(Stream inputStream)
	{
        Heading = "";
        _Data = new List<Dictionary<string,string>>();
        StreamReader reader = new StreamReader(inputStream);
        String line = reader.ReadLine();
        String[] headers = line.Split(',');
        while (!reader.EndOfStream)
        {
            Dictionary<String, String> row = new Dictionary<string, string>();
            line = reader.ReadLine();
            String[] values = line.Split(',');
            for (int i = 0; i < headers.Length && i < values.Length; i++)
            {
                if (Quoted.IsMatch(values[i]))
                {
                    values[i] = values[i].Substring(1, values[i].Length - 2);
                }
                row.Add(headers[i], values[i]);
            }

            _Data.Add(row);
        }
	}

    public void Add(Dictionary<String,String> row)
    {
        _Data.Add(row);
        _AllKeys = new List<string>();
    }

    public Boolean Contains(Dictionary<String, String> row)
    {
        for (int i = 0; i < _Data.Count; i++)
        {
            bool match = true;
            foreach (String key in row.Keys)
            {
                if (!_Data[i].ContainsKey(key))
                {
                    match = false;
                    break;
                }

                if (!_Data[i][key].Equals(row[key]))
                {
                    match = false;
                    break;
                }
            }

            if (match)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Get a CSV containing all the entries of this CSV which do not correspond to entries in the Other CSV.
    /// </summary>
    /// <param name="Other"></param>
    /// <returns></returns>
    public CSV NotIn(CSV Other)
    {
        CSV newCSV = new CSV();

        List<String> CommonKeys = new List<string>();
        foreach (String key in AllKeys)
        {
            if (Other.AllKeys.Contains(key))
            {
                CommonKeys.Add(key);
            }
        }

        foreach (Dictionary<String, String> row in _Data)
        {
            Dictionary<String, String> strippedRow = new Dictionary<string, string>();
            foreach (String key in CommonKeys)
            {
                strippedRow.Add(key, row[key]);
            }

            if (!Other.Contains(strippedRow))
            {
                newCSV.Add(row);
            }
        }

        return newCSV;
    }

    public void Remove(Dictionary<String, String> row)
    {
        #region Search
        int index = -1;
        bool foundMatch = false;
        do
        {
            foundMatch = false;
            for (int i = 0; i < _Data.Count; i++)
            {
                bool match = true;
                foreach (String key in row.Keys)
                {
                    if (!_Data[i].ContainsKey(key))
                    {
                        match = false;
                        break;
                    }

                    if (!_Data[i][key].Equals(row[key]))
                    {
                        match = false;
                        break;
                    }
                }

                if (match)
                {
                    index = i;
                    foundMatch = true;
                    break;
                }
            }

            if (index != -1)
                _Data.RemoveAt(index);
        } while (foundMatch);
        #endregion

        _AllKeys = new List<string>();
    }
    
    private List<String> _AllKeys;
    public List<String> AllKeys
    {
        get
        {
            if (_AllKeys == null || _AllKeys.Count == 0)
            {
                _AllKeys = new List<string>();

                foreach (Dictionary<String, String> row in _Data)
                {
                    foreach (String key in row.Keys)
                    {
                        if (!_AllKeys.Contains(key))
                        {
                            _AllKeys.Add(key);
                        }
                    }
                }
            }
            return _AllKeys;
        }
    }

    public int ColCount
    {
        get
        {
            return AllKeys.Count;
        }
    }

    public int RowCount
    {
        get
        {
            return Data.Count;
        }
    }

    public void Save(String fileName)
    {
        //"\\/:\\*\\?\"<>\\|"
        fileName = fileName.Replace("/", "\\");
        Regex invalid = new Regex("[\\*\\?\"<>\\|]");
        foreach (Match match in invalid.Matches(fileName))
        {
            fileName = fileName.Replace(match.Value, "");
        }
        if (File.Exists(fileName)) File.Delete(fileName);
        this.Save(new FileStream(fileName, FileMode.OpenOrCreate));
    }

    public void Save(Stream output)
    {
        StreamWriter writer = new StreamWriter(output);
        writer.AutoFlush = true;

        if(!Heading.Equals(""))
            writer.WriteLine(Heading);

        if (AllKeys.Count <= 0)
        {
            return;
        }

        writer.Write(AllKeys[0]);
        for (int i = 1; i < AllKeys.Count; i++)
        {
            writer.Write(",{0}", AllKeys[i]);
        }
        writer.WriteLine();

        foreach (Dictionary<String, String> row in _Data)
        {
            if(row.ContainsKey(AllKeys[0]))
                writer.Write(row[AllKeys[0]]);
            for(int i = 1; i < AllKeys.Count; i++)
            {
                writer.Write(",");
                if(row.ContainsKey(AllKeys[i]))
                {
                    writer.Write(row[AllKeys[i]]);
                }
            }
            writer.WriteLine();
        }


        writer.Close();
    }

    public void Add(CSV other)
    {
        foreach(Dictionary<String, String> row  in other.Data)
        {
            this.Add(row);
        }
    }
}
}
