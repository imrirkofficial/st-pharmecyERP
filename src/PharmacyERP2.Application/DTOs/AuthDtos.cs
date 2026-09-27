namespace PharmacyERP2.Application.DTOs;

public record LoginRequest(string Username, string Password);
public record LoginResponse(string Token, string Username, string FullName, string Role, DateTime ExpiresAt);
public record MeResponse(string Username, string FullName, string Role, string? Email);
