using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace tsp
{
    /// <summary>
    /// Алгоритм решения задачи коммивояжера методом Ветвей и Границ
    /// </summary>
    public class BranchAndBound
    {
        /// <summary>
        /// Режим выполнения алгоритма
        /// </summary>
        public enum Mode
        {
            /// <summary>
            /// Без трассировки по шагам
            /// </summary>
            Notracing,
            /// <summary>
            /// С трассировкой
            /// </summary>
            Tracing
        }

        // <summary>
        /// Редуцированная матрица
        /// </summary>
        public class ReducedMatrix : SqureMatrix
        {
            // реальный размер квадратной матрицы
            public int RealSize { get; private set; }

            /// <summary>
            /// Создание редуцированной матрицы с указанным размером
            /// </summary>
            /// <param name="n"> размер матрицы </param>
            public ReducedMatrix(int n)
                : base(n)
            {

            }

            /// <summary>
            /// Создание редуцированной на основе квадратной матрицы
            /// </summary>
            /// <param name="smatrix"></param>
            public ReducedMatrix(SqureMatrix smatrix)
                : base(smatrix)
            {
            }

            /// <summary>
            /// Создание редуцированной на основе другой редуцированной матрицы
            /// </summary>
            /// <param name="rmatrix"></param>
            public ReducedMatrix(ReducedMatrix rmatrix)
                : base(rmatrix)
            {
                RealSize = rmatrix.RealSize;
            }

            /// <summary>
            /// Приведение матрицы и вычисление нижне оценки множества всех гамильтоновых контуров
            /// </summary>
            /// <returns> нижняя оценка множества всех гамильтоновых контуров </returns>
            public double Reduce()
            {
                double min, minInRows = 0;
                for (int i = 0; i < Size(); i++)
                {
                    minInRows += min = MinInRow(i);
                    for (int j = 0; j < Size(); j++) this[i, j] -= min;
                }

                double minInColumns = 0;
                for (int i = 0; i < Size(); i++)
                {
                    minInColumns += min = MinInColumn(i);
                    for (int j = 0; j < Size(); j++) this[j, i] -= min;
                }

                return minInRows + minInColumns;
            }

            /// <summary>
            /// Нахождение минимума в строке
            /// </summary>
            /// <param name="row"> номер строки </param>
            /// <param name="notIncludeColumn"> номер столбца, который не содержит минимум </param>
            /// <returns> минимум в строке </returns>
            public double MinInRow(int row, int notIncludeColumn = -1)
            {
                double min = notIncludeColumn != 0 ? this[row, 0] : this[row, 1];
                for (int i = 0; i < Size(); i++)
                    if (notIncludeColumn != i && min > this[row, i]) min = this[row, i];
                return min;
            }

            /// <summary>
            /// Нахождение минимума в столбце
            /// </summary>
            /// <param name="column"> номер столбца </param>
            /// <param name="notIncludeRow"> номер строки, которая не содержит минимум </param>
            /// <returns> минимум в столбце </returns>
            public double MinInColumn(int column, int notIncludeRow = -1)
            {
                double min = notIncludeRow != 0 ? this[0, column] : this[1, column];
                for (int i = 0; i < Size(); i++)
                    if (notIncludeRow != i && min > this[i, column]) min = this[i, column];
                return min;
            }

            /// <summary>
            /// Исключение указанной строки и столбца из редуцированной матрицы
            /// </summary>
            /// <param name="row"> номер строки </param>
            /// <param name="column"> номер столбца </param>
            public void DeleteRowAndColumn(int row, int column)
            {
                for (int i = 0; i < Size(); i++) this[row, i] = double.PositiveInfinity;
                for (int i = 0; i < Size(); i++) this[i, column] = double.PositiveInfinity;

                RealSize--;
            }
        }

        /// <summary>
        /// Узел ветвление 
        /// </summary>
        public class Branch : IComparable<Branch>
        {
            /// <summary>
            /// Тип ориентацие узла(потомка) по отношению к родителю
            /// </summary>
            public enum Type
            {
                /// <summary>
                /// Левый потомок
                /// </summary>
                Left,
                /// <summary>
                /// Правый потомок
                /// </summary>
                Right
            }

            /// <summary>
            /// Редуцированная матрица для данного шага
            /// </summary>
            public ReducedMatrix Matrix { set; get; }

            /// <summary>
            /// Ребро над которомы происходит ветвление
            /// </summary>
            public Graph.Edge Edge { set; get; }

            /// <summary>
            /// Нижняя граница данного ветвления
            /// </summary>
            public double Bound { set; get; }

            /// <summary>
            /// Правый потомок данного узла ветвления
            /// </summary>
            public Branch Right { set; get; }

            /// <summary>
            /// Левый потомок данного узла ветвления
            /// </summary>
            public Branch Left { set; get; }

            /// <summary>
            /// Создание ветвления
            /// </summary>
            public Branch() { }
            
            /// <summary>
            /// Создание ветвления
            /// </summary>
            /// <param name="bound"> нижняя граница ветвления </param>
            /// <param name="matrix"> редуцированная матрица </param>
            /// <param name="edge"> ребро принадлежащее ветвлению </param>
            public Branch(double bound, ReducedMatrix matrix, Graph.Edge edge)
            {
                Bound = bound;
                Matrix = matrix;
                Edge = edge;
            }

            /// <summary>
            /// Создание ветвления на основе другого ветвления
            /// </summary>
            /// <param name="other"> другое ветвление </param>
            public Branch(Branch other)
            {
                Bound = other.Bound;
                Matrix = new ReducedMatrix(other.Matrix);
                Edge = new Graph.Edge(other.Edge);
                Right = other.Right;
                Left = other.Left;
            }

            /// <summary>
            /// Сравнение ветвлений на основе нижней границе 
            /// </summary>
            /// <param name="other"> другое ветвление </param>
            /// <returns></returns>
            public int CompareTo(Branch other)
            {
                return Bound.CompareTo(other.Bound);
            }
        }

        /// <summary>
        /// Дерево ветвлений 
        /// </summary>
        public class TreeBranch
        {
            /// <summary>
            /// Корень дерева ветвления
            /// </summary>
            public Branch Root { set; private get; }

            /// <summary>
            /// Создание дерева ветвлений
            /// </summary>
            /// <param name="root"></param>
            public TreeBranch(Branch root)
            {
                Root = root;
            }

            /// <summary>
            /// Добавление в дерево 
            /// </summary>
            /// <param name="parent"> родитель - узел к которому будет присоединен новый узел-потомок</param>
            /// <param name="direction"> напрваление ветвления </param>
            /// <param name="b"> узел-потомок </param>
            /// <returns> добавленный узел</returns>
            public Branch Add(Branch parent, Branch.Type direction, Branch b)
            {
                if (direction == Branch.Type.Left) parent.Left = b;
                else parent.Right = b;

                return b;
            }
        }

        public static Graph.Path SearchMinHamiltonianCyle(Graph g, Mode mode = Mode.Notracing)
        {
            // минимальная стоимость самого дещевого гамильтоновго цикла
            double minBound = double.PositiveInfinity;
            
            // редуцированная матрица 
            ReducedMatrix matrix = new ReducedMatrix(g.Adjacency);
            
            // текущий узел ветвления
            Branch currenBranch = new Branch(matrix.Reduce(), matrix, null);
            
            // создание дерева ветвления с указанным корнем
            TreeBranch tree = new TreeBranch(currenBranch);

            do 
            {
                // выбор ребра для следующего ветвления


            } while (minBound > currenBranch.Bound);

            return new Graph.Path();
        }
    }
}
