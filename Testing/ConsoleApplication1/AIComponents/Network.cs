using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ConsoleApplication1.AIComponents;

public class Network<T>
{
    private readonly Dictionary<int, T> _outputs;

    private List<List<Node>> Nodes { get; init; }

    public Network(Dictionary<int, T> outputs, List<List<Node>> nodes)
    {
        _outputs = outputs;
        Nodes = nodes;
        Stitch(() => 1d);
    }
    
    // TODO - implement
    public Network (Network<T> toClone, double maxDeviance)
    {
        Nodes = toClone.CloneNodes();
        _outputs = toClone._outputs;
        Stitch(() => (_rand.NextDouble() * 2 - 1) * maxDeviance);
    }

    // stitches the nodes together with a multiplier determined by the supplier argument
    private void Stitch(Func<double> multiplierSupplier)
    {
        // last nodes don't need linking because they're only outputs, but don't send out data
        for (int i = Nodes.Count - 1 - 1; i >= 0; i--)
        {
            List<Link> links = Nodes[i + 1]
                .Select(_ => new Link(multiplierSupplier.Invoke(), Nodes[i + 1])).ToList();
            foreach (Node node in Nodes[i])
            {
                node.SetLinks(links);
            }
        }
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

        // // check which index in the last nodes list has the highest value, then return the dictionary value
        // // tolerance check to hopefully prevent imprecision
        // return _outputs[Nodes[^1].FindIndex(n => Math.Abs(n.Strength - Nodes[^1]
        //     .Max(m => m.Strength)) < 0.01)];

        Node max = Nodes[^1].First();
        int index = 0;
        for (int i = 0; i < Nodes[^1].Count; i++)
        {
            if (Nodes[^1][i].Strength <= max.Strength) continue;
            max = Nodes[^1][i];
            index = i;
        }

        return _outputs[index];
    }

    // private void PrintOutputs(List<T> options)
    // {
    //     StringBuilder s = new StringBuilder();
    //     for (int i = 0; i < Nodes[^1].Count; i++)
    //     {
    //         double strength = Math.Round(Nodes[^1][i].Strength) * 100000d)/100000d;
    //         s.Append($"\t{_outputs[i]}: {strength}");
    //     }
    //     Console.WriteLine(s.ToString());
    // }
    
    private void ProcessNodes(IEnumerable<double> data)
    {
        for (int i = 0; i < Nodes[0].Count; i++)
        {
            // TODO - check for possible double enumeration
            Nodes[0][i].Accept(data.ElementAt(i));
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

    private List<List<Node>> CloneNodes()
    {
        return Nodes.Select(n => n.Select(node => node.Clone()).ToList()).ToList();
    }
}