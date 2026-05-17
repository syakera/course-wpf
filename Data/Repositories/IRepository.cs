using System.Linq;

namespace MedicalCenter.Data.Repositories
{
    public interface IRepository<T> where T : class
    {
        IQueryable<T> Query();
        void Add(T entity);
        void Remove(T entity);
    }
}
