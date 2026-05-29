using AadharLocation.Api.Data;
using AadharLocation.Api.Domain.Entities;
using AadharLocation.Shared.DTOs;
using AadharLocation.Shared.DTOs.Operators;
using AadharLocation.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AadharLocation.Api.Features.Operators;

[ApiController]
[Route("api/operators")]
[Authorize(Roles = "Admin")]
public class OperatorsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null)
    {
        var query = db.Operators
            .Include(o => o.AssignedMachine)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            query = query.Where(o =>
                o.Name.ToLower().Contains(s) ||
                o.EmployeeId.ToLower().Contains(s) ||
                o.Email.ToLower().Contains(s));
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(o => o.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new OperatorDto(
                o.Id, o.Name, o.EmployeeId, o.Email, o.Phone,
                o.AssignedMachineId, o.AssignedMachine != null ? o.AssignedMachine.Name : null,
                o.Status, o.CreatedAt))
            .ToListAsync();

        return Ok(new PagedResult<OperatorDto>(items, total, page, pageSize));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var op = await db.Operators
            .Include(o => o.AssignedMachine)
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == id);

        if (op is null) return NotFound();

        return Ok(new OperatorDto(
            op.Id, op.Name, op.EmployeeId, op.Email, op.Phone,
            op.AssignedMachineId, op.AssignedMachine?.Name,
            op.Status, op.CreatedAt));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOperatorRequest request)
    {
        if (await db.Operators.AnyAsync(o => o.EmployeeId == request.EmployeeId))
            return Conflict(new { message = $"Employee ID '{request.EmployeeId}' is already in use." });

        if (await db.Operators.AnyAsync(o => o.Email == request.Email.ToLower()))
            return Conflict(new { message = $"Email '{request.Email}' is already in use." });

        if (request.AssignedMachineId.HasValue &&
            await db.Operators.AnyAsync(o => o.AssignedMachineId == request.AssignedMachineId))
            return Conflict(new { message = "This machine is already assigned to another operator." });

        var op = new Operator
        {
            Name = request.Name,
            EmployeeId = request.EmployeeId,
            Email = request.Email.ToLower(),
            Phone = request.Phone,
            AssignedMachineId = request.AssignedMachineId,
            PasswordHash = request.TrackerPassword is not null
                ? BCrypt.Net.BCrypt.HashPassword(request.TrackerPassword)
                : null,
        };

        db.Operators.Add(op);
        await db.SaveChangesAsync();

        if (request.AssignedMachineId.HasValue)
        {
            var machine = await db.Machines.FindAsync(request.AssignedMachineId.Value);
            if (machine != null) machine.AssignedOperatorId = op.Id;
            await db.SaveChangesAsync();
        }

        return CreatedAtAction(nameof(GetById), new { id = op.Id },
            new OperatorDto(op.Id, op.Name, op.EmployeeId, op.Email, op.Phone,
                op.AssignedMachineId, null, op.Status, op.CreatedAt));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateOperatorRequest request)
    {
        var op = await db.Operators.FindAsync(id);
        if (op is null) return NotFound();

        if (await db.Operators.AnyAsync(o => o.Email == request.Email.ToLower() && o.Id != id))
            return Conflict(new { message = $"Email '{request.Email}' is already in use." });

        if (request.AssignedMachineId.HasValue &&
            await db.Operators.AnyAsync(o => o.AssignedMachineId == request.AssignedMachineId && o.Id != id))
            return Conflict(new { message = "This machine is already assigned to another operator." });

        // Sync Machine.AssignedOperatorId: clear old machine, set new machine
        if (op.AssignedMachineId != request.AssignedMachineId)
        {
            if (op.AssignedMachineId.HasValue)
            {
                var oldMachine = await db.Machines.FindAsync(op.AssignedMachineId.Value);
                if (oldMachine != null) oldMachine.AssignedOperatorId = null;
            }
            if (request.AssignedMachineId.HasValue)
            {
                var newMachine = await db.Machines.FindAsync(request.AssignedMachineId.Value);
                if (newMachine != null) newMachine.AssignedOperatorId = id;
            }
        }

        op.Name = request.Name;
        op.Email = request.Email.ToLower();
        op.Phone = request.Phone;
        op.AssignedMachineId = request.AssignedMachineId;
        op.Status = request.Status;

        if (request.NewTrackerPassword is not null)
            op.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewTrackerPassword);

        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var op = await db.Operators.FindAsync(id);
        if (op is null) return NotFound();

        db.Operators.Remove(op);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
