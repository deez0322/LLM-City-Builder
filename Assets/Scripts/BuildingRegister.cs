using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Mono.Data.Sqlite;
using UnityEngine.Tilemaps;

namespace AITransformer
{
    public class BuildingRegister : MonoBehaviour
    {
        private Dictionary<Vector3, string> _tiles = new Dictionary<Vector3, string>();
        private SqliteConnection _connection;

        private Tilemap tilemap;

        // Reference to your tile assets
        private TileBase grassTile;
        private TileBase riverTile;
        private TileBase ironTile;
        private TileBase treeTile;
        private TileBase stoneTile;
        private TileBase defaultTile;
        private TileBase houseTile;
        private TileBase fishingHutTile;
        private TileBase lumberjackTile;

        public bool _TestFailed = false;

        void Awake()
        {
            tilemap = GetComponent<Tilemap>();
            if (tilemap == null)
            {
                tilemap = FindObjectOfType<Tilemap>();
            }

            InitializeDatabase();
            LoadTiles();
            NormalizeTileSize();

            // Only initialize the tilemap if the database is empty
            if (IsDatabaseEmpty())
            {
                InitializeTilemap();
            }
            else
            {
                //SyncDictionaryWithDatabase();
            }

            PrintDatabaseContents();
        }

        private bool IsDatabaseEmpty()
        {
            using (var command = _connection.CreateCommand())
            {
                command.CommandText = "SELECT COUNT(*) FROM tiles";
                long count = (long)command.ExecuteScalar();
                return count == 0;
            }
        }

        private void LoadTiles()
        {
            grassTile = LoadTile("Grass");
            riverTile = LoadTile("River");
            ironTile = LoadTile("Iron");
            treeTile = LoadTile("Wood");
            stoneTile = LoadTile("Iron");
            defaultTile = LoadTile("Grass");
            houseTile = LoadTile("House");
            fishingHutTile = LoadTile("FishingHut");
            lumberjackTile = LoadTile("lum");
        }

        void NormalizeTileSize()
        {
            if (tilemap != null)
            {
                // If using a Grid component
                Grid grid = tilemap.GetComponentInParent<Grid>();
                if (grid != null)
                {
                    grid.cellSize = Vector3.one;
                }
            }
        }

        private TileBase LoadTile(string tileName)
        {
            string path = $"Tiles/{tileName}";
            TileBase tile = Resources.Load<TileBase>(path);
            //UnityEngine.Object[] allTiles = Resources.LoadAll("Tiles");
            //Debug.Log($"Found {allTiles.Length} tiles in the Tiles folder:");
            if (tile == null)
            {
                Debug.LogError($"Failed to load tile: {tileName}. Attempted path: {path}");
                // List all assets in the Tiles folder
                //UnityEngine.Object[] allTiles = Resources.LoadAll("Tiles");
                //Debug.Log($"Found {allTiles.Length} tiles in the Tiles folder:");
            }

            return tile;
        }

        private void InitializeDatabase()
        {
            _connection = new SqliteConnection("Data Source=:memory:");
            _connection.Open();

            using (var command = _connection.CreateCommand())
            {
                command.CommandText = new StringBuilder().Append(@"
                    CREATE TABLE IF NOT EXISTS tiles (
                        x REAL,
                        y REAL,
                        z REAL,
                        type TEXT,
                        PRIMARY KEY (x, y, z)
                    )").ToString();
                command.ExecuteNonQuery();
            }
        }

        private void SyncWithUnityMap(Vector3 position, string tileType)
        {
            // Convert world position to cell position
            Vector3Int cellPosition = new Vector3Int(
                Mathf.RoundToInt(position.x),
                Mathf.RoundToInt(position.y),
                Mathf.RoundToInt(position.z)
            );
            //tilemap.WorldToCell(position);

            string abbreviation;
            if (Enum.TryParse(typeof(Enums.BuildingType), tileType, out var parsedBuildingType))
            {
                abbreviation = Enums.GetBuildingTypeAbbreviation((Enums.BuildingType)parsedBuildingType);
            }
            else
            {
                abbreviation = tileType.Substring(0, 1);
            }

            // Set the appropriate tile based on the type
            TileBase tileToSet = null;
            switch (abbreviation)
            {
                case "G":
                    tileToSet = grassTile;
                    break;
                case "R":
                    tileToSet = riverTile;
                    break;
                case "I":
                    tileToSet = ironTile;
                    break;
                case "W":
                    tileToSet = treeTile;
                    break;
                case "S":
                    tileToSet = stoneTile;
                    break;
                case "Fi":
                    tileToSet = fishingHutTile;
                    break;
                case "Ho":
                    tileToSet = houseTile;
                    break;
                case "l":
                    tileToSet = lumberjackTile;
                    break;
                default:
                    if (Enum.TryParse(tileType, out Enums.BuildingType buildingType))
                    {
                        // Handle building types here
                        // You might want to have separate tiles for different building types
                        tileToSet = defaultTile;
                    }
                    else
                    {
                        tileToSet = defaultTile;
                    }

                    break;
            }

            // Set the tile in the Tilemap
            tilemap.SetTile(cellPosition, tileToSet);
        }

        private TileBase GetTileBaseFromType(string tileType)
        {
            // Map the tileType string to the corresponding TileBase
            switch (tileType)
            {
                case "Grass": return grassTile;
                case "River": return riverTile;
                case "Iron": return ironTile;
                case "Tree": return treeTile;
                case "Stone": return stoneTile;
                // Add more cases as needed
                default: return defaultTile;
            }
        }

        public void InitializeTilemap()
        {
            tilemap.ClearAllTiles();
            int width = 10;
            int height = 5;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Vector3Int position = new Vector3Int(x, y, 0);
                    TileBase tileToSet;

                    // Set up a basic layout
                    if ((x < 2 && y >= height - 2) || ((x == 2 && y >= height - 2) || (x < 2 && y == height - 3)))
                        tileToSet = treeTile;
                    else if (x == 5)
                        tileToSet = riverTile;
                    else
                        tileToSet = grassTile;
                    
                    //TODO: remove later, for AR Testing Purposes
                    // add house
                    if (x == 3 && y == 3)
                        tileToSet = houseTile;
                    if (x==4 && y==3)
                        tileToSet = lumberjackTile;
                    

                    //tilemap.SetTile(position, tileToSet);
                    RegisterTile(new Vector3(x, y, 0), tileToSet.name);
                }
            }
        }

        private void SyncDictionaryWithDatabase()
        {
            _tiles.Clear();

            using (var command = _connection.CreateCommand())
            {
                command.CommandText = "SELECT x, y, z, type FROM tiles";
                using (var reader = command.ExecuteReader())
                {
                    if (!reader.HasRows)
                    {
                        Debug.Log("No tiles found in the database.");
                        return;
                    }

                    while (reader.Read())
                    {
                        Vector3 position = new Vector3(reader.GetFloat(0), reader.GetFloat(1), reader.GetFloat(2));
                        string tileType = reader.GetString(3);

                        _tiles[position] = tileType;
                        TileBase tileToSet = GetTileBaseFromType(tileType);

                        // Properly convert Vector3 to Vector3Int
                        Vector3Int positionInt = new Vector3Int(Mathf.RoundToInt(position.x),
                            Mathf.RoundToInt(position.y), Mathf.RoundToInt(position.z));

                        tilemap.SetTile(positionInt, tileToSet);
                    }
                }
            }
        }

        public void RegisterBuilding(Vector3 location, Enums.BuildingType building)
        {
            RegisterTile(location, building.ToString());
        }

        public void RegisterTile(Vector3 location, string tileType)
        {
            _tiles[location] = tileType;
            using (var command = _connection.CreateCommand())
            {
                command.CommandText = @"
                    INSERT OR REPLACE INTO tiles (x, y, z, type) 
                    VALUES ($x, $y, $z, $type)";
                command.Parameters.AddWithValue("$x", location.x);
                command.Parameters.AddWithValue("$y", location.y);
                command.Parameters.AddWithValue("$z", location.z);
                command.Parameters.AddWithValue("$type", tileType);
                command.ExecuteNonQuery();
                PrintDatabaseContents();
            }

            SyncWithUnityMap(location, tileType);
        }

        public Enums.BuildingType GetBuildingAtLocation(Vector3 location)
        {
            if (_tiles.TryGetValue(location, out string tileType))
            {
                if (Enum.TryParse(tileType, out Enums.BuildingType buildingType))
                {
                    return buildingType;
                }
            }

            return Enums.BuildingType.NoBuilding;
        }

        public string GetTileAtLocation(Vector3 location)
        {
            return _tiles.TryGetValue(location, out string tileType) ? tileType : null;
        }

        public void UpdateBuildingAtLocation(Vector3 location, Vector3 newLocation, Enums.BuildingType newBuilding)
        {
            RemoveTileAtLocation(location);
            RegisterBuilding(newLocation, newBuilding);
        }

        public void RemoveBuildingAtLocation(Vector3 location)
        {
            RemoveTileAtLocation(location);
        }

        public void RemoveTileAtLocation(Vector3 location)
        {
            _tiles[location] = "Grass";
            using (var command = _connection.CreateCommand())
            {
                    command.CommandText = @"
                    INSERT OR REPLACE INTO tiles (x, y, z, type) 
                    VALUES ($x, $y, $z, $type)";
                    command.Parameters.AddWithValue("$x", location.x);
                    command.Parameters.AddWithValue("$y", location.y);
                    command.Parameters.AddWithValue("$z", location.z);
                    command.Parameters.AddWithValue("$type", "Grass");
                    command.ExecuteNonQuery();
                    PrintDatabaseContents();
            }
            SyncWithUnityMap(location, "Grass");
            tilemap.SetTile(tilemap.WorldToCell(location), null);
        }

        public List<Tuple<Vector3, Enums.BuildingType>> getAllGameObjects()
        {
            return _tiles
                .Where(kvp => Enum.TryParse(kvp.Value, out Enums.BuildingType _))
                .Select(kvp => new Tuple<Vector3, Enums.BuildingType>(kvp.Key,
                    (Enums.BuildingType)Enum.Parse(typeof(Enums.BuildingType), kvp.Value)))
                .ToList();
        }

        public string GetBuildingsAsJson()
        {
            var buildingList = getAllGameObjects().Select(building => new
            {
                type = building.Item2.ToString(),
                location = new { X = (int)building.Item1.x, Y = (int)building.Item1.y }
            }).ToList();

            return JsonConvert.SerializeObject(buildingList, Formatting.Indented);
        }

        public string ExecuteQuery(string query)
        {
            // Extract *all* SQL statements
            List<string> sqlStatements = ExtractSqlStatements(query);
            if (sqlStatements == null || sqlStatements.Count == 0)
            {
                return "Error: No valid SQL statement found.";
            }

            var overallResult = new System.Text.StringBuilder();

            // Execute each statement in turn
            foreach (var sqlStatement in sqlStatements)
            {
                try
                {
                    using (var command = _connection.CreateCommand())
                    {
                        command.CommandText = sqlStatement;

                        // If it is a SELECT query, we'll read rows
                        // Otherwise, you might want to do command.ExecuteNonQuery().
                        // You could detect SELECT vs. non-SELECT with command.CommandText.ToLower().StartsWith("select")
                        if (sqlStatement.Trim().ToLower().StartsWith("select"))
                        {
                            using (var reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    for (int i = 0; i < reader.FieldCount; i++)
                                    {
                                        overallResult.Append(reader.GetName(i))
                                            .Append(": ")
                                            .Append(reader.GetValue(i))
                                            .Append(", ");
                                    }

                                    overallResult.AppendLine();
                                }
                            }
                        }
                        else
                        {
                            // For UPDATE, INSERT, DELETE, etc.
                            int affectedRows = command.ExecuteNonQuery();
                            overallResult.AppendLine($"Executed statement: {sqlStatement}");
                            overallResult.AppendLine($"Affected rows: {affectedRows}");
                        }
                    }
                }
                catch (SqliteException ex)
                {
                    Debug.LogWarning($"SQLite query error: {ex.Message}");
                    // You can decide whether to continue executing the rest or break early
                    overallResult.AppendLine($"Error: {ex.Message}");
                }
            }

            return overallResult.ToString();
        }

        public List<string> ExtractSqlStatements(string content)
        {
            // 1. Gather all statements from ```sql ... ``` blocks
            var statements = new List<string>();

            // Regex to find all code blocks with the 'sql' language
            var codeBlockMatches = Regex.Matches(content, @"```sql\s*(.*?)\s*```", RegexOptions.Singleline);
            foreach (Match match in codeBlockMatches)
            {
                var sqlBlock = match.Groups[1].Value;
                // Split by semicolon to get individual statements
                var sqlPieces = sqlBlock.Split(';')
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrEmpty(s));
                statements.AddRange(sqlPieces);
            }

            // 2. If no ```sql``` code blocks found, try to treat the entire content as inline SQL
            //    and similarly split by semicolon.
            if (!statements.Any())
            {
                var inlinePieces = content.Split(';')
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrEmpty(s));

                foreach (var piece in inlinePieces)
                {
                    // Very rough check if the piece appears to be valid SQL
                    var lowered = piece.ToLower();
                    if (lowered.StartsWith("select") ||
                        lowered.StartsWith("insert") ||
                        lowered.StartsWith("update") ||
                        lowered.StartsWith("delete"))
                    {
                        statements.Add(piece);
                    }
                }
            }

            return statements;
        }

        public string GetTilesAsMinimap()
        {
            Dictionary<(int x, int y), string> tileMap = new Dictionary<(int x, int y), string>();
            int minX = int.MaxValue, maxX = int.MinValue;
            int minY = int.MaxValue, maxY = int.MinValue;

            using (var command = _connection.CreateCommand())
            {
                command.CommandText = "SELECT x, y, type FROM tiles";
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int x = (int)reader.GetFloat(0);
                        int y = (int)reader.GetFloat(1);
                        string type = reader.GetString(2);

                        tileMap[(x, y)] = type[0].ToString().ToUpper();

                        minX = Math.Min(minX, x);
                        maxX = Math.Max(maxX, x);
                        minY = Math.Min(minY, y);
                        maxY = Math.Max(maxY, y);
                    }
                }
            }

            StringBuilder minimap = new StringBuilder();

            for (int y = maxY; y >= minY; y--)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    if (tileMap.TryGetValue((x, y), out string tileType))
                    {
                        minimap.Append(tileType);
                    }
                    else
                    {
                        minimap.Append(' ');
                    }
                }

                minimap.AppendLine();
            }

            return minimap.ToString();
        }

        public string GetTilesAsJsonArray()
        {
            StringBuilder jsonBuilder = new StringBuilder();
            jsonBuilder.Append("[");
            bool isFirstTile = true;

            using (var command = _connection.CreateCommand())
            {
                command.CommandText = "SELECT x, y, type FROM tiles ORDER BY y DESC, x ASC";
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (!isFirstTile)
                        {
                            jsonBuilder.Append(",");
                        }
                        else
                        {
                            isFirstTile = false;
                        }

                        float x = reader.GetFloat(0);
                        float y = reader.GetFloat(1);
                        string type = reader.GetString(2);

                        jsonBuilder.Append("{");
                        jsonBuilder.Append($"\"x\":{x.ToString("F2")},");
                        jsonBuilder.Append($"\"y\":{y.ToString("F2")},");
                        jsonBuilder.Append($"\"type\":\"{EscapeJsonString(type)}\"");
                        jsonBuilder.Append("}");
                    }
                }
            }

            jsonBuilder.Append("]");
            return jsonBuilder.ToString();
        }

        private string EscapeJsonString(string str)
        {
            return str.Replace("\"", "\\\"")
                .Replace("\\", "\\\\")
                .Replace("/", "\\/")
                .Replace("\b", "\\b")
                .Replace("\f", "\\f")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t");
        }


        public void SyncWithTilemap(string tilemapJson)
        {
            var tiles = JsonConvert.DeserializeObject<List<TileData>>(tilemapJson);

            using (var transaction = _connection.BeginTransaction())
            {
                using (var command = _connection.CreateCommand())
                {
                    command.CommandText = @"
                        INSERT OR REPLACE INTO tiles (x, y, z, type) 
                        VALUES ($x, $y, $z, $type)";
                    foreach (var tile in tiles)
                    {
                        command.Parameters.AddWithValue("$x", tile.x);
                        command.Parameters.AddWithValue("$y", tile.y);
                        command.Parameters.AddWithValue("$z", tile.z);
                        command.Parameters.AddWithValue("$type", tile.type);
                        command.ExecuteNonQuery();
                        command.Parameters.Clear();
                    }
                }

                transaction.Commit();
            }

            SyncDictionaryWithDatabase();
        }

        public void PrintDatabaseContents()
        {
            //Debug.Log("Printing Building Register Database Contents:");
            using (var command = _connection.CreateCommand())
            {
                command.CommandText = "SELECT x, y, z, type FROM tiles ORDER BY y DESC, x ASC";
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        float x = reader.GetFloat(0);
                        float y = reader.GetFloat(1);
                        float z = reader.GetFloat(2);
                        string type = reader.GetString(3);
                        //Debug.Log($"Tile at ({x}, {y}, {z}): {type}");
                    }
                }
            }
        }

        void OnDestroy()
        {
            _connection?.Close();
            _connection?.Dispose();
        }

        // get all tiles by type
        public List<Vector3> GetTilesByType(string type)
        {
            List<Vector3> tiles = new List<Vector3>();
            using (var command = _connection.CreateCommand())
            {
                command.CommandText = "SELECT x, y, z FROM tiles WHERE type = $type";
                command.Parameters.AddWithValue("$type", type);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        float x = reader.GetFloat(0);
                        float y = reader.GetFloat(1);
                        float z = reader.GetFloat(2);
                        tiles.Add(new Vector3(x, y, z));
                    }
                }
            }

            return tiles;
        }

        public class TileData
        {
            public float x { get; set; }
            public float y { get; set; }
            public float z { get; set; }

            public string
                type { get; set; }
        }
    }
}
