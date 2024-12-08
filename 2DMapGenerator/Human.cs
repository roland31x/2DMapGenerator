using System;

namespace _2DMapGenerator
{
    public class Human
    {
        public Vector2 Position { get; set; }
        public Vector2 Direction { get; private set; }
        public float Speed { get; private set; }

        private Random random;

        public Human(Vector2 startPosition)
        {
            Position = startPosition;
            Speed = 1.0f;
            random = new Random();
            GenerateNewDirection();
        }

        public Human(Vector2 startPosition, float speed = 1.0f)
        {
            Position = startPosition;
            Speed = speed;
            random = new Random();
            GenerateNewDirection();
        }

        public void Move()
        {
            Position = new Vector2(Position.x + Direction.x * Speed, Position.y + Direction.y * Speed);

            // If out of bounds, generate a new direction
            if (random.NextDouble() < 0.1) // Randomly change direction
            {
                GenerateNewDirection();
            }
        }

        private void GenerateNewDirection()
        {
            double angle = random.NextDouble() * Math.PI * 2; // Random angle
            Direction = new Vector2(Math.Cos(angle), Math.Sin(angle));
        }
    }
}