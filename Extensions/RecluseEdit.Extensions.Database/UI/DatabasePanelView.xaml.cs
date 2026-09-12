using System.Data;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Data.Sqlite;
using Microsoft.Win32;

namespace RecluseEdit.Extensions.Database.UI;

public class TableColumnInfo
{
    public required string Name { get; set; }
    public required string Type { get; set; }
}

public class TableSchemaInfo
{
    public required string Name { get; set; }
    public List<TableColumnInfo> Columns { get; set; } = [];
}

public partial class DatabasePanelView : UserControl
{
    private string? _currentDbPath;
    private DataTable? _lastResults;

    public DatabasePanelView()
    {
        InitializeComponent();
        TxtQuery.Text = "SELECT * FROM sqlite_master WHERE type='table';";
    }

    private void OnOpenDbClick(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title = "Open SQLite Database File",
            Filter = "SQLite Databases (*.db;*.sqlite;*.sqlite3)|*.db;*.sqlite;*.sqlite3|All Files (*.*)|*.*"
        };

        if (dlg.ShowDialog() == true)
        {
            LoadDatabase(dlg.FileName);
        }
    }

    public void LoadDatabase(string filePath)
    {
        if (!File.Exists(filePath)) return;

        _currentDbPath = filePath;
        TxtDbName.Text = Path.GetFileName(filePath);
        TxtDbName.ToolTip = filePath;

        LoadSchema();
        RunQuery(TxtQuery.Text);
    }

    private void LoadSchema()
    {
        if (string.IsNullOrEmpty(_currentDbPath)) return;

        try
        {
            using var conn = new SqliteConnection($"Data Source={_currentDbPath};Mode=ReadOnly;");
            conn.Open();

            var tables = new List<TableSchemaInfo>();

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name;";
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    tables.Add(new TableSchemaInfo { Name = reader.GetString(0) });
                }
            }

            foreach (var table in tables)
            {
                using var colCmd = conn.CreateCommand();
                colCmd.CommandText = $"PRAGMA table_info(\"{table.Name}\");";
                using var colReader = colCmd.ExecuteReader();
                while (colReader.Read())
                {
                    table.Columns.Add(new TableColumnInfo
                    {
                        Name = colReader.GetString(1),
                        Type = colReader.IsDBNull(2) ? "TEXT" : colReader.GetString(2)
                    });
                }
            }

            TreeTables.ItemsSource = tables;
            ExpanderTables.IsExpanded = tables.Count > 0;
        }
        catch (Exception ex)
        {
            TxtStatus.Text = $"Failed to load schema: {ex.Message}";
        }
    }

    private void OnRunQueryClick(object sender, RoutedEventArgs e)
    {
        RunQuery(TxtQuery.Text);
    }

    private void OnQueryKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
        {
            e.Handled = true;
            RunQuery(TxtQuery.Text);
        }
    }

    private void RunQuery(string sql)
    {
        if (string.IsNullOrWhiteSpace(_currentDbPath))
        {
            TxtStatus.Text = "Please open an SQLite database first.";
            return;
        }

        if (string.IsNullOrWhiteSpace(sql))
        {
            TxtStatus.Text = "Query text is empty.";
            return;
        }

        try
        {
            TxtStatus.Text = "Executing query...";
            var sw = Stopwatch.StartNew();

            using var conn = new SqliteConnection($"Data Source={_currentDbPath};");
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;

            using var reader = cmd.ExecuteReader();
            var dt = new DataTable();
            dt.Load(reader);
            sw.Stop();

            _lastResults = dt;
            GridResults.ItemsSource = dt.DefaultView;
            TxtStatus.Text = $"{dt.Rows.Count} row(s) returned in {sw.ElapsedMilliseconds} ms.";
        }
        catch (Exception ex)
        {
            GridResults.ItemsSource = null;
            TxtStatus.Text = $"Query Error: {ex.Message}";
        }
    }

    private void OnTreeItemDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (TreeTables.SelectedItem is TableSchemaInfo table)
        {
            TxtQuery.Text = $"SELECT * FROM \"{table.Name}\" LIMIT 50;";
            RunQuery(TxtQuery.Text);
        }
    }

    private void OnExportCsvClick(object sender, RoutedEventArgs e)
    {
        if (_lastResults == null || _lastResults.Rows.Count == 0)
        {
            MessageBox.Show("No query results to export.", "Export CSV", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dlg = new SaveFileDialog
        {
            Title = "Export Results to CSV",
            Filter = "CSV File (*.csv)|*.csv|All Files (*.*)|*.*",
            FileName = "query_results.csv"
        };

        if (dlg.ShowDialog() == true)
        {
            try
            {
                var sb = new StringBuilder();
                var colNames = _lastResults.Columns.Cast<DataColumn>().Select(c => EscapeCsv(c.ColumnName));
                sb.AppendLine(string.Join(",", colNames));

                foreach (DataRow row in _lastResults.Rows)
                {
                    var fields = row.ItemArray.Select(field => EscapeCsv(field?.ToString() ?? ""));
                    sb.AppendLine(string.Join(",", fields));
                }

                File.WriteAllText(dlg.FileName, sb.ToString(), Encoding.UTF8);
                TxtStatus.Text = $"Exported {_lastResults.Rows.Count} rows to {Path.GetFileName(dlg.FileName)}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export failed:\n{ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private static string EscapeCsv(string field)
    {
        if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
        {
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }
        return field;
    }
}
