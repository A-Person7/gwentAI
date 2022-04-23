using System.Collections.Generic;
using System.Linq;
using ConsoleApplication1.Gwent;
using ConsoleApplication1.Gwent.GwentCards;
using ConsoleApplication1.Gwent.GwentInstance;
using ConsoleApplication1.Gwent.GwentInstance.AI;

namespace ConsoleApplication1;

public static class Program
{
    public static void Main(string[] args)
    {
        List<Card> playerCards = new List<Card>
        {
            new PoorFuckingInfantry(),
            new PoorFuckingInfantry(),
            new PoorFuckingInfantry(),
            new BirnaBran(),
            new BitingFrost(),
            new CirillaFionaElenRiannon(),
            new Cow(),
            new CommandersHorn(),
            new CommandersHorn(),
            new BitingFrost()
        };

        List<Card> opponentCards = new List<Card>
        {
            new BirnaBran(),
            new BitingFrost(),
            new Cow(),
            new CommandersHorn(),
            new CommandersHorn(),
            new BitingFrost(),
            new Olaf(),
            new BovineDefenseForce()
        };

        OpponentTrueAi.Simulate(playerCards, opponentCards);
        
        // GameInstance g = new GameInstance(playerCards, opponentCards);
        // g.Players[GameInstance.PlayerType.Opponent] = new OpponentTrueAi(g, GameInstance.PlayerType.Opponent,
            // opponentCards.Select(c => c.Clone).ToList());
        
        // g.Play();
    }
}