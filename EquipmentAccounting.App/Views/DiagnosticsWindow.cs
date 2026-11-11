using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using EquipmentAccounting.Core.Diagnostics;

namespace EquipmentAccounting.App.Views;

/// <summary>
/// Окно демонстрации отладки модулей и обработки исключений для ПР №10-11 (Вариант 4).
/// </summary>
public sealed class DiagnosticsWindow : Window
{
    private readonly Variant4DiagnosticsService _diagnosticsService = new();
    private readonly TextBox _inputN;
    private readonly DataGrid _grid;
    private readonly ListBox _logBox;
    private readonly TextBlock _resultText;
    private readonly TextBlock _buildConfigText;

    public DiagnosticsWindow()
    {
        Title = "ПР №10-11: Отладка отдельных модулей и обработка исключений (Вариант 4)";
        Width = 980;
        Height = 680;
        MinHeight = 550;
        MinWidth = 800;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = new SolidColorBrush(Color.FromRgb(244, 246, 248));

        var mainGrid = new Grid();
        mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Заголовок
        mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Панель управления
        mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Таблица и лог
        mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Статус

        // 1. Заголовок
        var headerBorder = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(36, 92, 154)),
            Padding = new Thickness(20, 14, 20, 14)
        };
        var headerPanel = new StackPanel();
        headerPanel.Children.Add(new TextBlock
        {
            Text = "Отладка модулей и обработка исключений — Вариант 4",
            FontSize = 20,
            FontWeight = FontWeights.Bold,
            Foreground = Brushes.White
        });
        headerPanel.Children.Add(new TextBlock
        {
            Text = "Студент: Чайковский А. А. | Дисциплина: ИСРПО | Формула: S = Σ [(-1)^(i-1) / i²] | Точки останова, Autos/Locals/Watch, try-catch-finally",
            FontSize = 12,
            Foreground = new SolidColorBrush(Color.FromRgb(220, 235, 252)),
            Margin = new Thickness(0, 4, 0, 0)
        });
        headerBorder.Child = headerPanel;
        Grid.SetRow(headerBorder, 0);
        mainGrid.Children.Add(headerBorder);

        // 2. Панель управления
        var controlPanel = new StackPanel { Margin = new Thickness(16, 12, 16, 8) };

        var row1 = new WrapPanel { Margin = new Thickness(0, 0, 0, 8) };
        row1.Children.Add(new TextBlock
        {
            Text = "Число итераций N:",
            VerticalAlignment = VerticalAlignment.Center,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 8, 0)
        });
        _inputN = new TextBox
        {
            Text = "10",
            Width = 100,
            Height = 30,
            VerticalContentAlignment = VerticalAlignment.Center,
            Padding = new Thickness(6, 0, 6, 0),
            Margin = new Thickness(0, 0, 12, 0)
        };
        row1.Children.Add(_inputN);

        var btnCalculate = CreateStyledButton("Рассчитать ряд (F9 / Watch)", Color.FromRgb(36, 92, 154));
        btnCalculate.Click += (_, _) => OnCalculateSeriesClick();
        row1.Children.Add(btnCalculate);

        var btnDepreciation = CreateStyledButton("Амортизация оборудования", Color.FromRgb(24, 112, 90));
        btnDepreciation.Click += (_, _) => OnCalculateDepreciationClick();
        row1.Children.Add(btnDepreciation);

        var btnClear = CreateStyledButton("Очистить журнал", Color.FromRgb(108, 117, 125));
        btnClear.Click += (_, _) => OnClearClick();
        row1.Children.Add(btnClear);

        controlPanel.Children.Add(row1);

        // Кнопки тестирования исключений
        var row2 = new WrapPanel();
        row2.Children.Add(new TextBlock
        {
            Text = "Моделирование исключений (try-catch-finally):",
            VerticalAlignment = VerticalAlignment.Center,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 8, 0)
        });

        var btnDivZero = CreateStyledButton("Деление на ноль (DivideByZero)", Color.FromRgb(180, 40, 40));
        btnDivZero.Click += (_, _) => OnSimulateExceptionClick("divide_by_zero");
        row2.Children.Add(btnDivZero);

        var btnFormat = CreateStyledButton("Ошибка формата (Format)", Color.FromRgb(200, 90, 20));
        btnFormat.Click += (_, _) => OnSimulateExceptionClick("format");
        row2.Children.Add(btnFormat);

        var btnRange = CreateStyledButton("Выход из диапазона (Range)", Color.FromRgb(160, 60, 120));
        btnRange.Click += (_, _) => OnSimulateExceptionClick("range");
        row2.Children.Add(btnRange);

        var btnOverflow = CreateStyledButton("Переполнение (Overflow)", Color.FromRgb(140, 40, 160));
        btnOverflow.Click += (_, _) => OnSimulateExceptionClick("overflow");
        row2.Children.Add(btnOverflow);

        var btnCustom = CreateStyledButton("Пользовательское исключение", Color.FromRgb(166, 71, 71));
        btnCustom.Click += (_, _) => OnSimulateExceptionClick("custom");
        row2.Children.Add(btnCustom);

        controlPanel.Children.Add(row2);
        Grid.SetRow(controlPanel, 1);
        mainGrid.Children.Add(controlPanel);

        // 3. Рабочая область (таблица + журнал)
        var contentGrid = new Grid { Margin = new Thickness(16, 4, 16, 8) };
        contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(3, GridUnitType.Star) });
        contentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });

        // Левая часть: DataGrid
        var leftPanel = new DockPanel { Margin = new Thickness(0, 0, 8, 0) };
        var leftTitle = new TextBlock
        {
            Text = "Итерации вычисления ряда / амортизации (для окон Autos, Locals, Watch):",
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 6)
        };
        DockPanel.SetDock(leftTitle, Dock.Top);
        leftPanel.Children.Add(leftTitle);

        _grid = new DataGrid
        {
            AutoGenerateColumns = true,
            IsReadOnly = true,
            CanUserAddRows = false,
            Background = Brushes.White,
            GridLinesVisibility = DataGridGridLinesVisibility.Horizontal,
            AlternatingRowBackground = new SolidColorBrush(Color.FromRgb(247, 250, 253)),
            RowHeight = 28
        };
        leftPanel.Children.Add(_grid);
        Grid.SetColumn(leftPanel, 0);
        contentGrid.Children.Add(leftPanel);

        // Правая часть: Лог выполнения и try-catch-finally
        var rightPanel = new DockPanel { Margin = new Thickness(8, 0, 0, 0) };
        var rightTitle = new TextBlock
        {
            Text = "Протокол трассировки и обработки исключений (try-catch-finally):",
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 6)
        };
        DockPanel.SetDock(rightTitle, Dock.Top);
        rightPanel.Children.Add(rightTitle);

        _logBox = new ListBox
        {
            Background = Brushes.White,
            FontFamily = new FontFamily("Consolas"),
            FontSize = 11
        };
        rightPanel.Children.Add(_logBox);
        Grid.SetColumn(rightPanel, 1);
        contentGrid.Children.Add(rightPanel);

        Grid.SetRow(contentGrid, 2);
        mainGrid.Children.Add(contentGrid);

        // 4. Строка состояния
        var statusBorder = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(232, 237, 242)),
            Padding = new Thickness(16, 8, 16, 8)
        };
        var statusDock = new DockPanel();

        _buildConfigText = new TextBlock
        {
            Text = $"Конфигурация сборки: {(Variant4DiagnosticsService.IsDebugBuild ? "DEBUG (отладка активна, без оптимизации)" : "RELEASE (оптимизация включена)")}",
            FontWeight = FontWeights.SemiBold,
            Foreground = Variant4DiagnosticsService.IsDebugBuild ? Brushes.DarkBlue : Brushes.DarkGreen
        };
        DockPanel.SetDock(_buildConfigText, Dock.Right);
        statusDock.Children.Add(_buildConfigText);

        _resultText = new TextBlock
        {
            Text = "Готово к работе. Введите N и нажмите 'Рассчитать ряд' или запустите проверку исключений.",
            Foreground = new SolidColorBrush(Color.FromRgb(50, 60, 70))
        };
        statusDock.Children.Add(_resultText);

        statusBorder.Child = statusDock;
        Grid.SetRow(statusBorder, 3);
        mainGrid.Children.Add(statusBorder);

        Content = mainGrid;
    }

    private Button CreateStyledButton(string text, Color bg)
    {
        return new Button
        {
            Content = text,
            Padding = new Thickness(10, 6, 10, 6),
            Margin = new Thickness(4),
            Background = new SolidColorBrush(bg),
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0),
            Cursor = System.Windows.Input.Cursors.Hand,
            FontWeight = FontWeights.Normal
        };
    }

    private void OnCalculateSeriesClick()
    {
        var log = new List<string>();
        string input = _inputN.Text.Trim();

        try
        {
            bool success = _diagnosticsService.ExecuteDiagnosticsWorkflow(input, "series_calculation", log);
            if (success && int.TryParse(input, out int n))
            {
                var (steps, finalSum) = _diagnosticsService.CalculateVariant4Series(n);
                _grid.ItemsSource = steps;
                _resultText.Text = $"N={n} | Сумма S={finalSum:F10} | Предел π²/12 ≈ {Variant4DiagnosticsService.TheoreticalLimit:F10} | Погрешность: {Math.Abs(finalSum - Variant4DiagnosticsService.TheoreticalLimit):E4}";
            }
            else
            {
                _grid.ItemsSource = null;
                _resultText.Text = "Вычисление прервано из-за ошибки (см. журнал справа).";
            }
        }
        catch (Exception ex)
        {
            log.Add($"[ВНЕШНИЙ ПЕРЕХВАТ]: {ex.Message}");
            _resultText.Text = $"Ошибка: {ex.Message}";
        }
        finally
        {
            UpdateLogBox(log);
        }
    }

    private void OnCalculateDepreciationClick()
    {
        var log = new List<string>();
        try
        {
            log.Add($"[{DateTime.Now:HH:mm:ss}] Расчет ускоренной амортизации оборудования (Вариант 4)...");
            decimal initialCost = 150000m;
            int years = 5;
            decimal salvageValue = 15000m;
            int annualHours = 2000;

            var steps = _diagnosticsService.CalculateDepreciation(initialCost, years, salvageValue, annualHours);
            _grid.ItemsSource = steps;
            log.Add($"[Успех] Рассчитана амортизация за {years} лет. Базовая стоимость: {initialCost:C}, Ликвидационная: {salvageValue:C}.");
            _resultText.Text = $"Амортизация рассчитана успешно. Стоимость часа за последний год: {steps[^1].CostPerHour:C}/час.";
        }
        catch (Exception ex)
        {
            log.Add($"[Ошибка амортизации]: {ex.Message}");
            _resultText.Text = $"Ошибка: {ex.Message}";
        }
        finally
        {
            log.Add("[finally] Расчет амортизации завершен.");
            UpdateLogBox(log);
        }
    }

    private void OnSimulateExceptionClick(string errorType)
    {
        var log = new List<string>();
        _diagnosticsService.ExecuteDiagnosticsWorkflow(errorType, "simulate_error", log);
        _grid.ItemsSource = null;
        _resultText.Text = $"Продемонстрирован перехват исключения: {errorType}. Блок finally успешно выполнен.";
        UpdateLogBox(log);
    }

    private void OnClearClick()
    {
        _logBox.Items.Clear();
        _grid.ItemsSource = null;
        _resultText.Text = "Журнал очищен.";
    }

    private void UpdateLogBox(List<string> log)
    {
        foreach (var entry in log)
        {
            _logBox.Items.Add(entry);
        }
        if (_logBox.Items.Count > 0)
        {
            _logBox.ScrollIntoView(_logBox.Items[^1]);
        }
    }
}
