using CustomerLeaderboard.Entity;
using System.Collections.Concurrent;

namespace CustomerLeaderboard.BizService
{
    public class CustomerRankingService
    {

        private static ConcurrentDictionary<long, decimal> scores = new ConcurrentDictionary<long, decimal>();

        public CustomerRankingService()
        {
        }


        public async Task<decimal> UpdateScore(long customerId, decimal score)
        {
            return await Task.Run(() => scores.AddOrUpdate(customerId, score, (_, existingScore) => existingScore + score));
        }

        public async Task<List<Customer>> GetCustomersByRank(int start, int end)
        {
            return await Task.Run(() =>
            {
                var leaderboard = scores
                      .OrderByDescending(entry => entry.Value)
                      .ThenBy(entry => entry.Key)
                      .Select((entry, index) => new Customer { CustomerID = entry.Key, Score = entry.Value, Rank = index + 1 })
                      .Where(customer => customer.Rank >= start && customer.Rank <= end)
                      .ToList();

                return leaderboard;
            });
        }

        public async Task<CustomerWithNeighbors> GetCustomersByCustomerId(long customerId, int high, int low)
        {
            return await Task.Run(() =>
            {
                if (!scores.ContainsKey(customerId))
                {
                    return CustomerWithNeighbors.EMPTY;
                }
                var customer = new Customer { CustomerID = customerId, Score = scores[customerId] };
                var leaderboard = scores
                    .OrderByDescending(entry => entry.Value)
                    .ThenBy(entry => entry.Key)
                    .Select((entry, index) => new Customer { CustomerID = entry.Key, Score = entry.Value, Rank = index + 1 })
                    .ToList();

                var startIndex = Math.Max(0, leaderboard.FindIndex(c => c.CustomerID == customerId) - high);
                var endIndex = Math.Min(leaderboard.Count - 1, leaderboard.FindIndex(c => c.CustomerID == customerId) + low);

                var customers = leaderboard
                    .Skip(startIndex)
                    .Take(endIndex - startIndex + 1)
                    .ToList();

                var result = new CustomerWithNeighbors { Customer = customer, Neighbors = customers };

                return result;
            }
                );
        }
    }
}
