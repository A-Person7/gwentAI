using System;
using System.Collections;
using System.Linq;
using System.Reflection;

namespace ConsoleApplication1.Gwent.GwentInstance.AI;

using System.Collections.Generic;

public static class AiUtils
{
    private static int GetAiHash(IEnumerable<IHashable> hashables)
    {
        return hashables.Select((t, i) => t.GetAiHash() * (int) Math.Pow(31, i)).Sum();
    }

    // lmao   
    public static int HashInternalElements(Object o)
    {
        Type t = o.GetType();
        int workingHash = 0;
        // private fields are respected as HashCode, HashInternalElements, and GetAiHash are all pure

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
                    workingHash += GetAiHash(hashList) *
                                   (int) Math.Pow(31, workingExponent);
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

        return workingHash;
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