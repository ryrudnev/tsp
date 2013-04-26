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
            private float[,] entries;

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
                entries = new float[Size, Size];
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
            public float this[int row, int col]
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
            public Edge(int begin, int end, float cost)
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
            public float Cost { get; set; }

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
        public class Path
        {
            // набор ребер принаддежащих маршруту
            private LinkedList<Edge> edges = new LinkedList<Edge>();

            #region Конструторы

            /// <summary>
            /// Создание маршрута
            /// </summary>
            public Path()
            {
                Cost = 0;
            }

            #endregion

            /// <summary>
            /// Суммарная стоимость маршрута
            /// </summary>
            public float Cost { get; private set; }

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
                return edges.Count() != 0 && !float.IsPositiveInfinity(Cost);
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
            /// Получение списка вершин в порядке их обхода
            /// </summary>
            /// <returns>список вершин в порядке обхода</returns>
            public List<int> GetVertexInOrderTraversal()
            {
                var vertexInOrder = new List<int>();

                var vertex = new List<int>();
                foreach(var e in edges) 
                {
                    if (!vertex.Contains(e.Begin))
                        vertex.Add(e.Begin);
                    if (!vertex.Contains(e.End))
                        vertex.Add(e.End);
                }
                vertex.Sort();

                if (vertex.Count == 0)
                    return vertexInOrder;

                int begin = vertex.First(), end = -1;

                vertexInOrder.Add(begin);

                for (int i = 0; i < vertex.Count; i++)
                {
                    foreach (var e in edges)
                    {
                        if (begin == e.Begin) 
                        {
                            end = e.End;
                            break;
                        }
                    }

                    if (end == -1)
                        return vertexInOrder;

                    vertexInOrder.Add(end);

                    begin = end;

                    end = -1;
                }

                return vertexInOrder;
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
                    if (!float.IsPositiveInfinity(Adjacency[i, j])) 
                        list.AddLast(new Edge(i, j, Adjacency[i, j]));

            foreach (var i in list)
                yield return i;
        }

        /// <summary>
        /// Получение стоимости ребра по матрице смежности
        /// </summary>
        /// <param name="begin">вершина-начало</param>
        /// <param name="end">вершина-конец</param>
        /// <returns></returns>
        public float this[int begin, int end]
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
