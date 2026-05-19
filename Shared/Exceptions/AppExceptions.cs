namespace CRM.Api.Shared.Exceptions;

/// <summary>404 - Cho phép tạo specific NotFound exceptions (UserNotFoundException, CustomerNotFoundException...)</summary>
public class NotFoundException : Exception
{
    public NotFoundException(string resource, string id)
        : base($"{resource} '{id}' không tìm thấy.") { }
    
    protected NotFoundException(string message) : base(message) { }
}

/// <summary>403 - Cho phép tạo specific Forbidden exceptions</summary>
public class ForbiddenException : Exception
{
    public ForbiddenException(string message = "Bạn không có quyền thực hiện thao tác này.")
        : base(message) { }
}

/// <summary>409 - Cho phép tạo specific Conflict exceptions</summary>
public class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }
}

/// <summary>400 - Cho phép tạo specific Validation exceptions</summary>
public class ValidationException : Exception
{
    public Dictionary<string, string[]> Errors { get; }

    public ValidationException(string field, string message) : base(message)
    {
        Errors = new Dictionary<string, string[]> { [field] = [message] };
    }

    public ValidationException(Dictionary<string, string[]> errors) : base("Dữ liệu không hợp lệ.")
    {
        Errors = errors;
    }
    
    protected ValidationException(string message, Dictionary<string, string[]> errors) : base(message)
    {
        Errors = errors;
    }
}

/// <summary>401 - Cho phép tạo specific Unauthorized exceptions</summary>
public class UnauthorizedException : Exception
{
    public UnauthorizedException(string message = "Phiên đăng nhập không hợp lệ.")
        : base(message) { }
}
