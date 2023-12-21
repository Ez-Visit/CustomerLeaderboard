# CustomerLeaderboard

### 提高并发处理能力的几种方式
#### 1、享元模式（Flyweight Pattern）：通过共享对象来减少内存占用和提高性能。
在并发处理中，可以使用享元模式来共享可重用的资源，例如线程池、连接池等，以提高性能和减少资源消耗。
#### 2、异步编程模式（Asynchronous Programming Pattern）：使用异步编程来提高应用的并发处理能力。
通过使用异步关键字和异步方法，可以在等待IO操作或其他耗时操作时释放线程资源，从而提高应用的并发处理能力。
#### 3、并发集合（Concurrent Collections）：使用并发集合来处理多线程并发操作。
并发集合提供了线程安全的数据结构，可以在多线程环境下实现高效的并发操作，例如ConcurrentQueue、ConcurrentDictionary等。
#### 4、任务并行库（Task Parallel Library）：使用任务并行库来管理并发任务的执行。
任务并行库提供了丰富的并行处理工具和API，可以方便地编写并发任务和处理并行操作。例如使用Task类和Parallel类来执行并行任务。

### v 1.1 问题思考： 
ConcurrentDictionary.OrderBy的性能在大数据量下是很一般的。再加上每次查询的时候都即时排序，高并发场景下会进一步降低性能。

问题改进：
#### 1、ConcurrentDictionary.OrderBy的性能不够好，改成 Dictionary<long, decimal>，自行维护并发场景的线程安全。
#### 2、为了减少常规lock造成的性能损失，改用 ReaderWriterLockSlim，实现类似于读写锁的机制。
允许多个线程同时读取字典的内容，但在写入时会独占资源。目的是提高读取操作的性能。
#### 3、取消每次查询的时候都即时排序，在内存中持久化客户排名，
#### 4、为了提高内存中查询客户排名的性能，引入KeyedCollection。
它内部使用了 Dictionary 实现的索引，这使得根据键值快速查找项的性能非常高，使得可以通过键来访问和管理集合中的元素。 


参考资料：
https://learn.microsoft.com/zh-cn/dotnet/standard/collections/sorted-collection-types


新的思路 CustomerRankingBySkipListService，代码待完善，基于跳表 
更新排行榜算法：当前代码的更新排行榜算法比较简单粗暴，每次更新分数后都重新计算整个排行榜。
这样会导致排行榜数据量大时的性能问题。
可以考虑使用更高效的数据结构来维护排行榜，例如使用跳表（Skip List）或平衡二叉树（如AVL树）来实现有序的排行榜，
从而在更新分数时只需更新相应的节点，而不必重新计算整个排行榜。


选择跳表的理由：
跳表拥有平衡二叉树相同的查询效率，但是跳表对于树平衡的实现是基于一种随机化的算法的，相对于AVL树/B树（B-Tree）/B+树（B+Tree）/红黑树的实现简单得多。
针对大体量、海量数据集中查找指定数据有更好的解决方案，我们得评估时间、空间的成本和收益。
跳表同样支持对数据进行高效的查找，插入和删除数据操作时间复杂度能与平衡二叉树媲美，最重要的是跳表的实现比平衡二叉树简单几个级别。缺点就是“以空间换时间”方式存在一定数据冗余。
如果存储的数据是大对象，跳表冗余的只是指向数据的指针，几乎可以不计使用的内存空间。
https://zhuanlan.zhihu.com/p/68516038 数据结构与算法——跳表
https://www.cnblogs.com/Laymen/p/14084664.html 跳表(SkipList)原理篇