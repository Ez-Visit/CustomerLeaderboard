﻿using CustomerLeaderboard.Entity;
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
                    if (customers.TryGetValue(customerId, out var customer))
                    {
                        rwLock.EnterWriteLock();
                        try
                        {
                            customer.Score += score;
                            //重新排序跳表
                            sortedCustomers.Remove(customer);
                            sortedCustomers.Add(customer);
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
                            customer = new Customer
                            {
                                CustomerID = customerId,
                                Score = score,
                                Rank = 0 // 初始排名为0
                            };
                            // 添加到字典和跳表中
                            customers.Add(customerId, customer);
                            sortedCustomers.Add(customer);
                        }
                        finally
                        {
                            rwLock.ExitWriteLock();
                        }
                    }

                    // 记录跳表的长度
                    uint length = sortedCustomers.Count;

                    UpdateRank(length);
                    return customer.Score;
                }
                finally
                {
                    rwLock.ExitUpgradeableReadLock();
                }
            });
        }

        /// <summary>
        /// 更新客户排名
        /// </summary>
        /// <param name="length"></param>
        private void UpdateRank(uint length)
        {
            // 获取刚才添加的客户实体的前一个节点
            SkipListNode<Customer>? prevNode = sortedCustomers.GetNodeByIndex(length - 1);
            // 初始化排名为前一个节点的排名加一
            int rank = (prevNode?.Item?.Rank ?? 1) + 1;
            // 跳过已经排好序的客户实体，只遍历刚才添加或更新的客户实体
            // 获取刚才添加或更新的客户实体的第一个节点
            var firstNode = prevNode?.LevelsInfo[0].Next;
            // 从第一个节点开始，使用一个 while 循环，遍历刚才添加或更新的客户实体
            var current = firstNode;
            while (current != null)
            {
                // 设置客户的排名
                current.Item.Rank = rank;
                // 排名加一
                rank++;
                // 移动到下一个节点
                current = current.LevelsInfo[0].Next;
            }
        }

        public async Task<List<Customer>> GetCustomersByRank(int start, int end)
        {
            return await Task.Run(() =>
            {
                List<Customer> result = new List<Customer>();
                //尝试获取读锁，如果失败则等待
                rwLock.EnterReadLock();
                try
                {
                    // 判断起始和截止排名是否合法
                    if (start > 0 && end >= start && end <= sortedCustomers.Count)
                    {
                        var startNode = sortedCustomers.GetNodeByRank(start);
                        // 从 startNode 开始，使用一个 while 循环，遍历指定范围内的客户实体
                        SkipListNode<Customer> current = startNode;
                        while (current != null && current.Item.Rank <= end)
                        {
                            // 将客户实体添加到结果列表中
                            result.Add(current.Item);
                            // 移动到下一个节点
                            current = current.LevelsInfo[0].Next;
                        }
                    }

                    return result;
                }
                finally
                {
                    rwLock.ExitReadLock();
                }
            });
        }

        public async Task<List<Customer>> GetCustomersByCustomerId(long customerId, int high, int low)
        {
            return await Task.Run(() =>
            {
                // 创建空的客户实体列表
                List<Customer> result = new List<Customer>();
                // 尝试获取读锁，如果失败则等待
                rwLock.EnterReadLock();
                try
                {
                    // 判断字典中是否存在该客户id
                    if (customers.TryGetValue(customerId, out Customer? customer))
                    {
                        // 存在则添加到结果列表中
                        result.Add(customer);
                        // 获取该客户的排名
                        int rank = customer.Rank;
                        // 判断高位和低位参数是否合法
                        if (high >= 0 && low >= 0)
                        {
                            int startRank = rank + low - 1;
                            int endRank = rank - high - 1;

                            var startNode = sortedCustomers.GetNodeByRank(startRank);
                            // 从 startNode 开始，使用一个 while 循环，遍历指定范围内的客户实体
                            SkipListNode<Customer> current = startNode;
                            while (current != null && current.Item.Rank <= endRank)
                            {
                                // 将客户实体添加到结果列表中
                                result.Add(current.Item);
                                // 移动到下一个节点
                                current = current.LevelsInfo[0].Next;
                            }
                        }
                    }
                    // 返回结果列表
                    return result;
                }
                finally
                {
                    // 释放读锁
                    rwLock.ExitReadLock();
                }
            });
        }
    }
}