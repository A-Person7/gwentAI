using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ConsoleApplication1.Gwent.GwentInstance.AI;

public abstract class OpponentBaseUtils : Player
{
    protected OpponentBaseUtils(Player toCopy) : base(toCopy)
    {
    }

    protected OpponentBaseUtils(GameInstance game, GameInstance.PlayerType type, IEnumerable<Card> deck) : base(game,
        type, deck)
    {
    }

    public sealed class Move : IHashable
    {
        public static readonly Move PassConst = new(null, Card.Types.Melee);

        // note - the type is treated as both the type of the card for general cards and the row to play a special
        // ability card in
        public Move(Card card, Card.Types type, Row.RowTypes rowTypes)
        {
            Card = card;
            Type = type;
            RowType = rowTypes;
        }
        
        public Move (Card card, Card.Types type)
        {
            Card = card;
            Type = type;
            RowType = Row.RowTypes.Melee;
        }

        public bool Pass => Card == null;
        [HashField]
        public Card Card { get; init; }
        [HashField]
        public Card.Types Type { get; init; }
        [HashField]
        public Row.RowTypes RowType { get; init; }
        
        public Move GetClone()
        {
            return Pass ? PassConst : new Move(Card.Clone, Type, RowType);
        }
        
        public long GetAiHash()
        {
            return Pass ? short.MaxValue : AiUtils.HashInternalElements(this);
        }

        // from when Move was a Record
        public void Deconstruct(out Card card, out Card.Types type, out Row.RowTypes rowTypes)
        {
            card = Card;
            type = Type;
            rowTypes = RowType;
        }

        public override string ToString()
        {
            return Pass ? "Pass" : $"{Card.Name} {Type}";
        }

        private bool Equals(Move other)
        {
            return other.Pass != Pass
                   || Card.GetType() == other.Card.GetType() 
                   && Type == other.Type 
                   && RowType == other.RowType;
        }

        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj) || obj is Move other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Card, (int) Type, (int) RowType);
        }
    }
   
    
    protected List<Move> GetPossibleMoves()
    {
        return GetPossibleMoves(Hand);
    }
    
    private static List<Move> GetPossibleMoves(IEnumerable cards)
    {
        List<Move> workingList = new List<Move>();
        
        foreach (Card c in cards)
        {
            if (c.IsSpecialAbility)
            {
                workingList.AddRange
                (from Row.RowTypes t in Enum.GetValues(typeof(Row.RowTypes)) 
                    select new Move(c, Card.Types.SpecialAbility, t));

                continue;
            }

            workingList.AddRange(Card.GetPossibleTypes(c).Select(t => new Move(c, t)));
        }

        workingList.Add(Move.PassConst);

        return workingList;
    }

    protected static Dictionary<int, Move> GetAllMoves()
    {
        List<Move> allMoves = GetPossibleMoves(CardStorer.Cards);
        Dictionary<int, Move> workingDictionary = new Dictionary<int, Move>();

        for (int i = 0; i < allMoves.Count; i++)
        {
            workingDictionary.Add(i, allMoves[i]);
        }

        return workingDictionary;
    }
    
    protected void Play(Move m)
    {
        if (m.Pass)
        {
            Pass();
            return;
        }

        PlayCard(m.Card, m.Type, () => m.RowType);
    }
}