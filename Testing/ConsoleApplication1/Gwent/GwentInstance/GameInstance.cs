using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ConsoleApplication1.Gwent.GwentInstance.AI;

namespace ConsoleApplication1.Gwent.GwentInstance;

/// <summary>
/// God object for Gwent game
///
/// If something is public it's either so <see cref="OpponentAI"/> can simulate it, or so json serialization can work.
/// </summary>
public class GameInstance : IHashable
{
    // whether the game will display with (G)UI or not
    public bool Silent { get; set; }

    [HashField] public readonly Dictionary<Row.RowTypes, Card> _weatherCards;

    public enum PlayerType
    {
        Opponent,
        Player
    }

    // precondition - the game is over and all rounds are played
    public int NumTies()
    {
        int numTies = 0;
        for (int i = 0; i < RoundNumber; i++)
        {
            if (_rounds[i].Tie)
            {
                numTies++;
            }

            if (numTies == 2)
            {
                break;
            }
        }

        return numTies;
    }

    [HashField] public const int RoundNumber = 3;

    [HashField] public readonly Round[] _rounds;
    [HashField] public int _roundNumber;

    public Player Player => Players[PlayerType.Player];
    public Player Opponent => Players[PlayerType.Opponent];

    public static PlayerType OpponentOf(PlayerType playerType)
    {
        if (playerType == PlayerType.Opponent)
        {
            return PlayerType.Player;
        }

        return PlayerType.Opponent;
    }

    public int RoundsWon(PlayerType playerType)
    {
        int roundsWon = 0;
        foreach (Round r in _rounds)
        {
            if (r.Winner == playerType)
            {
                roundsWon++;
            }
        }

        return roundsWon;
    }

    // Note - changes (in function) here should be reflected in the opponent ai simulation of this methid
    public void Play()
    {
        // TODO - implement real gui
        // TODO - implement a check against user cancellation exceptions propagating here to end the game
        while (RoundsWon(PlayerType.Player) < 2 && RoundsWon(PlayerType.Opponent) < 2)
        {
            while (!(Player.Passed && Opponent.Passed))
            {
                if (!Silent)
                {
                    Console.WriteLine();
                    Console.WriteLine("Round  {0}", _roundNumber + 1);
                    Console.WriteLine();

                    Console.WriteLine(ToString());
                    Console.WriteLine();

                    Console.WriteLine("Player turn");
                }

                Player.OnTurn();

                if (!Silent)
                {
                    Console.WriteLine();

                    Console.WriteLine("Opponent turn");
                }

                Opponent.OnTurn();
            }

            ResetForNewRound();

            // TODO - update how ties are handled
            _rounds[_roundNumber].Winner = Opponent.Value > Player.Value ? PlayerType.Opponent : PlayerType.Player;

            _rounds[_roundNumber].Tie = Player.Value == Opponent.Value;

            _roundNumber++;
        }

        // round number has one added to it on the last round, so this undoes that
        _roundNumber--;

        PlayerType winningType = _rounds[_roundNumber].Winner.Value;

        Winner = Players[winningType];

        if (!Silent)
        {
            // since the last winner must always be the winner of the game
            Console.WriteLine("Game over, winner is " +
                              (_rounds[_roundNumber].Winner == PlayerType.Player ? "Player" : "Opponent"));
        }
    }

    public void ResetForNewRound()
    {
        // reset weather cards
        foreach (Row.RowTypes t in _weatherCards.Keys)
        {
            _weatherCards[t] = null;
        }

        Player.ResetForNewRound();
        Opponent.ResetForNewRound();

        Player.DrawCard();
        Opponent.DrawCard();
    }

    [HashField] public Dictionary<Row.RowTypes, Card> WeatherCards => _weatherCards;

    [HashField] public Dictionary<PlayerType, Player> Players { get; }

    [HashField] public PlayerType CurrentPlayer { get; set; }

    [HashField] public Player Winner { get; set; }

    public override string ToString()
    {
        StringBuilder output = new StringBuilder();

        foreach (PlayerType playerType in Players.Keys)
        {
            output.Append(playerType + ":\n");
            output.Append(Players[playerType]).Append("\n");

            for (int i = 0; i < 30; i++)
            {
                output.Append("-");
            }

            output.Append("\n");
        }

        output.Append("Weather Cards: ");

        foreach (Row.RowTypes row in _weatherCards.Keys)
        {
            output.Append(row + ": " + (_weatherCards[row] != null ? _weatherCards[row].Name + " " : "null "));
        }


        return output.ToString();
    }

    public long GetAiHash()
    {
        return AiUtils.HashInternalElements(this);
    }

    public GameInstance(GameInstance toClone)
    {
        _weatherCards = new Dictionary<Row.RowTypes, Card>();
        foreach (Row.RowTypes row in toClone._weatherCards.Keys)
        {
            Card weatherCard = toClone._weatherCards[row];
            _weatherCards.Add(row, weatherCard?.Clone);
        }

        _rounds = new Round[RoundNumber];
        for (int i = 0; i < RoundNumber; i++)
        {
            _rounds[i] = toClone._rounds[i].GetClone();
        }

        _roundNumber = toClone._roundNumber;

        Players = new Dictionary<PlayerType, Player>();
        foreach (PlayerType playerType in toClone.Players.Keys)
        {
            // TODO - make player cloner able to deal with opponentai instances
            Players.Add(playerType, Player.CopyOf(toClone.Players[playerType]));
        }

        foreach (Player player in Players.Values)
        {
            player.GameInstance = this;
            foreach (Row.RowTypes row in Enum.GetValues(typeof(Row.RowTypes)))
            {
                player.Rows[row].GameInstance = this;
            }
        }

        Winner = toClone.Winner;

        CurrentPlayer = toClone.CurrentPlayer;
    }

    /// <summary>
    /// Creates a game instance with the given player decks.
    /// 
    /// </summary>
    /// <param name="playerDeck">Pure</param>
    /// <param name="opponentDeck">Pure</param>
    public GameInstance(IEnumerable<Card> playerDeck, IEnumerable<Card> opponentDeck)
    {
        Winner = null;
        List<Card> playerDeckCopy = playerDeck.Select(c => c.Clone).ToList();
        List<Card> opponentDeckCopy = opponentDeck.Select(c => c.Clone).ToList();

        // define all collections
        Players = new Dictionary<PlayerType, Player>
        {
            [PlayerType.Player] = new Player(this, PlayerType.Player, playerDeckCopy),
            [PlayerType.Opponent] = new Player(this, PlayerType.Opponent, opponentDeckCopy)
        };

        _weatherCards = new Dictionary<Row.RowTypes, Card>
        {
            [Row.RowTypes.Melee] = null,
            [Row.RowTypes.Ranged] = null,
            [Row.RowTypes.Siege] = null
        };

        _rounds = new Round[RoundNumber];
        for (int i = 0; i < RoundNumber; i++)
        {
            _rounds[i] = new Round();
        }

        _roundNumber = 0;
    }

    public sealed record Round
    {
        public PlayerType? Winner { get; set; }
        public bool Tie { get; set; }

        public Round GetClone()
        {
            return new Round
            {
                Winner = Winner
            };
        }
    }
}