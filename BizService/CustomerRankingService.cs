using CustomerLeaderboard.Entity;

namespace CustomerLeaderboard.BizService
{
    public class CustomerRankingService
    {
        private static readonly ReaderWriterLockSlim locker = new ReaderWriterLockSlim();
        private static readonly Dictionary<long, decimal> scores = new Dictionary<long, decimal>();
        private static CustomersWithRank customersWithRank = new CustomersWithRank();

        public CustomerRankingService()
        {
        }

        public async Task<decimal> UpdateScore(long customerId, decimal score)
        {
            return await Task.Run(() =>
                       {
                           locker.EnterWriteLock();
                           try
                           {
                               if (!scores.TryAdd(customerId, score))
                               {
                                   scores[customerId] += score;
                               }

                               RefreshCustomerRank();
                               return scores[customerId];
                           }
                           finally
                           {
                               locker.ExitWriteLock();
                           }
                       }
                );
        }

        private void RefreshCustomerRank()
        {
            var customers = scores.OrderByDescending(entry => entry.Value)
                        .ThenBy(entry => entry.Key)
                        .Select((entry, index) => new Customer
                        {
                            CustomerID = entry.Key,
                            Score = entry.Value,
                            Rank = index + 1
                        })
                        .ToList();

            foreach (var customer in customers)
            {
                var customerId = customer.CustomerID;
                if (customersWithRank.Contains(customerId))
                {
                    customersWithRank[customerId].Score = customer.Score;
                    customersWithRank[customerId].Rank = customer.Rank;
                }
                else
                {
                    customersWithRank.Add(customer);
                }
            }
        }

        public async Task<List<Customer>> GetCustomersByRank(int start, int end)
        {
            return await Task.Run(() =>
            {
                locker.EnterReadLock();
                try
                {
                    return customersWithRank
                        .Where(customer => customer.Rank >= start && customer.Rank <= end)
                        .ToList();
                }
                finally
                {
                    locker.ExitReadLock();
                }
            });
        }

        public async Task<CustomerWithNeighbors> GetCustomersByCustomerId(long customerId, int high, int low)
        {
            return await Task.Run(() =>
            {
                locker.EnterReadLock();
                try
                {
                    bool hasValue = scores.TryGetValue(customerId, out decimal value);
                    if (!hasValue)
                    {
                        return CustomerWithNeighbors.EMPTY;
                    }

                    var customer = customersWithRank[customerId];
                    int customerRank = customer?.Rank ?? 0;

                    var customers = customersWithRank
                    .Where(cust => cust.Rank >= customerRank - high && cust.Rank <= customerRank + low && cust.CustomerID != customerId)
                    .ToList();

                    var result = new CustomerWithNeighbors { Customer = customer, Neighbors = customers };
                    return result;
                }
                finally
                {
                    locker.ExitReadLock();
                }

            });
        }
    }
}
