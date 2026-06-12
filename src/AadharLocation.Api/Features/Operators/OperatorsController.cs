using System.Text;
using AadharLocation.Api.Data;
using AadharLocation.Api.Domain.Entities;
using AadharLocation.Shared.DTOs;
using AadharLocation.Shared.DTOs.Activation;
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
    [HttpGet("export")]
    public async Task<IActionResult> Export()
    {
        var operators = await db.Operators
            .Include(o => o.AssignedMachine)
            .AsNoTracking()
            .OrderBy(o => o.Name)
            .Select(o => new
            {
                o.Name, o.Email, o.Phone, o.District,
                AssignedMachineName = o.AssignedMachine != null ? o.AssignedMachine.Name : string.Empty,
                o.Status
            })
            .ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("Name,Email,Phone,District,Assigned Machine,Status");

        foreach (var op in operators)
        {
            var name     = op.Name.Replace("\"", "\"\"");
            var email    = op.Email.Replace("\"", "\"\"");
            var phone    = (op.Phone    ?? string.Empty).Replace("\"", "\"\"");
            var district = (op.District ?? string.Empty).Replace("\"", "\"\"");
            var machine  = op.AssignedMachineName.Replace("\"", "\"\"");
            sb.AppendLine($"\"{name}\",\"{email}\",\"{phone}\",\"{district}\",\"{machine}\",\"{op.Status}\"");
        }

        var fileName = $"operators_{DateTime.Now:yyyyMMddHHmmss}.csv";
        return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", fileName);
    }

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
                o.Email.ToLower().Contains(s) ||
                (o.District != null && o.District.ToLower().Contains(s)));
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(o => o.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new OperatorDto(
                o.Id, o.Name, o.Email, o.Phone, o.District,
                o.AssignedMachineId, o.AssignedMachine != null ? o.AssignedMachine.Name : null,
                o.Status, o.CreatedAt, o.UpdatedAt, o.LastLoginAt, o.IsDeleted))
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
            op.Id, op.Name, op.Email, op.Phone, op.District,
            op.AssignedMachineId, op.AssignedMachine?.Name,
            op.Status, op.CreatedAt, op.UpdatedAt, op.LastLoginAt, op.IsDeleted));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOperatorRequest request)
    {
        if (await db.Operators.AnyAsync(o => o.Email == request.Email.ToLower()))
            return Conflict(new { message = $"Email '{request.Email}' is already in use." });

        var op = new Operator
        {
            Name = request.Name,
            Email = request.Email.ToLower(),
            Phone = request.Phone,
            District = request.District,
            PasswordHash = request.TrackerPassword is not null
                ? BCrypt.Net.BCrypt.HashPassword(request.TrackerPassword)
                : null,
        };

        db.Operators.Add(op);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = op.Id },
            new OperatorDto(op.Id, op.Name, op.Email, op.Phone, op.District,
                null, null, op.Status, op.CreatedAt, null, null, false));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateOperatorRequest request)
    {
        var op = await db.Operators.FindAsync(id);
        if (op is null) return NotFound();

        if (await db.Operators.AnyAsync(o => o.Email == request.Email.ToLower() && o.Id != id))
            return Conflict(new { message = $"Email '{request.Email}' is already in use." });

        op.Name = request.Name;
        op.Email = request.Email.ToLower();
        op.Phone = request.Phone;
        op.District = request.District;
        op.Status = request.Status;
        op.UpdatedAt = DateTime.UtcNow;

        if (request.NewTrackerPassword is not null)
            op.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewTrackerPassword);

        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{id:int}/generate-uninstall-code")]
    public async Task<IActionResult> GenerateUninstallCode(int id)
    {
        if (!await db.Operators.AnyAsync(o => o.Id == id))
            return NotFound();

        var activation = await db.TrackerActivations
            .FirstOrDefaultAsync(t => t.OperatorId == id && t.IsActive);

        if (activation is null)
            return BadRequest(new { message = "Operator has no active device. They must log in to the tracker app first." });

        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var code = new string(Enumerable.Range(0, 6)
            .Select(_ => chars[Random.Shared.Next(chars.Length)])
            .ToArray());

        activation.UninstallCodeHash = BCrypt.Net.BCrypt.HashPassword(code);
        await db.SaveChangesAsync();

        return Ok(new GenerateUninstallCodeResponse(code));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var op = await db.Operators.FindAsync(id);
        if (op is null) return NotFound();

        op.IsDeleted = true;
        op.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return NoContent();
    }
}
