using System;
using System.Collections.Generic;

namespace _2DMapGenerator;

public class Human
{
    public enum Gender { Male, Female }

    public Vector2 Position { get; set; }
    public Vector2 Direction { get; private set; }
    public float Speed { get; private set; }
    public float Lifespan { get; private set; }
    public float EnergyEfficiency { get; private set; }
    public Gender HumanGender { get; private set; }
    public float BirthCooldown { get; private set; }
    public List<Human> Parents { get; private set; }
    public Tribe Tribe { get; set; }

    public float Age { get; private set; }
    public float MinReproductiveAge { get; private set; }
    public float MaxReproductiveAge { get; private set; }
    public float ReproductionEnergyThreshold { get; private set; }
    public float Energy { get; private set; }
    public float Strength { get; private set; }

    public float Food { get; private set; }
    public float Money { get; private set; }
    public float ToolEfficiency { get; private set; }

    private bool canReproduce;
    private float currentCooldown;
    private Random random;

    public Human(Vector2 startPosition, Gender gender, List<Tribe> tribes, float speed = 1.0f, float lifespan = 100.0f, float energyEfficiency = 1.0f, Tribe tribe = null)
    {
        random = new Random();
        Position = startPosition;
        HumanGender = gender;
        Speed = speed;
        Lifespan = Math.Min(lifespan, random.Next(30, 100));
        EnergyEfficiency = energyEfficiency;
        Energy = random.Next(10,90); // Starting energy
        Age = 0.0f;
        Parents = new List<Human>();
        canReproduce = HumanGender == Gender.Male || true; // Males can always reproduce
        BirthCooldown = HumanGender == Gender.Female ? 20 : 2; // Females have a cooldown period
        currentCooldown = 0;
        MinReproductiveAge = 20.0f; // Example: Humans can reproduce starting from age 20
        MaxReproductiveAge = 50.0f; // Example: Humans stop reproducing after age 50
        ReproductionEnergyThreshold = 30.0f; // Example: Humans need at least 30 energy to reproduce
        Tribe = tribe;
        Food = 50.0f; // Starting food
        Money = 50.0f;  // Starting money for new concept
        ToolEfficiency = 1.0f;
        GenerateNewDirection();
    }

    public void GatherFood(Map map)
    {
        if (map[(int)Position.x, (int)Position.y] >= 0.3f && map[(int)Position.x, (int)Position.y] <= 0.7f)
        {
            Food += 10.0f * ToolEfficiency;
        }
    }

    public void ConsumeFood()
    {
        if (Food > 0)
        {
            Food -= 1.0f;
            Energy += 5.0f;
        }
        else
        {
            Energy -= 2.0f;
        }
    }

    public void Move(Map map, List<Food> foodItems, List<Human> humans)
    {
        // Check if female is in cooldown period
        if (currentCooldown > 0)
        {
            currentCooldown--;
            if (currentCooldown == 0)
                canReproduce = true;
        }

        // Check for nearby food
        Food nearestFood = FindNearestFood(foodItems);
        if (nearestFood != null && GetDistance(Position, nearestFood.Position) < 1.0)
        {
            // Already at the food; do not move
        }
        else if (nearestFood != null)
        {
            Seek(nearestFood.Position);
        }
        else
        {
            // Check for nearby humans to reproduce
            Human nearestHuman = FindNearestHuman(humans);
            if (nearestHuman != null)
            {
                Seek(nearestHuman.Position);
            }
            else
            {
                // Default movement in the current direction
                Position = new Vector2(
                    Position.x + Direction.x * Speed,
                    Position.y + Direction.y * Speed
                );
            }
        }

        // After any movement (even if done via Seek), clamp the position
        Position = new Vector2(
            Math.Clamp(Position.x, 1, map.Width - 1),
            Math.Clamp(Position.y, 1, map.Height - 1)
        );

        // Consume energy and increase age
        Energy -= 1.0f / EnergyEfficiency;
        Age++;
    }


    public bool IsAlive()
    {
        return Energy > 0 && Age < Lifespan;
    }

    public void Eat()
    {
        Energy += 50; // Replenish energy when eating
    }

    public void Farm(Map map)
    {
        // Assuming land is defined by values between 0.3 and 0.7
        int x = (int)Position.x;
        int y = (int)Position.y;
        if (map[x, y] >= 0.3f && map[x, y] <= 0.7f)
        {
            // Increase money based on tool efficiency (or could increase Food/Resources)
            float produce = 10.0f * ToolEfficiency;
            Money += produce;
            Tribe.Resources += produce * 0.2f;
        }
    }

    public void Steal(Human target)
    {
        // Steal food if hungry
        if (target.Food > 0 && Food < 10.0f)
        {
            float stolenFood = Math.Min(5.0f, target.Food);
            Food += stolenFood;
            target.Food -= stolenFood;
        }
        // Steal money if target has money and this human has little money
        if (target.Money > 0 && Money < 20.0f)
        {
            float stolenMoney = Math.Min(10.0f, target.Money);
            Money += stolenMoney;
            target.Money -= stolenMoney;
            // Affect tribe reputation if theft is noticed
            Tribe.Reputation -= 0.05f;
        }
    }

    public Human Reproduce(Human partner, Map map)
    {
        float childSpeed = MutateTrait((Speed + partner.Speed) / 2);
        float childLifespan = MutateTrait((Lifespan + partner.Lifespan) / 2);
        float childEnergyEfficiency = MutateTrait((EnergyEfficiency + partner.EnergyEfficiency) / 2);

        Vector2 rawChildPosition = new Vector2(
            Position.x + (float)(random.NextDouble() * 2 - 1),
            Position.y + (float)(random.NextDouble() * 2 - 1)
        );

        Vector2 childPosition = new Vector2(
            Math.Clamp(rawChildPosition.x, 1, map.Width - 11),
            Math.Clamp(rawChildPosition.y, 1, map.Height - 11)
        );

        Gender childGender = (random.NextDouble() < 0.5) ? Gender.Male : Gender.Female;

        // Create the child, passing the parent's tribe
        Human child = new Human(childPosition, childGender, null, childSpeed, childLifespan, childEnergyEfficiency, this.Tribe);
        Tribe.Members.Add(child);
        child.Parents.Add(this);
        child.Parents.Add(partner);

        GenerateNewDirection();
        partner.GenerateNewDirection();
        child.GenerateNewDirection();

        // Apply reproduction cooldown
        currentCooldown = this.BirthCooldown;
        partner.currentCooldown = partner.BirthCooldown;

        return child;
    }


    private float MutateTrait(float traitValue)
    {
        // Small random mutation
        return (float)(traitValue + random.NextDouble() * 0.2 - 0.1);
    }

    private void Seek(Vector2 targetPosition)
    {
        Vector2 direction = new Vector2(targetPosition.x - Position.x, targetPosition.y - Position.y);
        double magnitude = Math.Sqrt(direction.x * direction.x + direction.y * direction.y);

        if (magnitude > 0)
        {
            // Normalize the direction
            direction = new Vector2(direction.x / magnitude, direction.y / magnitude);
            // Update position (we leave clamping to the Move method)
            Position = new Vector2(
                Position.x + direction.x * Speed,
                Position.y + direction.y * Speed
            );
        }
    }


    private Food FindNearestFood(List<Food> foodItems)
    {
        Food nearestFood = null;
        double nearestDistance = double.MaxValue;

        foreach (var food in foodItems)
        {
            double distance = GetDistance(Position, food.Position);
            if (distance < 5.0 && distance < nearestDistance) // Within 5 units
            {
                nearestFood = food;
                nearestDistance = distance;
            }
        }

        return nearestFood;
    }

    private Human FindNearestHuman(List<Human> humans)
    {
        Human nearestHuman = null;
        double nearestDistance = double.MaxValue;

        foreach (var human in humans)
        {
            if (!CanReproduce(human)) continue;

            double distance = GetDistance(Position, human.Position);
            if (distance < nearestDistance) // Check for nearest human
            {
                nearestHuman = human;
                nearestDistance = distance;
            }
        }

        return nearestHuman;
    }

    public bool CanReproduce(Human partner)
    {
        return this != null 
            && partner != null
            && partner.HumanGender != this.HumanGender
            && this.canReproduce && partner.canReproduce
            && Age >= MinReproductiveAge && Age <= MaxReproductiveAge
            && Energy >= ReproductionEnergyThreshold
            && partner.Age >= partner.MinReproductiveAge && partner.Age <= partner.MaxReproductiveAge
            && partner.Energy >= partner.ReproductionEnergyThreshold
            && !this.Parents.Contains(partner)
            && !partner.Parents.Contains(this)
            && (Tribe == partner.Tribe || random.NextDouble() < 0.2); // 20% chance of reproduction between different tribes
    }

    private double GetDistance(Vector2 a, Vector2 b)
    {
        return Math.Sqrt((a.x - b.x) * (a.x - b.x) + (a.y - b.y) * (a.y - b.y));
    }

    private void GenerateNewDirection()
    {
        double angle = random.NextDouble() * Math.PI * 2;
        Direction = new Vector2(Math.Cos(angle), Math.Sin(angle));
    }
}