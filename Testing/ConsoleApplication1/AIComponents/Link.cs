using System.Collections.Generic;

namespace ConsoleApplication1.AIComponents;

public class Link
{
    private double _multiplier;
    private List<Node> _targets;

    public double Multiplier
    {
        get => _multiplier;
        set => _multiplier = value;
    }

    public List<Node> Targets
    {
        get => _targets;
        set => _targets = value;
    }

    public Link(double multiplier, List<Node> targets)
    {
        Multiplier = multiplier;
        Targets = targets;
    }

    // TODO - update this to generate clones with deviance
    public Link(Link link)
    {
        Multiplier = link.Multiplier;
        Targets = link.Targets;
    }
    
    public Link()
    {
        Multiplier = 1;
        Targets = new List<Node>();
    }

    public void Send(double value)
    {
        Targets.ForEach(t => t.Accept(value));
    }
}