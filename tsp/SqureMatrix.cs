using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace tsp
{
    /// <summary>
    /// Квадратная матрица
    /// </summary>
    public class SqureMatrix
    {
        // элементы матрицы
        private double[,] items;

        /// <summary>
        /// Получение размера матрицы
        /// </summary>
        /// <returns> размер матрицы </returns>
        public int Size()
        {
            return items.GetLength(0);
        }

        /// <summary>
        /// Создание квадратной матрицы с указанным размером
        /// </summary>
        /// <param name="n"> размер матрицы </param>
        public SqureMatrix(int n)
        {
            items = new double[n, n];
        }

        /// <summary>
        /// Создание матрицы на основе другой квадратной матрицы
        /// </summary>
        /// <param name="other"></param>
        public SqureMatrix(SqureMatrix other)
            : this(other.Size())
        {
            for (int i = 0; i < Size(); i++)
                for (int j = 0; j < Size(); j++)
                    items[i, j] = other[i, j];
        }

        /// <summary>
        /// Индексирование 
        /// </summary>
        /// <param name="row"> номер строки </param>
        /// <param name="col"> номер столбца </param>
        /// <returns> значение элемента матрицы </returns>
        public double this[int row, int col] 
        {
            get { return items[row, col]; }
            set { items[row, col] = value; }
        }

        /// <summary>
        /// Копирование матрицы
        /// </summary>
        /// <param name="other"> матрицы над которой будет совершено копирование </param>
        /// <returns> копия матрицы </returns>
        public static SqureMatrix Copy(SqureMatrix other)
        {
            return new SqureMatrix(other);
        }
    }
}
