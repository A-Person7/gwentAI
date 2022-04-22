using System;
using System.Collections.Generic;

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
        public static readonly Move PassConst =
            new Move(null, Card.Types.Melee);

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
            if (Pass)
            {
                return PassConst;
            }

            return new Move(Card.Clone, Type, RowType);
        }
        
        public int  GetAiHash()
        {
            return AiUtils.HashInternalElements(this);
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
    }

    protected List<Move> GetPossibleMoves()
    {
        List<Move> workingList = new List<Move>();
        
        foreach (Card c in Hand)
        {
            if (c.IsSpecialAbility)
            {
                foreach (Row.RowTypes t in Enum.GetValues(typeof(Row.RowTypes)))
                {
                    workingList.Add(new Move(c, Card.Types.SpecialAbility, t));
                }

                continue;
            }
            foreach (Card.Types t in Card.GetPossibleTypes(c))
            {
                {
                    workingList.Add(new Move(c, t));
                }
            }
        }

        workingList.Add(Move.PassConst);

        return workingList;
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