#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using ConsoleApplication1.Gwent.GwentInstance;

namespace ConsoleApplication1.Gwent;

/// <summary>
/// This class is responsible for getting user input between options.
///
/// TODO - when ported over to tmodloader, update this for a genuine gui
/// </summary>
public static class UserInterface
{
    public static Row.RowTypes GetRowType()
    {
        List<Row.RowTypes> l = Enum.GetValues(typeof(Row.RowTypes)).Cast<Row.RowTypes>().ToList();

        var actions = new List<Func<String, Row.RowTypes>>
        {
            s => l.First(t => t.ToString().ToLower() == s.ToLower())
        };

        return GetChoiceFromInput(l, t => t.ToString(), actions);
    }
    
    public static Card GetCard(IList<Card> list)
    {
        List<Func<String, Card>> actions = new List<Func<string, Card>>
        {
            s => list.First(c => String.Equals(c.Name, s, StringComparison.CurrentCultureIgnoreCase)),
            s => list.First(c => String.Equals(c.Name.Split(" ")[0], s, StringComparison.CurrentCultureIgnoreCase)),
            s => list.First(c => String.Equals(c.GetType().Name, s, StringComparison.CurrentCultureIgnoreCase))
        };
        
        return GetChoiceFromInput(list, c => c.Name, actions);
    }

    private static void WriteList<T>(IList<T> list, Func<T, String> toString)
    {
        // for each card in list, print out its name
        for (int i = 0; i < list.Count; i++)
        {
            Console.Write(toString.Invoke(list[i]));
            if (i == list.Count - 1)
            {
                Console.WriteLine("");
            }
            else
            {
                Console.Write(", ");
            }
        }
    }

    public static Card.Types GetTypeFromList(List<Card.Types> list)
    {
        List<Func<String, Card.Types>> actions = new List<Func<string, Card.Types>>
        {
            s => list.First(t => String.Equals(t.ToString(), s, 
                StringComparison.CurrentCultureIgnoreCase))
        };

        return GetChoiceFromInput(list, t => t.ToString(), actions);
    }

    /// <summary>
    /// This method takes a list of possible choices, prints them out, grabs the next user input, and runs it against
    /// a list of actions to see if it matches any of the choices. If it does, it returns the result of the action.
    /// If it doesn't, it repeats until all actions have been tried, in which case it considers that an illegal input,
    /// and asks the user to try again.
    /// 
    /// </summary>
    /// <param name="choices">An <see cref="IList{T}"/> of the desired type the user can choose between</param>
    /// <param name="toString">A <see cref="Func{String, T}"/> that takes something of the desired type, and converts it into a string
    /// representing it so users can see it in the list</param>
    /// <param name="actions">An <see cref="IList{T}"/> of functions that return which of item of the list an input corresponds to. If
    /// one fails, the next is tried. This runs first to last. If these functions fail, it is expected that they
    /// call an <see cref="InvalidOperationException"/>. It is advised that these functions ignore String casing.</param>
    /// <typeparam name="T">The type to get from the user</typeparam>
    /// <returns>An instance of type T from choices</returns>
    public static T GetChoiceFromInput<T>(IList<T> choices, Func<T, String> toString, IList<Func<String, T>> actions)
    {
        Debug.Assert(choices != null, "Choices cannot be null");
        Debug.Assert(choices.Any(), "Choices cannot be empty");
        
        while (true)
        {
            WriteList(choices, toString.Invoke);

            if (choices.Count == 1)
            {
                return choices[0];
            }
            
            String? input = Console.ReadLine();

            foreach (Func<String, T> function in actions)
            {
                try
                {
                    return function.Invoke(input ?? "");
                }
                catch (InvalidOperationException)
                {
                    // do nothing
                }
            }
            
            Console.WriteLine("\nInvalid input. Try again.");
        }
    }

    [SerializableAttribute]
    public class UserCancellationException : Exception
    {
        public UserCancellationException()
        {
            // calls the default base constructor
        }

        public UserCancellationException(string? message)
            : base(message)
        { }

        public UserCancellationException(string? message, Exception? innerException)
            : base(message, innerException)
        { }

        protected UserCancellationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }
    }

    /// <summary>
    /// "Wraps" actions with a way to give them names so that users can chose between them.
    /// </summary>
    public class ActionWrapper
    {
        private readonly Action _action;
        private readonly string _name;

        public ActionWrapper(Action action, string name)
        {
            _action = action;
            _name = name;
        }

        public override string ToString()
        {
            return _name;
        }

        public Action ToAction()
        {
            return _action;
        }
    }
}