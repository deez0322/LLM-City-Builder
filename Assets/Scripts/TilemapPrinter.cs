using System.Text;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace AITransformer
{
    public class TilemapPrinter : MonoBehaviour
    {
        private Tilemap _tilemap;
        private BuildingRegister _buildingRegister;
        private bool _hasInitialized = false;
        
        void Start()
        {
            _tilemap = this.GetComponent<Tilemap>();
    
            if (_tilemap == null)
            {
                Debug.LogError("Tilemap component not found on the GameObject.");
                return;
            }
            
            GameObject buildingRegisterObject = GameObject.Find("BuildingRegister");
            if (buildingRegisterObject != null)
            {
                _buildingRegister = buildingRegisterObject.GetComponent<BuildingRegister>();
            }
            else
            {
                Debug.LogError("BuildingRegister GameObject not found in the scene.");
            }
            
            //InitializeTilemap();
        }

        private void InitializeTilemap()
        {
            if (_hasInitialized) return;

            BoundsInt bounds = _tilemap.cellBounds;
            TileBase[] allTiles = _tilemap.GetTilesBlock(bounds);

            if (allTiles == null)
            {
                Debug.LogError("No tiles found on the Tilemap.");
                return;
            }

            for (int y = bounds.size.y - 1; y >= 0; y--)
            {
                for (int x = 0; x < bounds.size.x; x++)
                {
                    TileBase tile = allTiles[x + y * bounds.size.x];
                    string tileType = GetTileType(tile, x, y);

                    // Register each tile with the database
                    _buildingRegister.RegisterTile(new Vector3(x, y, 0), tileType);
                }
            }

            _hasInitialized = true;
        }

        public string GetTilemap()
        {
            BoundsInt bounds = _tilemap.cellBounds;
            TileBase[] allTiles = _tilemap.GetTilesBlock(bounds);
            var jsonBuilder = new StringBuilder("[");

            if (allTiles == null)
            {
                Debug.LogError("No tiles found on the Tilemap.");
                return "[]";
            }
    
            var debugOutput = new StringBuilder();
            bool isFirst = true;
            for (int y = bounds.size.y - 1; y >= 0; y--)
            {
                var debugLine = new StringBuilder();
                for (int x = 0; x < bounds.size.x; x++)
                {
                    TileBase tile = allTiles[x + y * bounds.size.x];
                    string tileType = GetTileType(tile, x, y);

                    if (!isFirst)
                    {
                        jsonBuilder.Append(",");
                    }
                    jsonBuilder.Append($"{{\"type\":\"{tileType}\",\"x\":{x},\"y\":{y},\"z\":0}}");
                    isFirst = false;
                    debugLine.Append(tileType).Append(" ");
                }
                debugOutput.AppendLine(debugLine.ToString().Trim());
            }
    
            jsonBuilder.Append("]");
            Debug.Log(debugOutput.ToString());

            return jsonBuilder.ToString();
        }

        string GetTileType(TileBase tile, int x, int y)
        {
            // Check if there's a building or special tile type first
            string specialTile = _buildingRegister.GetTileAtLocation(new Vector3(x, y, 0));
            if (!string.IsNullOrEmpty(specialTile))
            {
                return specialTile;
            }

            // Default tile types
            if (tile == null)
                return "Empty";
            else
                return "Unknown"; // Unknown or other type
        }

        void SyncTilemapWithDatabase()
        {
            string tilemapJson = GetTilemap();
            _buildingRegister.SyncWithTilemap(tilemapJson);
        }
    }
}
