namespace ConsoleApplication1.Gwent.GwentCards;

public class Cow : Card
{
    protected override void SetInfo()
    {
        Name = "Cow";
        Description = "Mooo!";
        Type = Types.Melee;
        SummonCard = true;
        Replacement = typeof(BovineDefenseForce);
        Value = 0;
    }
}