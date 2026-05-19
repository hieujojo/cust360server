namespace CRM.Api.Shared.Models;

/// <summary>Kết quả trả về từ Service layer có data.</summary>
public sealed class ServiceResult<T>
{
    public bool    IsSuccess    { get; private init; }
    public T?      Data         { get; private init; }
    public string? ErrorCode    { get; private init; }
    public string? ErrorMessage { get; private init; }

    public static ServiceResult<T> Ok(T data)
        => new() { IsSuccess = true, Data = data };

    public static ServiceResult<T> Fail(string errorCode, string errorMessage)
        => new() { IsSuccess = false, ErrorCode = errorCode, ErrorMessage = errorMessage };
}

/// <summary>Kết quả trả về từ Service layer không có data.</summary>
public sealed class ServiceResult
{
    public bool    IsSuccess    { get; private init; }
    public string? ErrorCode    { get; private init; }
    public string? ErrorMessage { get; private init; }

    public static ServiceResult Ok()
        => new() { IsSuccess = true };

    public static ServiceResult Fail(string errorCode, string errorMessage)
        => new() { IsSuccess = false, ErrorCode = errorCode, ErrorMessage = errorMessage };

    /// <summary>Convert Fail sang ServiceResult&lt;T&gt; để dùng trong validation helpers.</summary>
    public ServiceResult<T> ToTyped<T>()
        => ServiceResult<T>.Fail(ErrorCode!, ErrorMessage!);
}
