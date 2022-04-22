using System;
using System.Collections;
using System.Linq;
using System.Reflection;
// ReSharper disable All

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

        // t.Get
        
        int workingExponent = 0;
        
        foreach (FieldInfo fieldInfo in fields)
        {
            object value = fieldInfo.GetValue(o);

            if (AttributesContainsType(fieldInfo, typeof(HashFieldAttribute)))
            {
                switch (value)
                {
                    case null:
                        continue;
                    case IHashable iHashable:
                        workingHash += iHashable.GetAiHash() * (int) Math.Pow(31, workingExponent);
                        break;
                    default:
                        workingHash += value.GetHashCode() * (int) Math.Pow(31, workingExponent);
                        break;
                }

                workingExponent++;
            } else if (AttributesContainsType(fieldInfo, typeof(HashListAttribute)))
            {
                workingHash += GetAiHash((IEnumerable<IHashable>) value) *
                               (int) Math.Pow(31, workingExponent);
                workingExponent++;
            } else if (AttributesContainsType(fieldInfo, typeof(HashDictionaryAttribute)))
            {
                // hopefully this works...
                IEnumerable<IHashable> enumerable = (ICollection<IHashable>) ((IDictionary) value).Values;

                workingHash += GetAiHash(enumerable) *
                               (int) Math.Pow(31, workingExponent);
                workingExponent++;
            }
        }
        
        return workingHash;
    }

    private static bool AttributesContainsType(FieldInfo field, Type t) {
        return field.IsDefined(t, false);
    }
}

public interface IHashable
{
    int GetAiHash();
}

public class AiHashAttribute : Attribute
{
}

public class HashFieldAttribute : AiHashAttribute
{

}

// conditions - all elements annotated with HashListAttribute must be IEnumerables of a type that implements IHashable
public class HashListAttribute : AiHashAttribute
{

}

// basically HashListAttribute but for a values of a dictionary
public class HashDictionaryAttribute : AiHashAttribute
{
    
}