using Unity.Mathematics;

namespace EDIVE.GeoToolkit.Area
{
    public static class GeoAreaRectExtensions
    {
        public static GeoAreaRect[,] Split(this GeoAreaRect box, int2 gridSize)
        {
            var diffX = (box.Max.x - box.Min.x ) / gridSize.x;
            var diffY = (box.Max.y - box.Min.y) / gridSize.y;
            return Split(box, gridSize, diffX, diffY);
        }

        public static GeoAreaRect[,] Split(this GeoAreaRect box, float2 totalAreaSize, float2 maxTileSize)
        {
            var gridSize = new int2((int) math.ceil(totalAreaSize.x / maxTileSize.x), (int) math.ceil(totalAreaSize.y / maxTileSize.y));
            var diffX = (box.Max.x  - box.Min.x ) / totalAreaSize.x * maxTileSize.x;
            var diffY = (box.Max.y - box.Min.y) / totalAreaSize.y * maxTileSize.y;
            return Split(box, gridSize, diffX, diffY);
        }

        private static GeoAreaRect[,] Split(GeoAreaRect box, int2 gridSize, double xTileSize, double yTileSize)
        {
            var result = new GeoAreaRect[gridSize.x, gridSize.y];
            
            for (var x = 0; x < gridSize.x; x++)
            {
                var newMinX = box.Min.x  + x * xTileSize;
                var newMaxX = x == gridSize.x - 1 ? box.Max.x  : box.Min.x  + (x + 1) * xTileSize;

                for (var y = 0; y < gridSize.y; y++)
                {
                    var newMinY = box.Min.y + y * yTileSize;
                    var newMaxY = y == gridSize.y - 1 ? box.Max.y : box.Min.y + (y + 1) * yTileSize;

                    result[x, y] = new GeoAreaRect(new double2(newMinX, newMinY), new double2(newMaxX, newMaxY), box.CoordinateSystem);
                }  
            }

            return result;
        }
    }
}