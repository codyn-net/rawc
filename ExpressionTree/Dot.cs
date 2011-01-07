using System;
using System.Collections.Generic;
using System.IO;

namespace Cpg.RawC.ExpressionTree
{
	public class Dot
	{
		private List<ExpressionTree.Node> d_nodes;

		public Dot(params ExpressionTree.Node[] nodes)
		{
			d_nodes = new List<Node>(nodes);
		}
		
		public void Write(string filename)
		{
			FileStream stream = new FileStream(filename, FileMode.Create);
			StreamWriter writer = new StreamWriter(stream);
			
			writer.WriteLine("digraph {");
			
			foreach (ExpressionTree.Node node in d_nodes)
			{
				writer.WriteLine("subgraph {");
				Write(writer, node);
				writer.WriteLine("}");
			}
			
			writer.WriteLine("}");
			
			writer.Flush();
			writer.Close();
		}

		private string Label(ExpressionTree.Node node)
		{
			if (node.Instruction is InstructionFunction)
			{
				return (node.Instruction as InstructionFunction).Name;
			}
			else if (node.Instruction is InstructionCustomFunction)
			{
				return (node.Instruction as InstructionCustomFunction).Function.Id;
			}
			else if (node.Instruction == null)
			{
				return String.Format("{0}", node.Label);
			}
			else if (node.Instruction is InstructionNumber)
			{
				return "N";
			}
			else if (node.Instruction is InstructionProperty)
			{
				return "P";
			}
			else
			{
				return "";
			}
		}
		
		private uint Id(ExpressionTree.Node node)
		{
			return (uint)node.GetHashCode();
		}
		
		private string Color(ExpressionTree.Node node)
		{
			if (node.IsLeaf)
			{
				return "lightyellow";
			}
			else
			{
				return "white";
			}
		}
		
		private void WriteNode(TextWriter writer, ExpressionTree.Tree tree)
		{
			writer.WriteLine("{0} [shape=record,style=filled,fillcolor=lightblue,label=\"{1}|{2}.{3}\"];", Id(tree), Label(tree), tree.State.Property.Object.FullId, tree.State.Property.Name);
		}
		
		private void WriteNode(TextWriter writer, ExpressionTree.Node node)
		{
			if (node.Instruction is InstructionFunction || node.Instruction is InstructionCustomFunction)
			{
				writer.WriteLine("{0} [shape=record,width=0.75,style=filled,label=\"{1}|{2}|{3}\",fillcolor=\"{4}\"];", Id(node), Label(node), node.Degree, node.Height, Color(node));
			}
			else
			{
				writer.WriteLine("{0} [shape=record,style=filled,label=\"{1}|{2}\",fillcolor=\"{3}\"];", Id(node), Label(node), node.Top.Leaves.IndexOf(node), Color(node));
			}
		}
		
		private void Write(TextWriter writer, ExpressionTree.Node node)
		{
			if (node is ExpressionTree.Tree)
			{
				WriteNode(writer, (ExpressionTree.Tree)node);
			}
			else
			{
				WriteNode(writer, node);
			}
			
			foreach (var child in node.Children)
			{
				Write(writer, child);
				writer.WriteLine("{0} -> {1};", Id(node), Id(child));
			}
		}
	}
}

