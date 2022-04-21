namespace ConsoleApplication1.Gwent.GwentCards;

public class Olaf : Card {
    protected override void SetInfo() {
        Name = "Olaf";
        Type = Types.Melee | Types.Ranged;
        Abilities = AbilityTypes.MoraleBoost;
        
        Description = "Many've tried to defeat Olaf. But won't hear about it from them - they're dead.";
        Value = 12;
    }
}