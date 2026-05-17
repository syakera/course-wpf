using System.Data.Entity;
using System.Threading.Tasks;
using MedicalCenter.Data.Entities;

namespace MedicalCenter.Data.Repositories
{
    public class UserRepository : Repository<UserEntity>, IUserRepository
    {
        public UserRepository(MedicalCenterDbContext context) : base(context)
        {
        }

        public Task<UserEntity> GetByCredentialsAsync(string username, string password)
        {
            return Set.FirstOrDefaultAsync(u => u.Username == username && u.Password == password);
        }

        public Task<UserEntity> GetByUsernameAsync(string username)
        {
            return Set.FirstOrDefaultAsync(u => u.Username == username);
        }
    }
}
