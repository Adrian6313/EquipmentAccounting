using System.Diagnostics;

namespace EquipmentAccounting.Core.Optimization;

public sealed record SearchRecord(string InventoryNumber, string Name);
public sealed record BenchmarkResult(int RecordCount, int Repetitions, double SequentialMilliseconds, double DictionaryMilliseconds, double Speedup, bool ResultsMatch);

public static class SearchBenchmark
{
    public static SearchRecord? SequentialSearch(IReadOnlyList<SearchRecord> records, string inventoryNumber)
    {
        foreach (var record in records)
            if (string.Equals(record.InventoryNumber, inventoryNumber, StringComparison.OrdinalIgnoreCase)) return record;
        return null;
    }

    public static BenchmarkResult Run(int recordCount = 100_000, int repetitions = 15)
    {
        if (recordCount < 1 || repetitions < 1) throw new ArgumentOutOfRangeException(nameof(recordCount));
        // Dataset and index creation are deliberately outside the timed portions.
        var records = Enumerable.Range(0, recordCount).Select(i => new SearchRecord($"INV-{i:D7}", $"Equipment {i}")).ToArray();
        var index = records.ToDictionary(x => x.InventoryNumber, StringComparer.OrdinalIgnoreCase);
        string target = records[^1].InventoryNumber;
        SearchRecord? expected = SequentialSearch(records, target);
        SearchRecord? actual = index.GetValueOrDefault(target);
        if (expected != actual) throw new InvalidOperationException("Алгоритмы поиска возвращают разные результаты.");
        for (int i = 0; i < 3; i++) { _ = SequentialSearch(records, target); _ = index.GetValueOrDefault(target); }

        var sequentialTimes = new double[repetitions];
        var dictionaryTimes = new double[repetitions];
        for (int i = 0; i < repetitions; i++)
        {
            var sw = Stopwatch.StartNew();
            for (int n = 0; n < 10; n++) _ = SequentialSearch(records, target);
            sw.Stop(); sequentialTimes[i] = sw.Elapsed.TotalMilliseconds;

            sw.Restart();
            for (int n = 0; n < 10; n++) _ = index.GetValueOrDefault(target);
            sw.Stop(); dictionaryTimes[i] = sw.Elapsed.TotalMilliseconds;
        }
        double sequential = Median(sequentialTimes), dictionary = Median(dictionaryTimes);
        return new BenchmarkResult(recordCount, repetitions, sequential, dictionary, dictionary == 0 ? 0 : sequential / dictionary, true);
    }

    private static double Median(double[] values)
    {
        Array.Sort(values);
        return values[values.Length / 2];
    }
}
