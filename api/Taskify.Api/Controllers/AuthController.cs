using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Taskify.Api.Data;
using Taskify.Api.Models;
using Taskify.Api.Dtos;
using BCrypt.Net;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _config;
    private readonly IMapper _mapper;

    public AuthController(AppDbContext context, IConfiguration config, IMapper mapper)
    {
        _context = context;
        _config = config;
        _mapper = mapper;
    }

    [HttpPost("register")]
    [AllowAnonymous] // Allow registration without authentication
    public IActionResult Register(CreateUserDto dto)
    {
        if (_context.Users.Any(u => u.Email == dto.Email))
            return BadRequest("Email already exists");

        var user = new Users
        {
            Username = dto.Username,
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = dto.Role
        };

        _context.Users.Add(user);
        _context.SaveChanges();

        return Ok(new { message = "User registered successfully" });
    }

    [HttpPost("login")]
    [AllowAnonymous] // Allow login without authentication
    public IActionResult Login(LoginDto dto)
    {
        var user = _context.Users.SingleOrDefault(u => u.Email == dto.Email);
        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            return Unauthorized("Invalid credentials");

        var token = GenerateJwtToken(user);
        return Ok(new
        {
            token,
            expiresAt = DateTime.UtcNow.AddMinutes(Convert.ToDouble(_config["Jwt:ExpireMinutes"])),
            user = new { user.Id, user.Username, user.Email, user.Role }
        });
    }

    private string GenerateJwtToken(Users user)
    {
        var claims = new[]
        {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),   // user id
        new Claim(ClaimTypes.Name, user.Username),                  // username
        new Claim(ClaimTypes.Email, user.Email),                    // email
        new Claim(ClaimTypes.Role, user.Role)                       // role
    };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"] ?? throw new InvalidOperationException("JWT key missing")));
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

    // GET: api/auth/users -> returns users for current user; admin sees all
    [HttpGet("users")]
    [Authorize]
    public async Task<IActionResult> GetUsers()
    {
        try
        {
            var userId = GetCurrentUserId();
            var isAdmin = User.IsInRole("Admin") || User.IsInRole("admin");

            var query = _context.Users.AsQueryable();

            if (!isAdmin)
                query = query.Where(u => u.Id == userId);

            var list = await query.OrderByDescending(u => u.CreatedAt).ToListAsync();
            var dto = _mapper.Map<IEnumerable<UserDto>>(list);
            return Ok(dto);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized("Invalid user token");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    // GET: api/auth/users/{id}
    [HttpGet("users/{id}")]
    [Authorize]
    public async Task<IActionResult> GetUser(int id)
    {
        try
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            var userId = GetCurrentUserId();
            var isAdmin = User.IsInRole("Admin") || User.IsInRole("admin");
            if (!isAdmin && user.Id != userId)
                return NotFound(); // don't reveal existence

            return Ok(_mapper.Map<UserDto>(user));
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized("Invalid user token");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    // PUT: api/auth/users/{id}
    [HttpPut("users/{id}")]
    [Authorize]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] CreateUserDto dto)
    {
        try
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            var userId = GetCurrentUserId();
            var isAdmin = User.IsInRole("Admin") || User.IsInRole("admin");
            if (!isAdmin && user.Id != userId)
                return Forbid();

            // Check if email is being changed and if it already exists
            if (dto.Email != user.Email && _context.Users.Any(u => u.Email == dto.Email))
                return BadRequest("Email already exists");

            // Update user properties
            user.Username = dto.Username;
            user.Email = dto.Email;
            user.Role = dto.Role;

            // Only update password if provided
            if (!string.IsNullOrWhiteSpace(dto.Password))
            {
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
            }

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized("Invalid user token");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    // DELETE: api/auth/users/{id} -> only Admins
    [HttpDelete("users/{id}")]
    [Authorize(Roles = "Admin,admin")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        try
        {
            // Debug: Log current user info
            var currentUserId = GetCurrentUserId();
            var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;
            Console.WriteLine($"DELETE User - Current User ID: {currentUserId}, Role: {currentUserRole}");
            
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            // Prevent deleting the last admin
            if (user.Role == "Admin" || user.Role == "admin")
            {
                var adminCount = await _context.Users.CountAsync(u => u.Role == "Admin" || u.Role == "admin");
                if (adminCount <= 1)
                    return BadRequest("Cannot delete the last admin user");
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    private int GetCurrentUserId()
    {
        var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(idClaim, out var id)) return id;
        throw new UnauthorizedAccessException("Invalid user claim");
    }

}
