using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VSuiteLab.Models;
using VSuiteLab.Utils;

namespace VSuiteLab.Services;

public class DatabaseService
{
    private readonly DatabaseContext _db;

    public DatabaseService()
    {
        _db = new DatabaseContext();
    }
    
    public void Dispose()
    {
        _db.Dispose();
    }
    
    public void DetachEntity<T>(T entity) where T : class => _db.Entry(entity).State = EntityState.Detached;
    
    public async Task<StatusResponse<List<T>>> ReadAllAsync<T>(Func<IQueryable<T>, IQueryable<T>>? include = null) where T : class
    {
        try
        {
            IQueryable<T> query = _db.Set<T>();

            if (include != null)
                query = include(query);

            var result = await query.ToListAsync();
            return StatusResponse<List<T>>.Ok(result);
        } catch (Exception ex)
        {
            return StatusResponse<List<T>>.Error(ex.Message);
        }
    }
    public async Task<StatusResponse<bool>> ReadExistsWhereAsync<T>(Expression<Func<T, bool>> predicate) where T : class
    {
        try
        {
            var data = await _db.Set<T>().AnyAsync(predicate);
            return StatusResponse<bool>.Ok(data);
        } catch (Exception ex)
        {
            return StatusResponse<bool>.Error(ex.Message);
        }
    }

    public async Task<StatusResponse<List<T>>> ReadWhereAsync<T>(Expression<Func<T, bool>> predicate) where T : class
    {
        try
        {
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
            _db.Set<T>().Update(entity);
            await _db.SaveChangesAsync();

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
    
    public async Task SaveChangesAsync()
    {
        await _db.SaveChangesAsync();
    }
}