﻿using IHunger.Services.Restaurants.Core.Entities;
using IHunger.Services.Restaurants.Core.Models;
using IHunger.Services.Restaurants.Core.Repositories;
using IHunger.Services.Restaurants.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace IHunger.Services.Restaurants.Infrastructure.Repositories
{
    public abstract class Repository<TEntity> : IRepository<TEntity> where TEntity : AggregateRoot, new()
    {
        protected readonly RestaurantDbContext Db;
        protected readonly DbSet<TEntity> DbSet;

        protected Repository(RestaurantDbContext db)
        {
            Db = db;
            DbSet = db.Set<TEntity>();
        }

        public virtual async Task Add(TEntity entity)
        {
            entity.CreatedAt = DateTime.Now;
            await DbSet.AddAsync(entity);

            await Db.SaveChangesAsync();
        }

        public virtual async Task<BaseResult<TEntity>> Search(
            Expression<Func<TEntity, bool>>? predicate = null, 
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null, 
            int pageSize = 10, int page = 1)
        {
            var query = DbSet.AsQueryable();

            var paged = new PagedResult();
            paged.CurrentPage = page;
            paged.PageSize = pageSize;
            paged.RowCount = query.Count();
            var pageCount = (double)paged.RowCount / pageSize;
            paged.PageCount = (int)Math.Ceiling(pageCount);
            var skip = (page - 1) * pageSize;

            if (predicate != null)
            {
                query = query.Where(predicate);
            }

            query = query.OrderBy(x => x.Id).Skip(skip).Take(pageSize);

            if (orderBy != null)
            {
                var data = await orderBy(query).ToListAsync();
                return new BaseResult<TEntity>(data, paged);
            }

            return new BaseResult<TEntity>(await query.ToListAsync(), paged);
        }

        public virtual async Task<IEnumerable<TEntity>> Find(Expression<Func<TEntity, bool>> predicate)
        {
            return await DbSet.AsNoTracking().Where(predicate).ToListAsync();
        }

        public virtual async Task<List<TEntity>> GetAll()
        {
            return await DbSet.ToListAsync();
        }

        public async Task<TEntity?> GetById(Guid id)
        {
            return await DbSet.FindAsync(id);
        }

        public virtual async Task Update(TEntity entity)
        {
            entity.CreatedAt = DateTime.Now;
            DbSet.Update(entity);

            await Db.SaveChangesAsync();
        }

        public virtual async Task Remove(Guid id)
        {
            var entity = DbSet.Find(id);

            if (entity != null)
            {
                entity.DeletedAt = DateTime.Now;
                await Update(entity);
            }
        }

        public virtual void Dispose()
        {
            Db?.Dispose();
        }
    }
}
