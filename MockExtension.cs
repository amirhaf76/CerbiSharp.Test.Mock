using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using MockQueryable.Moq;
using Moq;

namespace CerbiSharp.Test.Mock
{
    public class MockExtension
    {
        public static Mock<DbContext> CreateMockDbContextAndSetItUp<TEntity>(List<TEntity> entities) where TEntity : class
        {
            var mockDbSet = entities.AsQueryable().BuildMockDbSet();

            mockDbSet = SetUpMockDbSet(entities, mockDbSet);

            var dbContext = new Mock<DbContext>();

            dbContext.Setup(db => db.Set<TEntity>()).Returns(mockDbSet.Object);

            return dbContext;
        }

        public static Mock<DbContext> CreateMockDbContextAndSetItUp<TKey, TEntity>(Dictionary<TKey, TEntity> keyValues, Func<TEntity, TKey> getKey)
            where TEntity : class
            where TKey : notnull
        {
            var mockDbSet = keyValues.Values.AsQueryable().BuildMockDbSet();

            mockDbSet = SetUpMockDbSet(keyValues, mockDbSet, getKey);

            var dbContext = new Mock<DbContext>();

            dbContext.Setup(db => db.Set<TEntity>()).Returns(mockDbSet.Object);

            return dbContext;
        }


        public static Mock<DbSet<TEntity>> SetUpMockDbSet<TEntity>(List<TEntity> entities, Mock<DbSet<TEntity>> mock)
            where TEntity : class
        {
            mock
                .Setup(x => x.Remove(It.IsAny<TEntity>()))
                .Callback((TEntity entity) => entities.Remove(entity))
                .Returns((TEntity e) => CreateMockEntityEntry(e).Object);

            mock
                .Setup(x => x.Add(It.IsAny<TEntity>()))
                .Callback((TEntity entity) => entities.Add(entity))
                .Returns((TEntity e) => CreateMockEntityEntry(e).Object);

            mock
                .Setup(x => x.AddAsync(It.IsAny<TEntity>(), It.IsAny<CancellationToken>()))
                .Callback((TEntity e, CancellationToken ct) => entities.Add(e))
                .Returns((TEntity e, CancellationToken ct) => CreateEntityEntryAsync(e));

            mock
                .Setup(x => x.AddRange(It.IsAny<TEntity[]>()))
                .Callback((TEntity[] es) => entities.AddRange(es));

            mock
                .Setup(x => x.AddRange(It.IsAny<IEnumerable<TEntity>>()))
                .Callback((IEnumerable<TEntity> es) => entities.AddRange(es));

            mock
                .Setup(x => x.AddRangeAsync(It.IsAny<TEntity[]>()))
                .Callback((TEntity[] es) => entities.AddRange(es))
                .Returns(Task.CompletedTask);

            mock
                .Setup(x => x.AddRangeAsync(It.IsAny<IEnumerable<TEntity>>(), It.IsAny<CancellationToken>()))
                .Callback((IEnumerable<TEntity> es, CancellationToken ct) => entities.AddRange(es))
                .Returns(Task.CompletedTask);

            mock
                .Setup(x => x.Update(It.IsAny<TEntity>()))
                .Callback((TEntity entity) =>
                {
                    var foundEntity = entities.FirstOrDefault(e => e == entity);

                    if (foundEntity != null)
                    {
                        int index = entities.IndexOf(foundEntity);

                        entities[index] = entity;
                    }

                    throw new Exception("There is no entity like entered entity!");
                })
                .Returns((TEntity e) => CreateMockEntityEntry(e).Object);

            TEntity findEntity(params object[] objs)
            {
                if (objs.Length > 1)
                {
                    throw new Exception("For more than one key there is no behavior!");
                }

                if (objs[0] is int id)
                {
                    var entityType = typeof(TEntity);

                    var property = entityType.GetProperty("Id");

                    return entities.Find(e => (int)property.GetValue(e) == id);
                }

                throw new Exception("Obj type must be int!");
            }

            mock
                .Setup(x => x.Find(It.IsAny<object[]>()))
                .Returns((object[] objs) => findEntity(objs));

            mock
                .Setup(x => x.FindAsync(It.IsAny<object[]>()))
                .ReturnsAsync((object[] objs) => findEntity());

            return mock;
        }

        public static Mock<DbSet<TEntity>> SetUpMockDbSet<TKey, TEntity>(Dictionary<TKey, TEntity> dict, Mock<DbSet<TEntity>> mock, Func<TEntity, TKey> getKey)
            where TEntity : class
            where TKey : notnull
        {
            mock
                .Setup(x => x.Remove(It.IsAny<TEntity>()))
                .Callback((TEntity e) => dict.Remove(getKey(e)))
                .Returns((TEntity e) => CreateMockEntityEntry(e).Object);

            mock
                .Setup(x => x.Add(It.IsAny<TEntity>()))
                .Callback((TEntity e) => dict.Add(getKey(e), e))
                .Returns((TEntity e) => CreateMockEntityEntry(e).Object);

            mock
                .Setup(x => x.AddAsync(It.IsAny<TEntity>(), It.IsAny<CancellationToken>()))
                .Callback((TEntity e, CancellationToken ct) => dict.Add(getKey(e), e))
                .Returns((TEntity e, CancellationToken ct) => CreateEntityEntryAsync(e));

            mock
                .Setup(x => x.AddRange(It.IsAny<TEntity[]>()))
                .Callback((TEntity[] es) =>
                {
                    foreach (var e in es)
                    {
                        dict.Add(getKey(e), e);
                    }
                });

            mock
                .Setup(x => x.AddRange(It.IsAny<IEnumerable<TEntity>>()))
                .Callback((IEnumerable<TEntity> es) =>
                {
                    foreach (var e in es)
                    {
                        dict.Add(getKey(e), e);
                    }
                });

            mock
                .Setup(x => x.AddRangeAsync(It.IsAny<TEntity[]>()))
                .Callback((TEntity[] es) =>
                {
                    foreach (var e in es)
                    {
                        dict.Add(getKey(e), e);
                    }
                })
                .Returns(Task.CompletedTask);

            mock
                .Setup(x => x.AddRangeAsync(It.IsAny<IEnumerable<TEntity>>(), It.IsAny<CancellationToken>()))
                .Callback((IEnumerable<TEntity> es, CancellationToken ct) =>
                {
                    foreach (var e in es)
                    {
                        dict.Add(getKey(e), e);
                    }
                })
                .Returns(Task.CompletedTask);

            mock
                .Setup(x => x.Update(It.IsAny<TEntity>()))
                .Callback((TEntity e) => { dict[getKey(e)] = e; })
                .Returns((TEntity e) => CreateMockEntityEntry(e).Object);

            TEntity findEntity(params object[] objs)
            {
                if (objs.Length > 1)
                {
                    throw new Exception("For more than one key there is no behavior!");
                }

                if (objs[0] is TKey key)
                {
                    dict.TryGetValue(key, out TEntity entity);

                    return entity;
                }

                throw new Exception($"Obj type must be {typeof(TKey).Name}!");
            }

            mock
                .Setup(x => x.Find(It.IsAny<object[]>()))
                .Returns((object[] objs) => findEntity(objs));

            mock
                .Setup(x => x.FindAsync(It.IsAny<object[]>()))
                .ReturnsAsync((object[] objs) => findEntity());

            return mock;
        }


        public static Mock<DbSet<TEntity>> CreateMockRepository<TKey, TEntity, TRepository>(Dictionary<TKey, TEntity> dict, Mock<DbSet<TEntity>> mock, Func<TEntity, TKey> getKey)
            where TEntity : class
            where TKey : notnull
        {
            mock
               .Setup(x => x.Remove(It.IsAny<TEntity>()))
               .Callback((TEntity e) => dict.Remove(getKey(e)))
               .Returns((TEntity e) => CreateMockEntityEntry(e).Object);

            mock
                .Setup(x => x.Add(It.IsAny<TEntity>()))
                .Callback((TEntity e) => dict.Add(getKey(e), e))
                .Returns((TEntity e) => CreateMockEntityEntry(e).Object);

            mock
                .Setup(x => x.AddAsync(It.IsAny<TEntity>(), It.IsAny<CancellationToken>()))
                .Callback((TEntity e, CancellationToken ct) => dict.Add(getKey(e), e))
                .Returns((TEntity e, CancellationToken ct) => CreateEntityEntryAsync(e));

            mock
                .Setup(x => x.AddRange(It.IsAny<TEntity[]>()))
                .Callback((TEntity[] es) =>
                {
                    foreach (var e in es)
                    {
                        dict.Add(getKey(e), e);
                    }
                });

            mock
                .Setup(x => x.AddRange(It.IsAny<IEnumerable<TEntity>>()))
                .Callback((IEnumerable<TEntity> es) =>
                {
                    foreach (var e in es)
                    {
                        dict.Add(getKey(e), e);
                    }
                });

            mock
                .Setup(x => x.AddRangeAsync(It.IsAny<TEntity[]>()))
                .Callback((TEntity[] es) =>
                {
                    foreach (var e in es)
                    {
                        dict.Add(getKey(e), e);
                    }
                })
                .Returns(Task.CompletedTask);

            mock
                .Setup(x => x.AddRangeAsync(It.IsAny<IEnumerable<TEntity>>(), It.IsAny<CancellationToken>()))
                .Callback((IEnumerable<TEntity> es, CancellationToken ct) =>
                {
                    foreach (var e in es)
                    {
                        dict.Add(getKey(e), e);
                    }
                })
                .Returns(Task.CompletedTask);

            mock
                .Setup(x => x.Update(It.IsAny<TEntity>()))
                .Callback((TEntity e) => { dict[getKey(e)] = e; })
                .Returns((TEntity e) => CreateMockEntityEntry(e).Object);

            TEntity findEntity(params object[] objs)
            {
                if (objs.Length > 1)
                {
                    throw new Exception("For more than one key there is no behavior!");
                }

                if (objs[0] is TKey key)
                {
                    dict.TryGetValue(key, out TEntity entity);

                    return entity;
                }

                throw new Exception($"Obj type must be {typeof(TKey).Name}!");
            }

            mock
                .Setup(x => x.Find(It.IsAny<object[]>()))
                .Returns((object[] objs) => findEntity(objs));

            mock
                .Setup(x => x.FindAsync(It.IsAny<object[]>()))
                .ReturnsAsync((object[] objs) => findEntity());

            return mock;
        }


        public static ValueTask<EntityEntry<TEntity>> CreateEntityEntryAsync<TEntity>(TEntity entity) where TEntity : class
        {
            return new ValueTask<EntityEntry<TEntity>>(CreateMockEntityEntry(entity).Object);
        }


        public static Mock<EntityEntry<TEntity>> CreateMockEntityEntry<TEntity>(TEntity entity) where TEntity : class
        {
            var mockEntity = CreateMockEntityEntry<TEntity>();

            mockEntity.Setup(x => x.Entity).Returns(entity);

            return mockEntity;
        }

        private static Mock<EntityEntry<TEntity>> CreateMockEntityEntry<TEntity>() where TEntity : class
        {
            return new Mock<EntityEntry<TEntity>>(null);
        }
    }
}