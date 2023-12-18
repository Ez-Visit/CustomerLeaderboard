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

                decimal customerScore = scores[customerId];

                var leaderboard = scores
                    .OrderByDescending(entry => entry.Value)
                    .ThenBy(entry => entry.Key)
                    .Select((entry, index) => new Customer
                    {
                        CustomerID = entry.Key,
                        Score = entry.Value,
                        Rank = index + 1
                    })
                    .ToList();

                var customer = leaderboard.Find(cust => cust.CustomerID == customerId);
                int customerRank = customer?.Rank ?? 0;

                var customers = leaderboard
                .Where(cust => cust.Rank >= customerRank - high && cust.Rank <= customerRank + low && cust.CustomerID != customerId)
                .ToList();

                var result = new CustomerWithNeighbors { Customer = customer, Neighbors = customers };
                return result;
            });
        }
    }
}
