using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

namespace EquipmentAccounting.Core.Diagnostics;

/// <summary>
/// Шаг вычисления математического ряда Варианта 4: S = Σ [(-1)^(i-1) / i²].
/// </summary>
public sealed record SeriesStep(int Step, int Sign, double Denominator, double Term, double AccumulatedSum);

/// <summary>
/// Шаг амортизации оборудования.
/// </summary>
public sealed record DepreciationStep(int Year, decimal StartCost, decimal DepreciationAmount, decimal EndCost, decimal CostPerHour);

/// <summary>
/// Сервис диагностики, математических расчетов и демонстрации отладки для ПР №10-11 (Вариант 4).
/// </summary>
public sealed class Variant4DiagnosticsService
{
    /// <summary>
    /// Теоретический предел ряда Варианта 4: π² / 12 ≈ 0.8224670334241132
    /// </summary>
    public const double TheoreticalLimit = Math.PI * Math.PI / 12.0;

    /// <summary>
    /// Флаг конфигурации сборки для демонстрации отличий Debug и Release.
    /// </summary>
    public static bool IsDebugBuild
    {
        get
        {
#if DEBUG
            return true;
#else
            return false;
#endif
        }
    }

    /// <summary>
    /// Расчет знакочередующегося ряда Варианта 4 с итерациями.
    /// Предназначен для демонстрации точек останова, пошаговой отладки и мониторинга переменных в Autos, Locals и Watch.
    /// </summary>
    /// <param name="n">Количество итераций (1..1 000 000).</param>
    /// <param name="onStep">Необязательный обратный вызов для отслеживания промежуточных шагов.</param>
    /// <returns>Список шагов вычисления и итоговая сумма.</returns>
    public (List<SeriesStep> Steps, double FinalSum) CalculateVariant4Series(int n, Action<SeriesStep>? onStep = null)
    {
        if (n < 1 || n > 1_000_000)
        {
            throw new ArgumentOutOfRangeException(
                nameof(n),
                n,
                "Число итераций n должно находиться в диапазоне от 1 до 1 000 000.");
        }

        Debug.WriteLine($"[DEBUG] Запуск расчета ряда Варианта 4 для n={n}...");

        var steps = new List<SeriesStep>(Math.Min(n, 1000));
        double sum = 0.0;

        // Цикл итерационного вычисления суммы ряда
        for (int i = 1; i <= n; i++)
        {
            // Точка останова (F9) рекомендуется здесь для проверки переменных i, sign, denominator, term, sum
            int sign = (i % 2 == 1) ? 1 : -1;
            double denominator = (double)i * i; // Защита от переполнения int: каст к double
            double term = sign / denominator;
            sum += term;

            var currentStep = new SeriesStep(i, sign, denominator, term, sum);

            // Сохраняем первые 1000 строк для отображения в таблице
            if (i <= 1000)
            {
                steps.Add(currentStep);
            }

            onStep?.Invoke(currentStep);

#if DEBUG
            if (i <= 5 || i == n)
            {
                Debug.WriteLine($"[DEBUG] Шаг {i}: sign={sign}, denom={denominator}, term={term:F6}, sum={sum:F6}");
            }
#endif
        }

        Debug.WriteLine($"[DEBUG] Расчет завершен. Итоговая сумма: {sum:F10}, расхождение с теорией: {Math.Abs(sum - TheoreticalLimit):E4}");
        return (steps, sum);
    }

    /// <summary>
    /// Расчет амортизационной стоимости оборудования по годам.
    /// Демонстрирует циклы, расчеты с типом decimal и проверку граничных условий.
    /// </summary>
    public List<DepreciationStep> CalculateDepreciation(decimal initialCost, int years, decimal salvageValue, int annualHours)
    {
        if (initialCost < 0)
            throw new ArgumentOutOfRangeException(nameof(initialCost), "Начальная стоимость не может быть отрицательной.");

        if (years <= 0)
            throw new ArgumentOutOfRangeException(nameof(years), "Срок эксплуатации должен быть не менее 1 года.");

        if (salvageValue < 0 || salvageValue > initialCost)
            throw new EquipmentDiagnosticsException(
                $"Ликвидационная стоимость ({salvageValue:C}) не может превышать начальную стоимость ({initialCost:C}) или быть отрицательной.",
                "ERR_SALVAGE_INVALID");

        if (annualHours == 0)
            throw new DivideByZeroException("Годовой фонд рабочего времени оборудования не может быть равен нулю (деление на ноль).");

        var steps = new List<DepreciationStep>();
        decimal currentCost = initialCost;
        decimal depreciableBase = initialCost - salvageValue;
        decimal annualRate = 2.0m / years; // Метод уменьшаемого остатка с коэффициентом 2 (ускоренная амортизация)

        for (int year = 1; year <= years; year++)
        {
            decimal depreciationAmount = (year == years)
                ? (currentCost - salvageValue)
                : Math.Min(currentCost * annualRate, currentCost - salvageValue);

            if (depreciationAmount < 0) depreciationAmount = 0;

            decimal endCost = currentCost - depreciationAmount;

            // Расчет стоимости часа работы
            decimal costPerHour = depreciationAmount / annualHours;

            steps.Add(new DepreciationStep(year, currentCost, depreciationAmount, endCost, costPerHour));
            currentCost = endCost;
        }

        return steps;
    }

    /// <summary>
    /// Метод принудительного моделирования типовых ошибок для демонстрации окон Exception Settings и обработки catch.
    /// </summary>
    public void SimulateError(string errorType)
    {
        switch (errorType?.ToLowerInvariant())
        {
            case "divide_by_zero":
            case "dividebyzero":
                int zero = 0;
                _ = 42 / zero;
                break;

            case "format":
            case "formatexception":
                _ = int.Parse("не_число_abc", CultureInfo.InvariantCulture);
                break;

            case "range":
            case "argumentoutofrange":
                throw new ArgumentOutOfRangeException("simulationParam", -99, "Значение параметра выходит за пределы допустимого диапазона (1..1000000).");

            case "overflow":
            case "overflowexception":
                checked
                {
                    int max = int.MaxValue;
                    _ = max + 100;
                }
                break;

            case "custom":
            case "equipmentdiagnostics":
                throw new EquipmentDiagnosticsException("Критический сбой инвентаризационного модуля: датчик оборудования не отвечает.", "ERR_SENSOR_TIMEOUT");

            default:
                throw new InvalidOperationException($"Неизвестный сценарий моделирования ошибки: '{errorType}'.");
        }
    }

    /// <summary>
    /// Комплексный сценарий диагностики с полным блоком try-catch-finally.
    /// Фиксирует ход выполнения и результат каждого блока в журнале executionLog.
    /// </summary>
    public bool ExecuteDiagnosticsWorkflow(string inputN, string scenario, List<string> executionLog)
    {
        ArgumentNullException.ThrowIfNull(executionLog);

        bool success = false;
        executionLog.Add($"[{DateTime.Now:HH:mm:ss}] Начало выполнения диагностического сценария '{scenario}' (вход: '{inputN}').");

        try
        {
            executionLog.Add("[try] Вход в защищенный блок try.");

            if (scenario.Equals("simulate_error", StringComparison.OrdinalIgnoreCase))
            {
                executionLog.Add($"[try] Моделирование исключения типа: {inputN}");
                SimulateError(inputN);
            }
            else
            {
                executionLog.Add("[try] Парсинг входного параметра N...");
                if (!int.TryParse(inputN, NumberStyles.Integer, CultureInfo.InvariantCulture, out int n))
                {
                    throw new FormatException($"Значение '{inputN}' не является корректным целым числом.");
                }

                executionLog.Add($"[try] Вызов CalculateVariant4Series(n={n})...");
                var (steps, finalSum) = CalculateVariant4Series(n);
                executionLog.Add($"[try] Вычисление успешно завершено! Шагов: {steps.Count}, Сумма: {finalSum:F10}");
            }

            success = true;
            executionLog.Add("[try] Завершение блока try без непойманных ошибок.");
        }
        catch (FormatException ex)
        {
            executionLog.Add($"[catch FormatException] Перехвачена ошибка формата данных: {ex.Message}");
            Debug.WriteLine($"[CATCH] FormatException: {ex}");
        }
        catch (ArgumentOutOfRangeException ex)
        {
            executionLog.Add($"[catch ArgumentOutOfRangeException] Параметр вне диапазона: {ex.Message}");
            Debug.WriteLine($"[CATCH] ArgumentOutOfRangeException: {ex}");
        }
        catch (DivideByZeroException ex)
        {
            executionLog.Add($"[catch DivideByZeroException] Деление на ноль: {ex.Message}");
            Debug.WriteLine($"[CATCH] DivideByZeroException: {ex}");
        }
        catch (OverflowException ex)
        {
            executionLog.Add($"[catch OverflowException] Переполнение числового диапазона: {ex.Message}");
            Debug.WriteLine($"[CATCH] OverflowException: {ex}");
        }
        catch (EquipmentDiagnosticsException ex)
        {
            executionLog.Add($"[catch EquipmentDiagnosticsException] Ошибка оборудования [{ex.DiagnosticCode}]: {ex.Message}");
            Debug.WriteLine($"[CATCH] EquipmentDiagnosticsException: {ex}");
        }
        catch (Exception ex)
        {
            executionLog.Add($"[catch Общее исключение ({ex.GetType().Name})] {ex.Message}");
            Debug.WriteLine($"[CATCH] Exception: {ex}");
        }
        finally
        {
            // Блок finally выполняется ВСЕГДА: и при успешном выполнении, и при возникновении исключения.
            executionLog.Add("[finally] Блок finally выполнен: ресурсы освобождены, флаги сброшены, транзакция закрыта.");
            Debug.WriteLine("[FINALLY] Блок finally завершил работу.");
        }

        executionLog.Add($"[{DateTime.Now:HH:mm:ss}] Завершение сценария. Результат: {(success ? "УСПЕХ" : "ОШИБКА ОБРАБОТАНА")}.");
        return success;
    }
}
