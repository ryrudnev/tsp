using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;

namespace tsp
{
    /// <summary>
    /// Метод ветвей и границ для решения задачи коммивояжера
    /// </summary>
    public class BranchAndBound : IEnumerable, IEnumerator
    {
        #region Итерационное представление метода ветвей и границ

        // итерационное состояние метода ветвей и границ 
        private enum IterationState
        {
            // остановка метода
            Stop,
            // инициализация метода
            Start,
            // левое ветвление метода
            LeftBranching,
            // правое ветвление метода
            RightBranching,
            // малый размер текущей матрицы метода
            LittleMatrix,
            // окончание метода
            End
        }

        // следующее состояние метода
        private IterationState next;

        // текущий индекс, определяющий изображение ветвления метода
        // и графа с выделенным маршрутом
        private int index = 0;

        // список изображений ветвления метода и графа с выделенным маршрутом
        // на всех итерациях метода
        List<Bitmap[]> iterations = new List<Bitmap[]>();

        // текущее непересекающиеся множество
        private Dsu dsu;

        // текущее ребро ветвления
        private Digraph.Edge edge;

        // текущая матрица
        private ReductionMatrix matrix;

        // ветвление: родительское, левое, правое, минимальное
        private Branch parent, left, right, min;

        // дерево ветвлений
        private TreeBranch tree;

        // орг. граф для которого находится маршрут коммивояжера
        public Digraph Graph { set; get; }

        // результирующий маршрут коммивояжера
        public Digraph.Path TsPath { private set; get; }

        /// <summary>
        /// Создание пошагового метода ветвей и границ
        /// </summary>
        /// <param name="graph">орг. граф</param>
        public BranchAndBound(Digraph graph)
        {
            Graph = graph;

            // переход в следующее состояние
            next = IterationState.Start;

            // создание пустого маршрута коммивояжера
            TsPath = new Digraph.Path();

            if (Graph.CountVertex() == 0)
            {
                // пустой маршрут
                // окончание метода
                next = IterationState.Stop;
            }
            else if (Graph.CountVertex() == 1)
            {
                // маршрут из одной вершины
                TsPath.Append(new Digraph.Edge(0, 0, Graph[0, 0]));
                // окончание метода
                next = IterationState.Stop;
            }
            else if (Graph.CountVertex() == 2)
            {
                // маршрут из двух вершин
                TsPath.Append(new Digraph.Edge(0, 1, Graph[0, 1]));
                TsPath.Append(new Digraph.Edge(1, 0, Graph[1, 0]));
                // окончание метода
                next = IterationState.Stop;
            }
        }

        public void Dispose()
        {

        }

        public IEnumerator GetEnumerator()
        {
            return this;
        }

        object IEnumerator.Current
        {
            get { return Current; }
        }

        /// <summary>
        /// Битовые изображения, ветвления метода и графа с выделенным маршрутом
        /// </summary>
        public Bitmap[] Current { private set; get; }

        public bool MoveNext()
        {
            // если текущий индекс не находиться в конце списка
            if (index < iterations.Count)
            {
                // текущее изображения берется из списка уже
                // пройденных на итерациях изображений
                Current = iterations[index++];
                return true;
            }
            // иначе определение следующего изображения
            else
            {
                // определение действий на текущей итерациии
                switch (next)
                {
                        // остановка метода ветвей и границ
                    case IterationState.Stop:
                        {
                            return false;
                        }
                        // начало метода ветвей и границ
                    case IterationState.Start:
                        {
                            // иницилизация данных
                            dsu = new Dsu(Graph.CountVertex());
                            min = new Branch(float.PositiveInfinity, null);
                            matrix = new ReductionMatrix(Graph.Adjacency);
                            parent = new Branch(matrix.Reduce(), null);
                            tree = new TreeBranch(parent);

                            // создание и добавление нового изображения ветвления и маршрута на графе для данной итерации
                            Current = new Bitmap[] { Painter.Drawing(tree), Painter.Drawing(Graph, tree.CreatePathFromBranch(parent))};
                            iterations.Add(Current);
                            // перемещение текущего индекса на конец списка
                            index = iterations.Count;

                            // переход в следующее состояние - левое ветвление метода
                            next = IterationState.LeftBranching;
                            return true;
                        }
                        // левое ветвление метода ветвей и границ
                    case IterationState.LeftBranching:
                        {
                            // определение ребер с нулевой стоимостью
                            var zeroEdges = new List<Digraph.Edge>();
                            for (int i = 0; i < matrix.Size; i++)
                                for (int j = 0; j < matrix.Size; j++)
                                    if (matrix[i, j] == 0)
                                        zeroEdges.Add(new Digraph.Edge(i, j, matrix.MinInRow(i, j) + matrix.MinInColumn(j, i)));

                            // если нет ребер ветвления - нет маршрута коммивояжера
                            if (zeroEdges.Count == 0)
                            {
                                TsPath = new Digraph.Path();

                                // остановка метода ветвей и границ
                                next = IterationState.Stop;
                                return false;
                            }

                            // определение ребра ветвления - ребра с максимальным штрафом
                            edge = zeroEdges.OrderByDescending(e => e.Cost).ToList().First();

                            // создание левого потомка для данного родителя
                            left = new Branch(parent.LowerBound + edge.Cost,
                                new Digraph.Edge(-edge.Begin, -edge.End, float.PositiveInfinity));
                            // добавление в дерево ветвлений
                            tree.Add(parent, Branch.Direction.Left, left);

                            // создание и добавление нового изображения ветвления и маршрута на графе для данной итерации
                            Current = new Bitmap[] { Painter.Drawing(tree), Painter.Drawing(Graph, tree.CreatePathFromBranch(left)) };
                            iterations.Add(Current);
                            // перемещение текущего индекса на конец списка
                            index = iterations.Count;

                            // переход в следующее состояние - правое ветвление метода
                            next = IterationState.RightBranching;
                            return true;
                        }
                        // правое ветвление метода
                    case IterationState.RightBranching:
                        {
                            // исключение подмаршрутов для данного ребра
                            ExcludeSubRoute(matrix, dsu, edge);

                            // создание правого потомка для данного родителя
                            right = new Branch(parent.LowerBound + matrix.Reduce(),
                                new Digraph.Edge(edge.Begin, edge.End, Graph[edge.Begin, edge.End]));
                            // добавление в дерево ветвлений
                            tree.Add(parent, Branch.Direction.Right, right);

                            // создание и добавление нового изображения ветвления и маршрута на графе для данной итерации
                            Current = new Bitmap[] { Painter.Drawing(tree), Painter.Drawing(Graph, tree.CreatePathFromBranch(right)) };
                            iterations.Add(Current);
                            // перемещение текущего индекса на конец списка
                            index = iterations.Count;

                            // если размер матрицы достаточно мал
                            if (matrix.RealSize == 2)
                            {
                                // переход в состояние - малый размер матрицы 
                                next = IterationState.LittleMatrix;
                                return true;
                            }

                            // выбор новой родительской вершины из еще не подвергшихся ветвлению
                            parent = tree.GetNotGoBranches().OrderBy(b => b.LowerBound).ToList().First();

                            // проверка на нахождения минимального ветвления и остановки
                            if (min.LowerBound < parent.LowerBound)
                            {
                                // окончание метода ветвей и границ
                                next = IterationState.End;
                                return true;
                            }

                            // корректировка матрицы для данного ветвления и редуцирование
                            if (parent != right)
                            {
                                // новые непересекающиеся множества вершин
                                dsu = new Dsu(Graph.CountVertex());
                                // исходная редуцированная матрица
                                matrix = new ReductionMatrix(Graph.Adjacency);
                                // получение текущих вершин для данного ветвления
                                var currentPath = tree.GetEdgesBranching(parent);

                                // исключение всех подмаршрутов
                                foreach (var e in currentPath)
                                    ExcludeSubRoute(matrix, dsu, e);

                                // редуцирование матрицы
                                matrix.Reduce();
                            }

                            // следующая итерация методав ветвей и границ - левое ветвление
                            next = IterationState.LeftBranching;
                            return true;
                        }
                        // малый рамзер матрицы, включение ребер в маршрут
                    case IterationState.LittleMatrix:
                        {
                            // продолжение вычисления матрицы
                            bool isContinue = false;
                            
                            // новый родитель
                            parent = right;
                            for (int i = 0; i < matrix.Size; i++)
                                for (int j = 0; j < matrix.Size; j++)
                                {
                                    if (matrix[i, j] == 0)
                                    {
                                        // исключение данного ребра из матрицы
                                        matrix[i, j] = float.PositiveInfinity;
                                        
                                        // создание и добавление правого ветвления к родителю
                                        right = new Branch(parent.LowerBound, new Digraph.Edge(i, j, Graph[i, j]));
                                        tree.Add(parent, Branch.Direction.Right, right);
                                        
                                        // продолжать включать ребра в маршрут
                                        isContinue = true;
                                    }

                                    // остановка на данной итерации
                                    if (isContinue) 
                                        break;
                                }

                            // если следующая итерация та же
                            if (isContinue)
                            {
                                // создание и добавление нового изображения ветвления и маршрута на графе для данной итерации
                                Current = new Bitmap[] { Painter.Drawing(tree), Painter.Drawing(Graph, tree.CreatePathFromBranch(right)) };
                                iterations.Add(Current);
                                // перемещение текущего индекса на конец списка
                                index = iterations.Count;

                                return true;
                            }

                            // иначе проверка на новое минимальное ветвление
                            if (parent.LowerBound < min.LowerBound)
                                min = parent;

                            // создание и добавление нового изображения ветвления и маршрута на графе для данной итерации
                            Current = new Bitmap[] { Painter.Drawing(tree), Painter.Drawing(Graph, tree.CreatePathFromBranch(parent)) };
                            iterations.Add(Current);
                            // перемещение текущего индекса на конец списка
                            index = iterations.Count;

                            // выбор новой родительской вершины из еще не подвергшихся ветвлению
                            parent = tree.GetNotGoBranches().OrderBy(b => b.LowerBound).ToList().First();

                            // проверка на нахождения минимального ветвления и остановки
                            if (min.LowerBound < parent.LowerBound)
                            {
                                // окончание метода ветвей и границ
                                next = IterationState.End;
                                return true;
                            }

                            // корректировка матрицы для данного ветвления и редуцирование
                            if (parent != right)
                            {
                                // новые непересекающиеся множества вершин
                                dsu = new Dsu(Graph.CountVertex());
                                // исходная редуцированная матрица
                                matrix = new ReductionMatrix(Graph.Adjacency);
                                // получение текущих вершин для данного ветвления
                                var currentPath = tree.GetEdgesBranching(parent);

                                // исключение всех подмаршрутов
                                foreach (var e in currentPath)
                                    ExcludeSubRoute(matrix, dsu, e);

                                // редуцирование матрицы
                                matrix.Reduce();
                            }

                            // следующая итерация методав ветвей и границ - левое ветвление
                            next = IterationState.LeftBranching;
                            return true;
                        }
                        // окончание метода
                    case IterationState.End:
                        {
                            // создание и добавление нового изображения ветвления и маршрута на графе для данной итерации
                            Current = new Bitmap[] { Painter.Drawing(tree), Painter.Drawing(Graph, tree.CreatePathFromBranch(min)) };
                            iterations.Add(Current);
                            // перемещение текущего индекса на конец списка
                            index = iterations.Count;

                            // формирование маршрута коммивояжера
                            TsPath = tree.CreatePathFromBranch(min);

                            // остановка метода
                            next = IterationState.Stop;
                            return false;
                        }
                    default:
                        return false;
                }
            }
        }

        public bool MovePrevious()
        {
            if (index > 1)
            {
                // текущее изображение указывает на изображение из списка согласно индексу
                Current = iterations[--index - 1];
                return true;
            }

            return false;
        }

        public void Reset()
        {
            // текущий индекс указывает на начало списка изображений
            index = 0;
        }

        #endregion

        /// <summary>
        /// Непересекающиеся множества
        /// </summary>
        public class Dsu
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
            /// <returns>представитель множества</returns>
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
            public float Reduce()
            {
                float min, minInRows = 0;
                for (int i = 0; i < Size; i++)
                {
                    min = MinInRow(i);
                    if (min != 0 && min != float.PositiveInfinity)
                    {
                        minInRows += min;
                        for (int j = 0; j < Size; j++)
                            this[i, j] -= min;
                    }
                }
                float minInColumns = 0;
                for (int i = 0; i < Size; i++)
                {
                    min = MinInColumn(i);
                    if (min != 0 && min != float.PositiveInfinity)
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
            public float MinInRow(int row, int notIncludeColumn = -1)
            {
                float min = float.PositiveInfinity;

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
            public float MinInColumn(int column, int notIncludeRow = -1)
            {
                float min = float.PositiveInfinity;

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
                    this[row, i] = float.PositiveInfinity;
                for (int i = 0; i < Size; i++)
                    this[i, column] = float.PositiveInfinity;

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
            public enum Direction
            {
                /// <summary>
                /// Левое ветвление
                /// </summary>
                Left,
                /// <summary>
                /// Правое ветвление
                /// </summary>
                Right
            }

            #region Конструкторы

            /// <summary>
            /// Создание пустого ветвления
            /// </summary>
            public Branch() { }

            /// <summary>
            /// Создания ветвления
            /// </summary>
            /// <param name="lowerBound">нижняя граница ветвления</param>
            /// <param name="branchingEdge">ребро ветвления</param>
            public Branch(float lowerBound, Digraph.Edge branchingEdge)
            {
                LowerBound = lowerBound;
                BranchingEdge = branchingEdge;
            }

            #endregion

            /// <summary>
            /// Нижняя граница ветвления
            /// </summary>
            public float LowerBound { set; get; }

            /// <summary>
            /// Ребро ветвления
            /// </summary>
            public Digraph.Edge BranchingEdge { set; get; }

            /// <summary>
            /// Родитель данного ветвления
            /// </summary>
            public Branch Parent { set; get; }

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
        public class TreeBranch
        {
            /// <summary>
            /// Корень дерева ветвления
            /// </summary>
            public Branch Root { set; get; }

            #region Конструкторы

            /// <summary>
            /// Создание пустого дерева ветвления
            /// </summary>
            public TreeBranch() { }

            /// <summary>
            /// Создание дерева ветвления
            /// </summary>
            /// <param name="root">корень дерева</param>
            public TreeBranch(Branch root)
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
            public void Add(Branch parent, Branch.Direction direct, Branch added)
            {
                if (direct == Branch.Direction.Left)
                    parent.Left = added;
                else
                    parent.Right = added;

                added.Parent = parent;
            }

            /// <summary>
            /// Получение ветвлений в дереве, которые не имеют потомков
            /// </summary>
            /// <returns>ветвления</returns>
            public List<Branch> GetNotGoBranches()
            {
                var brances = new List<Branch>();

                if (Root == null)
                    return brances;

                var stack = new Stack<Branch>();
                stack.Push(Root);

                while (stack.Count > 0)
                {
                    var branch = stack.Pop();
                    if (branch.Left == null && branch.Right == null)
                        brances.Add(branch);
                    if (branch.Right != null)
                        stack.Push(branch.Right);
                    if (branch.Left != null)
                        stack.Push(branch.Left);
                }
                return brances;
            }

            /// <summary>
            /// Получение текущих ребер для данного ветвления
            /// </summary>
            /// <param name="branch">ветвление</param>
            /// <returns>список ребер ветлений</returns>
            public List<Digraph.Edge> GetEdgesBranching(Branch branch)
            {
                var stack = new Stack<Digraph.Edge>();

                var current = branch;
                while (current.Parent != null)
                {
                    stack.Push(current.BranchingEdge);
                    current = current.Parent;
                }
                return stack.ToList();
            }

            /// <summary>
            /// Создание маршрута на основе ветвления 
            /// </summary>
            /// <param name="branch">ветвление</param>
            /// <returns>маршрут</returns>
            public Digraph.Path CreatePathFromBranch(Branch branch)
            {
                var edges = GetEdgesBranching(branch);

                var path = new Digraph.Path();

                foreach (var e in edges)
                    if (e.Begin >= 0 && e.End >= 0)
                        path.Append(e);

                return path;
            }
        }

        /// <summary>
        /// Исключение подмаршрутов в графе
        /// </summary>
        /// <param name="matrix">редуцированная матрица</param>
        /// <param name="dsu">непересекающиеся множества вершин</param>
        /// <param name="edges">ребро ветвления, которое уже находяться в маршруте или не входит в него</param>
        private static void ExcludeSubRoute(ReductionMatrix matrix, Dsu dsu, Digraph.Edge edge)
        {
            // если ребро не входит в ветвление
            if (edge.Begin < 0 || edge.End < 0)
                // исключение данного ребра из матрицы 
                matrix[Math.Abs(edge.Begin), Math.Abs(edge.End)] = float.PositiveInfinity;
            // ребро входит в ветвление
            else
            {
                // исключение строки и столбца из матрицы, соответсвующие началу и концу ребра
                matrix.DeleteRowColumn(edge.Begin, edge.End);

                // исключение оставщихся подмаршрутов
                for (int i = 0; i < matrix.Size; i++)
                    for (int j = 0; j < matrix.Size; j++)
                        if (dsu.Find(edge.Begin) == dsu.Find(i) && dsu.Find(edge.End) == dsu.Find(j))
                            matrix[j, i] = float.PositiveInfinity;

                // объединение двух вершин графа в одно множество 
                dsu.Union(edge.Begin, edge.End);
            }
        }

        /// <summary>
        /// Нахождение маршрута коммивояжера
        /// </summary>
        /// <param name="graph">ограф. граф</param>
        /// <returns>маршрут коммивояжера</returns>
        public static Digraph.Path Tsp(Digraph graph)
        {
            // маршрут коммивояжера
            var TsPath = new Digraph.Path();

            // если граф пуст
            if (graph.CountVertex() == 0)
            {
                // пустой маршрут
                return TsPath;
            }
            // если граф имеет одну вершину
            else if (graph.CountVertex() == 1)
            {
                TsPath.Append(new Digraph.Edge(0, 0, graph[0, 0]));
                // маршрут для одной вершины
                return TsPath;
            }
            // если граф имеет две вершины
            else if (graph.CountVertex() == 2)
            {
                TsPath.Append(new Digraph.Edge(0, 1, graph[0, 1]));
                TsPath.Append(new Digraph.Edge(1, 0, graph[1, 0]));
                // маршрут для двух вершин
                return TsPath;
            }

            /// Создания неперекающихся множеств вершин в графе, 
            /// для определения и исключения подмаршрутов графа
            var dsu = new Dsu(graph.CountVertex());

            // минимальное ветвление
            var minBranch = new Branch(float.PositiveInfinity, null);

            /// Получение исходной матрицы смежности данного графа
            var matrix = new ReductionMatrix(graph.Adjacency);

            /// Создание корня и дерева ветвления
            var parentBranch = new Branch(matrix.Reduce(), null);
            var tree = new TreeBranch(parentBranch);

            for (;;)
            {
                // ребра с нулевой стоимостью
                var zeroEdges = new List<Digraph.Edge>();
                // Получение всех ребер и соответсвующих штрафов
                for (int i = 0; i < matrix.Size; i++)
                    for (int j = 0; j < matrix.Size; j++)
                        if (matrix[i, j] == 0)
                            zeroEdges.Add(new Digraph.Edge(i, j, matrix.MinInRow(i, j) + matrix.MinInColumn(j, i)));

                // если нет ребер ветвления - нет маршрута коммивояжера
                if (zeroEdges.Count == 0)
                    return new Digraph.Path();

                /// Определение ребра ветвления - ребра с максимальным штрафом
                var branchingEdge = zeroEdges.OrderByDescending(e => e.Cost).ToList().First();

                /// Процесс ветления - не включая данное ребро
                var leftBranch = new Branch(parentBranch.LowerBound + branchingEdge.Cost,
                    new Digraph.Edge(-branchingEdge.Begin, -branchingEdge.End, float.PositiveInfinity));
                // добавление ветвления в дерево
                tree.Add(parentBranch, Branch.Direction.Left, leftBranch);

                /// Процесс ветления - включая данное ребро
                ExcludeSubRoute(matrix, dsu, branchingEdge);

                var rightBranch = new Branch(parentBranch.LowerBound + matrix.Reduce(),
                    new Digraph.Edge(branchingEdge.Begin, branchingEdge.End, graph[branchingEdge.Begin, branchingEdge.End]));
                // добавление ветвления в дерево
                tree.Add(parentBranch, Branch.Direction.Right, rightBranch);

                /// Проверка на достаточность размера матрцицы
                if (matrix.RealSize == 2)
                {
                    // новый родитель
                    parentBranch = rightBranch;
                    /// Добавление оставщихся ребер в дерево ветвлений
                    for (int i = 0; i < matrix.Size; i++)
                        for (int j = 0; j < matrix.Size; j++)
                            if (matrix[i, j] == 0)
                            {
                                // новый потомок 
                                rightBranch = new Branch(parentBranch.LowerBound, new Digraph.Edge(i, j, graph[i, j]));
                                tree.Add(parentBranch, Branch.Direction.Right, rightBranch);
                                // потомок теперь родитель
                                parentBranch = rightBranch;
                            }

                    /// Определение нового минимального ветвления
                    if (parentBranch.LowerBound < minBranch.LowerBound)
                        minBranch = parentBranch;
                }

                /// Выбор новой родительской вершины из еще не подвергшихся ветвлению
                parentBranch = tree.GetNotGoBranches().OrderBy(b => b.LowerBound).ToList().First();

                /// Проверка на нахождения минимального ветвления и остановки
                if (minBranch.LowerBound <= parentBranch.LowerBound)
                    break;

                /// Корректировка матрицы для данного ветвления и редуцирование
                if (parentBranch != rightBranch)
                {
                    // новые непересекающиеся множества вершин
                    dsu = new Dsu(graph.CountVertex());
                    // исходная редуцированная матрица
                    matrix = new ReductionMatrix(graph.Adjacency);
                    // получение текущих вершин для данного ветвления
                    var currentPath = tree.GetEdgesBranching(parentBranch);

                    // исключение всех подмаршрутов
                    foreach (var e in currentPath)
                        ExcludeSubRoute(matrix, dsu, e);

                    // редуцирование матрицы
                    matrix.Reduce();
                }
            }

            // формирование маршрута коммивояжера
            TsPath = tree.CreatePathFromBranch(minBranch);

            return TsPath;
        }
    }
}
