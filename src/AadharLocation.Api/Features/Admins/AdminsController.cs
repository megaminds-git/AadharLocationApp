using System.Security.Claims;
using AadharLocation.Api.Data;
using AadharLocation.Api.Domain.Entities;
using AadharLocation.Shared.DTOs.Admins;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AadharLocation.Api.Features.Admins;

[ApiController]
[Route("api/admins")]
[Authorize(Roles = "Admin")]
public class AdminsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var admins = await db.Users
            .AsNoTracking()
            .OrderBy(u => u.Name)
            .Select(u => new AdminDto(u.Id, u.Name, u.Email, u.CreatedAt))
            .ToListAsync();

        return Ok(admins);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
        if (user is null) return NotFound();
        return Ok(new AdminDto(user.Id, user.Name, user.Email, user.CreatedAt));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAdminRequest request)
    {
        if (await db.Users.AnyAsync(u => u.Email == request.Email.ToLower()))
            return Conflict(new { message = $"Email '{request.Email}' is already in use." });

        var user = new User
        {
            Name         = request.Name,
            Email        = request.Email.ToLower(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role         = "Admin",
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = user.Id },
            new AdminDto(user.Id, user.Name, user.Email, user.CreatedAt));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateAdminRequest request)
    {
        var user = await db.Users.FindAsync(id);
        if (user is null) return NotFound();

        if (await db.Users.AnyAsync(u => u.Email == request.Email.ToLower() && u.Id != id))
            return Conflict(new { message = $"Email '{request.Email}' is already in use." });

        user.Name  = request.Name;
        user.Email = request.Email.ToLower();

        if (!string.IsNullOrWhiteSpace(request.NewPassword))
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);

        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var callerIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (int.TryParse(callerIdClaim, out var callerId) && callerId == id)
            return StatusCode(403, new { message = "You cannot delete your own account." });

        var user = await db.Users.FindAsync(id);
        if (user is null) return NotFound();

        db.Users.Remove(user);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
