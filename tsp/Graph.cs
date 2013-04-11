using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Drawing;
using System.IO;

namespace tsp
{
    public class Graph
    {
        public class Painter
        {
            private static Bitmap RenderingOnGraphviz(string script)
            {
                if (!File.Exists(@"..\Graphviz\dot.exe")) throw new Exception("Приложения dot.exe не найдено");

                Process dot = new Process();
                try
                {
                    dot.StartInfo.UseShellExecute = false;
                    dot.StartInfo.FileName = @"..\Graphviz\dot.exe";
                    dot.StartInfo.Arguments = "-Tpng";
                    dot.StartInfo.RedirectStandardInput = true;
                    dot.StartInfo.RedirectStandardOutput = true;
                    dot.StartInfo.CreateNoWindow = true;
                    dot.Start();

                    dot.StandardInput.Write(script);
                    dot.StandardInput.Close();

                    Bitmap image = new Bitmap(dot.StandardOutput.BaseStream, true);
                    dot.StandardOutput.Close();

                    return image;
                }
                catch (Exception e)
                {
                    throw new Exception("Не возможно отрисовать граф", e);
                }
            }

            public static Bitmap Drawing(Graph g)
            {
                if (g == null) throw new Exception("Невозможно отрисовать граф. Граф пуст (null)");

                string script = "digraph G { nrankdir = LR ";

                for (int i = 0; i < g.CountVertex(); i++)
                    script += (i + 1) + " ";

                for (int i = 0; i < g.CountVertex(); i++)
                    for (int j = 0; j < g.CountVertex(); j++)
                        if (!Double.IsInfinity(g.Adjacency[i, j]))
                            script += (i + 1) + " -> " + (j + 1) + " [label=" + g.Adjacency[i, j] + "] ";

                script += "}";

                return RenderingOnGraphviz(script);
            }

            public static Bitmap Drawing(Graph g, Cycle p)
            {
                if (g == null) throw new Exception("Невозможно отрисовать граф. Граф пуст (null)");

                if (p == null) return Drawing(g);

                string script = "digraph G { nrankdir = LR ";

                for (int i = 0; i < g.CountVertex(); i++)
                    script += (i + 1) + " ";

                for (int i = 0; i < g.CountVertex(); i++)
                    for (int j = 0; j < g.CountVertex(); j++)
                    {
                        if (!Double.IsInfinity(g.Adjacency[i, j]))
                        {
                            script += (i + 1) + " -> " + (j + 1) + " [label=" + g.Adjacency[i, j];

                            if (p.IsContain(new Edge(i, j, g.Adjacency[i, j]))) script += ", color=red fontcolor=red";

                            script += "] ";
                        }
                    }

                script += "}";

                return RenderingOnGraphviz(script);
            }
        }

        public class Edge : IEquatable<Edge>, IComparable<Edge>, ICloneable
        {
            
            public Edge(int begin, int end, double cost)
            {
                Begin = begin;
                End = end;
                Cost = cost;
            }

            public Edge(Edge other)
            {
                Begin = other.Begin;
                End = other.End;
                Cost = other.Cost;
            }

            public int Begin { get; set; }

            public int End { get; set; }

            public double Cost { get; set; }

            public int CompareTo(Edge other)
            {
                return Cost.CompareTo(other);
            }

            public override bool Equals(object other)
            {
                if (other == null)
                    return false;

                if (object.ReferenceEquals(this, other))
                    return true;

                if (this.GetType() != other.GetType())
                    return false;

                return this.Equals(other as Edge);
            }

            public bool Equals(Edge other)
            {
                if (other == null)
                    return false;

                if (object.ReferenceEquals(this, other))
                    return true;

                if (this.GetType() != other.GetType())
                    return false;

                return Begin == other.Begin && End == other.End && Cost == other.Cost;
            }

            public override int GetHashCode()
            {
                return Begin ^ End ^ (int)Cost;
            }

            public object Clone()
            {
                var clone = new Edge(Begin, End, Cost);
                
                return clone as object;
            }
        }

        public class Cycle : ICloneable
        {
            public Cycle()
            {
                Cost = 0;
                Edges = new LinkedList<Edge>();
            }

            public Cycle(Cycle other)
            {
                var clone = other.Clone() as Cycle;

                Edges = clone.Edges;
                Cost = clone.Cost;
            }

            public LinkedList<Edge> Edges { get; private set; }

            public double Cost { get; private set; }

            public bool Append(Edge e)
            {
                if (Edges.Contains(e)) return false;

                Cost += e.Cost;
                Edges.AddLast(e);

                return true;
            }

            public bool IsExists()
            {
                foreach (var e in Edges) if (Double.IsInfinity(e.Cost)) return false;

                return Edges.Count() != 0;
            }

            public bool IsContain(Edge e)
            {
                return Edges.Contains(e);
            }

            public object Clone() {
                var clone = new Cycle();
                clone.Cost = Cost;
                clone.Edges = new LinkedList<Edge>(Edges);

                return clone as object;
            }
        }

        public SqureMatrix Adjacency { set; get; }

        public Graph(SqureMatrix adjacency)
        {
            Adjacency = adjacency;
        }

        public Graph(Graph other)
        {
            Adjacency = other.Adjacency.Clone() as SqureMatrix;
        }

        public static Graph ReadFromFile(string fileName)
        {
            if (!File.Exists(fileName)) throw new Exception("Ошибка! Заданного входного файла не существует.");

            var lines = File.ReadAllLines(fileName);

            if (lines.Length == 0) throw new Exception("Ошибка! Заданный входной файл пуст.");

            if (lines.Length < 2 || lines.Length > 35) throw new Exception("Ошибка! Недопустимый размер матрицы.");

            SqureMatrix matrix = new SqureMatrix(lines.Length);

            for (int i = 0; i < lines.Length; i++)
            {
                var values = lines[i].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                if (values.Length != lines.Length) throw new Exception("Ошибка! Матрица смежности должна быть квадратной.");

                for (int j = 0; j < lines.Length; j++)
                {
                    double c;

                    if (!double.TryParse(values[j], out c))
                    {
                        if (values[j].CompareTo("*") == 0) matrix[i, j] = System.Double.PositiveInfinity;
                        else throw new Exception("Ошибка! Матрица смежности содержит недопустимые символы.");
                    }
                    else if (c < 0) throw new Exception("Ошибка! Матрица смежности содержит отрицательные элементы.");
                    else if (i == j) throw new Exception("Ошибка! Матрица смежности должна описывать полный граф.");
                    else matrix[i, j] = c;
                }
            }

            return new Graph(matrix);
        }

        public int CountVertex()
        {
            return Adjacency.Size;
        }
    }
}
