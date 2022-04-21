namespace ConsoleApplication1.Gwent.GwentCards;

public class BirnaBran : Card
{
    protected override void SetInfo()
    {
        Name = "Birna Bran";
        Description = "Skellige must have a strong king. No matter what it takes.";
        Type = Types.Melee;
        Abilities = AbilityTypes.Medic;
        Value = 2;
    }
}