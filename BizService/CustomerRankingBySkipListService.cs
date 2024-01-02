using CustomerLeaderboard.Entity;

namespace CustomerLeaderboard.BizService
{
    public class CustomerRankingBySkipListService
    {
        private static readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
        private readonly SkipList _skipList;
        private readonly Dictionary<long, Customer> _customers = new Dictionary<long, Customer>();

        public CustomerRankingBySkipListService()
        {
            _skipList = new SkipList(4);
        }

        public CustomerRankingBySkipListService(int maxLevels)
        {
            _skipList = new SkipList(maxLevels);
        }

        public async Task<decimal> UpdateScore(long customerId, decimal score)
        {
            return await Task.Run(async () =>
            {
                _lock.EnterUpgradeableReadLock();
                try
                {
                    if (_customers.TryGetValue(customerId, out var customer))
                    {
                        _lock.EnterWriteLock();
                        try
                        {
                            customer.Score += score;
                            _skipList.UpdateRank(customer);
                            return customer.Score;
                        }
                        finally
                        {
                            _lock.ExitWriteLock();
                        }
                    }
                    else
                    {
                        _lock.EnterWriteLock();
                        try
                        {
                            var newCustomer = new Customer
                            {
                                CustomerID = customerId,
                                Score = score,
                                Rank = 0,
                                Level = 0,
                                NextCustomers = new Customer[1]
                            };
                            _customers.Add(customerId, newCustomer);

                            int _maxLevels = _skipList.MaxLevels;
                            var updateNodes = new CustomerNode[_maxLevels];
                            CustomerNode position;

                            if (_skipList.Search(score, out position))
                            {
                                while (position.Next != null && position.Next.Score == score && position.Next.CustomerID < customerId)
                                {
                                    position = position.Next;
                                }

                                if (position.Next != null && position.Next.Score == score && position.Next.CustomerID == customerId)
                                {
                                    position.Next.CustomerID = customerId;
                                    return score;
                                }
                            }

                            var newNode = new CustomerNode
                            {
                                CustomerID = customerId,
                                Score = score,
                                Next = position.Next,
                                LowerLevels = new List<CustomerNode>()
                            };

                            for (int level = 0; level < _maxLevels; level++)
                            {
                                newNode.LowerLevels.Add(level < _maxLevels - 1 ? updateNodes[level].Next : null);
                                updateNodes[level].Next = newNode;
                                if (level > 0 && _skipList.ShouldPromote())
                                {
                                    newNode = _skipList.Promote(newNode, level);
                                }
                            }

                            _skipList.InsertAndUpdateRanks(newNode);

                            //_skipList.InsertIntoSkipList(newCustomer);
                            return newCustomer.Score;
                        }
                        finally
                        {
                            _lock.ExitWriteLock();
                        }
                    }
                }
                finally
                {
                    _lock.ExitUpgradeableReadLock();
                }
            });
        }

        public async Task<List<CustomerNode>> GetCustomersByRank(int start, int end)
        {
            return await Task.Run(() =>
            {
                _lock.EnterReadLock();
                try
                {
                    return _skipList.GetCustomersByRank(start, end);
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            });
        }

        public async Task<CustomerWithNeighbors> GetCustomersByCustomerId(long customerId, int high, int low)
        {
            CustomerNode customer = null;
            List<CustomerNode> neighbors = new List<CustomerNode>();

            await Task.Run(() =>
            {
                _lock.EnterReadLock();
                try
                {
                    customer = _skipList.FindNodeByCustomerId(customerId);

                    if (customer != null)
                    {
                        var current = customer;
                        for (int level = 0; level < high; level++)
                        {
                            if (current.LowerLevels.Count > level && current.LowerLevels[level] != null)
                            {
                                var neighborNode = current.LowerLevels[level];
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
                    _lock.ExitReadLock();
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
