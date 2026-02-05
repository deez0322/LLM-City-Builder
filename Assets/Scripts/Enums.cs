using System;
using System.Collections.Generic;
using System.Linq;

namespace AITransformer
{
    public class Enums
    {
        public static readonly Dictionary<BuildingType, Dictionary<string, int>> BuildingCosts = new Dictionary<BuildingType, Dictionary<string, int>>
        {
            {BuildingType.FishingHut, new Dictionary<string, int> {{"Wood", 30}, {"Stone", 20}}},
            {BuildingType.House, new Dictionary<string, int> {{"Wood", 50}, {"Stone", 30}}},
            {BuildingType.Store, new Dictionary<string, int> {{"Wood", 100}, {"Stone", 80}, {"Iron", 20}}},
            {BuildingType.IronMine, new Dictionary<string, int> {{"Wood", 80}, {"Stone", 100}}},
            {BuildingType.LumberjackHut, new Dictionary<string, int> {{"Wood", 40}, {"Stone", 60}}},
            {BuildingType.Farm, new Dictionary<string, int> {{"Wood", 40}, {"Stone", 20}}}
        };

        public static string GetBuildingCost(BuildingType buildingType)
        {
            if (BuildingCosts.TryGetValue(buildingType, out var costs))
            {
                return string.Join(", ", costs.Select(kv => $"{kv.Key}: {kv.Value}"));
            }
            return "Cost not defined";
        }
        
        public static string GetAllBuildingCostsAsString()
        {
            return string.Join("\n", Enum.GetValues(typeof(BuildingType))
                .Cast<BuildingType>()
                .Where(bt => bt != BuildingType.NoBuilding)
                .Select(bt => $"{bt}: {GetBuildingCost(bt)}"));
        }
        
        public static string GetAllPricesAsString()
        {
            return $"Wood: 1 money\n" +
                   $"Salt: 2 money\n" +
                   $"Stone: 3 money\n" +
                   $"Iron: 4 money\n" +
                   $"Food: 2 money";
        }


        public static string AllBuildingTypesWithCosts()
        {
            return string.Join("\n", Enum.GetValues(typeof(BuildingType))
                .Cast<BuildingType>()
                .Where(bt => bt != BuildingType.NoBuilding)
                .Select(bt => $"{bt}: {GetBuildingCost(bt)}"));
        }
        
        public enum TaskType
        {
            Add,
            Delete,
            Move,
            Buy,
            Sell
        }
        
        public enum MapPart
        {
            Grass,
            River,
            Iron,
            Salt,
            Stone,
            Wood,
        }

        public enum BuildingType
        {
            FishingHut,
            House,
            Store,
            IronMine,
            LumberjackHut,
            Farm,
            NoBuilding
        }
        
         
        public static string AllBuildingTypes()
        {
            var allTypes = Enum.GetValues(typeof(BuildingType))
                .Cast<BuildingType>()
                .Where(b => b != BuildingType.NoBuilding)
                .Select(b => b.ToString());

            return string.Join(", ", allTypes);
        }
        
        static
        public String AllTaskTypes()
        {
            var allTypes = Enum.GetNames(typeof(TaskType));
            return string.Join(", ", allTypes);
        }
        
        static public String GetBuildingTypeAbbreviation(BuildingType buildingType)
        {
            return buildingType.ToString().Substring(0, 2);
        }

        public string AllMapPartsAndExplainations()
        {
            var allTypes = Enum.GetNames(typeof(MapPart));
            var explanations = new List<string>();

            foreach (var type in allTypes)
            {
                var abbreviation = type.Substring(0, 1);
                explanations.Add($"{abbreviation} = {type}");
            }


            return string.Join(", ", explanations);
        }
    }
}