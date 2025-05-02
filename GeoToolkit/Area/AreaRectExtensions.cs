using ProtoGIS.Scripts.Utils;
using UnityEngine;

namespace EDIVE.GeoToolkit.Area
{
    public static class AreaRectExtensions
    {
        public static AreaRect[,] Split(this IAreaRect box, Vector2Int gridSize)
        {
            var diffX = (box.MaxX - box.MinX) / gridSize.x;   
            var diffY = (box.MaxY - box.MinY) / gridSize.y;
            return Split(box, gridSize, diffX, diffY);
        }
        
        public static AreaRect[,] Split(this IAreaRect box, Vector2 totalAreaSize, Vector2 maxTileSize)
        {
            var gridSize = new Vector2Int(Mathf.CeilToInt(totalAreaSize.x / maxTileSize.x), Mathf.CeilToInt(totalAreaSize.y / maxTileSize.y));
            var diffX = (box.MaxX - box.MinX) / totalAreaSize.x * maxTileSize.x;   
            var diffY = (box.MaxY - box.MinY) / totalAreaSize.y * maxTileSize.y;   
            return Split(box, gridSize, diffX, diffY);
        }
        
        private static AreaRect[,] Split(IAreaRect box, Vector2Int gridSize, double xTileSize, double yTileSize)
        {
            var result = new AreaRect[gridSize.x, gridSize.y];
            
            for (var x = 0; x < gridSize.x; x++)
            {
                var newMinX = box.MinX + x * xTileSize;
                var newMaxX = x == gridSize.x - 1 ? box.MaxX : box.MinX + (x + 1) * xTileSize;

                for (var y = 0; y < gridSize.y; y++)
                {
                    var newMinY = box.MinY + y * yTileSize;
                    var newMaxY = y == gridSize.y - 1 ? box.MaxY : box.MinY + (y + 1) * yTileSize;

                    result[x, y] = new AreaRect(new DVector2(newMinX, newMinY), new DVector2(newMaxX, newMaxY), box.CoordRefSystem);
                }  
            }

            return result;
        }
    }
}