using TaskPulse.API.Data;

namespace TaskPulse.API.Repository.Infrastructure
{
    public abstract class BaseRepositoryClass
    {
        protected readonly AppDbContext _db;

        protected BaseRepositoryClass(AppDbContext db)
        {
            _db = db;
        }
    }
}
