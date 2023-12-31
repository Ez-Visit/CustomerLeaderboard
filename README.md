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


红黑树是一种自平衡的二叉搜索树，它的空间复杂度是 O(n)，其中 n 是元素的数量。
红黑树的优点是它可以提高查找的效率，因为它只需要维护一个层次的指针。
红黑树的缺点是它在插入和删除的时候可能需要做一些平衡的操作，这样的操作可能会涉及到整个树的其他部分，增加代码的复杂性和运行时的开销。
另外，红黑树在并发环境下可能需要加锁，这会降低性能和效率。

跳表是一种动态的数据结构，它可以在不重新分配内存的情况下添加或删除元素。跳表的空间复杂度是 O(n logn)，其中 n 是元素的数量。
跳表的优点是它可以节省内存，因为它不需要预先分配一个固定大小的数组。
跳表的缺点是它需要维护多个层次的指针，这会增加代码的复杂性和运行时的开销。 
但是，跳表在并发环境下有一个优势，跳表的操作更加局部性，锁需要盯住的节点更少，因此在这样的情况下性能好一些。

https://blog.csdn.net/gamekit/article/details/79047398 实时排序算法（跳表）C# 代码参考
https://cloud.tencent.com/developer/article/1867678 游戏排行榜-跳表实现原理分析
https://www.jianshu.com/p/9d8296562806 Skip List--跳表（全网最详细的跳表文章没有之一）

https://www.cnblogs.com/mushroom/p/4605690.html 探索c#之跳跃表(SkipList) 
https://github.com/kencausey/SkipList  github C# 跳表参考
https://codeleading.com/article/41394038855/ 跳跃表(C#代码)

https://zhuanlan.zhihu.com/p/268809846  游戏积分排行榜的实现
一种做法是用Redis的zset，相信也有很多游戏在用。
简单说zset通过dict和skiplist来保证查询更新都是O(log(N))复杂度。通过dict从玩家ID找到分数，然后通过skiplist找到玩家排名。

https://blog.csdn.net/u013709270/article/details/53470428 跳跃表的原理及实现

https://blog.csdn.net/weixin_34113237/article/details/86207744
https://cloud.tencent.com/developer/article/2169215
https://zhuanlan.zhihu.com/p/108386262 详解高级数据结构之 跳表
https://blog.csdn.net/qq_35247337/article/details/107045058
https://www.cnblogs.com/cdaniu/p/16369412.html 【C# 数据结构】 跳表(skip list)

https://zhuanlan.zhihu.com/p/638243227 
