using System;
using System.Collections;
using System.Linq;
using System.Reflection;

namespace ConsoleApplication1.Gwent.GwentInstance.AI;

using System.Collections.Generic;

public static class AiUtils
{
    private static long GetAiHash(IEnumerable<IHashable> hashables)
    {
        return hashables.Select((t, i) => t == null ? 0 : 
            t.GetAiHash() * (long) Math.Pow(31, i)).Sum();
    }
    
    public static long GetAiHash(params IHashable[] hashables)
    {
        return GetAiHash(hashables.ToList());
    }
    
    public static long HashInternalElements(Object o)
    {
        Type t = o.GetType();
        long workingHash = 0;
        // private fields are respected as HashCode, HashlongernalElements, and GetAiHash are all pure

        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

        FieldInfo[] fields = t.GetFields(flags);
        PropertyInfo[] properties = t.GetProperties(flags);

        MemberInfo[] members = ((MemberInfo[]) fields).Concat(properties)
            .Where(m => m.GetCustomAttribute<HashFieldAttribute>() != null)
            .ToArray();


        int workingExponent = 0;

        foreach (MemberInfo member in members)
        {
            object value = member switch
            {
                FieldInfo field => field.GetValue(o),
                PropertyInfo property => property.GetValue(o),
                _ => null
            };
            
            workingHash += value switch
            {
                IHashable iHashable => iHashable.GetAiHash(),
                IEnumerable<IHashable> hashList => GetAiHash(hashList),
                    
                IDictionary dictionary => GetAiHash(dictionary.Values.Cast<IHashable>().ToList()),
                
                null => 0,
                not null => value.GetHashCode()
            } * (long) Math.Pow(31, workingExponent);

            workingExponent++;
        }

        return workingHash;
    }
}

public interface IHashable
{
    long GetAiHash();
}

// Use this to tag fields the ai hasher should look at
public class HashFieldAttribute : Attribute
{
}