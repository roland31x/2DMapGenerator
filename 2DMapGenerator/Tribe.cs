using _2DMapGenerator;
using System.Collections.Generic;
using System;
using System.Linq;
using Windows.UI;

public class Tribe
{
    public string Name { get; set; }
    public List<Human> Members { get; private set; }
    public List<Tribe> Allies { get; private set; }
    public Human Leader { get; private set; }
    public float Resources { get; set; }
    public float Aggressiveness { get; private set; }
    public float Reputation { get; set; }
    public Vector2 TerritoryCenter { get; private set; }
    public float TerritoryRadius { get; private set; }
    public Color TribeColor { get; set; }
    private static Random random;

    public Tribe(string name, Human leader, float aggressiveness = 0.5f)
    {
        Name = name;
        Members = new List<Human> { leader };
        Leader = leader;
        Allies = new List<Tribe>();
        Resources = 100.0f;
        Aggressiveness = aggressiveness;
        Reputation = 0.5f;
        TerritoryCenter = leader.Position;
        TerritoryRadius = 10.0f;
    }

    public void AddMember(Human human)
    {
        Members.Add(human);
        human.Tribe = this;
    }

    public void RemoveMember(Human human)
    {
        Members.Remove(human);
        if (Leader == human)
            UpdateLeader();
        human.Tribe = null;
    }

    public void UpdateLeader()
    {
        Leader = Members.OrderByDescending(h => h.Energy).FirstOrDefault();
    }

    public void FormAlliance(Tribe otherTribe)
    {
        if (!Allies.Contains(otherTribe))
        {
            Allies.Add(otherTribe);
            otherTribe.Allies.Add(this);
            Reputation += 0.1f;
        }
    }

    public void BreakAlliance(Tribe otherTribe)
    {
        Allies.Remove(otherTribe);
        otherTribe.Allies.Remove(this);
        Reputation -= 0.1f;
    }

    public float CalculateProsperity()
    {
        float avgMoney = Members.Any() ? Members.Average(h => h.Money) : 0;
        return (Resources + avgMoney * 2) * Reputation;
    }

    public bool Fight(Tribe otherTribe, ref List<Tribe> tribes)
    {
        if (GetDistance(Leader.Position, otherTribe.Leader.Position) > TerritoryRadius * 2)
            return false;

        float thisTribeStrength = Members.Sum(h => h.Strength) * Aggressiveness;
        float otherTribeStrength = otherTribe.Members.Sum(h => h.Strength) * otherTribe.Aggressiveness;

        if (thisTribeStrength >= otherTribeStrength)
        {
            AbsorbTribe(otherTribe, ref tribes);
            Reputation += 0.2f;
            return true;
        }
        else
        {
            otherTribe.AbsorbTribe(this, ref tribes);
            Reputation -= 0.2f;
            return false;
        }
    }

    private void AbsorbTribe(Tribe losingTribe, ref List<Tribe> tribes)
    {
        foreach (var member in losingTribe.Members)
        {
            AddMember(member);
        }
        Resources += losingTribe.Resources;
        tribes.Remove(losingTribe);
    }

    private double GetDistance(Vector2 a, Vector2 b)
    {
        return Math.Sqrt((a.x - b.x) * (a.x - b.x) + (a.y - b.y) * (a.y - b.y));
    }

    public static void AssignTribeColors(List<Tribe> tribes)
    {
        List<Color> predefinedColors = new List<Color>
        {
            Color.FromArgb(255, 0, 0, 255),    // Blue
            Color.FromArgb(255, 255, 0, 0),    // Red
            Color.FromArgb(255, 0, 255, 0),    // Green
            Color.FromArgb(255, 255, 255, 0),  // Yellow
            Color.FromArgb(255, 255, 0, 255),  // Magenta
            Color.FromArgb(255, 0, 255, 255),  // Cyan
            Color.FromArgb(255, 128, 0, 128),  // Purple
            Color.FromArgb(255, 128, 128, 0),  // Olive
            Color.FromArgb(255, 0, 128, 128),  // Teal
            Color.FromArgb(255, 128, 0, 0)     // Maroon
        };

        for (int i = 0; i < tribes.Count; i++)
        {
            if (i < predefinedColors.Count)
            {
                tribes[i].TribeColor = predefinedColors[i];
            }
            else
            {
                tribes[i].TribeColor = Color.FromArgb(255, (byte)random.Next(256), (byte)random.Next(256), (byte)random.Next(256));
            }
        }
    }

    public Color GetTribeColor()
    {
        return TribeColor;
    }
}
