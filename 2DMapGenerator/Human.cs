﻿using System;
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
    public List<Human> Parents { get; private set; } // Track parents
    public Tribe Tribe { get; set; } // Each human belongs to a tribe

    public float Age { get; private set; } // Age of the human
    public float MinReproductiveAge { get; private set; }
    public float MaxReproductiveAge { get; private set; }
    public float ReproductionEnergyThreshold { get; private set; }
    public float Energy { get; private set; }
    public float Strength { get; private set; }

    private bool canReproduce;
    private int currentCooldown; // Tracks current cooldown
    private Random random;

    public Human(Vector2 startPosition, Gender gender, List<Tribe> tribes, float speed = 1.0f, float lifespan = 100.0f, float energyEfficiency = 1.0f, Tribe tribe = null)
    {
        random = new Random();
        Position = startPosition;
        HumanGender = gender;
        Speed = speed;
        Lifespan = Math.Min(lifespan, random.Next(30, 100));
        EnergyEfficiency = energyEfficiency;
        Energy = 100.0f; // Starting energy
        Age = 0.0f;
        Parents = new List<Human>();
        canReproduce = HumanGender == Gender.Male || true; // Males can always reproduce
        BirthCooldown = HumanGender == Gender.Female ? 10 : 0; // Females have a cooldown period (e.g., 10 ticks)
        currentCooldown = 0;
        MinReproductiveAge = 20.0f; // Example: Humans can reproduce starting from age 20
        MaxReproductiveAge = 50.0f; // Example: Humans stop reproducing after age 50
        ReproductionEnergyThreshold = 30.0f; // Example: Humans need at least 30 energy to reproduce
        Tribe = tribe;
        GenerateNewDirection();
    }

    public void Move(Map map, List<Food> foodItems, List<Human> humans)
    {
        // Check if female is in cooldown period
        if (HumanGender == Gender.Female && currentCooldown > 0)
        {
            currentCooldown--;
            if (currentCooldown == 0)
                canReproduce = true; // Female can reproduce again after cooldown
        }

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
        Energy -= 1.0f / EnergyEfficiency;

        // Increase age
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

    public Human Reproduce(Human partner)
    {
        float childSpeed = MutateTrait((Speed + partner.Speed) / 2);
        float childLifespan = MutateTrait((Lifespan + partner.Lifespan) / 2);
        float childEnergyEfficiency = MutateTrait((EnergyEfficiency + partner.EnergyEfficiency) / 2);

        Vector2 childPosition = new Vector2(
            Position.x + (float)(random.NextDouble() * 2 - 1), // Random offset [-1, 1]
            Position.y + (float)(random.NextDouble() * 2 - 1)
        );

        Gender childGender = (random.NextDouble() < 0.5) ? Gender.Male : Gender.Female;

        Human child = new Human(childPosition, childGender, null, childSpeed, childLifespan, childEnergyEfficiency);
        child.Parents.Add(this);
        child.Parents.Add(partner);

        GenerateNewDirection();
        partner.GenerateNewDirection();
        child.GenerateNewDirection();

        BirthCooldown = 20.0f; // Add a cooldown period for the mother
        partner.BirthCooldown = 20.0f;

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
            && !this.Parents.Contains(partner)
            && !partner.Parents.Contains(this)
            && Age >= MinReproductiveAge && Age <= MaxReproductiveAge
            && Energy >= ReproductionEnergyThreshold
            && partner.Age >= partner.MinReproductiveAge && partner.Age <= partner.MaxReproductiveAge
            && partner.Energy >= partner.ReproductionEnergyThreshold;
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