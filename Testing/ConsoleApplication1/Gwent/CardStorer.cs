using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;

namespace ConsoleApplication1.Gwent;

public static class CardStorer
{
    public static ReadOnlyCollection<Card> Cards => InitCards();

    // call this method before attempting 
    private static ReadOnlyCollection<Card> InitCards()
    {
        List<Card> workingCards = new List<Card>();

        Assembly assembly = Assembly.GetExecutingAssembly();
        
        String nameSpace = "ConsoleApplication1.Gwent.GwentCards";

        // create an array of all types in namespace ConsoleApplication1.GwentCards with CardAttribute
        List<Type> types = assembly
            .GetTypes()
            .Where(t => t.Namespace == nameSpace
                && t.IsSubclassOf(typeof(Card)))
            .ToList();

        if (types == null)
        {
            throw new NotImplementedException("No types found in namespace " + nameSpace + " that properly" +
                                              " inherit from Card. Check your namespace.");
        }

        foreach (Type t in types)
        {
            workingCards.Add(
                t.GetConstructor(new Type[] { })
                    ?.Invoke(new object[] { }) as Card
                );
        }


        return workingCards.AsReadOnly();
    }
}