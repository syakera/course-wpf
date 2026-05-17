using System.Threading.Tasks;
using MedicalCenter.Data.Entities;

namespace MedicalCenter.Data.Repositories
{
    public interface IUserRepository : IRepository<UserEntity>
    {
        Task<UserEntity> GetByCredentialsAsync(string username, string password);
        Task<UserEntity> GetByUsernameAsync(string username);
    }
}
