using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using ConsoleApplication1.AIComponents;
using MoreLinq;

namespace ConsoleApplication1.Gwent.GwentInstance.AI;

/// <summary>
/// This is intended to be a potential replacement of <see cref="OpponentAI"/> due to its slow nature. This should
/// theoretically use genuine genetic algorithms to determine what move to play.
/// </summary>
public class OpponentTrueAi : OpponentBaseUtils
{
    private readonly Network<Move> _network;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        AllowTrailingCommas = true,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        WriteIndented = true
    };

    private const string DataTargetPath = "ConsoleApplication1/Gwent/GwentInstance/AI/Target/";
    private const string DataSourcePath = DataTargetPath;
    private const string DataTargetFileEnding = "_generation.json";
    private static int _generations;

    // note - this is not a true clone -- the network is adjusted by a random amount to get a similar deck
    private OpponentTrueAi(OpponentTrueAi toCopy) : base(toCopy)
    {
        float randDeviance = _generations < 10 ? 300f : 20f;
        // morelinq
        Deck = Deck.Shuffle().ToList();

        try
        {
            FileInfo fileInfo = GetLastFile();
            _network = int.Parse(fileInfo.Name[..^DataTargetFileEnding.Length]) > _generations
                ? GetLastNetwork()
                : new Network<Move>(toCopy._network, randDeviance);
        }
        catch (IOException)
        {
            _network = new Network<Move>(toCopy._network, randDeviance);
        }
    }

    public OpponentTrueAi(GameInstance game, GameInstance.PlayerType type, IEnumerable<Card> deck) : base(game, type,
        deck)
    {
        try
        {
            _network = GetLastNetwork();
            Console.WriteLine("\nLoading coefficients...\n");
        }
        catch (IOException)
        {
            Console.WriteLine("\nNo previous data found, using default coefficients\n");

            // deviance is added so that starting weightCoefficients are not all the same
            _network = new Network<Move>(GetStartingNetwork(), 2.5);
        }
    }

    public OpponentTrueAi(GameInstance game, GameInstance.PlayerType type, IEnumerable<Card> deck,
        Network<Move> network) : base(game, type, deck)
    {
        _network = network;
    }

    private static void SetGenerations(int generations)
    {
        _generations = generations;
    }

    // throws IOException if file not found
    private static Network<Move> GetLastNetwork()
    {
        FileInfo lastFile = GetLastFile();

        SetGenerations(int.Parse(lastFile.Name[..^DataTargetFileEnding.Length]));

        Console.WriteLine($"Generations: {_generations}");

        string data = File.ReadAllText(lastFile.FullName);

        return JsonSerializer.Deserialize<Network<Move>>(data);
    }

    private static FileInfo GetLastFile()
    {
        DirectoryInfo directoryInfo = new DirectoryInfo(DataSourcePath);
        FileInfo[] files = directoryInfo.GetFiles("*.json");

        if (files.Length == 0)
        {
            throw new FileNotFoundException("No files found");
        }

        FileInfo lastFile = files.OrderByDescending(f => f.LastWriteTime).First();

        return lastFile;
    }

    private static Network<Move> GetStartingNetwork()
    {
        int[] startingShape = {3, 7, 8, GetAllMoves().Count};

        List<List<Node>> workingNodes = new List<List<Node>>();

        for (int i = 0; i < startingShape.Length; i++)
        {
            workingNodes.Add(new List<Node>());
            for (int j = 0; j < startingShape[i]; j++)
            {
                workingNodes[i].Add(new Node());
            }
        }

        return new Network<Move>(GetAllMoves(), workingNodes);
    }

    public override void OnTurn()
    {
        if (Passed)
        {
            return;
        }

        // TODO - consider removing
        if (Opponent.Passed && Value > Opponent.Value)
        {
            // you win
            Pass();
            return;
        }
        
        Move m = _network.GetOutput(DataAsList(), GetPossibleMoves(), Move.PassConst);
        Console.WriteLine($"{m}");
        Play(m);
    }

    private IEnumerable<double> DataAsList()
    {
        return new List<double>
        {
            Hand.Count,
            Value,
            Opponent.Value,
        };
    }

    private string GetDataOut()
    {
        return JsonSerializer.Serialize(_network, JsonOptions);
    }

    private void WriteData(int generation)
    {
        try
        {
            File.WriteAllText(DataTargetPath + generation + DataTargetFileEnding,
                GetDataOut());
        }
        catch (DirectoryNotFoundException)
        {
            Directory.CreateDirectory(DataTargetPath);
            WriteData(generation);
        }
    }

    // pure (in theory)
    // TODO - move to utility method
    public static void Simulate(List<Card> playerDeck, List<Card> opponentDeck)
    {
        OpponentTrueAi workingBest = new OpponentTrueAi(null, GameInstance.PlayerType.Player, playerDeck);

        // technically makes this killable
        while (Thread.CurrentThread.IsAlive)
        {
            GameInstance g = new GameInstance(playerDeck, opponentDeck)
            {
                Silent = true,
                Players =
                {
                    [GameInstance.PlayerType.Player] = new OpponentTrueAi(workingBest),
                    [GameInstance.PlayerType.Opponent] = new OpponentTrueAi(workingBest)
                }
            };

            g.Players[GameInstance.PlayerType.Opponent].GameInstance = g;
            g.Players[GameInstance.PlayerType.Opponent].Deck =
                new List<Card>(opponentDeck.Select(c => c.Clone).ToList());

            g.Players[GameInstance.PlayerType.Player].GameInstance = g;
            g.Players[GameInstance.PlayerType.Player].Deck =
                new List<Card>(playerDeck.Select(c => c.Clone).ToList());

            g.Play();

            Console.WriteLine("\nTies: " + g.NumTies() + "\n");

            if (g.NumTies() >= 2)
            {
                continue;
            }

            workingBest = (OpponentTrueAi) g.Winner;
            _generations++;

            // if (_generations % 10 != 0)
            // {
            //     continue;
            // }

            workingBest.WriteData(_generations);

            Console.WriteLine("\n{0} games simulated\n", _generations);
        }
    }
}