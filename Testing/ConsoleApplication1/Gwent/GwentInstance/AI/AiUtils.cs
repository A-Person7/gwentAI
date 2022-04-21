using System;
using System.Collections;
using System.Linq;
using System.Reflection;

namespace ConsoleApplication1.Gwent.GwentInstance.AI;

using System.Collections.Generic;

public static class AiUtils
{
    // certified sketchy
    public static int GetAiHash(IEnumerable<IHashable> hashables)
    {
        return hashables.Select((t, i) => t.GetAiHash() * (int) Math.Pow(31, i)).Sum();
    }

    public static int HashInternalElements(Object o)
    {
        Type t = o.GetType();
        int workingHash = 0;
        FieldInfo[] fields = t.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        int workingExponent = 0;
        
        for (int i = 0; i < fields.Length; i++)
        {
            if (Attribute.IsDefined(fields[i], typeof(HashFieldAttribute)))
            {
                workingExponent++;
                Object value = fields[i].GetValue(o);

                if (value == null)
                {
                    continue;
                }

                if (value is IHashable iHashable)
                {
                    workingHash += iHashable.GetAiHash() * (int) Math.Pow(31, workingExponent);
                }
                else
                {
                    workingHash += value.GetHashCode() * (int) Math.Pow(31, workingExponent);
                }
            } else if (Attribute.IsDefined(fields[i], typeof(HashListAttribute)))
            {
                Object value = fields[i].GetValue(o);
                workingHash += GetAiHash((IEnumerable<IHashable>) value) *
                               (int) Math.Pow(31, workingExponent);
                workingExponent++;
            } else if (Attribute.IsDefined(fields[i], typeof(HashDictionaryAttribute)))
            {
                Object value = fields[i].GetValue(o);
                // hopefully this works...
                IEnumerable<IHashable> enumerable = (ICollection<IHashable>) ((IDictionary) value).Values;

                workingHash += GetAiHash(enumerable) *
                                         (int) Math.Pow(31, workingExponent);
                workingExponent++;
            }
            
        }
        
        return workingHash;
    }
}

public interface IHashable
{
    int GetAiHash();
}

public class HashFieldAttribute : Attribute
{

}

// conditions - all elements annotated with HashListAttribute must be IEnumerables of a type that implements IHashable
public class HashListAttribute : Attribute
{

}

// basically HashListAttribute but for a values of a dictionary
public class HashDictionaryAttribute : Attribute
{
    
}