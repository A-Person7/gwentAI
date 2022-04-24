using System.Collections.Generic;
using System.Linq;

namespace ConsoleApplication1.AIComponents;

public class Node
{
    private List<Link> Targets { get; set; }

    public void Accept(double data)
    {
        Strength += data;
    }

    public void SendToTargets()
    {
        Targets.ForEach(t => t.Send(Strength));
        Strength = 0;
    }
    
    public void SetLinks(List<Link> targets)
    {
        Targets = targets;
    }

    public double Strength { get; private set; }

    public bool IsEndNode => !Targets.Any();

    public Node(List<Link> targets)
    {
        Strength = 0;
        Targets = targets;
    }

    public Node()
    {
        Strength = 0;
        Targets = new List<Link>();
    }

    public Node Clone()
    {
        return new Node(Targets);
    }
}