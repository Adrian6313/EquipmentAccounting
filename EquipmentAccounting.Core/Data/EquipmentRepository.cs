using EquipmentAccounting.Core.Models;
using Microsoft.Data.Sqlite;

namespace EquipmentAccounting.Core.Data;

public sealed class EquipmentRepository : IDisposable
{
    private readonly SqliteConnection _connection;

    static EquipmentRepository() => SQLitePCL.Batteries_V2.Init();

    public EquipmentRepository(string databasePath)
    {
        var builder = new SqliteConnectionStringBuilder { DataSource = databasePath, ForeignKeys = true };
        _connection = new SqliteConnection(builder.ToString());
        _connection.Open();
        Initialize();
    }

    private void Initialize()
    {
        using var command = _connection.CreateCommand();
        command.CommandText = """
            PRAGMA foreign_keys = ON;
            CREATE TABLE IF NOT EXISTS Departments(Id INTEGER PRIMARY KEY, Name TEXT NOT NULL UNIQUE);
            CREATE TABLE IF NOT EXISTS Equipment(Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT NOT NULL, InventoryNumber TEXT NOT NULL UNIQUE, Manufacturer TEXT NOT NULL, PurchaseDate TEXT NOT NULL, Cost TEXT NOT NULL, Department TEXT NOT NULL, LastInspectionDate TEXT NULL, RepairSummary TEXT NOT NULL DEFAULT '');
            CREATE TABLE IF NOT EXISTS Repairs(Id INTEGER PRIMARY KEY AUTOINCREMENT, EquipmentId INTEGER NOT NULL REFERENCES Equipment(Id) ON DELETE CASCADE, RepairDate TEXT NOT NULL, Description TEXT NOT NULL, Cost TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS Inspections(Id INTEGER PRIMARY KEY AUTOINCREMENT, EquipmentId INTEGER NOT NULL REFERENCES Equipment(Id) ON DELETE CASCADE, InspectionDate TEXT NOT NULL, Result TEXT NOT NULL, Inspector TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS EquipmentMovements(Id INTEGER PRIMARY KEY AUTOINCREMENT, EquipmentId INTEGER NOT NULL REFERENCES Equipment(Id) ON DELETE CASCADE, FromDepartment TEXT NOT NULL, ToDepartment TEXT NOT NULL, MovedAt TEXT NOT NULL);
            INSERT OR IGNORE INTO Departments(Name) VALUES ('ИТ'), ('Бухгалтерия'), ('Производство'), ('Склад');
            """;
        command.ExecuteNonQuery();
        using var count = _connection.CreateCommand();
        count.CommandText = "SELECT COUNT(*) FROM Equipment";
        if (Convert.ToInt32(count.ExecuteScalar()) == 0)
        {
            Add(new Equipment { Name = "Ноутбук ThinkPad", InventoryNumber = "INV-001", Manufacturer = "Lenovo", PurchaseDate = new DateTime(2024, 3, 12), Cost = 18500m, Department = "ИТ", LastInspectionDate = DateTime.Today.AddMonths(-2) });
            Add(new Equipment { Name = "Принтер LaserJet", InventoryNumber = "INV-002", Manufacturer = "HP", PurchaseDate = new DateTime(2023, 9, 5), Cost = 7200m, Department = "Бухгалтерия", LastInspectionDate = DateTime.Today.AddMonths(-1) });
            Add(new Equipment { Name = "Станок ЧПУ", InventoryNumber = "INV-003", Manufacturer = "DMG Mori", PurchaseDate = new DateTime(2022, 5, 20), Cost = 245000m, Department = "Производство" });
        }
    }

    public IReadOnlyList<Equipment> GetAll()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT Id,Name,InventoryNumber,Manufacturer,PurchaseDate,Cost,Department,LastInspectionDate,RepairSummary FROM Equipment ORDER BY Id";
        using var reader = cmd.ExecuteReader();
        var result = new List<Equipment>();
        while (reader.Read()) result.Add(ReadEquipment(reader));
        return result;
    }

    public IReadOnlyList<Equipment> Search(string query)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT Id,Name,InventoryNumber,Manufacturer,PurchaseDate,Cost,Department,LastInspectionDate,RepairSummary FROM Equipment WHERE Name LIKE $q COLLATE NOCASE OR InventoryNumber LIKE $q COLLATE NOCASE ORDER BY Id";
        cmd.Parameters.AddWithValue("$q", $"%{query.Trim()}%");
        using var reader = cmd.ExecuteReader();
        var result = new List<Equipment>();
        while (reader.Read()) result.Add(ReadEquipment(reader));
        return result;
    }

    public Equipment Add(Equipment e)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "INSERT INTO Equipment(Name,InventoryNumber,Manufacturer,PurchaseDate,Cost,Department,LastInspectionDate,RepairSummary) VALUES($n,$i,$m,$p,$c,$d,$l,$r); SELECT last_insert_rowid();";
        Bind(cmd, e);
        e.Id = Convert.ToInt32(cmd.ExecuteScalar());
        return e;
    }

    public void Update(Equipment e)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "UPDATE Equipment SET Name=$n,InventoryNumber=$i,Manufacturer=$m,PurchaseDate=$p,Cost=$c,Department=$d,LastInspectionDate=$l,RepairSummary=$r WHERE Id=$id";
        Bind(cmd, e); cmd.Parameters.AddWithValue("$id", e.Id);
        if (cmd.ExecuteNonQuery() == 0) throw new KeyNotFoundException("Оборудование не найдено.");
    }

    public bool Delete(int id)
    {
        using var cmd = _connection.CreateCommand(); cmd.CommandText = "DELETE FROM Equipment WHERE Id=$id"; cmd.Parameters.AddWithValue("$id", id);
        return cmd.ExecuteNonQuery() > 0;
    }

    public decimal GetTotalCost()
    {
        using var cmd = _connection.CreateCommand(); cmd.CommandText = "SELECT COALESCE(SUM(CAST(Cost AS NUMERIC)),0) FROM Equipment";
        return Convert.ToDecimal(cmd.ExecuteScalar());
    }

    public void Move(int id, string toDepartment)
    {
        if (string.IsNullOrWhiteSpace(toDepartment)) throw new ArgumentException("Укажите подразделение.", nameof(toDepartment));
        using var tx = _connection.BeginTransaction();
        using var get = _connection.CreateCommand(); get.Transaction = tx; get.CommandText = "SELECT Department FROM Equipment WHERE Id=$id"; get.Parameters.AddWithValue("$id", id);
        var from = get.ExecuteScalar() as string ?? throw new KeyNotFoundException("Оборудование не найдено.");
        if (string.Equals(from, toDepartment.Trim(), StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("Оборудование уже находится в этом подразделении.");
        using var update = _connection.CreateCommand(); update.Transaction = tx; update.CommandText = "UPDATE Equipment SET Department=$to WHERE Id=$id"; update.Parameters.AddWithValue("$to", toDepartment.Trim()); update.Parameters.AddWithValue("$id", id); update.ExecuteNonQuery();
        using var log = _connection.CreateCommand(); log.Transaction = tx; log.CommandText = "INSERT INTO EquipmentMovements(EquipmentId,FromDepartment,ToDepartment,MovedAt) VALUES($id,$from,$to,$date)"; log.Parameters.AddWithValue("$id", id); log.Parameters.AddWithValue("$from", from); log.Parameters.AddWithValue("$to", toDepartment.Trim()); log.Parameters.AddWithValue("$date", DateTime.Now.ToString("O")); log.ExecuteNonQuery();
        tx.Commit();
    }

    public Repair AddRepair(int id, DateTime date, string description, decimal cost)
    {
        if (string.IsNullOrWhiteSpace(description)) throw new ArgumentException("Укажите описание ремонта.", nameof(description));
        if (cost < 0) throw new ArgumentOutOfRangeException(nameof(cost));
        using var cmd = _connection.CreateCommand(); cmd.CommandText = "INSERT INTO Repairs(EquipmentId,RepairDate,Description,Cost) VALUES($id,$d,$x,$c); SELECT last_insert_rowid();";
        cmd.Parameters.AddWithValue("$id", id); cmd.Parameters.AddWithValue("$d", date.ToString("O")); cmd.Parameters.AddWithValue("$x", description.Trim()); cmd.Parameters.AddWithValue("$c", cost.ToString(System.Globalization.CultureInfo.InvariantCulture));
        return new Repair(Convert.ToInt32(cmd.ExecuteScalar()), id, date, description.Trim(), cost);
    }

    public IReadOnlyList<Repair> GetRepairs(int id)
    {
        using var cmd = _connection.CreateCommand(); cmd.CommandText = "SELECT Id,EquipmentId,RepairDate,Description,Cost FROM Repairs WHERE EquipmentId=$id ORDER BY RepairDate DESC"; cmd.Parameters.AddWithValue("$id", id);
        using var reader = cmd.ExecuteReader(); var rows = new List<Repair>();
        while (reader.Read()) rows.Add(new Repair(reader.GetInt32(0), reader.GetInt32(1), DateTime.Parse(reader.GetString(2)), reader.GetString(3), decimal.Parse(reader.GetString(4), System.Globalization.CultureInfo.InvariantCulture)));
        return rows;
    }

    public Inspection AddInspection(int id, DateTime date, string result, string inspector)
    {
        if (string.IsNullOrWhiteSpace(result) || string.IsNullOrWhiteSpace(inspector)) throw new ArgumentException("Заполните результат осмотра и фамилию проверяющего.");
        using var tx = _connection.BeginTransaction();
        using var cmd = _connection.CreateCommand(); cmd.Transaction = tx; cmd.CommandText = "INSERT INTO Inspections(EquipmentId,InspectionDate,Result,Inspector) VALUES($id,$d,$r,$i); SELECT last_insert_rowid();";
        cmd.Parameters.AddWithValue("$id", id); cmd.Parameters.AddWithValue("$d", date.ToString("O")); cmd.Parameters.AddWithValue("$r", result.Trim()); cmd.Parameters.AddWithValue("$i", inspector.Trim());
        var newId = Convert.ToInt32(cmd.ExecuteScalar());
        using var update = _connection.CreateCommand(); update.Transaction = tx; update.CommandText = "UPDATE Equipment SET LastInspectionDate=$d WHERE Id=$id"; update.Parameters.AddWithValue("$d", date.ToString("O")); update.Parameters.AddWithValue("$id", id); if (update.ExecuteNonQuery() == 0) throw new KeyNotFoundException("Оборудование не найдено.");
        tx.Commit(); return new Inspection(newId, id, date, result.Trim(), inspector.Trim());
    }

    public IReadOnlyList<Inspection> GetInspections(int id)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT Id,EquipmentId,InspectionDate,Result,Inspector FROM Inspections WHERE EquipmentId=$id ORDER BY InspectionDate DESC, Id DESC";
        cmd.Parameters.AddWithValue("$id", id);
        using var reader = cmd.ExecuteReader();
        var rows = new List<Inspection>();
        while (reader.Read()) rows.Add(new Inspection(reader.GetInt32(0), reader.GetInt32(1), DateTime.Parse(reader.GetString(2)), reader.GetString(3), reader.GetString(4)));
        return rows;
    }

    public IReadOnlyList<EquipmentMovement> GetMovements(int id)
    {
        using var cmd = _connection.CreateCommand(); cmd.CommandText = "SELECT Id,EquipmentId,FromDepartment,ToDepartment,MovedAt FROM EquipmentMovements WHERE EquipmentId=$id ORDER BY MovedAt DESC"; cmd.Parameters.AddWithValue("$id", id);
        using var reader = cmd.ExecuteReader(); var rows = new List<EquipmentMovement>();
        while (reader.Read()) rows.Add(new EquipmentMovement(reader.GetInt32(0), reader.GetInt32(1), reader.GetString(2), reader.GetString(3), DateTime.Parse(reader.GetString(4))));
        return rows;
    }

    public IReadOnlyList<InventoryRow> GetInventory()
    {
        return GetAll().Select(e => new InventoryRow(e.Id, e.Name, e.InventoryNumber, e.Manufacturer, e.PurchaseDate, e.Cost, e.Department, e.LastInspectionDate)).ToList();
    }

    private static Equipment ReadEquipment(SqliteDataReader r) => new() { Id = r.GetInt32(0), Name = r.GetString(1), InventoryNumber = r.GetString(2), Manufacturer = r.GetString(3), PurchaseDate = DateTime.Parse(r.GetString(4)), Cost = decimal.Parse(r.GetString(5), System.Globalization.CultureInfo.InvariantCulture), Department = r.GetString(6), LastInspectionDate = r.IsDBNull(7) ? null : DateTime.Parse(r.GetString(7)), RepairSummary = r.GetString(8) };
    private static void Bind(SqliteCommand cmd, Equipment e)
    {
        cmd.Parameters.AddWithValue("$n", e.Name.Trim()); cmd.Parameters.AddWithValue("$i", e.InventoryNumber.Trim()); cmd.Parameters.AddWithValue("$m", e.Manufacturer.Trim()); cmd.Parameters.AddWithValue("$p", e.PurchaseDate.ToString("O")); cmd.Parameters.AddWithValue("$c", e.Cost.ToString(System.Globalization.CultureInfo.InvariantCulture)); cmd.Parameters.AddWithValue("$d", e.Department.Trim()); cmd.Parameters.AddWithValue("$l", e.LastInspectionDate?.ToString("O") is { } date ? date : DBNull.Value); cmd.Parameters.AddWithValue("$r", e.RepairSummary ?? "");
    }
    public void Dispose() => _connection.Dispose();
}
