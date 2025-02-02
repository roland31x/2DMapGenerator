using System.Collections.Generic;
using System.Linq;

namespace _2DMapGenerator
{
    public static class SocietyReporter
    {
        /// <summary>
        /// Generate a comprehensive society report that summarizes individual traits and tribe statistics.
        /// </summary>
        public static string GenerateReport(List<Tribe> tribes, List<Human> humans)
        {
            // Calculate individual averages (if any humans exist)
            float avgSpeed = humans.Count > 0 ? humans.Average(h => h.Speed) : 0;
            float avgLifespan = humans.Count > 0 ? humans.Average(h => h.Lifespan) : 0;
            float avgEfficiency = humans.Count > 0 ? humans.Average(h => h.EnergyEfficiency) : 0;
            float avgMoney = humans.Count > 0 ? humans.Average(h => h.Money) : 0;
            int population = humans.Count;

            // Calculate tribe statistics
            float totalProsperity = tribes.Sum(t => t.CalculateProsperity());
            float avgProsperity = tribes.Count > 0 ? totalProsperity / tribes.Count : 0;
            float avgReputation = tribes.Count > 0 ? tribes.Average(t => t.Reputation) : 0;
            string crimeReport = avgReputation < 0.4 ? "High crime rate" : "Low crime rate";

            // Build the report string
            string report = $"Population: {population}\n" +
                            $"Avg Speed: {avgSpeed:F2}\n" +
                            $"Avg Lifespan: {avgLifespan:F2}\n" +
                            $"Avg Efficiency: {avgEfficiency:F2}\n" +
                            $"Avg Human Wealth: {avgMoney:F2}\n" +
                            $"Avg Prosperity: {avgProsperity:F2}\n" +
                            $"Crime Report: {crimeReport}";

            return report;
        }
    }
}
