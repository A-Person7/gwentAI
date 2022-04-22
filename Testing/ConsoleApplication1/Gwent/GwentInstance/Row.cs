using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ConsoleApplication1.Gwent.GwentInstance.AI;

namespace ConsoleApplication1.Gwent.GwentInstance;

public class Row : IList<Card>, IHashable
{
    public Row(Row toCopy)
    {
        // Callers - you will need to manually set the game instance in the game instance copy constructor (or
        // elsewhere)
        _specialAbility = toCopy._specialAbility;
        CardInRow = new List<Card>(toCopy.CardInRow.Select(c => c.Clone).ToList());
        PlayerType = toCopy.PlayerType;
        RowType = toCopy.RowType;
    }
    
    [HashField]
    private Card _specialAbility;
    [HashField]
    private RowTypes RowType { get; }
    public GameInstance GameInstance { get; set; }
    [HashField]
    private GameInstance.PlayerType PlayerType { get; }
    
    [HashField]
    private List<Card> CardInRow { get; }

    [HashField]
    public int Value => CardInRow.Sum(c => c.ActingValue);
    

    public static bool IsRowType(Card.Types type)
    {
        return Enum.GetValues(typeof(RowTypes)).Cast<int>().Contains((int) type);
    }
    
    public void AddWType(Card item, Card.Types type)
    {
        if ((int)type == (int)RowType)
        {
            Add(item);
            return;
        } 
        if ((type & Card.Types.SpecialAbility) == Card.Types.SpecialAbility)
        {
            if (_specialAbility == null)
            {
                throw new AggregateException("Special ability is already set");
            }
            _specialAbility = item;
            return;
        }
        
        throw new ArgumentException("Card " + item.Name + " with a given type of " + type + " is not of type " 
                                    + RowType);
    }
    
    /// <summary>
    /// This is a reference to the special ability card in play, or a null reference if there is none.
    ///
    /// SpecialAbility.IsSpecialAbility should always be true if this isn't null.
    /// </summary>
    public Card SpecialAbility
    {
        get => _specialAbility;
        set
        {
            // TODO - figure something out for this
            // UpdateCardOnRemoval(_specialAbility);
            if (value != null)
            {
                
                UpdateCardOnAdd(value);
            }

            _specialAbility = value;
        }
    }

    public override string ToString()
    {
        // print out the special ability card, weather card, then the cards in the row
        StringBuilder output = new StringBuilder();

        UpdateStringBuilderWCard(output, _specialAbility);

        output.Append("| ");

        foreach (Card card in CardInRow)
        {
            UpdateStringBuilderWCard(output, card);
        }

        return output.ToString();
    }

    public int GetAiHash()
    {
        return AiUtils.HashInternalElements(this);
    }

    private void UpdateStringBuilderWCard(StringBuilder output, Card card)
    {
        if (card == null)
        {
            return;
        }

        output.Append(card.Name + " " + (card != SpecialAbility ? card.ActingValue + ", " : ""));
    }

    private void UpdateCardOnAddRow(Card item)
    {
        if (item == null)
        {
            throw new ArgumentNullException(nameof(item));
        }

        if (((int) item.Type & (int) RowType) != (int) RowType)
        {
            throw new ArgumentException("Card type " + item.Type + " does not match row type " + RowType + ".");
        }

        UpdateCardOnAdd(item);
    }

    private void UpdateCardOnAdd(Card item)
    {
        if (item == null)
        {
            return;
        }
        
        if (_specialAbility != null)
        {
            item.OnSpecialAbility(_specialAbility);
        }

        item.OnPlay(RowType);
        
        item.GameInstance = GameInstance;
        item.PlayerType = PlayerType;
    }

    private void UpdateCardOnRemoval(Card item)
    {
        if (item == null)
        {
            return;
        }
        
        item.OnRemoval(RowType);
        GameInstance.Players[PlayerType].DiscardPile.Add(item);
    }

    public IEnumerator<Card> GetEnumerator()
    {
        return CardInRow.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Add(Card item)
    {
        UpdateCardOnAddRow(item);
        CardInRow.Add(item);
    }

    public void Clear()
    {
        CardInRow.Clear();
        SpecialAbility = null;
    }

    public bool Contains(Card item)
    {
        return CardInRow.Contains(item);
    }

    public void CopyTo(Card[] array, int arrayIndex)
    {
        CardInRow.CopyTo(array, arrayIndex);
    }

    public bool Remove(Card item)
    {
        UpdateCardOnRemoval(item);
        return CardInRow.Remove(item);
    }

    public int Count { get; }
    public bool IsReadOnly { get; }

    public int IndexOf(Card item)
    {
        return CardInRow.IndexOf(item);
    }

    public void Insert(int index, Card item)
    {
        UpdateCardOnAddRow(item);
        CardInRow.Insert(index, item);
    }

    public void RemoveAt(int index)
    {
        UpdateCardOnRemoval(CardInRow[index]);
        CardInRow.RemoveAt(index);
    }

    public Card this[int index]
    {
        get => CardInRow[index];
        set => CardInRow[index] = value;
    }

    public Row(GameInstance g, GameInstance.PlayerType playerType, RowTypes type)
    {
        CardInRow = new List<Card>();
        _specialAbility = null;
        Count = 0;
        IsReadOnly = false;
        RowType = type;
        GameInstance = g;
        PlayerType = playerType;
    }

    public void ForEach(Action<Card> action)
    {
        CardInRow.ForEach(action);
    }

    // these are not flags, do not use them as flags
    public enum RowTypes
    {
        Melee = Card.Types.Melee,
        Ranged = Card.Types.Ranged,
        Siege = Card.Types.Siege
    }
}