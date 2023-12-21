using CustomerLeaderboard.Entity;

namespace CustomerLeaderboard.BizService
{
    public class CustomerRankingBySkipListService
    {
        private static readonly ReaderWriterLockSlim locker = new ReaderWriterLockSlim();
        private readonly SkipList skipList;

        public CustomerRankingBySkipListService(int maxLevels)
        {
            skipList = new SkipList(maxLevels);
        }

        public async Task<decimal> UpdateScore(long customerId, decimal score)
        {
            return await Task.Run(() =>
            {
                locker.EnterWriteLock();
                try
                {
                    skipList.InsertOrUpdate(customerId, score);
                    //TODO: calclate the new socre
                    return score;
                }
                finally
                {
                    locker.ExitWriteLock();
                }
            });
        }

        public async Task<List<CustomerNode>> GetCustomersByRank(int start, int end)
        {
            return await Task.Run(() =>
            {
                locker.EnterReadLock();
                try
                {
                    return skipList.GetCustomersByRank(start, end);
                }
                finally
                {
                    locker.ExitReadLock();
                }
            });
        }

        public async Task<CustomerWithNeighbors> GetCustomersByCustomerId(long customerId, int high, int low)
        {
            CustomerNode customer = null;
            List<CustomerNode> neighbors = new List<CustomerNode>();

            await Task.Run(() =>
            {
                locker.EnterReadLock();
                try
                {
                    customer = skipList.FindNodeByCustomerId(customerId);

                    if (customer != null)
                    {
                        var current = customer;
                        for (int level = 0; level < high; level++)
                        {
                            if (current.DownLevels.Count > level && current.DownLevels[level] != null)
                            {
                                var neighborNode = current.DownLevels[level];
                                neighbors.Add(neighborNode);
                            }
                            else
                            {
                                break;
                            }
                        }

                        current = customer;
                        for (int level = 0; level <= low; level++)
                        {
                            if (current.Next != null)
                            {
                                current = current.Next;
                                neighbors.Add(current);
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                }
                finally
                {
                    locker.ExitReadLock();
                }
            });

            return new CustomerWithNeighbors
            {
                Customer = MaptoCustomer(customer),
                Neighbors = neighbors.Select(MaptoCustomer).ToList()
            };
        }


        private Customer MaptoCustomer(CustomerNode customerNode)
        {
            if (customerNode == null)
            {
                return null;
            }

            return new Customer
            {
                CustomerID = customerNode?.CustomerID ?? 0,
                Score = customerNode?.Score ?? 0,
                Rank = customerNode?.Rank ?? 0
            };

        }


    }
}
