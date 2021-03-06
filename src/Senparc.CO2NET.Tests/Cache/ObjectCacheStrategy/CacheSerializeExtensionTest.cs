using Microsoft.VisualStudio.TestTools.UnitTesting;
using Senparc.CO2NET.Cache;
using Senparc.CO2NET.Cache.Redis;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;

namespace Senparc.CO2NET.Tests
{
    [TestClass]
    public class CacheSerializeExtensionTest
    {
        public class TestClass
        {
            public string ID { get; set; }
            public long Star { get; set; }
            public DateTime AddTime { get; set; }
            public Type Type { get; set; }
        }

        [TestMethod]
        public void CacheWrapperTest()
        {
            var testClass = new TestClass()
            {
                ID = Guid.NewGuid().ToString(),
                Star = DateTime.Now.Ticks,
                AddTime = DateTime.Now
            };

            var json = testClass.SerializeToCache();
            Console.WriteLine(json);

            var obj = json.DeserializeFromCache<TestClass>();
            Assert.AreEqual(obj.ID, testClass.ID);
        }

        [TestMethod]
        public void CacheWrapperEfficiencyTest()
        {
            Console.WriteLine("开始CacheWrapper异步测试");
            var threadCount = 10;
            var finishCount = 0;
            List<Thread> threadList = new List<Thread>();
            for (int i = 0; i < threadCount; i++)
            {
                var thread = new Thread(() =>
                {

                    var testClass = new TestClass()
                    {
                        ID = Guid.NewGuid().ToString(),
                        Star = DateTime.Now.Ticks,
                        AddTime = DateTime.Now
                    };

                    var dtx = DateTime.Now;
                    var json = testClass.SerializeToCache();
                    //Console.WriteLine(json);
                    Console.WriteLine($"testClass.SerializeToCache 耗时：{(DateTime.Now - dtx).TotalMilliseconds}ms");


                    dtx = DateTime.Now;
                    var obj = json.DeserializeFromCache<TestClass>();
                    Console.WriteLine($"json.DeserializeFromCache<TestClass> 耗时：{(DateTime.Now - dtx).TotalMilliseconds}ms");
                    Assert.AreEqual(obj.ID, testClass.ID);
                    Assert.AreEqual(obj.Star, testClass.Star);
                    Assert.AreEqual(obj.AddTime, testClass.AddTime);

                    Console.WriteLine("");

                    finishCount++;
                });
                threadList.Add(thread);
            }

            threadList.ForEach(z => z.Start());

            while (finishCount < threadCount)
            {
                //等待
            }

        }

        [TestMethod]
        public void CacheWapper_VS_BinaryTest()
        {
            var count = 50000;
            var dt1 = DateTime.Now;
            for (int i = 0; i < count; i++)
            {
                var testClass = new TestClass()
                {
                    ID = Guid.NewGuid().ToString(),
                    Star = DateTime.Now.Ticks,
                    AddTime = DateTime.Now
                };

                var dtx = DateTime.Now;
                var json = testClass.SerializeToCache();
                //Console.WriteLine(json);
                //Console.WriteLine($"testClass.SerializeToCache 耗时：{(DateTime.Now - dtx).TotalMilliseconds}ms");

                dtx = DateTime.Now;
                var obj = json.DeserializeFromCache<TestClass>();
                //Console.WriteLine($"json.DeserializeFromCache<TestClass> 耗时：{(DateTime.Now - dtx).TotalMilliseconds}ms");
                Assert.AreEqual(obj.ID, testClass.ID);
                Assert.AreEqual(obj.Star, testClass.Star);
                Assert.AreEqual(obj.AddTime, testClass.AddTime);

            }
            var dt2 = DateTime.Now;
            Console.WriteLine($"CacheWrapper序列化 {count} 次，时间：{(dt2 - dt1).TotalMilliseconds}ms");

            dt1 = DateTime.Now;
            for (int i = 0; i < count; i++)
            {
                var testClass = new TestClass()
                {
                    ID = Guid.NewGuid().ToString(),
                    Star = DateTime.Now.Ticks,
                    AddTime = DateTime.Now
                };

                var dtx = DateTime.Now;
                var serializedObj = StackExchangeRedisExtensions.Serialize(testClass);
                //Console.WriteLine($"StackExchangeRedisExtensions.Serialize耗时：{(DateTime.Now - dtx).TotalMilliseconds}ms");

                dtx = DateTime.Now;
                var containerBag = StackExchangeRedisExtensions.Deserialize<TestClass>((RedisValue)serializedObj);//11ms
                //Console.WriteLine($"StackExchangeRedisExtensions.Deserialize耗时：{(DateTime.Now - dtx).TotalMilliseconds}ms");

                Assert.AreEqual(containerBag.AddTime.Ticks, testClass.AddTime.Ticks);
                Assert.AreNotEqual(containerBag.GetHashCode(), testClass.GetHashCode());

            }
            dt2 = DateTime.Now;
            Console.WriteLine($"StackExchangeRedisExtensions序列化 {count} 次，时间：{(dt2 - dt1).TotalMilliseconds}ms");


            dt1 = DateTime.Now;
            for (int i = 0; i < count; i++)
            {
                var testClass = new TestClass()
                {
                    ID = Guid.NewGuid().ToString(),
                    Star = DateTime.Now.Ticks,
                    AddTime = DateTime.Now,
                };

                //模拟CacheWrapper的Type额外工作量，对比效率，主要的效率损失就在反射类型上
                //testClass.Type = testClass.GetType();

                var dtx = DateTime.Now;
                var serializedObj = Newtonsoft.Json.JsonConvert.SerializeObject(testClass);
                //Console.WriteLine($"StackExchangeRedisExtensions.Serialize耗时：{(DateTime.Now - dtx).TotalMilliseconds}ms");

                dtx = DateTime.Now;
                var containerBag = Newtonsoft.Json.JsonConvert.DeserializeObject<TestClass>(serializedObj);//11ms
                                                                                                           //Console.WriteLine($"StackExchangeRedisExtensions.Deserialize耗时：{(DateTime.Now - dtx).TotalMilliseconds}ms");

                Assert.AreEqual(containerBag.AddTime.Ticks, testClass.AddTime.Ticks);
                Assert.AreNotEqual(containerBag.GetHashCode(), testClass.GetHashCode());

            }
            dt2 = DateTime.Now;
            Console.WriteLine($"Newtonsoft 序列化（无反射） {count} 次，时间：{(dt2 - dt1).TotalMilliseconds}ms");


            dt1 = DateTime.Now;
            for (int i = 0; i < count; i++)
            {
                var testClass = new TestClass()
                {
                    ID = Guid.NewGuid().ToString(),
                    Star = DateTime.Now.Ticks,
                    AddTime = DateTime.Now,
                };

                //模拟CacheWrapper的Type额外工作量，对比效率，主要的效率损失就在反射类型上
                testClass.Type = testClass.GetType();

                var dtx = DateTime.Now;
                var serializedObj = Newtonsoft.Json.JsonConvert.SerializeObject(testClass);
                //Console.WriteLine($"StackExchangeRedisExtensions.Serialize耗时：{(DateTime.Now - dtx).TotalMilliseconds}ms");

                dtx = DateTime.Now;
                var containerBag = Newtonsoft.Json.JsonConvert.DeserializeObject<TestClass>(serializedObj);//11ms
                //Console.WriteLine($"StackExchangeRedisExtensions.Deserialize耗时：{(DateTime.Now - dtx).TotalMilliseconds}ms");

                Assert.AreEqual(containerBag.AddTime.Ticks, testClass.AddTime.Ticks);
                Assert.AreNotEqual(containerBag.GetHashCode(), testClass.GetHashCode());

            }
            dt2 = DateTime.Now;
            Console.WriteLine($"Newtonsoft 序列化+反射 {count} 次，时间：{(dt2 - dt1).TotalMilliseconds}ms");


            dt1 = DateTime.Now;
            for (int i = 0; i < count; i++)
            {
                var testClass = new TestClass()
                {
                    ID = Guid.NewGuid().ToString(),
                    Star = DateTime.Now.Ticks,
                    AddTime = DateTime.Now,
                };

                //模拟CacheWrapper的Type额外工作量，对比效率，主要的效率损失就在反射类型上
                Expression<Func<TestClass>> fun = () => testClass;
                //Console.WriteLine(fun.Body.Type);

                testClass.Type = fun.Body.Type;

                var dtx = DateTime.Now;
                var serializedObj = Newtonsoft.Json.JsonConvert.SerializeObject(testClass);
                //Console.WriteLine($"StackExchangeRedisExtensions.Serialize耗时：{(DateTime.Now - dtx).TotalMilliseconds}ms");

                dtx = DateTime.Now;
                var containerBag = Newtonsoft.Json.JsonConvert.DeserializeObject<TestClass>(serializedObj);//11ms
                                                                                                           //Console.WriteLine($"StackExchangeRedisExtensions.Deserialize耗时：{(DateTime.Now - dtx).TotalMilliseconds}ms");
                Assert.AreEqual(typeof(TestClass), containerBag.Type);
                Assert.AreEqual(containerBag.AddTime.Ticks, testClass.AddTime.Ticks);
                Assert.AreNotEqual(containerBag.GetHashCode(), testClass.GetHashCode());

            }
            dt2 = DateTime.Now;
            Console.WriteLine($"Newtonsoft 序列化（Lambda） {count} 次，时间：{(dt2 - dt1).TotalMilliseconds}ms");

        }

    }
}
