﻿using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using VaccinaCare.Domain;
using VaccinaCare.Domain.Entities;
using VaccinaCare.Repository.Interfaces;

namespace VaccinaCare.Repository.Repositories;

public class GenericRepository<TEntity> : IGenericRepository<TEntity> where TEntity : BaseEntity
{
    private readonly DbSet<TEntity> _dbSet;
    private readonly VaccinaCareDbContext _dbContext;
    private readonly ICurrentTime _timeService;
    private readonly IClaimsService _claimsService;

    public GenericRepository(VaccinaCareDbContext context, ICurrentTime timeService, IClaimsService claimsService)
    {
        _dbSet = context.Set<TEntity>();
        _dbContext = context;
        _timeService = timeService;
        _claimsService = claimsService;
    }

    public async Task<TEntity> AddAsync(TEntity entity)
    {
        try
        {
            entity.CreatedAt = _timeService.GetCurrentTime();
            //var user = await _dbContext.Users.FindAsync(_claimsService.GetCurrentUserId);
            //if (user == null) throw new Exception("This user is no longer existed");
            entity.CreatedBy = _claimsService.GetCurrentUserId;
            var result = await _dbSet.AddAsync(entity);
            //await _dbContext.SaveChangesAsync();
            return result.Entity;
        }
        catch (Exception)
        {
            throw;
        }
    }

    // riêng hàm này đã sửa để adapt theo Unit Of Work
    public async Task AddRangeAsync(List<TEntity> entities)
    {
        foreach (var entity in entities)
        {
            entity.CreatedAt = _timeService.GetCurrentTime();
            entity.CreatedBy = _claimsService.GetCurrentUserId;
        }

        await _dbSet.AddRangeAsync(entities);
    }

    public Task<List<TEntity>> GetAllAsync(Expression<Func<TEntity, bool>> predicate = null,
        params Expression<Func<TEntity, object>>[] includes)
    {
        IQueryable<TEntity> query = _dbSet;

        foreach (var include in includes) query = query.Include(include);

        return query.ToListAsync();
    }

    public async Task<TEntity?> GetByIdAsync(Guid id, params Expression<Func<TEntity, object>>[] includes)
    {
        IQueryable<TEntity> query = _dbSet;
        foreach (var include in includes) query = query.Include(include);
        var result = await query.FirstOrDefaultAsync(x => x.Id == id);
        return result;
    }

    public async Task<bool> SoftRemove(TEntity entity)
    {
        entity.IsDeleted = true;
        entity.DeletedAt = _timeService.GetCurrentTime();
        entity.DeletedBy = _claimsService.GetCurrentUserId;
        entity.UpdatedAt = _timeService.GetCurrentTime();

        _dbSet.Update(entity);
        // await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> SoftRemoveRange(List<TEntity> entities)
    {
        foreach (var entity in entities)
        {
            entity.IsDeleted = true;
            entity.DeletedAt = _timeService.GetCurrentTime();
            entity.DeletedBy = _claimsService.GetCurrentUserId;
        }

        _dbSet.UpdateRange(entities);
        //  await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> SoftRemoveRangeById(List<Guid> entitiesId) // update hàng loạt cùng 1 trường thì làm y chang
    {
        var entities = await _dbSet.Where(e => entitiesId.Contains(e.Id)).ToListAsync();

        foreach (var entity in entities)
        {
            entity.IsDeleted = true;
            entity.DeletedAt = _timeService.GetCurrentTime();
            entity.DeletedBy = _claimsService.GetCurrentUserId;
        }

        _dbContext.UpdateRange(entities);
        return true;
    }

    public async Task<bool> Update(TEntity entity)
    {
        entity.UpdatedAt = _timeService.GetCurrentTime();
        entity.UpdatedBy = _claimsService.GetCurrentUserId;
        _dbSet.Update(entity);
        //   await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateRange(List<TEntity> entities)
    {
        foreach (var entity in entities)
        {
            entity.UpdatedAt = _timeService.GetCurrentTime();
            entity.UpdatedBy = _claimsService.GetCurrentUserId;
        }

        _dbSet.UpdateRange(entities);
        //  await _dbContext.SaveChangesAsync();
        return true;
    }

    public IQueryable<TEntity> GetQueryable()
    {
        return _dbSet;
    }

    public async Task<TEntity?> FirstOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate = null,
        params Expression<Func<TEntity, object>>[] includes)
    {
        IQueryable<TEntity> query = _dbSet;

        // Include các navigation properties
        foreach (var include in includes) query = query.Include(include);

        // Áp dụng predicate (nếu có)
        if (predicate != null) query = query.Where(predicate);

        // Lấy bản ghi đầu tiên
        return await query.FirstOrDefaultAsync();
    }

    public async Task<bool> HardRemove(Expression<Func<TEntity, bool>> predicate)
    {
        try
        {
            var entities = await _dbSet.Where(predicate).ToListAsync();
            if (entities.Any())
            {
                _dbSet.RemoveRange(entities);
                return true;
            }

            return false; // Không có gì để xóa
        }
        catch (Exception ex)
        {
            throw new Exception($"Error while performing hard remove: {ex.Message}");
        }
    }

    public async Task<bool> HardRemoveRange(List<TEntity> entities)
    {
        try
        {
            if (entities.Any())
            {
                _dbSet.RemoveRange(entities);
                return true;
            }

            return false; // Không có gì để xóa
        }
        catch (Exception ex)
        {
            throw new Exception($"Error while performing hard remove range: {ex.Message}");
        }
    }
}