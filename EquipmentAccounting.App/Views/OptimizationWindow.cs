using System.Windows;
using System.Windows.Controls;
using EquipmentAccounting.Core.Optimization;

namespace EquipmentAccounting.App;

internal sealed class OptimizationWindow : Window
{
    private readonly Button _run;
    private readonly TextBlock _result;
    public OptimizationWindow()
    {
        Title = "Оптимизация поиска"; Width = 720; Height = 520; WindowStartupLocation = WindowStartupLocation.CenterOwner; Background = System.Windows.Media.Brushes.White;
        var panel = new StackPanel { Margin = new Thickness(28) };
        panel.Children.Add(new TextBlock { Text = "Сравнение алгоритмов поиска", FontSize = 25, FontWeight = FontWeights.SemiBold, Foreground = System.Windows.Media.Brushes.DarkSlateBlue });
        panel.Children.Add(new TextBlock { Text = "Исходный алгоритм: последовательный просмотр списка от начала до искомой записи. Сложность поиска O(n).", TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 20, 0, 10), FontSize = 15 });
        panel.Children.Add(new TextBlock { Text = "Оптимизированный алгоритм: прямой поиск ключа в Dictionary по инвентарному номеру. Среднее время поиска O(1). Создание массива и словаря не входит в измерение.", TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 18), FontSize = 15 });
        _run = new Button { Content = "Запустить сравнение (100 000 записей)", Padding = new Thickness(16, 10, 16, 10), HorizontalAlignment = HorizontalAlignment.Left };
        _run.Click += Run_Click; panel.Children.Add(_run);
        _result = new TextBlock { Text = "Измерения еще не запускались. Результаты будут рассчитаны на этом компьютере.", TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 20, 0, 0), FontSize = 15 };
        panel.Children.Add(_result); Content = panel;
    }
    private async void Run_Click(object sender, RoutedEventArgs e)
    {
        _run.IsEnabled = false; _result.Text = "Выполняются повторные измерения...";
        try
        {
            var result = await Task.Run(() => SearchBenchmark.Run());
            _result.Text = $"Записей: {result.RecordCount:N0}\nПовторов: {result.Repetitions}\nПоследовательный поиск (медиана): {result.SequentialMilliseconds:F3} мс\nDictionary (медиана): {result.DictionaryMilliseconds:F3} мс\nУскорение: {(result.Speedup > 0 ? $"{result.Speedup:F2}×" : "ниже разрешения таймера")}\nРезультаты методов совпадают: {(result.ResultsMatch ? "да" : "нет")}.";
        }
        catch (Exception ex) { _result.Text = $"Ошибка измерения: {ex.Message}"; }
        finally { _run.IsEnabled = true; }
    }
}
