using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Drawing;
using System.IO;

namespace tsp
{
    /// <summary>
    /// Граф
    /// </summary>
    public class Graph
    {
        /// <summary>
        /// Визуализация графа
        /// </summary>
        public class Painter
        {
            /// <summary>
            /// Рендеринг графа средствами Graphviz, используя dot.exe
            /// </summary>
            /// <param name="script"> скрипт на языке dot </param>
            /// <returns> битовая карта, содержащая изображения графа.
            /// Если во время отрисовки произошли ошибки, выбрасывается исключение с указанием причины ошибки
            /// </returns>
            private static Bitmap RenderingOnGraphviz(string script)
            {
                // проверка существования исполняемого файла Graphviz
                if (!File.Exists(@"..\Graphviz\dot.exe")) throw new Exception("Приложения dot.exe не найдено");

                // запуск процесса
                Process dot = new Process();
                try
                {
                    // задание параметров процесса
                    dot.StartInfo.UseShellExecute = false;
                    dot.StartInfo.FileName = @"..\Graphviz\dot.exe";
                    dot.StartInfo.Arguments = "-Tpng";
                    dot.StartInfo.RedirectStandardInput = true;
                    dot.StartInfo.RedirectStandardOutput = true;
                    dot.StartInfo.CreateNoWindow = true;
                    dot.Start();

                    // передача входных данных для Graphviz
                    dot.StandardInput.Write(script);
                    dot.StandardInput.Close();

                    // формирование битового изображения из выходных данных Graphviz
                    Bitmap image = new Bitmap(dot.StandardOutput.BaseStream, true);
                    dot.StandardOutput.Close();

                    return image;
                }
                catch (Exception e)
                {
                    throw new Exception("Не возможно отрисовать граф", e);
                }
            }

            /// <summary>
            /// Простая отрисовка указанного графа
            /// </summary>
            /// <param name="g"> указанный граф </param>
            /// <returns> битовая карта </returns>
            public static Bitmap Drawing(Graph g)
            {
                if (g == null) throw new Exception("Невозможно отрисовать граф. Граф пуст (null)");

                // фомирование скрипта на языке dot
                string script = (g.IsType == Graph.Type.Directed ? "digraph " : "graph ") + "G { nrankdir = LR ";

                // взаимосвязь между вершинами, взависимости от типа графа
                string link = g.IsType == Graph.Type.Directed ? " -> " : " -- ";

                // задание вершин
                for (int i = 0; i < g.CountVertex(); i++)
                    script += (i + 1) + " ";

                // задание ребер графа
                for (int i = 0; i < g.CountVertex(); i++)
                    for (int j = 0; j < g.CountVertex(); j++)
                        if (!Double.IsInfinity(g.Adjacency[i, j]))
                            script += (i + 1) + link + (j + 1) + " [label=" + g.Adjacency[i, j] + "] ";

                script += "}";

                // передача скрипта и рендеринг в утилите Graphviz
                return RenderingOnGraphviz(script);
            }

            /// <summary>
            /// Отрисовка указанного графа с выделенныи путем
            /// </summary>
            /// <param name="g"> указанный граф </param>
            /// <param name="p"> указанный путь </param>
            /// <returns> битовая карта </returns>
            public static Bitmap Drawing(Graph g, Path p)
            {
                if (g == null) throw new Exception("Невозможно отрисовать граф. Граф пуст (null)");

                if (p == null) return Drawing(g);

                // проверка на принадлежность контура графу
                if (g != p.Owner) throw new Exception("Указанный контур не принадлежит данному графу");

                // фомирование скрипта на языке dot
                string script = (g.IsType == Graph.Type.Directed ? "digraph " : "graph ") + "G { ";

                // взаимосвязь между вершинами, взависимости от типа графа
                string link = g.IsType == Graph.Type.Directed ? " -> " : " -- ";

                // задание вершин
                for (int i = 0; i < g.CountVertex(); i++)
                    script += (i + 1) + " ";

                // задание ребер графа
                for (int i = 0; i < g.CountVertex(); i++)
                    for (int j = 0; j < g.CountVertex(); j++)
                    {
                        // если  ребро имеет стоимость не равную бесконечности
                        if (!Double.IsInfinity(g.Adjacency[i, j]))
                        {
                            script += (i + 1) + link + (j + 1) + " [label=" + g.Adjacency[i, j];

                            if (p.IsContain(new Edge(g, i, j, g.Adjacency[i, j]))) script += ", color=red fontcolor=red";

                            script += "] ";
                        }
                    }

                script += "}";

                return RenderingOnGraphviz(script);
            }
        }

        /// <summary>
        /// Определение типов графа
        /// </summary>
        public enum Type
        {
            /// <summary>
            /// Неориентированный граф
            /// </summary>
            Undirected,
            /// <summary>
            /// Ориентированный граф
            /// </summary>
            Directed
        }

        /// <summary>
        /// Ребро (дуга) графа
        /// </summary>
        public class Edge : IEquatable<Edge>
        {
            /// <summary>
            /// Создание ребра
            /// </summary>
            /// <param name="owner"> граф владелец </param>
            /// <param name="begin"> начало ребра </param>
            /// <param name="end"> конец ребра </param>
            /// <param name="cost"> стоимость ребра </param>
            public Edge(Graph owner, int begin, int end, double cost)
            {
                Owner = owner;
                Begin = begin;
                End = end;
                Cost = cost;
            }

            /// <summary>
            /// Обладатель данного ребра
            /// </summary>
            public Graph Owner { get; private set; }

            /// <summary>
            /// Вершина - начало ребра
            /// </summary>
            public int Begin { get; set; }

            /// <summary>
            /// Вершина - конец ребра
            /// </summary>
            public int End { get; set; }

            /// <summary>
            /// Стоимость (вес) ребра
            /// </summary>
            public double Cost { get; set; }

            /// <summary>
            /// Сравнение как объектов по умолчанию на эквивалентность
            /// </summary>
            /// <param name="other"> другой объект</param>
            /// <returns> результат сравнения эквивалентности </returns>
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
            /// Сравнение двух ребер на эквивалентность 
            /// </summary>
            /// <param name="other"> другое ребро </param>
            /// <returns> результат сравнения эквивалентности</returns>
            public bool Equals(Edge other)
            {
                if (other == null)
                    return false;

                if (object.ReferenceEquals(this, other))
                    return true;

                if (this.GetType() != other.GetType())
                    return false;

                return Owner == other.Owner && Begin == other.Begin && End == other.End && Cost == other.Cost;
            }

            /// <summary>
            /// Получение хеш - кода 
            /// </summary>
            /// <returns> хеш код </returns>
            public override int GetHashCode()
            {
                return Begin ^ End ^ (int)Cost;
            }
        }

        /// <summary>
        /// Путь (контур) в графе
        /// </summary>
        public class Path
        {
            /// <summary>
            /// Создание "пустого" контура для указанного графа
            /// </summary>
            /// <param name="owner"> граф </param>
            public Path(Graph owner)
            {
                Owner = owner;
                edges = new LinkedList<Edge>();
            }

            /// <summary>
            /// Обладатель данного контура
            /// </summary>
            public Graph Owner { get; private set; }

            /// <summary>
            /// Ребра графа входящие в контур 
            /// </summary>
            public LinkedList<Edge> edges { get; private set; }

            /// <summary>
            /// Стоимость контура
            /// </summary>
            public double Cost { get; private set; }

            /// <summary>
            /// Добавление ребра в контур графа
            /// </summary>
            /// <param name="edge"> добавляемое ребро </param>
            /// <returns> успешность добавления </returns>
            public bool Append(Edge e)
            {
                if (Owner == null || Owner != e.Owner || edges.Contains(e)) return false;

                Cost += e.Cost;
                edges.AddFirst(e);

                return true;
            }

            /// <summary>
            /// Проверка на существование контура в графе
            /// </summary>
            /// <returns> результат проверки </returns>
            public bool IsExists()
            {
                foreach (var e in edges)
                    if (Double.IsInfinity(e.Cost)) return false;

                return Owner != null && edges.Count() != 0;
            }

            /// <summary>
            /// Проверка принадлежности ребра в данный контур
            /// </summary>
            /// <param name="e"> ребро </param>
            /// <returns> результат проверки </returns>
            public bool IsContain(Edge e)
            {
                if (e.Owner == null || e.Owner != Owner) return false;

                return edges.Contains(e);
            }
        }

        /// <summary>
        /// Тип графа
        /// </summary>
        public Type IsType { set; get; }

        /// <summary>
        /// Матрица смежности 
        /// </summary>
        public SqureMatrix Adjacency { set; get; }

        /// <summary>
        /// Создание графа на основе матрицы смежности 
        /// </summary>
        /// <param name="adjacency"></param>
        public Graph(Type type, SqureMatrix adjacency)
        {
            IsType = type;
            Adjacency = SqureMatrix.Copy(adjacency);
        }

        /// <summary>
        /// Создание графа на основе другого графа
        /// </summary>
        /// <param name="other"></param>
        public Graph(Graph other)
        {
            IsType = other.IsType;
            Adjacency = SqureMatrix.Copy(other.Adjacency);
        }

        /// <summary>
        /// Считывание полного графа из текстового файла
        /// </summary>
        /// <param name="fileName"> имя текстового файла </param>
        /// <returns> полный граф.
        /// В случае ошибок, выбрасывается исключение с указанием причины ошибки
        /// </returns>
        public static Graph TakenFromFile(string fileName)
        {
            // проверка указанного файла на существование
            if (!File.Exists(fileName)) throw new Exception("Ошибка! Заданного входного файла не существует.");

            // считывание всех строк входного файла
            var lines = File.ReadAllLines(fileName);
            int size = lines.Length;

            // проверка на пустоту файла
            if (size == 0) throw new Exception("Ошибка! Заданный входной файл пуст.");

            // проверка на наличие типа графа 
            var values = lines.First().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (values.Length != 1) throw new Exception("Ошибка! Не указан тип графа.");

            int type; // тип графа

            //проверка на то является ли данный символ числом
            if (!int.TryParse(values.First(), out type)) throw new Exception("Ошибка! Матрица смежности содержит недопустимые символы.");

            // проверка на корректность указанного типа графа
            if (!(type == 0 || type == 1)) throw new Exception("Ошибка! Неверно указан тип графа.");

            size--;

            // проверка на допустимость размера матрицы 
            if (size < 2 || size > 35) throw new Exception("Ошибка! Недопустимый размер матрицы.");

            SqureMatrix matrix = new SqureMatrix(size);

            // заполнение матрицы 
            for (int i = 0; i < size; i++)
            {
                // чтение очередной строки матрицы
                values = lines[i + 1].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                // проверка матрицы на квадратность
                if (values.Length != size) throw new Exception("Ошибка! Матрица смежности должна быть квадратной.");

                // заполнение значений в строке
                for (int j = 0; j < size; j++)
                {
                    double c;

                    // проверка на то является ли данный символ числом
                    if (!double.TryParse(values[j], out c))
                    {
                        // проверка на возможность замены строки inf на бесконечность, для главной диагонали
                        if (values[j].CompareTo("inf") == 0) matrix[i, j] = System.Double.PositiveInfinity;
                        else throw new Exception("Ошибка! Матрица смежности содержит недопустимые символы.");
                    }
                    else if (c < 0) throw new Exception("Ошибка! Матрица смежности содержит отрицательные элементы.");
                    else if (i == j) throw new Exception("Ошибка! Матрица смежности должна описывать полный граф.");
                    else matrix[i, j] = c;
                }
            }

            return new Graph((Graph.Type)type, matrix);
        }

        /// <summary>
        /// Проверка принадлежности контура графу
        /// </summary>
        /// <param name="p"> контур </param>
        /// <returns> результат проверки </returns>
        public bool IsOwnedPath(Path p)
        {
            return this == p.Owner;
        }

        /// <summary>
        /// Проверка принадлежности ребра графу 
        /// </summary>
        /// <param name="e"> ребро </param>
        /// <returns> результат проверки </returns>
        public bool IsOwnedEdge(Edge e)
        {
            return this == e.Owner;
        }

        /// <summary>
        /// Количество вершин в графе
        /// </summary>
        /// <returns> количество вершин </returns>
        public int CountVertex()
        {
            return Adjacency.Size();
        }

        /// <summary>
        /// Количество ребер в графе
        /// </summary>
        /// <returns></returns>
        public int CountEdge()
        {
            return Edges().Count;
        }

        /// <summary>
        /// Получение ребер графа
        /// </summary>
        /// <returns> ребра </returns>
        public List<Edge> Edges()
        {
            List<Edge> edges = new List<Edge>();
            for (int i = 0; i < CountVertex(); i++)
                for (int j = 0; j < CountVertex(); j++)
                    edges.Add(new Edge(this, i, j, Adjacency[i, j]));
            return edges;
        }

    }
}
