using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using ConsoleApplication1.Gwent.GwentInstance;
using ConsoleApplication1.Gwent.GwentInstance.AI;

namespace ConsoleApplication1.Gwent;

/// <summary>
/// We do a little bit of god-classing...
///
/// This class is responsible for storing cards, their abilities, types, etc. It is also responsible for storing the
/// multipliers, value increases, etc. applied to this card.
/// </summary>
public abstract class Card : IHashable
{
    // TODO - possibly change siege to magic

    // TODO - change the naming conventions of this and card storer

    [Flags]
    public enum Types
    {
        // set bitwise types so I can refer to either of them
        Melee = 1,
        Ranged = 2,
        Siege = 4,
        SpecialAbility = 8,
        Weather = 16
    }


    [Flags]
    protected enum AbilityTypes
    {
        None = 0,
        Medic = 1,
        MoraleBoost = 2,
        TightBond = 4,
        Horn = 8,
        Muster = 16,
        Scorch = 32,
        Spy = 64,
        Mardroemer = 128,
        Hero = 256,
        Summon = 512,
    }

    [HashField]
    public int Value { get; set; }

    public String Name { get; set; }
    public List<string> AbilityDescription { get; set; }

    public String Description { get; set; }

    [HashField]
    // should never return true if the card has any other abilities
    public bool IsHero => Abilities == AbilityTypes.Hero;
    
    [field: HashField]
    public Types Type { get; set; }

    [HashField]
    private Row.RowTypes? _row;
    
    // to be used for getting what the value of this card could be
    public Row.RowTypes Row
    {
        set { _row = value; }
    }

    public GameInstance GameInstance { get; set; }

    [HashField]
    public GameInstance.PlayerType PlayerType { get; set; }

    public Player Player => GameInstance.Players[PlayerType];

    public Card Clone => (Card) MemberwiseClone();

// TODO - possibly change to just the nearby cards?
    [HashField]
    protected AbilityTypes Abilities { get; set; }
    // TODO - possibly change these booleans instead to enums that are checked?

    [HashField]
    public bool Agile => IsMelee && IsRanged;

    // this should only ever be true if special ability is true as well
    [HashField]
    protected bool SpecialAbilityDoubler => Abilities.HasFlag(AbilityTypes.Horn);

    [HashField]
    public bool Spy { get; set; }

    [HashField]
    public bool SummonCard { get; set; }

    /// <summary>
    /// Should only be defined if SummonCard is true.
    /// </summary>
    [HashField]
    public Type Replacement { get; set; }

    [HashField]
    public int ActingValue => GetActingValue();
    
    public bool IsMelee => (Type & Types.Melee) == Types.Melee;
    public bool IsRanged => (Type & Types.Ranged) == Types.Ranged;
    public bool IsSiege => (Type & Types.Siege) == Types.Siege;
    public bool IsSpecialAbility => (Type & Types.SpecialAbility) == Types.SpecialAbility;
    public bool IsWeather => (Type & Types.Weather) == Types.Weather;
    public bool TightBond => (Abilities & AbilityTypes.TightBond) == AbilityTypes.TightBond;
    public bool Medic => (Abilities & AbilityTypes.Medic) == AbilityTypes.Medic;
    public bool MoraleBoost => (Abilities & AbilityTypes.MoraleBoost) == AbilityTypes.MoraleBoost;


    /// <summary>
    /// Note - this is called externally in the card storer through reflection.
    ///
    /// Do not override this method. Undefined outcomes may occur as a result due to the way this method is called.
    /// </summary>
    protected Card()
    {
        SetInfo();
        CheckValidity();
        SetAbilityDescriptions();
    }

    // TODO - change name to something more descriptive
    protected abstract void SetInfo();

    // toString method that returns everything in a nice format
    public override string ToString()
    {
        String ability = "";

        AbilityDescription.ForEach(x => ability += "\t" + x + "\n");

        return "Name: " + Name + "\n" +
               "Default Value: " + Value + "\n" +
               "Type: " + Type + "\n" +
               "Description: " + Description + "\n" +
               "Ability Description:\n" + ability + "\n" +
               "Acting Value: " + ActingValue;
    }

    public int GetAiHash()
    {
        return AiUtils.HashInternalElements(typeof(Card));
    }

    /// <summary>
    /// A hook that calls on this card being played. It is advised to call the base method in any overloads.
    /// </summary>
    /// <param name="row">the row this card was played in</param>
    /// 
    public void OnPlay(Row.RowTypes row)
    {
        _row = row;
    }

    private int GetActingValue()
    {
        if (_row == null)
        {
            throw new InvalidOperationException("Cannot determine acting value of card without a row");
        }

        if (GameInstance == null)
        {
            throw new InvalidOperationException("Cannot determine acting value of card without its associated" +
                                                " game instance (or player type)");
        }

        if (IsHero)
        {
            return Value;
        }

        if (GameInstance.WeatherCards[(Row.RowTypes) _row] != null)
        {
            return 1;
        }

        int workingValue = Value;

        if (TightBond)
        {
            // for every card in the row, add one extra damage
            workingValue *= Player.Rows[_row.Value].Count(card => card.GetType() == GetType());
        }

        // wiki says these come after tight night
        if (MoraleBoost)
        {
            // add one extra damage for every morale boost card in this row, excluding this card if it is a morale
            // boost card
            workingValue += Player.Rows[_row.Value].Count(card => card.MoraleBoost) - (MoraleBoost ? 1 : 0);
        }

        if (Player.Rows[_row.Value].SpecialAbility is {SpecialAbilityDoubler: true})
        {
            workingValue *= 2;
        }

        return workingValue;
    }

    /// <summary>
    /// A hook that calls on a given 'SpecialAbility' being played in this row. It is advised to call the base method in
    /// any overloads.
    /// </summary>
    /// <param name="SpecialAbility">the SpecialAbility that is played</param>
    public void OnSpecialAbility(Card SpecialAbility)
    {
        // if (!IsHero && SpecialAbility.SpecialAbilityDoubler)
        // {
        //     ActingValue *= 2;
        // }
        // // override this method to do something when a SpecialAbility is played
    }

    public void OnRemoval(Row.RowTypes type)
    {
        if (SummonCard)
        {
            Player.Rows[type].Add(
                Replacement.GetConstructor(new Type[] { })
                    ?.Invoke(new object[] { }) as Card
            );
        }
    }

    private void SetAbilityDescriptions()
    {
        AbilityDescription ??= new List<string>();

        // TODO - sort the descriptions(?)

        Dictionary<AbilityTypes, string> descriptions = new Dictionary<AbilityTypes, string>
        {
            [AbilityTypes.TightBond] = "Tight Bond: Place next to a card with the same name to double the strength " +
                                       "of both cards.",
            [AbilityTypes.MoraleBoost] = "Morale boost: Adds +1 to all units in the row (excluding itself).",
            [AbilityTypes.Medic] = "Medic: Choose one card from your discard pile and play it instantly (no Heroes or" +
                                   " Special Cards).",
            [AbilityTypes.Summon] = "When this card is removed from the battlefield, it summons a powerful new Unit " +
                                    "Card to take its place."
        };

        foreach (AbilityTypes ability in descriptions.Keys)
        {
            if ((Abilities & ability) == ability)
            {
                AbilityDescription.Add(descriptions[ability]);
            }
        }

        if (Agile)
        {
            AbilityDescription.Add("Agile: Can be placed in either the Close Combat or the Ranged Combat row. " +
                                   "Cannot be moved once placed.");
        }
    }

    /// <summary>
    /// This method is responsible for ensuring all created cards are valid. Heroes, for example, should not have an
    /// ability.
    /// </summary>
    private void CheckValidity()
    {
        if (Abilities.HasFlag(AbilityTypes.Hero) && Abilities != AbilityTypes.Hero)
        {
            throw new InvalidEnumArgumentException("Hero cards cannot have abilities.");
        }

        if ((Abilities & AbilityTypes.Summon) == AbilityTypes.Summon && Replacement == null)
        {
            throw new InvalidEnumArgumentException("Summon cards must have a replacement card.");
        }
    }

    public static List<Types> GetPossibleTypes(Card card)
    {
        return Enum.GetValues(typeof(Types))
            .Cast<Types>()
            .Where(t => (card.Type & t) == t)
            .ToList();
    }
}