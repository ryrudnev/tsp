using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;

namespace tsp
{
    /// <summary>
    /// Отрисовщик
    /// </summary>
    public class Painter
    {
        /// <summary>
        /// Рендеринг изображения средствами Graphviz dot.exe
        /// </summary>
        /// <param name="script">скрипт на языке dot</param>
        /// <returns>битовое изображение</returns>
        private static Bitmap RenderingOnGraphviz(string script)
        {
            if (!File.Exists(@"..\Graphviz\dot.exe"))
                throw new Exception("Приложения dot.exe не найдено");

            var dot = new Process();
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

        /// <summary>
        /// Отрисовка ориентированного графа
        /// </summary>
        /// <param name="graph">орг. граф</param>
        /// <returns>битовое изображение</returns>
        public static Bitmap Drawing(Digraph graph)
        {
            if (graph == null || graph.CountVertex() == 0)
                throw new Exception("Невозможно отрисовать граф. Граф не задан.");

            string script = "digraph G { nrankdir=LR  node [style=\"filled\", fillcolor=\"skyblue\"]";

            for (int i = 0; i < graph.CountVertex(); i++)
                script += (i + 1) + " ";

            for (int i = 0; i < graph.CountVertex(); i++)
                for (int j = 0; j < graph.CountVertex(); j++)
                    if (!float.IsInfinity(graph[i, j]))
                        script += (i + 1) + " -> " + (j + 1) + " [label=\"" + graph[i, j] + "\"] ";

            script += "}";

            return RenderingOnGraphviz(script);
        }

        /// <summary>
        /// Отрисовка ориентированного графа с выделенным маршрутом
        /// </summary>
        /// <param name="graph">орг. граф</param>
        /// <param name="path">маршрут</param>
        /// <returns>битовое изображение</returns>
        public static Bitmap Drawing(Digraph graph, Digraph.Path path)
        {
            if (graph == null || graph.CountVertex() == 0)
                throw new Exception("Невозможно отрисовать граф. Граф пуст.");

            if (path == null || !path.IsExists())
                return Drawing(graph);

            string script = "digraph G { nrankdir=LR  node [style=\"filled\", fillcolor=\"skyblue\"]";

            for (int i = 0; i < graph.CountVertex(); i++)
                script += (i + 1) + " ";

            for (int i = 0; i < graph.CountVertex(); i++)
                for (int j = 0; j < graph.CountVertex(); j++)
                    if (!float.IsInfinity(graph[i, j]))
                    {
                        script += (i + 1) + " -> " + (j + 1) + " [label=\"" + graph[i, j] + "\"";

                        if (path.IsContain(new Digraph.Edge(i, j, graph[i, j])))
                            script += ", fontcolor=\"firebrick\", color=\"firebrick2\", penwidth=3, weight=1";

                        script += "] ";
                    }

            script += "}";

            return RenderingOnGraphviz(script);
        }

        /// <summary>
        /// Отрисовка дерева ветвлений
        /// </summary>
        /// <param name="tree">дерево ветвления</param>
        /// <returns>битовое изображение</returns>
        public static Bitmap Drawing(BranchAndBound.TreeBranch tree)
        {
            if (tree == null || tree.Root == null)
                throw new Exception("Невозможно отрисовать дерево ветвлений. Дерево пусто.");

            uint no = 0;

            var current = tree.Root;

            string script = "graph G { nrankdir = LR node [style=\"filled\", fillcolor=\"gray\"] node" + no + "[label = " + "\"" + (float.IsPositiveInfinity(current.LowerBound) ? "Inf" : current.LowerBound.ToString()) + "\"] ", scriptMain = "";

            var dictionaryBranch = new Dictionary<BranchAndBound.Branch, uint>();
            dictionaryBranch.Add(current, no++);

            var queue = new Queue<BranchAndBound.Branch>();

            for(;;)
            {
                if (current.Left != null)
                {
                    dictionaryBranch.Add(current.Left, no++);
                    queue.Enqueue(current.Left);

                    scriptMain = "node" + dictionaryBranch[current.Left] + "[label = " + "\"" + (float.IsPositiveInfinity(current.Left.LowerBound) ? "Inf" : current.Left.LowerBound.ToString()) + "\n" + (current.Left.BranchingEdge.Begin < 0 || current.Left.BranchingEdge.End < 0 ? "/(" : "(")
                       + (Math.Abs(current.Left.BranchingEdge.Begin) + 1) + ", " + (Math.Abs(current.Left.BranchingEdge.End) + 1) + ")\"] " + scriptMain;
                }
                if (current.Right != null)
                {
                    dictionaryBranch.Add(current.Right, no++);
                    queue.Enqueue(current.Right);

                    scriptMain = "node" + dictionaryBranch[current.Right] + "[label = " + "\"" + (float.IsPositiveInfinity(current.Right.LowerBound) ? "Inf" : current.Right.LowerBound.ToString()) + "\n" + (current.Right.BranchingEdge.Begin < 0 || current.Right.BranchingEdge.End < 0 ? "/(" : "(")
                        + (Math.Abs(current.Right.BranchingEdge.Begin) + 1) + ", " + (Math.Abs(current.Right.BranchingEdge.End) + 1) + ")\"] " + scriptMain;
                }

                if (queue.Count == 0)
                    break;

                current = queue.Dequeue();

                scriptMain += "node" + dictionaryBranch[current.Parent] + " -- " + "node" + dictionaryBranch[current] + " ";
            }

            if (!string.IsNullOrEmpty(scriptMain))
                script += scriptMain;

            script += "}";

            return RenderingOnGraphviz(script);
        }
    }
}
