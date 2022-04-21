namespace ConsoleApplication1.Gwent.GwentCards;

public class PoorFuckingInfantry : Card
{
    protected override void SetInfo()
    {
        Abilities = AbilityTypes.TightBond;
        Value = 1;
        Name = "Poor Fucking Infantry";
        Type = Types.Melee;
        Description = "I's a war veteran! ... spare me a crown?";
    }
}