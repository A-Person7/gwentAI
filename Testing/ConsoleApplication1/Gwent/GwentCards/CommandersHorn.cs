namespace ConsoleApplication1.Gwent.GwentCards;

public class CommandersHorn : Card
{
    protected override void SetInfo()
    {
        Name = "Commanders Horn";
        Description = "";
        Type = Types.SpecialAbility;
        Abilities = AbilityTypes.Horn;
    }
}