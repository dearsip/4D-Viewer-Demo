﻿using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PropertyStore : IStore {

    // --- fields ---

    private Dictionary<string, string> p;

    // --- construction ---

    public PropertyStore(Dictionary<string, string> p) {
        this.p = p;
    }

    // --- helpers ---

    /**
        * Parse a string to produce a boolean value.
        * This is different from the function Boolean.parseBoolean in that
        *     (a) it exists
        * and (b) it uses strict parsing, unlike the related functions in Boolean.
        */
    private bool parseBool(string s) {
        if (s == "true") return true;
        else if (s == "false") return false;
        else throw new /*NumberFormat*/Exception("not bool"); // not really a number, but close enough
    }

    /**
        * Get a property, converting the not-found condition into an exception.
        * In most cases, we could just return the null
        * and let the parse function convert it into a NumberFormatException,
        * but I think the error message is clearer this way.
        */
    private string getProperty(string key) {
        string value = p[key];
        if (value == null) throw new Exception(key + " is null");//throw App.getException("PropertyStore.e1",new Object[] { key });
        return value;
    }

    private string getNullableProperty(string key) {
        return p[key];
    }

    private string ArrayKey(string key, int i) {
        return key + "[" + i + "]";
    }

    private string FieldKey(string key, FieldInfo field) {
        return key + "." + field.Name;
    }

    /**
        * An analogue of Class.isPrimitive for primitive wrapper types.
        */
    //private bool isPrimitiveWrapper(Class c) {

        //return (    c == Boolean.class
                //|| c == Integer.class
                //|| c == Long.class
                //|| c == Double.class

                //|| c == Byte.class
                //|| c == Short.class
                //|| c == Float.class
                //|| c == Character.class );
    //}

    // --- get functions ---

    public bool getBool(string key) {
        string value = getProperty(key);
        try {
            return parseBool(value); // there is no Boolean.parseBoolean, see above
        } catch (/*NumberFormat*/Exception e) {
            throw e;//App.getException("PropertyStore.e2",new Object[] { key, value });
        }
    }

    public int getInteger(string key) {
        string value = getProperty(key);
        try {
            return Int32.Parse(value);
        } catch (OverflowException o) {
            Debug.Log(o);
            return (int)Int64.Parse(value);
        } catch (/*NumberFormat*/Exception e) {
            throw e;//App.getException("PropertyStore.e3",new Object[] { key, value });
        }
    }

    public long getLong(string key) {
        string value = getProperty(key);
        try {
            return Int64.Parse(value);
        } catch (/*NumberFormat*/Exception e) {
            throw e;//App.getException("PropertyStore.e4",new Object[] { key, value });
        }
    }

    public double getDouble(string key) {
        string value = getProperty(key);
        try {
            return Double.Parse(value);
        } catch (/*NumberFormat*/Exception e) {
            throw e;//App.getException("PropertyStore.e5",new Object[] { key, value });
        }
    }

    public string getString(string key) {
        return getProperty(key);
    }

    public int? getNullableInteger(string key) {
        string value = getNullableProperty(key);
        if (value == null) return null;
        try {
            return Int32.Parse(value);
        } catch (/*NumberFormat*/Exception e) {
            throw e;//App.getException("PropertyStore.e11",new Object[] { key, value });
        }
    }

    /**
        * Get data from the store into an array.
        * The array and any substructures and subarrays must exist and be correctly sized.
        */
    private void getArray(string key, object array) {

        Type componentType = array.GetType().GetElementType();
        bool isPrimitive = componentType.IsPrimitive;
        int len = ((Array)array).Length;

        for (int i=0; i<len; i++) {
            string arrayKey = ArrayKey(key,i);

            if (isPrimitive) ((Array)array).SetValue(getPrimitive(arrayKey,componentType),i);
            else getObject(arrayKey,((Array)array).GetValue(i));
        }

        // the Array functions can throw a couple of exceptions,
        // but only if there's programmer error, so don't catch them.
    }

    /**
        * Get data from the store into a structure.
        * The structure and any substructures and subarrays must exist and be correctly sized.
        */
    private void getStruct(string key, object o) {

        FieldInfo[] field = o.GetType().GetFields();

        for (int i=0; i<field.Length; i++) {

            if ( field[i].IsInitOnly 
              || field[i].IsLiteral ) continue; // ignore globals and constants

            Type fieldType = field[i].FieldType;
            string fieldKey = FieldKey(key,field[i]);

            try {
                Debug.Log("isPrimitive: "+fieldType);

                if (fieldType.IsPrimitive) field[i].SetValue(o,getPrimitive(fieldKey,fieldType));
                else getObject(fieldKey,field[i].GetValue(o));

            } catch (/*IllegalAccess*/Exception e) {
                throw e;//App.getException("PropertyStore.e9",new Object[] { key, e.getMessage() });
            }
        }

        if (o is IValidate) ((IValidate) o).validate();
    }

    /**
        * Get a (wrapped) object of primitive type from the store.
        */
    private object getPrimitive(string key, Type c) {

             if (c == typeof(bool)  ) return getBool   (key);
        else if (c == typeof(int)   ) return getInteger(key);
        else if (c == typeof(long)  ) return getLong   (key);
        else if (c == typeof(double)) return getDouble (key);
        else throw new Exception("not primitive "+key+", "+c);//App.getException("PropertyStore.e6",new Object[] { key, c.getName() });
    }

    /**
        * Get data from the store into an object.
        * The object and any substructures and subarrays must exist and be correctly sized.<p>
        *
        * Because the primitive wrapper types aren't mutable, i.e., don't act as pointers,
        * we can't handle primitive types here.  Use getPrimitive instead.
        */
    public void getObject(string key, object o) {
        Debug.Log("getObject key: "+key+", object: "+o);
        Type c = o.GetType();

             if (c.IsPrimitive) throw new Exception("not object "+key+", "+o+", "+c);//App.getException("PropertyStore.e7",new object[] { key, c.getName() });
        else if (c.IsArray    ) getArray (key,o);
        else                    getStruct(key,o);
    }

    // --- put functions ---

    public void putBool(string key, bool b) {
        p.Add(key,b.ToString());
    }

    public void putInteger(string key, int i) {
        p.Add(key,i.ToString());
    }

    public void putLong(string key, long l) {
        p.Add(key,l.ToString());
    }

    public void putDouble(string key, double d) {
        p.Add(key,d.ToString());
    }

    public void putString(string key, string s) {
        p.Add(key,s);
    }

    /**
        * Put an array into the store.
        */
    private void putArray(string key, object array) {

        int len = ((Array)array).Length;

        for (int i=0; i<len; i++) {
            putObject(ArrayKey(key,i),((Array)array).GetValue(i));
        }

        // the Array functions can throw a couple of exceptions,
        // but only if there's programmer error, so don't catch them.
    }

    /**
        * Put a structure into the store.
        */
    private void putStruct(string key, object o) {

        FieldInfo[] field = o.GetType().GetFields();

        for (int i=0; i<field.Length; i++) {

            if ( field[i].IsInitOnly 
              || field[i].IsLiteral ) continue; // ignore globals and constants

            try {

                putObject(FieldKey(key,field[i]),field[i].GetValue(o));

            } catch (/*IllegalAccess*/Exception e) {
                throw e;//App.getException("PropertyStore.e10",new object[] { key, e.getMessage() });
            }
        }
    }

    /**
        * Put an object into the store.  The object can be a primitive wrapper type.
        */
    public void putObject(string key, object o) {
        Type c = o.GetType();

             if (c == typeof(bool)  ) putBool   (key,((bool)  o));
        else if (c == typeof(int)   ) putInteger(key,((int)   o));
        else if (c == typeof(long)  ) putLong   (key,((long)  o));
        else if (c == typeof(double)) putDouble (key,((double)o));

        else if (c.IsPrimitive) throw new Exception("not object");//App.getException("PropertyStore.e8",new object[] { key, c.getName() });
        else if (c.IsArray    ) putArray (key,o);
        else                    putStruct(key,o);
    }

}

