using System;
using System.Collections.Generic;
using System.Linq;

namespace ConsoleApplication1.AIComponents;

public class Network<T>
{
    private readonly Dictionary<int, T> _outputs;
    private List<List<Node>> _nodes;

    private List<List<Node>> Nodes
    {
        get => _nodes;
        init
        {
            value[0].ForEach(n =>
            {
                // the multiplier of the first layer should be 1 to represent pure input
                // checks against that, with some tolerance due to imprecision
                if (Math.Abs(n.Multiplier - 1) > 0.1)
                {
                    throw new ArgumentException("First layer must be for inputs (multiplier = 1).");
                }
            });
            if (value[^1].Count != _outputs.Count)
            {
                throw new ArgumentException("The number of outputs does not match the number of nodes in the " +
                                            "last layer.");
            }

            _nodes = value;
        }
    }
    
    public Network(Dictionary<int, T> outputs, List<List<Node>> nodes)
    {
        _outputs = outputs;
        Nodes = nodes;
    }
    
    // note - options must have overridden equality operators or store the same references as _outputs.Values
    public T GetOutput(IEnumerable<double> data, List<T> options, T defaultValue)
    {
        ProcessNodes(data);

        // return the maximum value in the last nodes list if the object from the dictionary satisfies the condition
        T workingMax = defaultValue;
        double maxStrength = double.MinValue;
        
        List<Node> nodes = Nodes[^1];
        
        for (int i = 0; i < nodes.Count; i++)
        {

            // .Equals because == isn't known for type T
            if (!workingMax.Equals(defaultValue) || nodes[i].Strength <= maxStrength)
            {
                continue;
            }
            
            try
            {
                workingMax = options.First(o => o.Equals(_outputs[i]));
                maxStrength = nodes[i].Strength;
            }
            catch (InvalidOperationException)
            {
                // continue
            }
        }

        return workingMax;
    }

    public T GetOutput(IEnumerable<double> data)
    {
        ProcessNodes(data);
        
        // check which index in the last nodes list has the highest value, then return the dictionary value
        // tolerance check to hopefully prevent imprecision
        return _outputs[Nodes[^1].FindIndex(n => Math.Abs(n.Strength - Nodes[^1]
            .Max(m => m.Strength)) < 0.1)];
    }

    private void ProcessNodes(IEnumerable<double> data)
    {
        for (int i = 0; i < Nodes[0].Count; i++)
        {
            // TODO - check for possible double enumeration
            Nodes[0][i].AcceptData(data.ElementAt(i));
        }

        // don't send the last layer
        for (int i = 0; i < Nodes.Count - 1; i++)
        {
            Nodes[i].ForEach(n => n.SendToTargets());
        }
    }

    // TODO - update random method
    // apparently you can do this
    private readonly Random _rand = new(Guid.NewGuid().GetHashCode());

    public Network<T> CloneWithDeviance(double deviance)
    {
        List<List<Node>> workingNodes = new List<List<Node>>();

        List<Node> emptyNodeList = new List<Node>();

        for (int i = 0; i < Nodes.Count; i++)
        {
            List<Node> list = Nodes[i];
            workingNodes.Add(new List<Node>());
            foreach (Node node in list)
            {
                workingNodes[^1].Add(node.CloneWithDeviance(emptyNodeList, 
                    i == 0 ? 0 : (_rand.NextDouble() - 0.5) * 2 * deviance));
            }
        }

        // don't set the last layer
        for (int i = 0; i < workingNodes.Count - 1; i++)
        {
            foreach (Node n in workingNodes[i])
            {
                n.SetTargets(workingNodes[i + 1]);
            }
        }

        // TODO - update random use to use main.rand eventually
        return new Network<T>(_outputs, workingNodes);
    }
}