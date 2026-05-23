namespace Learnify.Application;

/// <summary>
/// Generic API response envelope for consistent response formatting.
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
    public List<string>? Errors { get; set; }

    public static ApiResponse<T> Ok(T data, string? message = null)
        => new() { Success = true, Data = data, Message = message };

    public static ApiResponse<T> NotFound(string message)
        => new() { Success = false, Message = message };

    public static ApiResponse<T> BadRequest(List<string> errors)
        => new() { Success = false, Errors = errors };

    public static ApiResponse<T> BadRequest(string message)
        => new() { Success = false, Message = message };
}

/// <summary>
/// Non-generic API response for operations without data.
/// </summary>
public class ApiResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }

    public static ApiResponse Ok(object? data, string? message = null)
        => new() { Success = true, Message = message };

    public static ApiResponse Ok(string? message = null)
        => new() { Success = true, Message = message };

    public static ApiResponse NotFound(string message)
        => new() { Success = false, Message = message };

    public static ApiResponse BadRequest(string message)
        => new() { Success = false, Message = message };
}