using System.Collections.Generic;
using System.Drawing;
using WindowsFormsApp1.Models;

namespace WindowsFormsApp1
{
    public class MatrixService
    {
        public static string VON_NEUMAN = "Von Neumann";
        public PixelModel[,] matrix;
        public Bitmap bitmap;
        public IList<GrainModel> SeedList;

        public int xDimension;
        public int yDimension;

        public MatrixService()
        {
            SeedList = new List<GrainModel>();

        }

        public void CreateMatrix(int xDimension, int yDimension)
        {
            matrix = new PixelModel[xDimension, yDimension];
            for (int x = 0; x < xDimension; x++)
            {
                for (int y = 0; y < yDimension; y++)
                {
                    matrix[x, y] = new PixelModel();
                }
            }
        }
    }
}
