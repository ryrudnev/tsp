using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace tsp
{
    /// <summary>
    /// Алгоритм ветвей и границ
    /// </summary>
    public class BranchAndBound
    {
        /// <summary>
        /// Режим выполнения алгоритма
        /// </summary>
        public enum RunMode
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

        /// <summary>
        /// Редуцированная матрица
        /// </summary>
        private class ReducedMatrix : SqureMatrix
        {
            // реальный размер квадратной матрицы
            private int size;

            /// <summary>
            /// Создание редуцированной матрицы с указанным размером
            /// </summary>
            /// <param name="n"> размер матрицы </param>
            ReducedMatrix(int n)
                : base(n)
            {

            }

            /// <summary>
            /// Создание редуцированной на основе квадратной матрицы
            /// </summary>
            /// <param name="smatrix"></param>
            ReducedMatrix(SqureMatrix smatrix)
                : base(smatrix)
            {
                // To Do
            }

            /// <summary>
            /// Получение размер матрицы 
            /// </summary>
            /// <returns> реальный размер матрицы </returns>
            public new int Size()
            {
                return size;
            }

            /// <summary>
            /// Удаление строки и столбца редуцированной матрицы
            /// </summary>
            /// <param name="row"> номер строки </param>
            /// <param name="col"> номер столбца </param>
            public void Remove(int row, int col)
            {
                if (size <= 2) return;
                size--;
                for (int i = 0; i < size; i++)
                    for (int j = 0; j < size; j++)
                    {
                        if (i >= row && j >= col)
                            this[i, j] = this[i + 1, j + 1];
                        else if (i >= row)
                            this[i, j] = this[i + 1, j];
                        else if (j >= col)
                            this[i, j] = this[i, j + 1];
                    }
            }
        }

        /// <summary>
        /// Ссылка на граф для которого будет выполняться поиск маршрута коммивояжера
        /// </summary>
        public Graph g { set; get; }
    }
}
