using System;
using System.Collections.Generic;
using System.Linq;

namespace _2DMapGenerator;

public class Tribe
{
    public string Name { get; set; }
    public List<Human> Members { get; private set; }
    public List<Tribe> Allies { get; private set; }
    public Human Leader { get; private set; }

    public Tribe(string name, Human leader)
    {
        Name = name;
        Members = new List<Human> { leader };
        Leader = leader;
    }

    public void AddMember(Human human)
    {
        Members.Add(human);
    }

    public void RemoveMember(Human human)
    {
        Members.Remove(human);
    }

    public void UpdateLeader()
    {
        // Example: Leader is the member with the highest energy
        Leader = Members.OrderByDescending(h => h.Energy).FirstOrDefault();
    }
    public void FormAlliance(Tribe otherTribe)
    {
        if (!Allies.Contains(otherTribe))
        {
            Allies.Add(otherTribe);
            otherTribe.Allies.Add(this); // Mutual alliance
        }
    }

    public void BreakAlliance(Tribe otherTribe)
    {
        Allies.Remove(otherTribe);
        otherTribe.Allies.Remove(this); // Mutual breakup
    }

    public bool Fight(Tribe otherTribe, ref List<Tribe> tribes)
    {
        if(GetDistance(this.Leader.Position, otherTribe.Leader.Position) < 10.0) return false;
        // Calculate total strength for both tribes
        float thisTribeStrength = Members.Sum(h => h.Strength);
        float otherTribeStrength = otherTribe.Members.Sum(h => h.Strength);

        // Decide winner based on strength
        if (thisTribeStrength >= otherTribeStrength)
        {
            // Winning tribe absorbs the losing tribe's resources
            AbsorbTribe(otherTribe, ref tribes);
            return true; // This tribe wins
        }
        else
        {
            otherTribe.AbsorbTribe(this,ref tribes);
            return false; // Other tribe wins
        }
    }

    private void AbsorbTribe(Tribe losingTribe, ref List<Tribe> tribes)
    {
        // Transfer members
        foreach (var member in losingTribe.Members)
        {
            AddMember(member);
            member.Tribe = this;
        }

        // Remove losing tribe
        tribes.Remove(losingTribe);
    }
    private double GetDistance(Vector2 a, Vector2 b)
    {
        return Math.Sqrt((a.x - b.x) * (a.x - b.x) + (a.y - b.y) * (a.y - b.y));
    }
}
