using System;
using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class PropertyFile
{
    public delegate void Loader(IStore store);
    public delegate void Saver(IStore store);
    private static bool testProperties(string file) //throws IOException 
    {
        try {
            using (StreamReader sr = new StreamReader(file))
            {
                return sr.Read() == '#';
            }
        }
        catch (Exception)
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
            }
        }
    }

    private static void storeProperties(string file, Dictionary<string, string> p) {
        try {
            using (StreamWriter sw = new StreamWriter(file))
            {
                sw.WriteLine("#"+DateTime.Now.ToString("ddd MMM dd HH:mm:ss K yyyy"));
                foreach(KeyValuePair<string, string> c in p) sw.WriteLine(c.Key+"="+c.Value);
            }
        }
        catch (Exception t)
        {
          Debug.Log(t);
        }
    }

    public static bool test(string file) {
        return testProperties(file);
    }

    public static void load(string file, Loader storable) {
        Dictionary<string, string> p = new Dictionary<string, string>();
        try
        {
            loadProperties(file, p);
        }
        catch (Exception e)
        {
            Debug.Log(e);
        }
        try {
            PropertyStore store = new PropertyStore(p);
            storable(store);
        } catch (Exception e) {
            Debug.Log(e);
            //throw App.getException("PropertyFile.e2",new Object[] { file.getName(), e.getMessage() });
        }
    }

    public static void save(string file, Saver storable) {
        Dictionary<string, string> p = new Dictionary<string, string>();

        try {
            PropertyStore store = new PropertyStore(p);
            storable(store);
        } catch (Exception e) {
            Debug.Log(e);
        }

        try {
            storeProperties(file,p);
        } catch (IOException e) {
            Debug.Log(e);
        }
    }
}