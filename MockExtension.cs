using Microsoft.EntityFrameworkCore.ChangeTracking;
using Moq;

namespace CerbiSharp.Test.Mock
{
    public static class MockExtension
    {
        public static Mock<TRepository> CreateMockBaseRepository<TEntity, TRepository>(List<TEntity> entities)
           where TRepository : class, IBaseRepository<TEntity>
           where TEntity : class
        {
            var mockIBaseRepository = new Mock<TRepository>();

            mockIBaseRepository
                .Setup(x => x.SaveChangesAsync())
                .ReturnsAsync(entities.Count);


            mockIBaseRepository
                .Setup(x => x.Remove(It.IsAny<TEntity>()))
                .Callback((TEntity entity) => entities.Remove(entity))
                .Returns((TEntity entity) => entity);

            mockIBaseRepository
                .Setup(x => x.Add(It.IsAny<TEntity>()))
                .Callback((TEntity entity) => entities.Add(entity))
                .Returns((TEntity entity) => entity);

            mockIBaseRepository
                .Setup(x => x.AddAsync(It.IsAny<TEntity>()))
                .Callback((TEntity entity) => entities.Add(entity))
                .ReturnsAsync((TEntity entity) =>
                {
                    return entities.FirstOrDefault(x => x == entity);
                });

            mockIBaseRepository
                .Setup(x => x.AddRange(It.IsAny<TEntity[]>()))
                .Callback((TEntity[] es) => entities.AddRange(es));

            mockIBaseRepository
                .Setup(x => x.AddRange(It.IsAny<IEnumerable<TEntity>>()))
                .Callback((IEnumerable<TEntity> es) => entities.AddRange(es));

            mockIBaseRepository
                .Setup(x => x.AddRangeAsync(It.IsAny<TEntity[]>()))
                .Callback((TEntity[] es) => entities.AddRange(es))
                .Returns(Task.CompletedTask);

            mockIBaseRepository
                .Setup(x => x.AddRangeAsync(It.IsAny<IEnumerable<TEntity>>()))
                .Callback((IEnumerable<TEntity> es) => entities.AddRange(es))
                .Returns(Task.CompletedTask);

            mockIBaseRepository
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
                .Returns((TEntity entity) => entity);

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

                    return entities.Find(e => ((int)property.GetValue(e)) == id);
                }

                throw new Exception("Obj type must be int!");
            }

            mockIBaseRepository
                .Setup(x => x.Find(It.IsAny<object[]>()))
                .Returns((object[] objs) => findEntity(objs));

            mockIBaseRepository
                .Setup(x => x.FindAsync(It.IsAny<object[]>()))
                .ReturnsAsync((object[] objs) => findEntity());

            return mockIBaseRepository;
        }

        public static Mock<TRepository> CreateMockBaseRepository<TKey, TEntity, TRepository>(Dictionary<TKey, TEntity> dict, Func<TEntity, TKey> getKey)
            where TRepository : class, IBaseRepository<TEntity>
            where TEntity : class
        {
            var mockIBaseRepository = new Mock<TRepository>();

            mockIBaseRepository
               .Setup(x => x.Remove(It.IsAny<TEntity>()))
               .Callback((TEntity e) => dict.Remove(getKey(e)))
               .Returns((TEntity e) => CreateEntityEntry(e).Entity);

            mockIBaseRepository
                .Setup(x => x.Add(It.IsAny<TEntity>()))
                .Callback((TEntity e) => dict.Add(getKey(e), e))
                .Returns((TEntity e) => CreateEntityEntry(e).Entity);

            mockIBaseRepository
                .Setup(x => x.AddAsync(It.IsAny<TEntity>()))
                .Callback((TEntity e) => dict.Add(getKey(e), e))
                .Returns((TEntity e) => Task.FromResult(CreateEntityEntry(e).Entity));

            mockIBaseRepository
                .Setup(x => x.AddRange(It.IsAny<TEntity[]>()))
                .Callback((TEntity[] es) =>
                {
                    foreach (var e in es)
                    {
                        dict.Add(getKey(e), e);
                    }
                });

            mockIBaseRepository
                .Setup(x => x.AddRange(It.IsAny<IEnumerable<TEntity>>()))
                .Callback((IEnumerable<TEntity> es) =>
                {
                    foreach (var e in es)
                    {
                        dict.Add(getKey(e), e);
                    }
                });

            mockIBaseRepository
                .Setup(x => x.AddRangeAsync(It.IsAny<TEntity[]>()))
                .Callback((TEntity[] es) =>
                {
                    foreach (var e in es)
                    {
                        dict.Add(getKey(e), e);
                    }
                })
                .Returns(Task.CompletedTask);

            mockIBaseRepository
                .Setup(x => x.AddRangeAsync(It.IsAny<IEnumerable<TEntity>>()))
                .Callback((IEnumerable<TEntity> es) =>
                {
                    foreach (var e in es)
                    {
                        dict.Add(getKey(e), e);
                    }
                })
                .Returns(Task.CompletedTask);

            mockIBaseRepository
                .Setup(x => x.Update(It.IsAny<TEntity>()))
                .Callback((TEntity e) => { dict[getKey(e)] = e; })
                .Returns((TEntity e) => CreateEntityEntry(e).Entity);

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

            mockIBaseRepository
                .Setup(x => x.Find(It.IsAny<object[]>()))
                .Returns((object[] objs) => findEntity(objs));

            mockIBaseRepository
                .Setup(x => x.FindAsync(It.IsAny<object[]>()))
                .ReturnsAsync((object[] objs) => findEntity());

            return mockIBaseRepository;
        }


        public static EntityEntry<TEntity> CreateEntityEntry<TEntity>(TEntity entity) where TEntity : class
        {
            var mockEntity = CreateMockEntityEntry<TEntity>();

            mockEntity.Setup(x => x.Entity).Returns(entity);

            return mockEntity.Object;
        }


        private static Mock<EntityEntry<TEntity>> CreateMockEntityEntry<TEntity>() where TEntity : class
        {
            return new Mock<EntityEntry<TEntity>>(null);
        }
    }
}