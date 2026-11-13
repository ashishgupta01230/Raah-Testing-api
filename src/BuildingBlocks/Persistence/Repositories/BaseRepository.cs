using HautSphere.BuildingBlocks.Domain;
using Microsoft.EntityFrameworkCore;

namespace HautSphere.BuildingBlocks.Persistence.Repositories
{
    public abstract class BaseRepository
    {
        protected DbContext DbContext { get; set; }

        public void SetContext(DbContext dbContext)
        {
            DbContext = dbContext;
        }
    }

    public class RepositoryProvider
    {
        private static Dictionary<Type, Type> repositoryDictionary = new Dictionary<Type, Type>();

        public static BaseRepository GetInstance<T>(DbContext dbContext) where T : DomainBase
        {
            if (repositoryDictionary.ContainsKey(typeof(T)))
            {
                return (BaseRepository)Activator.CreateInstance(repositoryDictionary[typeof(T)], dbContext);
            }

            return new Repository<T>(dbContext);
        }
    }
}
