using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ConsoleApplication1.Gwent.GwentInstance.AI;

namespace ConsoleApplication1.Gwent.GwentInstance;

public class Player : IHashable
{
    public static Player CopyOf(Player player)
    {
        if (player is OpponentAI opponentAi)
        {
            return new OpponentAI(opponentAi);
        }

        return new Player(player);
    }
    
    protected Player(Player toCopy)
    {
        // To callers - you need to set gameinstance properly in the gameinstance copy ctor
        
        Hand = new List<Card>(toCopy.Hand.Select(c => c.Clone).ToList());
        Deck = new List<Card>(toCopy.Deck.Select(c => c.Clone).ToList());
        Passed = toCopy.Passed;
        DiscardPile = new List<Card>(toCopy.DiscardPile.Select(c => c.Clone).ToList());
        
        // these rows should all be overwritten by the gameinstance copy ctor. They are here to define the keys
        Rows = new Dictionary<Row.RowTypes, Row>
        {
            [Row.RowTypes.Melee] = new Row(toCopy.GameInstance, PlayerType, Row.RowTypes.Melee),
            [Row.RowTypes.Ranged] = new Row(toCopy.GameInstance, PlayerType, Row.RowTypes.Ranged),
            [Row.RowTypes.Siege] = new Row(toCopy.GameInstance, PlayerType, Row.RowTypes.Siege)
        };
    }
    
    [HashField]
    public bool Passed { get; set; }

    // not readonly because it will need to be modified in the gameinstance copy constructor
    public GameInstance GameInstance;
    [HashField]
    public readonly GameInstance.PlayerType PlayerType;
    [HashField]
    public int Value => Rows.Values.Sum(r => r.Value);

    [HashList]
    public Dictionary<Row.RowTypes, Row> Rows { get; set; }
    [HashList]
    public List<Card> DiscardPile { get; set; }
    [HashList]
    protected List<Card> Hand { get; set; }
    [HashList]
    public List<Card> Deck { get; set; }

    // This is needed for OpponentTrueAI calculations as well
    protected const int NumCardsStartingHand = 10;
    
    public Player(GameInstance game, GameInstance.PlayerType type, IEnumerable<Card> deck)
    {
        GameInstance = game;
        PlayerType = type;

        Rows = new Dictionary<Row.RowTypes, Row>
        {
            [Row.RowTypes.Melee] = new Row(GameInstance, PlayerType, Row.RowTypes.Melee),
            [Row.RowTypes.Ranged] = new Row(GameInstance, PlayerType, Row.RowTypes.Ranged),
            [Row.RowTypes.Siege] = new Row(GameInstance, PlayerType, Row.RowTypes.Siege)
        };

        Hand = new List<Card>();
        // shuffles the deck
        // TODO - use Main.rand.Next
        Deck = deck.OrderBy(_ => new Random().Next()).ToList();
        DiscardPile = new List<Card>();
        // TODO - implement a way to redraw cards
        for (int i = 0; i < NumCardsStartingHand; i++)
        {
            DrawCard();
        }
    }

    public void ResetForNewRound()
    {
        Passed = false;
        foreach (Row row in Rows.Values)
        {
            row.Clear();
        }
    }
    
    public override string ToString()
    {
        StringBuilder output = new StringBuilder();

        Array rowTypes = Enum.GetValues(typeof(Row.RowTypes));

        foreach (Row.RowTypes type in rowTypes)
        {
            output.Append(type + ":\n\t");
            output.Append(Rows[type]).Append('\n');
        }

        return output.ToString();
    }

    public int GetAiHash()
    {
        return AiUtils.HashInternalElements(this);
    }

    public void DrawCard()
    {
        if (!Deck.Any())
        {
            // TODO - determine what to do when deck is empty
            return;
        }
        
        Card c = Deck[0];
        Deck.RemoveAt(0);
        Hand.Add(c);
    }

    public virtual void OnTurn()
    {
        if (Passed)
        {
            return;
        }
        
        while (true)
        {
            try
            {
                // TODO - implement action switching here
                List<UserInterface.ActionWrapper> actions = new List<UserInterface.ActionWrapper>
                {
                    new UserInterface.ActionWrapper(Pass, "Pass")
                };

                if (Hand.Any())
                {
                    actions.Add(new UserInterface.ActionWrapper(PlayCard, "Play Card"));
                }

                List<Func<String, UserInterface.ActionWrapper>> inputToMethod = new List<Func<string, UserInterface.ActionWrapper>>
                {
                    s => actions.First(a => a.ToString()?.ToLower() == s.ToLower())
                };

                Action action = UserInterface.GetChoiceFromInput(actions, a => a.ToString(),
                    inputToMethod)
                    .ToAction();
                
                action.Invoke();
                
                break;
            }
            catch (UserInterface.UserCancellationException)
            {
                // repeat getting user input 
            }
        }
    }

    protected void Pass()
    {
        // TODO - implement
        Passed = true;
    }

    private void PlayCard()
    {
        Card c = UserInterface.GetCard(Hand);
        Card.Types type = UserInterface.GetTypeFromList(Card.GetPossibleTypes(c));
        PlayCard(c, type, () => UserInterface.GetRowType());
    }

    // This takes a function for getting the row for a special card because that might involve asking the user, which
    // should only happen if the card is a special card, which is not always the case.
    public void PlayCard(Card c, Card.Types type, Func<Row.RowTypes> getRowSpecialCard)
    {
        Hand.Remove(c);

        if (Row.IsRowType(type))
        {
            Rows[(Row.RowTypes) type].AddWType(c, type);
        }
        else if (type == Card.Types.SpecialAbility)
        {
            Row.RowTypes rowTypes = getRowSpecialCard.Invoke();
            Rows[rowTypes].SpecialAbility = c;
        }
        else if (type == Card.Types.Weather)
        {
            // TODO - implement this
            // throw new NotImplementedException();
        }
    }
}