using System;
using System.Collections.Generic;
using System.Linq;

namespace tsp
{
    /// <summary>
    /// Ориентированный граф
    /// </summary>
    public class Digraph
    {
        /// <summary>
        /// Матрица смежности графа
        /// </summary>
        public class AdjacencyMatrix : ICloneable
        {
            private double[,] entries;

            /// <summary>
            /// Размер матрицы смежности
            /// </summary>
            public int Size { private set; get; }

            #region Конструторы

            /// <summary>
            /// Создание матрицы с указанным размером
            /// </summary>
            /// <param name="n">размер матрицы</param>
            public AdjacencyMatrix(int n)
            {
                Size = n;
                entries = new double[Size, Size];
            }

            /// <summary>
            /// Создание матрицы на основе другой матрицы
            /// </summary>
            /// <param name="other">другая матрица</param>
            public AdjacencyMatrix(AdjacencyMatrix other)
            {
                var clone = other.Clone() as AdjacencyMatrix;

                Size = clone.Size;
                entries = clone.entries;
            }

            #endregion

            /// <summary>
            /// Индексатор
            /// </summary>
            /// <param name="row">индекс строки</param>
            /// <param name="col">индекс столбца</param>
            /// <returns>значение элемента матрицы</returns>
            public double this[int row, int col]
            {
                get { return entries[row, col]; }
                set { entries[row, col] = value; }
            }

            /// <summary>
            ///  Клонирование объекта
            /// </summary>
            /// <returns>клон объекта</returns>
            public object Clone()
            {
                var clone = new AdjacencyMatrix(Size);

                for (int i = 0; i < Size; i++)
                    for (int j = 0; j < Size; j++)
                        clone[i, j] = entries[i, j];

                return clone as object;
            }
        }

        /// <summary>
        /// Ребро графа
        /// </summary>
        public class Edge : IEquatable<Edge>
        {
            #region Конструторы

            /// <summary>
            /// Создание ребра
            /// </summary>
            /// <param name="begin">вершина-начало</param>
            /// <param name="end">вершина-конец</param>
            /// <param name="cost">стоимость ребра</param>
            public Edge(int begin, int end, double cost)
            {
                Begin = begin;
                End = end;
                Cost = cost;
            }

            #endregion

            /// <summary>
            /// Вершина-начало ребра 
            /// </summary>
            public int Begin { get; set; }

            /// <summary>
            /// Вершина-конец ребра
            /// </summary>
            public int End { get; set; }

            /// <summary>
            /// Стоимость ребра
            /// </summary>
            public double Cost { get; set; }

            /// <summary>
            /// Проверка на эквивалентность ребер
            /// </summary>
            /// <param name="other">другое ребро</param>
            /// <returns>результат проверки</returns>
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

            /// <summary>
            /// Проверка на эквивалентность ребер
            /// </summary>
            /// <param name="other">другое ребро</param>
            /// <returns>результат проверки</returns>
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

            /// <summary>
            /// Получение хеш-кода
            /// </summary>
            /// <returns>хеш-код</returns>
            public override int GetHashCode()
            {
                return Begin ^ End ^ (int)Cost;
            }
        }

        /// <summary>
        /// Маршрут
        /// </summary>
        public class Path : ICloneable
        {
            private LinkedList<Edge> edges = new LinkedList<Edge>();

            #region Конструторы

            /// <summary>
            /// Создание пустого маршрута
            /// </summary>
            public Path()
            {
                Cost = 0;
            }

            /// <summary>
            /// Создание маршрута на основе другого
            /// </summary>
            /// <param name="other">другой маршрут</param>
            public Path(Path other)
            {
                var clone = other.Clone() as Path;

                edges = clone.edges;
                Cost = clone.Cost;
            }

            #endregion

            /// <summary>
            /// Суммарная стоимость маршрута
            /// </summary>
            public double Cost { get; private set; }

            /// <summary>
            /// Добавление ребра в маршрут
            /// </summary>
            /// <param name="e">ребро</param>
            /// <returns>успешность добавления</returns>
            public bool Append(Edge e)
            {
                if (edges.Contains(e)) 
                    return false;

                Cost += e.Cost;
                edges.AddLast(e);

                return true;
            }

            /// <summary>
            /// Проверка маршрута на существование
            /// </summary>
            /// <returns>результат существования</returns>
            public bool IsExists()
            {
                return edges.Count() != 0 && !double.IsPositiveInfinity(Cost);
            }

            /// <summary>
            /// Проверка принадлежности ребра к данному маршруту
            /// </summary>
            /// <param name="e">ребро</param>
            /// <returns>результат проверки</returns>
            public bool IsContain(Edge e)
            {
                return edges.Contains(e);
            }

            /// <summary>
            /// Получение вершины-конца для вершины-начала в данном маршруте
            /// </summary>
            /// <param name="begin">вершина-начало</param>
            /// <returns>-1: данная вершина не имеет инцидетного ребра, иначе вершина-конец</returns>
            public int GetEndOfEdge(int begin)
            {
                int end = -1;
                foreach (var e in edges)
                {
                    if (begin == e.Begin)
                        return e.End;
                }
                return end;
            }

            /// <summary>
            /// Получение перечислителя
            /// </summary>
            /// <returns>перечислитель</returns>
            public IEnumerator<Edge> GetEnumerator()
            {
                foreach (var i in edges) 
                    yield return i;
            }

            /// <summary>
            /// Клонирование
            /// </summary>
            /// <returns>клон объекта</returns>
            public object Clone() {
                var clone = new Path();
                
                clone.Cost = Cost;
                clone.edges = new LinkedList<Edge>(edges);

                return clone as object;
            }
        }

        #region Конструторы

        /// <summary>
        /// Создание пустого орг. графа
        /// </summary>
        public Digraph() 
        {
            Adjacency = new AdjacencyMatrix(0);
        }

        /// <summary>
        /// Создание орг. графа на основе указанный матрице смежности
        /// </summary>
        /// <param name="matrix"></param>
        public Digraph(AdjacencyMatrix matrix)
        {
            Adjacency = matrix;
        }

        #endregion Конструторы

        /// <summary>
        /// Матрица смежности
        /// </summary>
        public AdjacencyMatrix Adjacency { set; get; }

        /// <summary>
        /// Получение перечислителия для обхода всех ребер графа
        /// </summary>
        /// <returns>перечислитель</returns>
        public IEnumerator<Edge> GetEnumerator()
        {
            var list = new LinkedList<Edge>();

            for (int i = 0; i < CountVertex(); ++i)
                for (int j = 0; j < CountVertex(); ++j)
                    if (!double.IsPositiveInfinity(Adjacency[i, j])) list.AddLast(new Edge(i, j, Adjacency[i, j]));

            foreach (var i in list) 
                yield return i;
        }

        /// <summary>
        /// Получение стоимости ребра по матрице смежности
        /// </summary>
        /// <param name="begin">вершина-начало</param>
        /// <param name="end">вершина-конец</param>
        /// <returns></returns>
        public double this[int begin, int end]
        {
            get { return Adjacency[begin, end]; }
            set { Adjacency[begin, end] = value; }
        }

        /// <summary>
        /// Количество вершин в графе
        /// </summary>
        /// <returns>количество вершин</returns>
        public int CountVertex()
        {
            return Adjacency.Size;
        }
    }
}
