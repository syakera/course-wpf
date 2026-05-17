using System.Data.Entity;
using System.Linq;

namespace MedicalCenter.Data.Repositories
{
    public class Repository<T> : IRepository<T> where T : class
    {
        protected readonly MedicalCenterDbContext Context;
        protected readonly DbSet<T> Set;

        public Repository(MedicalCenterDbContext context)
        {
            Context = context;
            Set = context.Set<T>();
        }

        public IQueryable<T> Query() => Set;

        public void Add(T entity) => Set.Add(entity);

        public void Remove(T entity) => Set.Remove(entity);
    }
}
