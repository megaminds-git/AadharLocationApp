using AadharLocation.Api.Data;
using AadharLocation.Api.Domain.Entities;
using AadharLocation.Shared.DTOs;
using AadharLocation.Shared.DTOs.Machines;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AadharLocation.Api.Features.Machines;

[ApiController]
[Route("api/machines")]
[Authorize(Roles = "Admin")]
public class MachinesController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = db.Machines
            .Include(m => m.AssignedOperator)
            .AsNoTracking();

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(m => m.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(m => ToDto(m))
            .ToListAsync();

        return Ok(new PagedResult<MachineDto>(items, total, page, pageSize));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var machine = await db.Machines
            .Include(m => m.AssignedOperator)
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == id);

        if (machine is null) return NotFound();
        return Ok(ToDto(machine));
    }

    [HttpGet("live")]
    [Authorize]
    public async Task<IActionResult> GetLive()
    {
        var machines = await db.Machines
            .Include(m => m.AssignedOperator)
            .AsNoTracking()
            .Select(m => ToDto(m))
            .ToListAsync();

        return Ok(machines);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateMachineRequest request)
    {
        if (await db.Machines.AnyAsync(m => m.SerialNumber == request.SerialNumber))
            return Conflict(new { message = $"Serial number '{request.SerialNumber}' is already in use." });

        var machine = new Machine
        {
            Name = request.Name,
            SerialNumber = request.SerialNumber,
            Type = request.Type,
            AssignedOperatorId = request.AssignedOperatorId,
        };

        db.Machines.Add(machine);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = machine.Id }, ToDto(machine));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateMachineRequest request)
    {
        var machine = await db.Machines.FindAsync(id);
        if (machine is null) return NotFound();

        machine.Name = request.Name;
        machine.Type = request.Type;
        machine.AssignedOperatorId = request.AssignedOperatorId;
        machine.Status = request.Status;

        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var machine = await db.Machines.FindAsync(id);
        if (machine is null) return NotFound();

        db.Machines.Remove(machine);
        await db.SaveChangesAsync();
        return NoContent();
    }

    private static MachineDto ToDto(Machine m) => new(
        m.Id, m.Name, m.SerialNumber, m.Type,
        m.AssignedOperatorId, m.AssignedOperator?.Name,
        m.CurrentLatitude, m.CurrentLongitude, m.LastSeenAt,
        m.Status, m.CreatedAt);
}
