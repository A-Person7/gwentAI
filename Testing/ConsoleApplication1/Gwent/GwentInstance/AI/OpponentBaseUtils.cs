using System;
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
        public static readonly Move PassConst =
            new Move(null, Card.Types.Melee, Row.RowTypes.Melee);

        public Move(Card Card, Card.Types Type, Row.RowTypes TypeSpecialAbility)
        {
            this.Card = Card;
            this.Type = Type;
            this.TypeSpecialAbility = TypeSpecialAbility;
        }

        public bool Pass => Card == null;
        [HashField]
        public Card Card { get; init; }
        [HashField]
        public Card.Types Type { get; init; }
        [HashField]
        public Row.RowTypes TypeSpecialAbility { get; init; }

        public Move GetClone()
        {
            if (Pass)
            {
                return PassConst;
            }

            return new Move(Card.Clone, Type, TypeSpecialAbility);
        }
        
        public int GetAiHash()
        {
            return AiUtils.HashInternalElements(this);
        }

        public void Deconstruct(out Card Card, out Card.Types Type, out Row.RowTypes TypeSpecialAbility)
        {
            Card = this.Card;
            Type = this.Type;
            TypeSpecialAbility = this.TypeSpecialAbility;
        }
        
        public override string ToString()
        {
            return Pass ? "Pass" : $"{Card.Name} {Type} {TypeSpecialAbility}";
        }
    }

    protected List<Move> GetPossibleMoves()
    {
        List<Move> workingList = new List<Move>();
        
        foreach (Card c in Hand)
        {
            foreach (Card.Types t in Card.GetPossibleTypes(c))
            {
                if (c.IsSpecialAbility)
                {
                    workingList.AddRange(from rowType in (Row.RowTypes[]) Enum.GetValues(typeof(Row.RowTypes)) 
                        select new Move(c, t, rowType));
                }
                else
                {
                    workingList.Add(new Move(c, t, Row.RowTypes.Melee));
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

        PlayCard(m.Card, m.Type, () => m.TypeSpecialAbility);
    }
}