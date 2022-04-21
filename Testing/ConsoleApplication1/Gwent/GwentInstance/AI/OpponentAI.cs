using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace ConsoleApplication1.Gwent.GwentInstance.AI;

/// <summary>
/// Throw a <see cref="StackOverflowException"/>, it's much faster than waiting for this code to do it.
/// </summary>
public class OpponentAI : OpponentBaseUtils
{
    private GameInstance.PlayerType OpponentType => GameInstance.OpponentOf(PlayerType);

    private int Depth { get; set; }

    private List<Move> PrevMoves { get; set; }

    private Player Opponent => GameInstance.Players[OpponentType];


    public OpponentAI(GameInstance game, GameInstance.PlayerType type, List<Card> deck) : base(game, type, deck)
    {
        Depth = 5;
        PrevMoves = new List<Move>();
    }

    public OpponentAI(GameInstance game, GameInstance.PlayerType type, List<Card> deck, int depth)
        : base(game, type, deck)
    {
        Depth = depth;
        PrevMoves = new List<Move>();
    }

    // for predicting player moves. In theory, none of this needs to be pure because it's a one-off clone anyways
    public OpponentAI(Player player, int depth) : base(player.GameInstance, player.PlayerType, player.Deck)
    {
        Depth = depth;
    }

    // Note - depth will be the same as the original ai's depth, meaning that it will need to be changed elsewhere
    public OpponentAI(OpponentAI toCopy) : base(toCopy)
    {
        Depth = toCopy.Depth;
        PrevMoves = toCopy.PrevMoves ?? new List<Move>();

        if (PrevMoves.Any())
        {
            PrevMoves = PrevMoves.Select(m => m.GetClone())
                .ToList();
        }
    }

    public override void OnTurn()
    {
        // lmao
        Play(Turn());
    }

    // certified based
    private Move Turn()
    {
        if (!Hand.Any())
        {
            return Move.PassConst;
        }

        if (Opponent.Passed)
        {
            if (Value > Opponent.Value)
            {
                return Move.PassConst;
            }

            int difference = Value - Opponent.Value;

            try
            {
                return OptimalMoveGivenCondition((best, working) => working < best && working > difference + 1);
            }
            catch (NoNullAllowedException)
            {
                // do nothing, as this leads to the strongest card being played, so this can be tried again
            }
        }

        // at the end, look for the highest value
        List<Move> moves = DeepOptimalMove((best, working) => working > best);

        return moves.Any() ? moves.First() : Move.PassConst;
    }

    // TODO - adjust fitness appropriately
    private const int RoundFitness = 200;
    private const int TwoRoundFitness = 1000;
    private const int RewardPerPointDifference = 50;

    // todo - make
    private int FitnessOf(List<Move> moves)
    {
        int workingValue = 0;

        GameInstance gDelta = new GameInstance(GameInstance);

        using IEnumerator<Move> enumerator = moves.GetEnumerator();

        ((OpponentAI) gDelta.Players[PlayerType]).Depth = Depth - 1;
        gDelta.Players[GameInstance.OpponentOf(PlayerType)] =
            new OpponentAI(gDelta.Players[GameInstance.OpponentOf(PlayerType)], Depth - 1);

        // ((OpponentAI) gDelta.Players[PlayerType]).PrevMoves = PrevMoves;

        while (gDelta.RoundsWon(GameInstance.PlayerType.Player) < 2 &&
               gDelta.RoundsWon(GameInstance.PlayerType.Opponent) < 2)
        {
            while (!(gDelta.Player.Passed && gDelta.Opponent.Passed))
            {
                if (PlayerType == GameInstance.PlayerType.Player)
                {
                    ((OpponentAI) gDelta.Player).Play(enumerator.Current);
                    Opponent.OnTurn();
                    if (!enumerator.MoveNext())
                    {
                        workingValue += RewardPerPointDifference * (gDelta.Players[PlayerType].Value -
                                                                       gDelta.Players[
                                                                           GameInstance.OpponentOf(PlayerType)].Value);
                        return workingValue;
                    }
                }
                else
                {
                    gDelta.Player.OnTurn();
                    ((OpponentAI) gDelta.Opponent).Play(enumerator.Current);
                    if (!enumerator.MoveNext())
                    {
                        workingValue += RewardPerPointDifference * (gDelta.Players[PlayerType].Value -
                                                                       gDelta.Players[
                                                                           GameInstance.OpponentOf(PlayerType)].Value);
                        return workingValue;
                    }
                }
            }

            gDelta.ResetForNewRound();

            // TODO - update how ties are handled
            gDelta._rounds[gDelta._roundNumber].Winner = Opponent.Value > gDelta.Player.Value
                ? GameInstance.PlayerType.Opponent
                : GameInstance.PlayerType.Player;

            if (gDelta._rounds[gDelta._roundNumber].Winner == PlayerType)
            {
                workingValue += RoundFitness;
            }
            else
            {
                workingValue -= RoundFitness;
            }

            gDelta._roundNumber++;
        }

        // round number has one added to it on the last round, so this undoes that
        gDelta._roundNumber--;

        if (gDelta._rounds[gDelta._roundNumber].Winner == PlayerType)
        {
            workingValue += TwoRoundFitness;
        }
        else
        {
            workingValue -= TwoRoundFitness;
        }

        return workingValue;
    }

    // condition is for optimal move at end of recursion
    private List<Move> DeepOptimalMove(Func<int, int, bool> condition)
    {
        if (Depth == 0)
        {
            return new List<Move>
            {
                OptimalMoveGivenCondition(condition)
            };
        }

        List<List<Move>> moveChainsToAnalyze = new List<List<Move>>();

        foreach (Card c in Hand)
        {
            foreach (Card.Types t in Card.GetPossibleTypes(c))
            {
                if (c.IsSpecialAbility)
                {
                    foreach (Row.RowTypes rowType in (Row.RowTypes[]) Enum.GetValues(typeof(Row.RowTypes)))
                    {
                        DeepMoveAdd(c, t, rowType, ref moveChainsToAnalyze);
                    }
                }
                else
                {
                    DeepMoveAdd(c, t, Row.RowTypes.Melee, ref moveChainsToAnalyze);
                }
            }
        }

        // morelinq
        List<Move> sortedMoveChains = moveChainsToAnalyze.MaxBy(FitnessOf);

        return sortedMoveChains.Any() ? sortedMoveChains : new List<Move> {Move.PassConst};
    }

    private void DeepMoveAdd(Card c, Card.Types t, Row.RowTypes rowType,
        ref List<List<Move>> moveChainsToAnalyze)
    {
        Move m = new Move(c, t, rowType);

        // everything from here on out should specifically be objects in gDelta aside from noted exceptions that are
        // constant for all instances of cloned classes (e.g. PlayerType)
        GameInstance gDelta = new GameInstance(GameInstance);

        ((OpponentAI) gDelta.Players[PlayerType]).Depth = Depth - 1;
        gDelta.Players[GameInstance.OpponentOf(PlayerType)] =
            new OpponentAI(gDelta.Players[GameInstance.OpponentOf(PlayerType)], Depth - 1);

        List<Move> workingMoves = new List<Move>(PrevMoves ?? new List<Move>()) {m};

        ((OpponentAI) gDelta.Players[PlayerType]).PrevMoves = workingMoves;

        while (gDelta.RoundsWon(GameInstance.PlayerType.Player) < 2 &&
               gDelta.RoundsWon(GameInstance.PlayerType.Opponent) < 2)
        {
            while (!(gDelta.Player.Passed && gDelta.Opponent.Passed))
            {
                // player type should be the same for all clones, so it can be checked against this class instance's
                // player type
                if (PlayerType == GameInstance.PlayerType.Player)
                {
                    // PrevMoves.Add(((OpponentAI) gDelta.Player).Turn());
                    ((OpponentAI) gDelta.Players[PlayerType]).PrevMoves.Add(((OpponentAI) gDelta.Player).Turn());
                    gDelta.Opponent.OnTurn();
                }
                else
                {
                    gDelta.Player.OnTurn();
                    // PrevMoves.Add(((OpponentAI)gDelta.Opponent).Turn());
                    ((OpponentAI) gDelta.Players[PlayerType]).PrevMoves.Add(((OpponentAI) gDelta.Player).Turn());
                }
            }

            // these object updates are so the next opponentai clone can see how the game object treated the previous 
            // moves (in theory)
            gDelta.ResetForNewRound();

            // TODO - update how ties are handled
            gDelta._rounds[gDelta._roundNumber].Winner = Opponent.Value > gDelta.Player.Value
                ? GameInstance.PlayerType.Opponent
                : GameInstance.PlayerType.Player;

            gDelta._roundNumber++;

            moveChainsToAnalyze.Add(((OpponentAI) gDelta.Players[PlayerType]).PrevMoves);
        }
    }

    // TODO - understand how to use Comparer<int> and see if using it is worth it
    // arguments for condition are bestDifference, workingDifference. The result is if workingDifference and its move
    // should replace best difference.
    private Move OptimalMoveGivenCondition(Func<int, int, bool> condition)
    {
        if (!Hand.Any())
        {
            throw new InvalidOperationException("Pre-requisites to call function not met:n\\t" +
                                                "Hand is not empty.");
        }

        Card starter = Hand.First();

        Move bestMove = new Move(starter, Card.GetPossibleTypes(starter).First(),
            Row.RowTypes.Melee);

        int bestValue = GetPossibleValueDifference(bestMove);

        foreach (Card c in Hand)
        {
            foreach (Card.Types t in Card.GetPossibleTypes(c))
            {
                if (c.IsSpecialAbility)
                {
                    foreach (Row.RowTypes rowType in (Row.RowTypes[]) Enum.GetValues(typeof(Row.RowTypes)))
                    {
                        CheckMove(c, t, rowType, condition, ref bestValue, ref bestMove);
                    }
                }
                else
                {
                    CheckMove(c, t, Row.RowTypes.Melee, condition, ref bestValue, ref bestMove);
                }
            }
        }

        if (bestMove == null)
        {
            // TODO - fix, as a condition could prevent there being a best move
            throw new NoNullAllowedException();
        }

        return bestMove;
    }

    private void CheckMove(Card c, Card.Types t, Row.RowTypes rowType, Func<int, int, bool> condition,
        ref int bestValue, ref Move bestMove)
    {
        Move m = new Move(c, t, rowType);
        int workingValue = GetPossibleValueDifference(m);
        if (condition.Invoke(bestValue, workingValue))
        {
            bestValue = workingValue;
            bestMove = m;
        }
    }

    private int GetPossibleValueDifference(Move m)
    {
        return GetPossibleValueDifference(m.Card, m.Type, m.TypeSpecialAbility);
    }

    // pure
    private int GetPossibleValueDifference(Card c, Card.Types type, Row.RowTypes rowType)
    {
        // game instance with the possible change
        GameInstance gDelta = new GameInstance(GameInstance);

        gDelta.Players[PlayerType].PlayCard(c.Clone, type, () => rowType);

        return gDelta.Players[PlayerType].Value - gDelta.Players[GameInstance.OpponentOf(PlayerType)].Value;
    }
}