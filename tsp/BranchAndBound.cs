using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace tsp
{
    /// <summary>
    /// Алгоритм ветвей и границ
    /// </summary>
    public class BranchBound : IEnumerable<Bitmap>
    {
        /// <summary>
        /// Непересекающиеся множества
        /// </summary>
        public class Dsu : ICloneable
        {
            private List<int> p = new List<int>();

            #region Конструторы

            /// <summary>
            /// Создание пустого множества
            /// </summary>
            public Dsu() { }

            /// <summary>
            /// Создание множества указанного размера
            /// </summary>
            /// <param name="n"></param>
            public Dsu(int n)
            {
                Init(n);
            }

            #endregion

            /// <summary>
            /// Инициализация множества
            /// </summary>
            /// <param name="n"></param>
            public void Init(int n)
            {
                p = new List<int>(n);
                for (int i = 0; i < n; ++i)
                    p.Add(i);
            }

            /// <summary>
            /// Нахождения индефикатора множества, которому принадлежит данный элемент 
            /// </summary>
            /// <param name="x">элемент множества</param>
            /// <returns></returns>
            public int Find(int x)
            {
                return p[x] == x ? x : p[x] = Find(p[x]);
            }

            /// <summary>
            /// Объединение двух множеств, которым принадлежат указанные элементы 
            /// </summary>
            /// <param name="x">первый элемент</param>
            /// <param name="y">второй элемент</param>
            public void Union(int x, int y)
            {
                p[Find(x)] = Find(y);
            }

            /// <summary>
            /// Клонирование
            /// </summary>
            /// <returns>клон объекта</returns>
            public object Clone()
            {
                var clone = new Dsu();
                clone.p = new List<int>(p);

                return clone as object;
            }
        }

        /// <summary>
        /// Редуцированная матрица
        /// </summary>
        public class ReductionMatrix : Digraph.AdjacencyMatrix
        {
            #region Конструторы

            /// <summary>
            /// Создание матрицы указанного размера
            /// </summary>
            /// <param name="n">размер матрицы</param>
            public ReductionMatrix(int n)
                : base(n)
            {
                RealSize = Size;
            }

            /// <summary>
            /// Создание матрицы на основе матрциы смежности
            /// </summary>
            /// <param name="matrix">матрца смежности</param>
            public ReductionMatrix(Digraph.AdjacencyMatrix matrix)
                : base(matrix)
            {
                RealSize = Size;
            }

            /// <summary>
            /// Создание матрицы на основе другой матрицы
            /// </summary>
            /// <param name="other">другая матрица</param>
            public ReductionMatrix(ReductionMatrix other)
                : base(other)
            {
                RealSize = other.RealSize;
            }

            #endregion Конструторы

            /// <summary>
            /// Размер матрицы без "вычеркнутых" строк и столбцов 
            /// </summary>
            public int RealSize { get; private set; }

            /// <summary>
            /// Приведение матрицы
            /// </summary>
            /// <returns></returns>
            public double Reduce()
            {
                double min, minInRows = 0;
                for (int i = 0; i < Size; i++)
                {
                    min = MinInRow(i);
                    if (min != 0 && min != double.PositiveInfinity)
                    {
                        minInRows += min;
                        for (int j = 0; j < Size; j++)
                            this[i, j] -= min;
                    }
                }
                double minInColumns = 0;
                for (int i = 0; i < Size; i++)
                {
                    min = MinInColumn(i);
                    if (min != 0 && min != double.PositiveInfinity)
                    {
                        minInColumns += min;
                        for (int j = 0; j < Size; j++)
                            this[j, i] -= min;
                    }
                }

                return minInRows + minInColumns;
            }

            /// <summary>
            /// Нахождение минимума в строке
            /// </summary>
            /// <param name="row">номер строки</param>
            /// <param name="notIncludeColumn">номер столбца, который не учитывается в нахождение минимума</param>
            /// <returns>минимум в строке</returns>
            public double MinInRow(int row, int notIncludeColumn = -1)
            {
                double min = double.PositiveInfinity;
                for (int i = 0; i < Size; i++)
                    if (notIncludeColumn != i && min > this[row, i])
                        min = this[row, i];

                return min;
            }

            /// <summary>
            /// Нахождение минимума в столбце
            /// </summary>
            /// <param name="column">номер столбца</param>
            /// <param name="notIncludeRow">номер строки, который не учитывается в нахождение минимума</param>
            /// <returns>минимум в столбце</returns>
            public double MinInColumn(int column, int notIncludeRow = -1)
            {
                double min = double.PositiveInfinity;
                for (int i = 0; i < Size; i++)
                    if (notIncludeRow != i && min > this[i, column])
                        min = this[i, column];

                return min;
            }

            /// <summary>
            /// Исключение указанной строки и столбца редуцированной матрице
            /// </summary>
            /// <param name="row">номер строки</param>
            /// <param name="column">номер столбца</param>
            public void DeleteRowColumn(int row, int column)
            {
                for (int i = 0; i < Size; i++)
                    this[row, i] = double.PositiveInfinity;
                for (int i = 0; i < Size; i++)
                    this[i, column] = double.PositiveInfinity;

                RealSize--;
            }
        }

        /// <summary>
        /// Ветвление в методе ветвей и границ
        /// </summary>
        public class Branch
        {
            /// <summary>
            /// Направление ветвления
            /// </summary>
            public enum Type
            {
                Left, Right
            }

            #region Конструкторы

            /// <summary>
            /// Создание пустого ветвления
            /// </summary>
            public Branch() { }

            /// <summary>
            /// Создания ветвления
            /// </summary>
            /// <param name="bound">нижняя граница ветвления</param>
            /// <param name="matrix">текущая редуцированная матрица</param>
            /// <param name="edge">ребро ветвления</param>
            /// <param name="path">текущий маршрут</param>
            /// <param name="dsu">текущее множество вершин</param>
            public Branch(double bound, ReductionMatrix matrix, Digraph.Edge edge, Digraph.Path path, Dsu dsu)
            {
                Bound = bound;
                Matrix = matrix;
                Edge = edge;
                Path = path;
                SetVertex = dsu;
            }

            #endregion

            /// <summary>
            /// Нижняя граница ветвления
            /// </summary>
            public double Bound { set; get; }

            /// <summary>
            /// Ребро ветвления
            /// </summary>
            public Digraph.Edge Edge { set; get; }

            /// <summary>
            /// Текущий маршрут
            /// </summary>
            public Digraph.Path Path { set; get; }

            /// <summary>
            /// Множество вершин для данного ветвления
            /// </summary>
            public Dsu SetVertex { set; get; }

            /// <summary>
            /// Редуцированная матрица
            /// </summary>
            public ReductionMatrix Matrix { set; get; }

            /// <summary>
            /// Правый потомок ветвления
            /// </summary>
            public Branch Right { set; get; }

            /// <summary>
            /// Левый потомок ветвления
            /// </summary>
            public Branch Left { set; get; }
        }

        /// <summary>
        /// Дерево ветвления в методе ветвей и границ
        /// </summary>
        public class TreeBranching
        {
            /// <summary>
            /// Корень дерева ветвления
            /// </summary>
            public Branch Root { private set; get; }

            #region Конструкторы

            /// <summary>
            /// Создание дерева ветвления
            /// </summary>
            /// <param name="root">корень дерева</param>
            public TreeBranching(Branch root)
            {
                Root = root;
            }

            #endregion

            /// <summary>
            /// Добавление ветвления в дерево
            /// </summary>
            /// <param name="parent">родитель</param>
            /// <param name="direction">направление ветвления</param>
            /// <param name="b">ветвление</param>
            /// <returns></returns>
            public Branch Add(Branch parent, Branch.Type direction, Branch b)
            {
                if (direction == Branch.Type.Left)
                    parent.Left = b;
                else
                    parent.Right = b;

                return b;
            }

            /// <summary>
            /// Получение листьев дерева
            /// </summary>
            /// <returns>листья дерева ветвления</returns>
            public List<Branch> Leaves()
            {
                var leaves = new List<Branch>();

                if (Root == null)
                    return leaves;

                var stack = new Stack<Branch>();
                stack.Push(Root);

                while (stack.Count > 0)
                {
                    var branch = stack.Pop();
                    if (branch.Left == null && branch.Right == null)
                        leaves.Add(branch);
                    if (branch.Right != null)
                        stack.Push(branch.Right);
                    if (branch.Left != null)
                        stack.Push(branch.Left);
                }
                return leaves;
            }
        }

        /// <summary>
        /// Решение задачи коммивояжера методом ветвей и границ
        /// </summary>
        /// <param name="graph">граф</param>
        /// <returns>маршрут коммивояжера</returns>
        public static Digraph.Path Tsp(Digraph graph)
        {
            // минимальный маршрут
            var minPath = new Digraph.Path();

            // если граф пуст
            if (graph.CountVertex() == 0)
            {
                // пустой мпршрут
                return new Digraph.Path();
            }
            // если граф имеет одну вершину
            else if (graph.CountVertex() == 1)
            {
                minPath.Append(new Digraph.Edge(0, 0, graph[0, 0]));
                // маршрут для одной вершины
                return minPath;
            }
            // если граф имеет две вершины
            else if (graph.CountVertex() == 2)
            {
                minPath.Append(new Digraph.Edge(0, 1, graph[0, 1]));
                minPath.Append(new Digraph.Edge(1, 0, graph[1, 0]));
                // маршрут для двух вершин
                return minPath;
            }
            // минимальная нижняя граница всех ветвлений
            double lowBound = double.PositiveInfinity;

            // редуцированная матрица родительского ветвления
            var pMatrix = new ReductionMatrix(graph.Adjacency);

            /// Приведение матрицы и создание родительского ветвления
            var pBranch = new Branch(pMatrix.Reduce(), pMatrix, null, new Digraph.Path(), new Dsu(graph.CountVertex()));

            /// Создание дерева ветления
            var tree = new TreeBranching(pBranch);

            do
            {
                // редуцированная матрица левого потомка
                var lMatrix = new ReductionMatrix(pBranch.Matrix);
                // ребра с нулевой стоимостью
                var zeroEdges = new List<Digraph.Edge>();
                // Получение всех ребер и соответсвующих штрафов
                for (int i = 0; i < lMatrix.Size; i++)
                    for (int j = 0; j < lMatrix.Size; j++)
                        if (lMatrix[i, j] == 0)
                            zeroEdges.Add(new Digraph.Edge(i, j, lMatrix.MinInRow(i, j) + lMatrix.MinInColumn(j, i)));

                // если нет ребер ветвления - нет маршрута коммивояжера
                if (zeroEdges.Count == 0)
                    return new Digraph.Path();

                /// Определение ребра ветвления - ребра с максимальным штрафом
                var bEdge = zeroEdges.OrderByDescending(e => e.Cost).ToList().First();

                /// Процесс ветвления - не включая данное ребро 
                lMatrix[bEdge.Begin, bEdge.End] = double.PositiveInfinity;

                /// Создание левого потомка ветвления
                var lBranch = new Branch(pBranch.Bound + bEdge.Cost, // нижняя граница текущего ветвления
                    lMatrix,                                            // редуцированния матрица
                    new Digraph.Edge(-bEdge.Begin, -bEdge.End, double.PositiveInfinity), // без исходного ребра ветвления
                    pBranch.Path.Clone() as Digraph.Path, // без изменений маршрута
                    pBranch.SetVertex.Clone() as Dsu    // без изменений множеств вершин
                    );
                // добавление ветвления в дерево
                tree.Add(pBranch, Branch.Type.Left, lBranch);

                /// Процесс ветления - включая данное ребро
                var rMatrix = new ReductionMatrix(pBranch.Matrix);
                var rVertex = pBranch.SetVertex.Clone() as Dsu;

                /// Исключение подмаршрутов для данной матрицы и множества вершин
                for (int i = 0; i < rMatrix.Size; i++)
                    for (int j = 0; j < rMatrix.Size; j++)
                        if (rVertex.Find(bEdge.Begin) == rVertex.Find(i) && rVertex.Find(bEdge.End) == rVertex.Find(j))
                            rMatrix[j, i] = double.PositiveInfinity;
                // объединение вершин данного ребра в одно множество 
                rVertex.Union(bEdge.Begin, bEdge.End);
                // исключение строки и столбца соответсвующие начала и конца ребра
                rMatrix.DeleteRowColumn(bEdge.Begin, bEdge.End);

                // формирование добавляемого ребра
                var rEdge = new Digraph.Edge(bEdge.Begin, bEdge.End, graph[bEdge.Begin, bEdge.End]);
                // добавление в маршрут
                var rPath = pBranch.Path.Clone() as Digraph.Path;
                rPath.Append(rEdge);

                /// Создание правого потомка ветвления
                var rBranch = new Branch(pBranch.Bound + rMatrix.Reduce(), // нижняя граница текущего ветвления
                    rMatrix, // редуцированния матрица
                    rEdge,   // с исходным ребром ветвления
                    rPath,   // с изменением маршрута
                    rVertex  // с изменением множеств вершин
                    );
                // добавление ветвления в дерево
                tree.Add(pBranch, Branch.Type.Right, rBranch);

                /// Проверка на достаточность размера матрцицы
                if (rMatrix.RealSize == 2)
                {
                    /// Добавление оставщихся ребер в маршрут
                    for (int i = 0; i < rMatrix.Size; i++)
                        for (int j = 0; j < rMatrix.Size; j++)
                            if (rMatrix[i, j] == 0)
                            {
                                // добавление в маршрут
                                rPath.Append(new Digraph.Edge(i, j, graph[i, j]));
                            }

                    /// Определение нового маршрута с более низкой стоимостью
                    if (rBranch.Bound < lowBound)
                    {
                        // новая нижняя граница
                        lowBound = rBranch.Bound;
                        // новый маршрут
                        minPath = rBranch.Path;
                    }
                }

                /// Выбор новой родительской вершины
                pBranch = tree.Leaves().OrderBy(b => b.Bound).ToList().First();

            } while (lowBound > pBranch.Bound);

            // минимальный маршрут коммивояжера
            return minPath;
        }

        #region Итерационное ветвление метода ветвей и границ

        // дерево ветвления алгоритма
        private TreeBranching tree;

        // текущее ветвление
        private Branch current;

        // минимальная нижняя граница всех ветвлений
        private double minLowBound;

        #region Конструкторы

        public BranchBound(Digraph graph)
        {
            Graph = graph;
        }

        #endregion

        /// <summary>
        /// Орг. граф
        /// </summary>
        public Digraph Graph { set; get; }

        /// <summary>
        /// Минимальный маршрут коммивояжера
        /// </summary>
        public Digraph.Path MinPath { private set; get; }

        public IEnumerator<Bitmap> GetEnumerator()
        {
            // минимальный маршрут
            MinPath = new Digraph.Path();

            // если граф пуст
            if (Graph.CountVertex() == 0)
            {
                // пустой мпршрут
                yield break;
            }
            // если граф имеет одну вершину
            else if (Graph.CountVertex() == 1)
            {
                MinPath.Append(new Digraph.Edge(0, 0, Graph[0, 0]));
                // маршрут для одной вершины
                yield break;
            }
            // если граф имеет две вершины
            else if (Graph.CountVertex() == 2)
            {
                MinPath.Append(new Digraph.Edge(0, 1, Graph[0, 1]));
                MinPath.Append(new Digraph.Edge(1, 0, Graph[1, 0]));
                // маршрут для двух вершин
                yield break;
            }

            // минимальная нижняя граница всех ветвлений
            minLowBound = double.PositiveInfinity;

            // редуцированная матрица родительского ветвления
            var pMatrix = new ReductionMatrix(Graph.Adjacency);

            /// Приведение матрицы и создание родительского ветвления
            current = new Branch(pMatrix.Reduce(), pMatrix, null, new Digraph.Path(), new Dsu(Graph.CountVertex()));

            /// Создание дерева ветления
            tree = new TreeBranching(current);

            do
            {
                // редуцированная матрица левого потомка
                var lMatrix = new ReductionMatrix(current.Matrix);
                // ребра с нулевой стоимостью
                var zeroEdges = new List<Digraph.Edge>();
                // Получение всех ребер и соответсвующих штрафов
                for (int i = 0; i < lMatrix.Size; i++)
                    for (int j = 0; j < lMatrix.Size; j++)
                        if (lMatrix[i, j] == 0)
                            zeroEdges.Add(new Digraph.Edge(i, j, lMatrix.MinInRow(i, j) + lMatrix.MinInColumn(j, i)));

                // если нет ребер ветвления - нет маршрута коммивояжера
                if (zeroEdges.Count == 0)
                {
                    MinPath = new Digraph.Path();
                    yield break;
                }
                /// Определение ребра ветвления - ребра с максимальным штрафом
                var bEdge = zeroEdges.OrderByDescending(e => e.Cost).ToList().First();

                /// Процесс ветвления - не включая данное ребро 
                lMatrix[bEdge.Begin, bEdge.End] = double.PositiveInfinity;

                /// Создание левого потомка ветвления
                var lBranch = new Branch(current.Bound + bEdge.Cost, // нижняя граница текущего ветвления
                    lMatrix,                                            // редуцированния матрица
                    new Digraph.Edge(-bEdge.Begin, -bEdge.End, double.PositiveInfinity), // без исходного ребра ветвления
                    current.Path.Clone() as Digraph.Path, // без изменений маршрута
                    current.SetVertex.Clone() as Dsu    // без изменений множеств вершин
                    );
                // добавление ветвления в дерево
                tree.Add(current, Branch.Type.Left, lBranch);

                /// Процесс ветления - включая данное ребро
                var rMatrix = new ReductionMatrix(current.Matrix);
                var rVertex = current.SetVertex.Clone() as Dsu;

                /// Исключение подмаршрутов для данной матрицы и множества вершин
                for (int i = 0; i < rMatrix.Size; i++)
                    for (int j = 0; j < rMatrix.Size; j++)
                        if (rVertex.Find(bEdge.Begin) == rVertex.Find(i) && rVertex.Find(bEdge.End) == rVertex.Find(j))
                            rMatrix[j, i] = double.PositiveInfinity;
                // объединение вершин данного ребра в одно множество 
                rVertex.Union(bEdge.Begin, bEdge.End);
                // исключение строки и столбца соответсвующие начала и конца ребра
                rMatrix.DeleteRowColumn(bEdge.Begin, bEdge.End);

                // формирование добавляемого ребра
                var rEdge = new Digraph.Edge(bEdge.Begin, bEdge.End, Graph[bEdge.Begin, bEdge.End]);
                // добавление в маршрут
                var rPath = current.Path.Clone() as Digraph.Path;
                rPath.Append(rEdge);

                /// Создание правого потомка ветвления
                var rBranch = new Branch(current.Bound + rMatrix.Reduce(), // нижняя граница текущего ветвления
                    rMatrix, // редуцированния матрица
                    rEdge,   // с исходным ребром ветвления
                    rPath,   // с изменением маршрута
                    rVertex  // с изменением множеств вершин
                    );
                // добавление ветвления в дерево
                tree.Add(current, Branch.Type.Right, rBranch);

                /// Проверка на достаточность размера матрцицы
                if (rMatrix.RealSize == 2)
                {
                    current = rBranch;
                    /// Добавление оставщихся ребер в маршрут
                    for (int i = 0; i < current.Matrix.Size; i++)
                        for (int j = 0; j < current.Matrix.Size; j++)
                            if (rMatrix[i, j] == 0)
                            {
                                // отображения дерева ветвлений на текущей итерации
                                yield return Painter.Drawing(tree);

                                var edge = new Digraph.Edge(i, j, Graph[i, j]);
                                // добавление в маршрут
                                current.Path.Append(edge);
                                // добавление в дерево ветвлений
                                var branch = new Branch(current.Bound, current.Matrix, edge, current.Path, current.SetVertex);

                                tree.Add(current, Branch.Type.Right, branch);

                                // новый родитель
                                current = branch;
                            }

                    /// Определение нового маршрута с более низкой стоимостью
                    if (current.Bound < minLowBound)
                    {
                        // новая нижняя граница
                        minLowBound = current.Bound;
                        // новый маршрут
                        MinPath = current.Path;
                    }
                }

                // отображения дерева ветвлений на текущей итерации
                yield return Painter.Drawing(tree);

                /// Выбор новой родительской вершины
                current = tree.Leaves().OrderBy(b => b.Bound).ToList().First();

            } while (minLowBound > current.Bound);

            // минимальный маршрут коммивояжера
            yield break;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }
}
