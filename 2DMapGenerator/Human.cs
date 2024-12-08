using System;
using System.Collections.Generic;

namespace _2DMapGenerator;

public class Human
{
    public Vector2 Position { get; set; }
    public Vector2 Direction { get; private set; }
    public float Speed { get; private set; }
    public float Lifespan { get; private set; }
    public float EnergyEfficiency { get; private set; }
    private float energy;
    private float age;

    private Random random;

    public Human(Vector2 startPosition, float speed = 1.0f, float lifespan = 100.0f, float energyEfficiency = 1.0f)
    {
        Position = startPosition;
        Speed = speed;
        Lifespan = lifespan;
        EnergyEfficiency = energyEfficiency;
        energy = 100.0f; // Starting energy
        age = 0.0f;
        random = new Random();
        GenerateNewDirection();
    }

    public void Move(Map map, List<Food> foodItems, List<Human> humans)
    {
        // Check for nearby food
        Food nearestFood = FindNearestFood(foodItems);
        if (nearestFood != null && GetDistance(Position, nearestFood.Position) < 1.0)
        {
            // Skip moving if already at the food
            return;
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
                // Move in the current direction
                Position = new Vector2(
                    Position.x + Direction.x * Speed,
                    Position.y + Direction.y * Speed
                );

                // Keep humans within map bounds
                Position = new Vector2(
                    Math.Clamp(Position.x, 0, map.Width - 1),
                    Math.Clamp(Position.y, 0, map.Height - 1)
                );

                // Randomly change direction
                if (random.NextDouble() < 0.1)
                    GenerateNewDirection();
            }
        }

        // Consume energy based on movement
        energy -= 1.0f / EnergyEfficiency;

        // Increase age
        age++;
    }

    public bool IsAlive()
    {
        return energy > 0; //age < Lifespan && ;
    }

    public void Eat()
    {
        energy += 50; // Replenish energy when eating
    }

    public Human Reproduce(Human partner)
    {
        float childSpeed = MutateTrait((Speed + partner.Speed) / 2);
        float childLifespan = MutateTrait((Lifespan + partner.Lifespan) / 2);
        float childEnergyEfficiency = MutateTrait((EnergyEfficiency + partner.EnergyEfficiency) / 2);

        // Add a small random offset to the child's position
        Vector2 childPosition = new Vector2(
            Position.x + (float)(random.NextDouble() * 2 - 1), // Random offset [-1, 1]
            Position.y + (float)(random.NextDouble() * 2 - 1)  // Random offset [-1, 1]
        );

        Human child = new Human(childPosition, childSpeed, childLifespan, childEnergyEfficiency);

        // Make both parents and the child move in new random directions
        GenerateNewDirection(); // This human
        partner.GenerateNewDirection(); // Partner
        child.GenerateNewDirection(); // Child

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

        // Normalize direction and move
        if (magnitude > 0)
        {
            direction = new Vector2(direction.x / magnitude, direction.y / magnitude);
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
            if (human == this) continue; // Skip self

            double distance = GetDistance(Position, human.Position);
            if (distance < 5.0 && distance < nearestDistance) // Within 5 units
            {
                nearestHuman = human;
                nearestDistance = distance;
            }
        }

        return nearestHuman;
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
