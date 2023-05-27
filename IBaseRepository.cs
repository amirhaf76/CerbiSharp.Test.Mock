using System.Linq.Expressions;

namespace CerbiSharp.Test.Mock
{
    public interface IBaseRepository<TEntity> : IDisposable where TEntity : class
    {
        TEntity Find(params object[] keyValues);

        TEntity Add(TEntity entity);

        TEntity Remove(TEntity entity);

        TEntity Update(TEntity entity);


        void AddRange(params TEntity[] entities);

        void AddRange(IEnumerable<TEntity> entities);


        Task<TEntity> FindAsync(params object[] keyValues);

        Task<TEntity> AddAsync(TEntity entity);


        Task AddRangeAsync(params TEntity[] entities);

        Task AddRangeAsync(IEnumerable<TEntity> entities);


        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

        Task<int> SaveChangesAsync();

        TEntity FindAndLoadProperties<TProperty>(
            Expression<Func<TEntity, IEnumerable<TProperty>>> includeProperties,
            params object[] keyValues) where TProperty : class;

        Task<TEntity> FindAndLoadPropertiesAsync<TProperty>(
            Expression<Func<TEntity, IEnumerable<TProperty>>> includeProperties,
            params object[] keyValues) where TProperty : class;



        IEnumerable<TEntity> Get(
            Expression<Func<TEntity, bool>> filter = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null,
            params string[] includeProperties);

        Task<IEnumerable<TEntity>> GetAsync(
            Expression<Func<TEntity, bool>> filter = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null,
            params string[] includeProperties);


    }
}