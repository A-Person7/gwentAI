using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace ConsoleApplication1.Gwent.GwentInstance.AI;

/// <summary>
/// This is intended to be a potential replacement of <see cref="OpponentAI"/> due to its slow nature. This should
/// theoretically use genuine genetic algorithms to determine what move to play.
/// </summary>
public class OpponentTrueAi : OpponentBaseUtils
{
    // only copies should technically be deviated from
    private readonly List<List<float>> _weightCoefficients;

    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        AllowTrailingCommas = true,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        WriteIndented = true
    };
    
    private const string DataTargetPath = "ConsoleApplication1/Gwent/GwentInstance/AI/Target/";
    private const string DataSourcePath = DataTargetPath;
    private const string DataTargetFileEnding = "_round.json";
    private static int _generations;

    // TODO - update random method
    // apparently you can do this
    private readonly Random _rand = new(Guid.NewGuid().GetHashCode());

    // note - this is not a true clone -- weightCoefficients list is adjusted by a random amount to get a similar deck
    private OpponentTrueAi(OpponentTrueAi toCopy) : base(toCopy)
    {
        try
        {
            FileInfo fileInfo = GetLastFile();
            _weightCoefficients = int.Parse(fileInfo.Name.Substring(0, fileInfo.Name.Length -
                                                                       DataTargetFileEnding.Length)) > _generations ? 
                GetLastCoefficients() : 
                GetCoefficientsWDeviance(toCopy._weightCoefficients, _generations < 10 ? 100f : 20f);
        }
        catch (IOException)
        {
            _weightCoefficients = GetCoefficientsWDeviance(toCopy._weightCoefficients, _generations < 10 ? 100f : 20f);
        }
    }

    public OpponentTrueAi(GameInstance game, GameInstance.PlayerType type, IEnumerable<Card> deck) : base(game, type,
        deck)
    {
        try
        {
            _weightCoefficients = GetLastCoefficients();
            Console.WriteLine("\nLoading coefficients...\n");
        }
        catch (IOException)
        {
            Console.WriteLine("\nNo previous data found, using default coefficients\n");
            
            // deviance is added so that starting weightCoefficients are not all the same
            _weightCoefficients = GetCoefficientsWDeviance(GetStartingCoefficents(), 5f);
        }
    }

    public OpponentTrueAi(GameInstance game, GameInstance.PlayerType type, IEnumerable<Card> deck,
        List<List<float>> weightCoefficients) : base(game, type, deck)
    {
        _weightCoefficients = weightCoefficients;
    }

    // throws IOException if file not found
    private List<List<float>> GetLastCoefficients() {
        FileInfo lastFile = GetLastFile();
        
        _generations = int.Parse(lastFile.Name.Substring(0, lastFile.Name.Length - 
                                                            DataTargetFileEnding.Length));

        Console.WriteLine("Generations: " + _generations);
        
        string data = File.ReadAllText(lastFile.FullName);

        return JsonSerializer.Deserialize<List<List<float>>>(data);
    }
    
    private FileInfo GetLastFile()
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

    private static List<List<float>> GetStartingCoefficents()
    {
        List<List<float>> workingList = new List<List<float>>();

        // first list should include weightCoefficients for every part of a card that effects play
        // second list should 

        // v is starting value
        float v = 0.75f;

        int[] startingShape = {4, 4, 5, 6, 5, 4, 3};

        foreach (int i in startingShape)
        {
            List<float> l = new List<float>();
            for (int j = 0; j < i; j++)
            {
                l.Add(v);
            }

            workingList.Add(l);
        }

        return workingList;
    }

    // time for some cognitive complexity coding and magic numbers/values/methods, etc.
    public override void OnTurn()
    {
        // morelinq
        Move m = GetPossibleMoves().MaxBy(MoveToStrength);
        Play(m);
        Console.WriteLine(m.ToString());
    }

    private float MoveToStrength(Move m)
    {
        float workingValue = m.GetAiHash();
        float lastValue = workingValue;

        foreach (float workingLocalValue in _weightCoefficients
                     .Select(list => list.Sum(f => f * lastValue)))
        {
            lastValue = workingLocalValue;
            workingValue += workingLocalValue;
        }

        return workingValue;
    }


    // note - must be pure with respect to prev weightCoefficients
    private List<List<float>> GetCoefficientsWDeviance(List<List<float>> prevCoefficients, float maxDeviance)
    {
        List<List<float>> workingCoefficients = new List<List<float>>();

        // TODO - implement adding/removing nodes and adding/removing columns

        foreach (List<float> listCoefficients in prevCoefficients)
        {
            List<float> workingList = new List<float>();
            foreach (float f in listCoefficients)
            {
                workingList.Add(f + (float) (_rand.NextDouble() - 0.5)
                    * maxDeviance * 2 - maxDeviance);

                if (_rand.NextDouble() <= 0.925)
                {
                    continue;
                }

                if (_rand.NextDouble() > 0.5)
                {
                    if (_rand.NextDouble() > 0.5)
                    {
                        workingList.Add(-0.5f);
                    }
                    else if (_rand.NextDouble() > 0.5)
                    {
                        workingList.Add(0.5f);
                    }
                }
                else
                {
                    workingList.Remove(f);
                }
            }

            workingCoefficients.Add(workingList);

            if (_rand.NextDouble() <= 0.975)
            {
                continue;
            }

            if (_rand.NextDouble() > 0.5)
            {
                workingCoefficients.Add(new List<float>
                {
                    (float) _rand.NextDouble(),
                    (float) _rand.NextDouble(),
                    (float) _rand.NextDouble(),
                });
            }
            else if (workingCoefficients.Count > 2)
            {
                workingCoefficients.RemoveAt(workingCoefficients.Count - 1);
            }
        }

        return workingCoefficients;
    }

    private string GetDataOut()
    {
        return JsonSerializer.Serialize(_weightCoefficients, JsonOptions);
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
    public static void Simulate(List<Card> playerDeck, List<Card> opponentDeck)
    {
        OpponentTrueAi workingBest = new OpponentTrueAi(null, GameInstance.PlayerType.Player, playerDeck);
        
        while (true)
        {

            GameInstance g = new GameInstance(playerDeck, opponentDeck);
            g.Silent = true;

            g.Players[GameInstance.PlayerType.Opponent] = new OpponentTrueAi(workingBest);
            g.Players[GameInstance.PlayerType.Opponent].GameInstance = g;
            g.Players[GameInstance.PlayerType.Opponent].Deck =
                new List<Card>(opponentDeck.Select(c => c.Clone).ToList());

            g.Players[GameInstance.PlayerType.Player] = new OpponentTrueAi(workingBest);
            g.Players[GameInstance.PlayerType.Player].GameInstance = g;
            g.Players[GameInstance.PlayerType.Player].Deck = new List<Card>(playerDeck.Select(c => c.Clone).ToList());

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