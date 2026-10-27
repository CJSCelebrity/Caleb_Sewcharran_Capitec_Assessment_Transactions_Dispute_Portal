using System;
using System.ComponentModel.DataAnnotations;

namespace CapitecDisputes.Application.Contracts;

public sealed record LoginRequest(
    [Required][EmailAddress][StringLength(256)] string Email,
    [Required][StringLength(128, MinimumLength = 1)] string Password);

public sealed record LoginResponse(
    string AccessToken,
    DateTimeOffset ExpiresAtUtc,
    string Email,
    string Role);
