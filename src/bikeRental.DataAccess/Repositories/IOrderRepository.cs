﻿using bikeRental.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bikeRental.DataAccess.Repositories
{
    public interface IOrderRepository<TEntity> where TEntity : Order
    {
        Task<TEntity> AddAsync(TEntity entity);
        Task<IEnumerable<TEntity>> GetAllAsync();
        Task<TEntity> GetByIdAsync(Guid? id);
        Task UpdateAsync(TEntity entity);
        Task DeleteAsync(Guid id);
    }
}
