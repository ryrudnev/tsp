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

            string script = "digraph G { nrankdir = LR ";

            for (int i = 0; i < graph.CountVertex(); i++)
                script += (i + 1) + " ";

            for (int i = 0; i < graph.CountVertex(); i++)
                for (int j = 0; j < graph.CountVertex(); j++)
                    if (!Double.IsInfinity(graph[i, j]))
                        script += (i + 1) + " -> " + (j + 1) + " [label=" + graph[i, j] + "] ";

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

            string script = "digraph G { nrankdir = LR  node [style=\"filled\", fillcolor=\"red\"]";

            for (int i = 0; i < graph.CountVertex(); i++)
                script += (i + 1) + " ";

            for (int i = 0; i < graph.CountVertex(); i++)
                for (int j = 0; j < graph.CountVertex(); j++)
                    if (!Double.IsInfinity(graph[i, j]))
                    {
                        script += (i + 1) + " -> " + (j + 1) + " [label=" + graph[i, j];

                        if (path.IsContain(new Digraph.Edge(i, j, graph[i, j]))) script += ", color=\"red\"";

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
        public static Bitmap Drawing(BranchBound.TreeBranching tree)
        {
            if (tree == null || tree.Root == null)
                throw new Exception("Невозможно отрисовать дерево ветвлений. Дерево пусто.");

            string script = "graph G { nrankdir = LR ";

            var queue = new Queue<BranchBound.Branch>();
            queue.Enqueue(tree.Root);

            while (queue.Count > 0)
            {
                var branch = queue.Dequeue();

                string strParent = "\"" + branch.Bound;

                if (branch.Edge != null)
                    strParent += "\n" + (branch.Edge.Begin < 0 || branch.Edge.End < 0 ? "/(" : "(")
                        + (Math.Abs(branch.Edge.Begin) + 1) + ", " + (Math.Abs(branch.Edge.End) + 1) + ")\"";
                else
                    strParent += "\"";

                script += strParent + " ";

                if (branch.Left != null)
                {
                    string strLChild = "\"" + branch.Left.Bound + "\n" + (branch.Left.Edge.Begin < 0 || branch.Left.Edge.End < 0 ? "/(" : "(")
                        + (Math.Abs(branch.Left.Edge.Begin) + 1) + ", " + (Math.Abs(branch.Left.Edge.End) + 1) + ")\"";
                    script += strParent + " -- " + strLChild + " ";

                    queue.Enqueue(branch.Left);
                }
                if (branch.Right != null)
                {
                    string strRChild = "\"" + branch.Right.Bound + "\n" + (branch.Right.Edge.Begin < 0 || branch.Right.Edge.End < 0 ? "/(" : "(")
                    + (Math.Abs(branch.Right.Edge.Begin) + 1) + ", " + (Math.Abs(branch.Right.Edge.End) + 1) + ")\"";
                    script += strParent + " -- " + strRChild + " ";

                    queue.Enqueue(branch.Right);
                }
            }

            script += "}";

            return RenderingOnGraphviz(script);
        }
    }
}
