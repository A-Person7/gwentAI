using System.Collections.Generic;
using System.Linq;

namespace ConsoleApplication1.AIComponents;

public class Node
{
    private List<Node> _targets;
    private double _sum;
    private readonly double _multiplier;

    public double Multiplier => _multiplier;

    public void AcceptData(double data)
    {
        _sum += data * _multiplier;
    }

    public void SendToTargets()
    {
        _targets.ForEach(t => t.AcceptData(_sum));
        _sum = 0;
    }
    
    public void SetTargets(List<Node> targets)
    {
        _targets = targets;
    }

    public double Strength => _sum;

    public bool IsEndNode => !_targets.Any();

    public Node(List<Node> targets, double multiplier)
    {
        _sum = 0;
        _targets = targets;
        _multiplier = multiplier;
    }

    public Node CloneWithDeviance(List<Node> targets, double deviance)
    {
        return new Node(_targets, _multiplier + deviance);
    }
}