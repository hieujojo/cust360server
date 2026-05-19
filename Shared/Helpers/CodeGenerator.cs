namespace CRM.Api.Shared.Helpers;

/// <summary>
/// Auto-generate mã nghiệp vụ theo format PREFIX-YYYY-NNNN.
/// Dùng chung cho: EmployeeCode (NV), CustomerCode (KH), DealCode (DL).
/// </summary>
public static class CodeGenerator
{
    /// <summary>VD: NV-2025-0042</summary>
    public static string Employee(long sequence)
        => Generate("NV", sequence);

    /// <summary>VD: KH-2025-0042</summary>
    public static string Customer(long sequence)
        => Generate("KH", sequence);

    /// <summary>VD: DL-2025-0042</summary>
    public static string Deal(long sequence)
        => Generate("DL", sequence);

    /// <summary>Generate với prefix tùy chỉnh.</summary>
    public static string Generate(string prefix, long sequence, int padWidth = 4)
        => $"{prefix}-{DateTime.UtcNow.Year}-{sequence:D4}";
}
