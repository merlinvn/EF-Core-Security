﻿using NUnit.Framework;
using System;
using System.Linq;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using DevExpress.EntityFramework.SecurityDataStore.Tests.Helpers;
using DevExpress.EntityFramework.SecurityDataStore.Tests.DbContexts;

namespace DevExpress.EntityFramework.SecurityDataStore.Tests.Performance {
    [TestFixture]
    public abstract class DeleteTests {
        [Test]
        public void DeleteObjectsWithoutPermissions() {
            DeleteObjects(TestType.WithoutPermissions);
        }
        [Test]
        public void DeleteObjectsWithOnePermission() {
            DeleteObjects(TestType.WithOnePermission);
        }
        [Test]
        public void DeleteObjectsWithMultiplyPermissions() {
            DeleteObjects(TestType.WithMultiplePermissions);
        }

        public void DeleteObjects(TestType testType) {
            int count = 100;
            List<long> times = new List<long>();
            List<Func<IDbContextMultiClass>> contexts = PerformanceTestsHelper.GetContextCreators();

            foreach(Func<IDbContextMultiClass> createContext in contexts) {
                using(IDisposable disposableInterface = (IDisposable)createContext()) {
                    IDbContextMultiClass contextInterface = (IDbContextMultiClass)disposableInterface;
                    DbContext context = (DbContext)contextInterface;
                    context.ResetDatabase();

                    for(int i = 0; i < count; i++) {
                        DbContextObject1 obj = new DbContextObject1();
                        obj.Description = "Description " + i.ToString();
                        context.Add(obj);
                    }
                    context.SaveChanges();
                }

                using(IDisposable disposableInterface = (IDisposable)createContext()) {
                    IDbContextMultiClass contextInterface = (IDbContextMultiClass)disposableInterface;
                    DbContext context = (DbContext)contextInterface;

                    List<DbContextObject1> objects = contextInterface.dbContextDbSet1.Select(obj => obj).ToList();
                    Assert.AreEqual(count, objects.Count);

                    if(testType == TestType.WithOnePermission) {
                        SecurityDbContext securityDbContext = context as SecurityDbContext;
                        if(securityDbContext != null)
                            PerformanceTestsHelper.AddOnePermission(securityDbContext, SecurityOperation.Delete);
                    }

                    if(testType == TestType.WithMultiplePermissions) {
                        SecurityDbContext securityDbContext = context as SecurityDbContext;
                        if(securityDbContext != null)
                            PerformanceTestsHelper.AddMultiplePermissions(securityDbContext, SecurityOperation.Delete);
                    }

                    Stopwatch watch = new Stopwatch();
                    watch.Start();

                    foreach(DbContextObject1 obj in objects)
                        context.Remove(obj);

                    context.SaveChanges();

                    watch.Stop();
                    times.Add(watch.ElapsedMilliseconds);
                }
            }

            double securedContextTime = PerformanceTestsHelper.GetSecuredContextTime(times);
            double nativeContextTime = PerformanceTestsHelper.GetNativeContextTime(times);

            Assert.IsTrue(false, "our: " + securedContextTime.ToString() + " ms, native: " + nativeContextTime.ToString() + " ms");
        }
    }

    [TestFixture]
    public class InMemoryDeleteTests : DeleteTests {
        [SetUp]
        public void Setup() {
            SecurityTestHelper.CurrentDatabaseProviderType = SecurityTestHelper.DatabaseProviderType.IN_MEMORY;
        }
    }

    [TestFixture]
    public class LocalDb2012DeleteTests : DeleteTests {
        [SetUp]
        public void Setup() {
            SecurityTestHelper.CurrentDatabaseProviderType = SecurityTestHelper.DatabaseProviderType.LOCALDB_2012;
        }
    }
}
