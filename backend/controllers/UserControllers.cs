using backend.models;
using backend.Data;
using backend.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using backend.Services;  // ADD THIS - for FirstOrDefaultAsync

namespace backend.controllers;

[ApiController]
[Route("api/[controller]")]
public class UserControllers : ControllerBase  // ADDED : ControllerBase
{
    private readonly Database _context;
    private readonly JwtService _jwtService;

    public UserControllers(Database context, JwtService jwtService)
    {
        _context = context;
        _jwtService = jwtService;
    }

    [HttpPost("register")]
    public async Task<ActionResult> Register([FromBody] RegisterDto dto)  // ADDED [FromBody]
    {
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == dto.Email);

        if (existingUser != null)
        {
            return BadRequest("User already exists");
        }

        // Make sure you have BCrypt installed
        // Run: dotnet add package BCrypt.Net-Next
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

        var user = new User
        {
            Email = dto.Email,
            PasswordHash = passwordHash,
            Username = dto.Email,  // ADDED - required field
            Firstname = "",  // ADDED - required field (maybe from DTO)
            Lastname = ""    // ADDED - required field (maybe from DTO)
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "User registered successfully",
            userId = user.Id,
            email = user.Email,
        });
    }

    [HttpPost("login")]
    public async Task<ActionResult> Login([FromBody] LoginDto dto)
    {

        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == dto.Email);

        if (existingUser == null)
            return BadRequest("User does not exist");

        bool valid = BCrypt.Net.BCrypt.Verify(dto.Password, existingUser.PasswordHash);

        if (!valid)
        {
            return BadRequest("Invalid credentials");
        }

        var token = _jwtService.GenerateToken(existingUser);

        existingUser.LastLogin = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new
        {
            token,
            userId = existingUser.Id,
            email = existingUser.Email,
            usename = existingUser.Username
        });

    }
}