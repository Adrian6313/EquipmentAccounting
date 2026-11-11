using System;

namespace EquipmentAccounting.Core.Diagnostics;

/// <summary>
/// Пользовательское исключение предметной области для демонстрации отладки и обработки ошибок в ПР №10-11 (Вариант 4).
/// </summary>
public class EquipmentDiagnosticsException : Exception
{
    public string? DiagnosticCode { get; }
    public DateTime ErrorTimestamp { get; }

    public EquipmentDiagnosticsException() : base("Произошла ошибка диагностики оборудования.")
    {
        DiagnosticCode = "ERR_DIAG_GENERIC";
        ErrorTimestamp = DateTime.UtcNow;
    }

    public EquipmentDiagnosticsException(string message) : base(message)
    {
        DiagnosticCode = "ERR_DIAG_CUSTOM";
        ErrorTimestamp = DateTime.UtcNow;
    }

    public EquipmentDiagnosticsException(string message, string diagnosticCode) : base(message)
    {
        DiagnosticCode = diagnosticCode;
        ErrorTimestamp = DateTime.UtcNow;
    }

    public EquipmentDiagnosticsException(string message, Exception innerException) : base(message, innerException)
    {
        DiagnosticCode = "ERR_DIAG_INNER";
        ErrorTimestamp = DateTime.UtcNow;
    }
}
