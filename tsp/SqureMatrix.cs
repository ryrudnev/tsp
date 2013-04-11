using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace tsp
{
    public class SqureMatrix : ICloneable 
    {
        private double[,] items;

        public int Size { private set; get; }
        
        public SqureMatrix(int n)
        {
            Size = n;
            items = new double[Size, Size];
        }

        public SqureMatrix(SqureMatrix other)
        {
            var clone = other.Clone() as SqureMatrix;

            Size = clone.Size;
            items = clone.items;
        }

        public double this[int row, int col] 
        {
            get { return items[row, col]; }
            set { items[row, col] = value; }
        }

        public object Clone()
        {
            var clone = new SqureMatrix(Size);

            for (int i = 0; i < Size; i++)
                for (int j = 0; j < Size; j++)
                    clone[i, j] = items[i, j];

            return clone as object;
        }
    }
}
