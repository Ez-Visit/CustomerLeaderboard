using CustomerLeaderboard.Entity;
using CustomerLeaderboard.SkipList;

namespace CustomerLeaderboard.BizService
{
    public class CustomerRankingManager
    {
        private static readonly CustomerRankingManager INSTANCE = new CustomerRankingManager();
        private readonly ReaderWriterLockSlim rwLock = new ReaderWriterLockSlim();
        private readonly Dictionary<long, Customer> customers = new Dictionary<long, Customer>();
        private readonly SkipList<Customer> sortedCustomers;

        /// <summary>
        /// 私有构造函数，防止外部实例化
        /// </summary>
        private CustomerRankingManager()
        {
            sortedCustomers = new SkipList<Customer>((x, y) =>
            {
                // 比较两个客户实体的积分和客户id，返回-1(小于)，0(等于)，1(大于)
                int result = y.Score.CompareTo(x.Score); // 按积分降序
                if (result == 0)
                {
                    // 按客户id升序
                    result = x.CustomerID.CompareTo(y.CustomerID);
                }
                return result;
            });
        }

        /// <summary>
        /// 获取单例实例
        /// </summary>
        /// <returns></returns>
        public static CustomerRankingManager GetInstance()
        {
            return INSTANCE;
        }

        /// <summary>
        /// 添加或更新客户实体的方法，返回最新的积分
        /// </summary>
        /// <param name="customerId">客户ID</param>
        /// <param name="score">最新的积分</param>
        /// <returns></returns>
        public async Task<decimal> UpdateScore(long customerId, decimal score)
        {
            return await Task.Run(async () =>
            {
                rwLock.EnterUpgradeableReadLock();
                try
                {
                    if (customers.TryGetValue(customerId, out var existCustomer))
                    {
                        rwLock.EnterWriteLock();
                        try
                        {
                            existCustomer.Score += score;

                            //重新排序跳表
                            sortedCustomers.Remove(existCustomer);
                            sortedCustomers.Add(existCustomer);

                            return existCustomer.Score;
                        }
                        finally
                        {
                            rwLock.ExitWriteLock();
                        }
                    }
                    else
                    {
                        rwLock.EnterWriteLock();
                        try
                        {
                            // 不存在则创建新的客户实体
                            var newCustomer = new Customer
                            {
                                CustomerID = customerId,
                                Score = score,
                                Rank = 0 // 初始排名为0
                            };
                            // 添加到字典和跳表中
                            customers.Add(customerId, newCustomer);
                            sortedCustomers.Add(newCustomer);

                            int _maxLevels = sortedCustomers.MaxLevels;
                            var updateNodes = new CustomerNode[_maxLevels];
                            CustomerNode position;

                            if (sortedCustomers.Search(score, out position))
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
                                if (level > 0 && sortedCustomers.ShouldPromote())
                                {
                                    newNode = sortedCustomers.Promote(newNode, level);
                                }
                            }

                            sortedCustomers.InsertAndUpdateRanks(newNode);

                            //_skipList.InsertIntoSkipList(newCustomer);
                            return newCustomer.Score;
                        }
                        finally
                        {
                            rwLock.ExitWriteLock();
                        }
                    }
                }
                finally
                {
                    rwLock.ExitUpgradeableReadLock();
                }
            });
        }

        public async Task<List<CustomerNode>> GetCustomersByRank(int start, int end)
        {
            return await Task.Run(() =>
            {
                rwLock.EnterReadLock();
                try
                {
                    return sortedCustomers.GetCustomersByRank(start, end);
                }
                finally
                {
                    rwLock.ExitReadLock();
                }
            });
        }

        public async Task<CustomerWithNeighbors> GetCustomersByCustomerId(long customerId, int high, int low)
        {
            CustomerNode customer = null;
            List<CustomerNode> neighbors = new List<CustomerNode>();

            await Task.Run(() =>
            {
                rwLock.EnterReadLock();
                try
                {
                    customer = sortedCustomers.FindNodeByCustomerId(customerId);

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
                    rwLock.ExitReadLock();
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
