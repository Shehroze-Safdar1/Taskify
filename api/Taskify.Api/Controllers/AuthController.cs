using AutoMapper;
using BCrypt.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Taskify.Api.Data;
using Taskify.Api.Dtos;
using Taskify.Api.Models;
using Taskify.Api.Services;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _config;
    private readonly IMapper _mapper;
    private readonly IActivityLogService _logService;

    public AuthController(AppDbContext context, IConfiguration config, IMapper mapper, IActivityLogService logService)
    {
        _context = context;
        _config = config;
        _mapper = mapper;
        _logService = logService;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register(CreateUserDto dto)
    {
        if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
            return BadRequest("Email already exists");

<<<<<<< HEAD
        // 🔹 Use AutoMapper to map dto -> entity
=======
        // Map DTO → Entity
>>>>>>> bade0adab4088872b4a7b8f4325dd25155f790b4
        var user = _mapper.Map<Users>(dto);
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

<<<<<<< HEAD
        // ✅ Log with service
=======
        // Log activity: EntityType = "User", EntityId = newly created user ID
>>>>>>> bade0adab4088872b4a7b8f4325dd25155f790b4
        await _logService.LogAsync("User", user.Id, "Register", user.Id);

        return Ok(new { message = "User registered successfully" });
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var user = await _context.Users.SingleOrDefaultAsync(u => u.Email == dto.Email);
        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            return Unauthorized("Invalid credentials");

        var token = GenerateJwtToken(user);

        // Log activity: login action
        await _logService.LogAsync("User", user.Id, "Login", user.Id);

        return Ok(new
        {
            token,
            expiresAt = DateTime.UtcNow.AddMinutes(Convert.ToDouble(_config["Jwt:ExpireMinutes"])),
<<<<<<< HEAD
            user = _mapper.Map<UserDto>(user) // 🔹 Map entity -> DTO
=======
            user = _mapper.Map<UserDto>(user)
>>>>>>> bade0adab4088872b4a7b8f4325dd25155f790b4
        });
    }

    [HttpGet("users")]
    [Authorize]
    public async Task<IActionResult> GetUsers()
    {
        var userId = GetCurrentUserId();
        var isAdmin = User.IsInRole("Admin") || User.IsInRole("admin");

        var query = _context.Users.AsQueryable();
        if (!isAdmin) query = query.Where(u => u.Id == userId);

        var list = await query.OrderByDescending(u => u.CreatedAt).ToListAsync();

        // Log activity: viewing users
        await _logService.LogAsync("User", 0, "Viewed list", userId);

        return Ok(_mapper.Map<IEnumerable<UserDto>>(list));
    }

    [HttpGet("users/{id}")]
    [Authorize]
    public async Task<IActionResult> GetUser(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return NotFound();

        var currentUserId = GetCurrentUserId();
        var isAdmin = User.IsInRole("Admin") || User.IsInRole("admin");
        if (!isAdmin && user.Id != currentUserId) return Forbid();

        // Log activity: viewing single user
        await _logService.LogAsync("User", id, "Viewed", currentUserId);

        return Ok(_mapper.Map<UserDto>(user));
    }

    [HttpPut("users/{id}")]
    [Authorize]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] CreateUserDto dto)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return NotFound();

        var currentUserId = GetCurrentUserId();
        var isAdmin = User.IsInRole("Admin") || User.IsInRole("admin");
        if (!isAdmin && user.Id != currentUserId) return Forbid();

        if (dto.Email != user.Email && await _context.Users.AnyAsync(u => u.Email == dto.Email))
            return BadRequest("Email already exists");

        _mapper.Map(dto, user);
        if (!string.IsNullOrWhiteSpace(dto.Password))
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

        _context.Users.Update(user);
        await _context.SaveChangesAsync();

        await _logService.LogAsync("User", id, "Update", currentUserId);

        return NoContent();
    }

    [HttpDelete("users/{id}")]
    [Authorize(Roles = "Admin,admin")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var currentUserId = GetCurrentUserId();
        var user = await _context.Users.FindAsync(id);
        if (user == null) return NotFound();

        if (user.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
        {
            var adminCount = await _context.Users.CountAsync(u => u.Role == "Admin" || u.Role == "admin");
            if (adminCount <= 1)
                return BadRequest("Cannot delete the last admin user");
        }

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        await _logService.LogAsync("User", id, "Delete", currentUserId);

        return NoContent();
    }

    private string GenerateJwtToken(Users user)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(Convert.ToDouble(_config["Jwt:ExpireMinutes"])),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

<<<<<<< HEAD
    [HttpGet("users")]
    [Authorize]
    public async Task<IActionResult> GetUsers()
    {
        var userId = GetCurrentUserId();
        var isAdmin = User.IsInRole("Admin") || User.IsInRole("admin");

        var query = _context.Users.AsQueryable();
        if (!isAdmin)
            query = query.Where(u => u.Id == userId);

        var list = await query.OrderByDescending(u => u.CreatedAt).ToListAsync();
        return Ok(_mapper.Map<IEnumerable<UserDto>>(list));
    }

    [HttpGet("users/{id}")]
    [Authorize]
    public async Task<IActionResult> GetUser(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return NotFound();

        var userId = GetCurrentUserId();
        var isAdmin = User.IsInRole("Admin") || User.IsInRole("admin");
        if (!isAdmin && user.Id != userId)
            return Forbid();

        return Ok(_mapper.Map<UserDto>(user));
    }

    [HttpPut("users/{id}")]
    [Authorize]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] CreateUserDto dto)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return NotFound();

        var currentUserId = GetCurrentUserId();
        var isAdmin = User.IsInRole("Admin") || User.IsInRole("admin");
        if (!isAdmin && user.Id != currentUserId)
            return Forbid();

        if (dto.Email != user.Email && await _context.Users.AnyAsync(u => u.Email == dto.Email))
            return BadRequest("Email already exists");

        // 🔹 Map dto -> entity (except password)
        _mapper.Map(dto, user);
        if (!string.IsNullOrWhiteSpace(dto.Password))
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

        _context.Users.Update(user);
        await _context.SaveChangesAsync();

        await _logService.LogAsync("User", user.Id, "Update", currentUserId);

        return NoContent();
    }

    [HttpDelete("users/{id}")]
    [Authorize(Roles = "Admin,admin")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var currentUserId = GetCurrentUserId();
        var user = await _context.Users.FindAsync(id);
        if (user == null) return NotFound();

        if (user.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
        {
            var adminCount = await _context.Users.CountAsync(u => u.Role == "Admin" || u.Role == "admin");
            if (adminCount <= 1)
                return BadRequest("Cannot delete the last admin user");
        }

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        await _logService.LogAsync("User", user.Id, "Delete", currentUserId);

        return NoContent();
    }

=======
>>>>>>> bade0adab4088872b4a7b8f4325dd25155f790b4
    private int GetCurrentUserId()
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(idClaim, out var id)) return id;
        throw new UnauthorizedAccessException("Invalid user claim");
    }
}
