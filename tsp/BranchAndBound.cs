using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace tsp
{
    public class Dsu : ICloneable
    {
        private List<int> dp = new List<int>();

        public Dsu() { }

        public Dsu(int n) 
        { 
            Init(n); 
        }

        public Dsu(Dsu other) 
        {
            var clone = other.Clone() as Dsu;

            dp = clone.dp;
        }

        public void Init(int n)
        {
            dp = new List<int>(n);
            for (int i = 0; i < n; i++) dp.Add(i);
        }

        public int Find(int x)
        {
            return dp[x] == x ? x : dp[x] = Find(dp[x]);
        }

        public void Union(int x, int y)
        {
            dp[Find(x)] = Find(y);
        }

        public object Clone()
        {
            var clone = new Dsu();

            clone.dp = new List<int>(dp);

            return clone as object;
        }
    }

    public class BranchAndBound
    {
        public class CycleInfo : ICloneable
        {
            public CycleInfo() { }

            public CycleInfo(Dsu dsu, ReducedMatrix matrix, Graph.Cycle cycle)
            {
                DSU = dsu;
                Matrix = matrix;
                Cycle = cycle;
            }

            public CycleInfo(CycleInfo other)
            {
                var clone = other.Clone() as CycleInfo;

                DSU = clone.DSU;
                Matrix = clone.Matrix;
                Cycle = clone.Cycle;
            }

            public Dsu DSU { private set; get; }

            public ReducedMatrix Matrix { set; get; }

            public Graph.Cycle Cycle { set; get; }

            public void EliminateSubroute(Graph.Edge e)
            {
                for (int i = 0; i < Matrix.Size; ++i)
                {
                    for (int j = 0; j < Matrix.Size; ++j)
                    {
                        if (DSU.Find(e.Begin) == DSU.Find(i) && DSU.Find(e.End) == DSU.Find(j))
                            Matrix[j, i] = double.PositiveInfinity;
                    }
                }
                DSU.Union(e.Begin, e.End);
            }

            public object Clone()
            {
                var clone = new CycleInfo();

                clone.DSU = DSU.Clone() as Dsu;
                clone.Matrix = new ReducedMatrix(Matrix);
                clone.Cycle = Cycle.Clone() as Graph.Cycle;

                return clone as object;
            }
        }

        public class ReducedMatrix : SqureMatrix
        {
            public int RealSize { get; private set; }

            public ReducedMatrix(int n)
                : base(n)
            {
                RealSize = Size;
            }

            public ReducedMatrix(SqureMatrix smatrix)
                : base(smatrix)
            {
                RealSize = Size;
            }

            public ReducedMatrix(ReducedMatrix rmatrix)
                : base(rmatrix)
            {
                RealSize = rmatrix.RealSize;
            }

            public double Reduce()
            {
                double min, minInRows = 0;
                for (int i = 0; i < Size; i++)
                {
                    min = MinInRow(i);
                    if (min != 0 && min != double.PositiveInfinity)
                    {
                        minInRows += min;
                        for (int j = 0; j < Size; j++) this[i, j] -= min;
                    }
                }

                double minInColumns = 0;
                for (int i = 0; i < Size; i++)
                {
                    min = MinInColumn(i);
                    if (min != 0 && min != double.PositiveInfinity)
                    {
                        minInColumns += min;
                        for (int j = 0; j < Size; j++) this[j, i] -= min;
                    }                   
                }

                return minInRows + minInColumns;
            }

            public double MinInRow(int row, int notIncludeColumn = -1)
            {
                double min = double.PositiveInfinity;
                for (int i = 0; i < Size; i++)
                    if (notIncludeColumn != i && min > this[row, i]) min = this[row, i];

                return min;
            }

            public double MinInColumn(int column, int notIncludeRow = -1)
            {
                double min = double.PositiveInfinity;
                for (int i = 0; i < Size; i++)
                    if (notIncludeRow != i && min > this[i, column]) min = this[i, column];

                return min;
            }

            public void DeleteRowAndColumn(int row, int column)
            {
                for (int i = 0; i < Size; i++) this[row, i] = double.PositiveInfinity;
                for (int i = 0; i < Size; i++) this[i, column] = double.PositiveInfinity;

                RealSize--;
            }
        }

        public class Branch : IComparable<Branch>, ICloneable
        {
            public enum Type
            {
                Left, Right
            }

            public Graph.Edge Edge { set; get; }

            public double Bound { set; get; }

            public CycleInfo Info { set; get; }

            public Branch Right { set; get; }

            public Branch Left { set; get; }

            public Branch() { }

            public Branch(double bound, Graph.Edge edge, CycleInfo info)
            {
                Bound = bound;
                Edge = edge;
                Info = info;
            }

            public Branch(Branch other)
            {
                var clone = other.Clone() as Branch;

                Bound = clone.Bound;
                Edge = clone.Edge;
                Info = clone.Info;
                Right = clone.Right;
                Left = clone.Left;
            }

            public int CompareTo(Branch other)
            {
                return Bound.CompareTo(other.Bound);
            }

            public object Clone()
            {
                var clone = new Branch();
                clone.Bound = Bound;
                clone.Edge = Edge.Clone() as Graph.Edge;
                clone.Info = Info.Clone() as CycleInfo;
                clone.Right = Right;
                clone.Left = Left;

                return clone;
            }
        }

        public class TreeBranching
        {
            public Branch Root { set; private get; }

            public TreeBranching(Branch root)
            {
                Root = root;
            }

            public Branch Add(Branch parent, Branch.Type direction, Branch b)
            {
                if (direction == Branch.Type.Left) parent.Left = b;
                else parent.Right = b;

                return b;
            }

            public List<Branch> Leaves()
            {
                var leaves = new List<Branch>();

                if (Root == null) return leaves;

                var stack = new Stack<Branch>();
                stack.Push(Root);

                while (stack.Count > 0)
                {
                    var branch = stack.Pop();
                    if (branch.Left == null && branch.Right == null) leaves.Add(branch);
                    if (branch.Right != null) stack.Push(branch.Right);
                    if (branch.Left != null) stack.Push(branch.Left);
                }

                return leaves;
            }
        }

        public static Graph.Cycle SearchMinHamiltonianCyle(Graph g)
        {
            double minBound = double.PositiveInfinity;

            var minHamilton = new Graph.Cycle();

            var pMatrix = new ReducedMatrix(g.Adjacency);

            var pBranch = new Branch(pMatrix.Reduce(), null, new CycleInfo(new Dsu(g.CountVertex()), pMatrix, new Graph.Cycle()));

            var tree = new TreeBranching(pBranch);

            do
            {
                var lInfo = pBranch.Info.Clone() as CycleInfo;

                var zeroEdges = new List<Graph.Edge>();

                for (int i = 0; i < lInfo.Matrix.Size; i++)
                {
                    for (int j = 0; j < lInfo.Matrix.Size; j++)
                    {
                        if (lInfo.Matrix[i, j] == 0) 
                            zeroEdges.Add(
                            new Graph.Edge(i, j, lInfo.Matrix.MinInRow(i, j) + lInfo.Matrix.MinInColumn(j, i))
                            );
                    }
                }

                var bEdge = zeroEdges.OrderByDescending(e => e.Cost).ToList().First();

                lInfo.Matrix[bEdge.Begin, bEdge.End] = double.PositiveInfinity;

                var lBranch = new Branch(pBranch.Bound + bEdge.Cost,
                    new Graph.Edge(-bEdge.Begin, -bEdge.End, double.PositiveInfinity),
                    lInfo
                    );

                tree.Add(pBranch, Branch.Type.Left, lBranch);


                var rInfo = pBranch.Info.Clone() as CycleInfo;

                rInfo.EliminateSubroute(bEdge);
                rInfo.Matrix.DeleteRowAndColumn(bEdge.Begin, bEdge.End);

                /*rMatrix[bEdge.End, bEdge.Begin] = double.PositiveInfinity;
                rMatrix.DeleteRowAndColumn(bEdge.Begin, bEdge.End);
                */

                var addEdge = new Graph.Edge(bEdge.Begin, bEdge.End, g.Adjacency[bEdge.Begin, bEdge.End]);
                rInfo.Cycle.Append(addEdge);

                var rBranch = new Branch(pBranch.Bound + rInfo.Matrix.Reduce(),
                    addEdge,
                    rInfo
                    );

                tree.Add(pBranch, Branch.Type.Right, rBranch);

                if (rInfo.Matrix.RealSize == 2)
                {
                    for (int i = 0; i < rInfo.Matrix.Size; i++)
                    {
                        for (int j = 0; j < rInfo.Matrix.Size; j++)
                        {
                            if (rInfo.Matrix[i, j] == 0)
                                rBranch.Info.Cycle.Append(
                                new Graph.Edge(i, j, g.Adjacency[i, j])
                                );
                        }
                    }

                    if (rBranch.Bound < minBound)
                    {
                        minBound = rBranch.Bound;
                        minHamilton = rBranch.Info.Cycle;
                    }
                }

                pBranch = tree.Leaves().OrderBy(b => b.Bound).ToList().First();

            } while (minBound > pBranch.Bound);

            return minHamilton;
        }
    }
}
