using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TaskManagerPro.Data.Context;
using TaskManagerPro.Data.Entities;

namespace TaskManagerPro.Data.Repositories;

public class SqliteTaskRepository : ITaskRepository
{
    private readonly AppDbContext _context;

    public SqliteTaskRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<TaskItem>> GetAllAsync()
    {
        var tasks = await _context.Tasks
            .Include(t => t.Category)
            .AsNoTracking()
            .ToListAsync();
            
        return tasks.OrderByDescending(t => t.PriorityScore);
    }

    public async Task<TaskItem?> GetByIdAsync(int id)
    {
        return await _context.Tasks
            .Include(t => t.Category)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task AddAsync(TaskItem task)
    {
        task.CreatedAt = DateTime.Now;
        task.UpdatedAt = DateTime.Now;
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(TaskItem task)
    {
        // Try to find the entity in the local cache or database
        var existingTask = _context.Tasks.Local.FirstOrDefault(t => t.Id == task.Id);
        
        if (existingTask == null)
        {
            existingTask = await _context.Tasks.FindAsync(task.Id);
        }

        if (existingTask != null)
        {
            // Copy scalar properties
            _context.Entry(existingTask).CurrentValues.SetValues(task);
            
            // Explicitly set UpdatedAt
            existingTask.UpdatedAt = DateTime.Now;

            // Save changes
            await _context.SaveChangesAsync();
            
            // Detach to allow future updates with different instances
            _context.Entry(existingTask).State = EntityState.Detached;
        }
    }

    public async Task DeleteAsync(int id)
    {
        var task = await _context.Tasks.FindAsync(id);
        if (task != null)
        {
            _context.Tasks.Remove(task);
            await _context.SaveChangesAsync();
        }
    }
}
