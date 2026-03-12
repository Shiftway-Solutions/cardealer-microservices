namespace AuthService.Shared;

/// <summary>
/// Generic wrapper for API responses
/// </summary>
/// <typeparam name="T">Type of the data payload</typeparam>
public class ApiResponse<T>
{
    /// <summary>Indicates if the operation was successful</summary>
    public bool Success { get; set; }

    /// <summary>Data returned when Success is true</summary>
    public T? Data { get; set; }

    /// <summary>Error message when Success is false</summary>
    public string? Error { get; set; }

    /// <summary>Additional metadata about the response</summary>
    public Dictionary<string, object>? Metadata { get; set; }

    /// <summary>Timestamp when the response was generated</summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>Creates a successful response with the provided data</summary>
    public static ApiResponse<T> Ok(T data, Dictionary<string, object>? metadata = null) =>
        new()
        {
            Success = true,
            Data = data,
            Metadata = metadata
        };

    /// <summary>Creates a failed response with the provided error message</summary>
    public static ApiResponse<T> Fail(string errorMessage, Dictionary<string, object>? metadata = null) =>
        new()
        {
            Success = false,
            Error = errorMessage,
            Metadata = metadata
        };

    /// <summary>Creates a failed response with validation errors</summary>
    public static ApiResponse<T> ValidationFail(Dictionary<string, string[]> errors) =>
        new()
        {
            Success = false,
            Error = "Validation failed",
            Metadata = new Dictionary<string, object> { ["validationErrors"] = errors }
        };
}

/// <summary>
/// Non-generic API response for void operations
/// </summary>
public class ApiResponse
{
    /// <summary>Indicates if the operation was successful</summary>
    public bool Success { get; set; }

    /// <summary>Error message when Success is false</summary>
    public string? Error { get; set; }

    /// <summary>Additional metadata about the response</summary>
    public Dictionary<string, object>? Metadata { get; set; }

    /// <summary>Timestamp when the response was generated</summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>Creates a successful response</summary>
    public static ApiResponse Ok(Dictionary<string, object>? metadata = null) =>
        new()
        {
            Success = true,
            Metadata = metadata
        };

    /// <summary>Creates a failed response with the provided error message</summary>
    public static ApiResponse Fail(string errorMessage, Dictionary<string, object>? metadata = null) =>
        new()
        {
            Success = false,
            Error = errorMessage,
            Metadata = metadata
        };
}
