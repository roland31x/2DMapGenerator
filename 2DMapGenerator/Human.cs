using System;

namespace _2DMapGenerator;

public class Human
{
    public Vector2 Position { get; set; }
    public Vector2 Direction { get; private set; }
    public float Speed { get; private set; }
    public int Lifespan { get; private set; }
    private Random random;

    public Human(Vector2 startPosition, float speed = 1.0f, int initialLifespan = 100)
    {
        Position = startPosition;
        Speed = speed;
        Lifespan = initialLifespan; // Time to live
        random = new Random();
        GenerateNewDirection();
    }

    public void Move(Map map)
    {
        // Get terrain type at current position
        int x = (int)Math.Clamp(Position.x, 0, map.Width - 1);
        int y = (int)Math.Clamp(Position.y, 0, map.Height - 1);
        float terrainValue = map[x, y];

        // Adjust speed based on terrain type
        float terrainSpeedModifier = 1.0f; // Default for plains
        if (terrainValue < 0.3f) // Water
        {
            terrainSpeedModifier = 0.5f; // Move slower
        }
        else if (terrainValue > 0.7f) // Mountains
        {
            terrainSpeedModifier = 0.7f; // Move slower
        }
        else if (terrainValue >= 0.3f && terrainValue <= 0.7f) // Plains
        {
            terrainSpeedModifier = 1.2f; // Move faster
        }

        // Update position with adjusted speed
        Position = new Vector2(
            Position.x + Direction.x * Speed * terrainSpeedModifier,
            Position.y + Direction.y * Speed * terrainSpeedModifier
        );

        // Decrease lifespan
        Lifespan--;

        // Occasionally change direction
        if (random.NextDouble() < 0.1)
        {
            GenerateNewDirection();
        }
    }

    private void GenerateNewDirection()
    {
        double angle = random.NextDouble() * Math.PI * 2; // Random angle
        Direction = new Vector2(Math.Cos(angle), Math.Sin(angle));
    }

    public bool IsAlive()
    {
        return Lifespan > 0;
    }

    public void Eat()
    {
        Lifespan += 50; // Replenish lifespan
    }
}
