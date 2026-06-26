using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DomainLayer.Contracts;
using DomainLayer.Models;
using Presistence.Data;

namespace Presistence.Repositories
{
    public class UnitOfWork(FitMateDbContext dbContext) : IUnitOfWork
    {
        private readonly Dictionary<string, object> repositories = []; // Dictionary to hold repositories
        public IGenericRepository<TEntity, TKey> GetRepository<TEntity, TKey>() where TEntity : BaseEntity<TKey>
        {
            var typeName = typeof(TEntity).Name; // Get the name of the entity type
            if (repositories.ContainsKey(typeName)) 
            {
                return (IGenericRepository<TEntity, TKey>)repositories[typeName]; // Return existing repository if it exists
            }
            else
            {
                var repositoryInstance = new GenericRepository<TEntity, TKey>(dbContext); // Create a new repository instance
                repositories[typeName] = repositoryInstance; 
                return repositoryInstance; 

            }
        }

        public async Task<int> SaveChangesAsync() => await dbContext.SaveChangesAsync();

    }
}
