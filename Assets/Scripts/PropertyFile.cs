using System;
using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class PropertyFile
{
    private static bool testProperties(string file) //throws IOException 
    {
        try {
            using (StreamReader sr = new StreamReader(file))
            {
                return sr.Read() == '#';
            }
        }
        catch (Exception t)
        {
          return false;
        }
    }

    // https://stackoverflow.com/questions/485659/can-net-load-and-parse-a-properties-file-equivalent-to-java-properties-class
    // modified for maze properties
    private static void loadProperties(string file, Dictionary<string, string> dict){
        foreach (string line in System.IO.File.ReadAllLines(file))
        {
            if ((!String.IsNullOrEmpty(line)) &&
                (!line.StartsWith("#")) &&
                (line.Contains("=")))
            {
                int index = line.IndexOf('=');
                string key = line.Substring(0, index).Trim();
                string value = line.Substring(index + 1).Trim();
                dict.Add(key, value);
                Debug.Log("import key: "+key+", value: "+value);
            }
        }
    }

    public static bool test(string file) {
        return testProperties(file);
    }

    public static Dictionary<string, string> load(string file) {
        Dictionary<string, string> dict = new Dictionary<string, string>();
        try
        {
            loadProperties(file, dict);
        }
        catch (Exception e)
        {
            Debug.Log(e);
        }
        return dict;
    }
}