using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VSuiteLab.Models;
using VSuiteLab.Models.Contexts;
using VSuiteLab.Models.Helpers;

namespace VSuiteLab.Services;

public class DatabaseService
{
    public async Task<StatusResponse<List<T>>> ReadAllAsync<T>(
        Func<IQueryable<T>, IQueryable<T>>? include = null,
        bool noTracking = false) where T : class
    {
        try
        {
            await using var db = new DatabaseContext();

            IQueryable<T> query = db.Set<T>();
            if (include != null) query = include(query);

            if (noTracking) query = query.AsNoTracking();

            var data = await query.ToListAsync();
            return StatusResponse<List<T>>.Ok(data);
        }
        catch (Exception ex)
        {
            return StatusResponse<List<T>>.Error(ex.Message);
        }
    }

    public async Task<StatusResponse<bool>> ReadExistsWhereAsync<T>(Expression<Func<T, bool>> predicate) where T : class
    {
        try
        {
            await using var _db = new DatabaseContext();

            var data = await _db.Set<T>().AnyAsync(predicate);
            return StatusResponse<bool>.Ok(data);
        }
        catch (Exception ex)
        {
            return StatusResponse<bool>.Error(ex.Message);
        }
    }


    public async Task<StatusResponse<List<T>>> ReadWhereAsync<T>(Expression<Func<T, bool>> predicate) where T : class
    {
        try
        {
            await using var _db = new DatabaseContext();

            var data = await _db.Set<T>().Where(predicate).ToListAsync();
            return StatusResponse<List<T>>.Ok(data);
        }
        catch (Exception ex)
        {
            return StatusResponse<List<T>>.Error(ex.Message);
        }
    }

    public async Task<StatusResponse<T>> CreateAsync<T>(T entity) where T : class
    {
        try
        {
            await using var _db = new DatabaseContext();

            _db.Set<T>().Add(entity);
            await _db.SaveChangesAsync();

            return StatusResponse<T>.Ok(entity);
        }
        catch (Exception ex)
        {
            return StatusResponse<T>.Error(ex.Message);
        }
    }

    public async Task<StatusResponse<T>> UpdateAsync<T>(T entity) where T : class
    {
        try
        {
            await using var db = new DatabaseContext();

            var entry = db.Attach(entity);
            entry.State = EntityState.Modified;

            await db.SaveChangesAsync();

            return StatusResponse<T>.Ok(entity);
        }
        catch (Exception ex)
        {
            return StatusResponse<T>.Error(ex.Message);
        }
    }

    public async Task<StatusResponse<T>> DeleteAsync<T>(T entity) where T : class
    {
        try
        {
            await using var _db = new DatabaseContext();

            _db.Set<T>().Remove(entity);
            await _db.SaveChangesAsync();

            return StatusResponse<T>.Ok(entity);
        }
        catch (Exception ex)
        {
            return StatusResponse<T>.Error(ex.Message);
        }
    }

    public async Task<StatusResponse<int>> DeleteWhereAsync<T>(
        Expression<Func<T, bool>> predicate) where T : class
    {
        try
        {
            await using var _db = new DatabaseContext();

            var entities = await _db.Set<T>()
                .Where(predicate)
                .ToListAsync();

            if (!entities.Any())
                return StatusResponse<int>.Ok(0);

            _db.Set<T>().RemoveRange(entities);

            var affectedRows = await _db.SaveChangesAsync();

            return StatusResponse<int>.Ok(affectedRows);
        }
        catch (Exception ex)
        {
            return StatusResponse<int>.Error(ex.Message);
        }
    }
}