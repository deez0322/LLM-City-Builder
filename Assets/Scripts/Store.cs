using System;
using UnityEngine.UI;

namespace AITransformer
{
    public class ResourceStore
    {
        public class Resources
        {
            public int Wood { get; set; }
            public int Salt { get; set; }
            public int Stone { get; set; }
            public int Iron { get; set; }
            public int Money { get; set; }
            public int Food { get; set; }

            public Resources(int wood = 0, int salt = 0, int stone = 0, int iron = 0, int money = 0, int food = 0)
            {
                Wood = wood;
                Salt = salt;
                Stone = stone;
                Iron = iron;
                Money = money;
                Food = food;
            }
            
            public string GetOutputString()
            {
                return $"Wood: {Wood}, Salt: {Salt}, Stone: {Stone}, Iron: {Iron}, Money: {Money}, Food: {Food}";
            }

            public int GetPrice()
            {
                return Wood + Salt * 2 + Stone * 3 + Iron * 4 + Food * 2;
            }

            public bool IsEmpty()
            {
                return Wood == 0 && Salt == 0 && Stone == 0 && Iron == 0 && Food == 0;
            }
        }

        private Resources cityResources;

        public ResourceStore()
        {
            cityResources = new Resources();
        }

        public void Buy(Resources waresToBuy)
        {
            
            if (waresToBuy.IsEmpty())
            {
                Console.WriteLine("Resources empty, did not buy anything");
                return;
            }

            int cost = waresToBuy.GetPrice();
            if (cityResources.Money < cost)
            {
                Console.WriteLine("Not enough gold to buy these resources");
                return;
            }

            cityResources.Wood += waresToBuy.Wood;
            cityResources.Salt += waresToBuy.Salt;
            cityResources.Stone += waresToBuy.Stone;
            cityResources.Iron += waresToBuy.Iron;
            cityResources.Food += waresToBuy.Food;
            cityResources.Money -= cost;

            Console.WriteLine($"Bought resources for {cost} gold");
        }

        public void Sell(Resources waresToSell)
        {
            if (waresToSell.IsEmpty())
            {
                Console.WriteLine("Resources empty, did not sell anything");
                return;
            }

            if (cityResources.Wood < waresToSell.Wood || cityResources.Salt < waresToSell.Salt ||
                cityResources.Stone < waresToSell.Stone || cityResources.Iron < waresToSell.Iron ||
                cityResources.Food < waresToSell.Food)
            {
                Console.WriteLine("Not enough resources to sell");
                return;
            }

            int profit = waresToSell.GetPrice();
            cityResources.Wood -= waresToSell.Wood;
            cityResources.Salt -= waresToSell.Salt;
            cityResources.Stone -= waresToSell.Stone;
            cityResources.Iron -= waresToSell.Iron;
            cityResources.Food -= waresToSell.Food;
            cityResources.Money += profit;

            Console.WriteLine($"Sold resources for {profit} gold");
        }

        public Resources GetCurrentResources()
        {
            return new Resources(
                cityResources.Wood,
                cityResources.Salt,
                cityResources.Stone,
                cityResources.Iron,
                cityResources.Money,
                cityResources.Food
            );
        }

        //set resources
        public void SetResources(Resources resources)
        {
            cityResources = resources;
        }

        public void DisplayResources()
        {
            Console.WriteLine($"Current Resources:");
            Console.WriteLine($"Wood: {cityResources.Wood}");
            Console.WriteLine($"Salt: {cityResources.Salt}");
            Console.WriteLine($"Stone: {cityResources.Stone}");
            Console.WriteLine($"Iron: {cityResources.Iron}");
            Console.WriteLine($"Money: {cityResources.Money}");
            Console.WriteLine($"Food: {cityResources.Food}");
        }
    }
}