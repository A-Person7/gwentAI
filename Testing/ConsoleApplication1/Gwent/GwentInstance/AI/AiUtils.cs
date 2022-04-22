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
        
        int workingExponent = 0;
        
        foreach (FieldInfo fieldInfo in fields)
        {
            object? value = fieldInfo.GetValue(o);
            
            if (AttributesContainsType(fieldInfo, typeof(HashFieldAttribute)))
            {
                // todo - potentially change switch statement to yield value, and then multiply that by
                // 31^workingExponent
                switch (value)
                {
                    case null:
                        break;
                    case IHashable iHashable:
                        workingHash += iHashable.GetAiHash() * (int) Math.Pow(31, workingExponent);
                        break;
                    case IEnumerable<IHashable> hashList:
                        workingHash += GetAiHash((IEnumerable<IHashable>) value) *
                                       (int) Math.Pow(31, workingExponent) ;
                        break;
                    case IDictionary dictionary:
                        workingHash += GetAiHash((ICollection<IHashable>) dictionary.Values) *
                            (int) Math.Pow(31, workingExponent);
                        break;
                    default:
                        workingHash += value.GetHashCode() * (int) Math.Pow(31, workingExponent);
                        break;
                }

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

// Use this to tag fields the ai hasher should look at
public class HashFieldAttribute : Attribute
{

}