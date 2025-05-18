using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System.Text.Json;
using AutomationApp.Services;
using AutomationApp.Core;
using Avalonia.Platform.Storage;

namespace AutomationApp
{
    public partial class MainWindow : Window
    {
        private readonly Logger _logger;
        private readonly AutomationEngine _engine;

        public MainWindow(AutomationEngine engine, Logger logger)
        {
            _engine = engine ?? throw new ArgumentNullException(nameof(engine));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            InitializeComponent();
            InitializeEventHandlers();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void InitializeEventHandlers()
        {
            var ruleTypeComboBox = this.FindControl<ComboBox>("RuleTypeComboBox");
            var sourceLabel = this.FindControl<TextBlock>("SourceLabel");
            var sourceTextBox = this.FindControl<TextBox>("SourceTextBox");
            var targetLabel = this.FindControl<TextBlock>("TargetLabel");
            var targetTextBox = this.FindControl<TextBox>("TargetTextBox");
            var extensionsLabel = this.FindControl<TextBlock>("ExtensionsLabel");
            var extensionsTextBox = this.FindControl<TextBox>("ExtensionsTextBox");
            var timestampCheckBox = this.FindControl<CheckBox>("TimestampCheckBox");
            var backupCheckBox = this.FindControl<CheckBox>("BackupCheckBox");
            var saveButton = this.FindControl<Button>("SaveButton");

            ruleTypeComboBox.SelectionChanged += (s, e) =>
            {
                bool isFileMove = ruleTypeComboBox.SelectedIndex == 0;
                sourceLabel.IsVisible = isFileMove;
                sourceTextBox.IsVisible = isFileMove;
                targetLabel.IsVisible = isFileMove;
                targetTextBox.IsVisible = isFileMove;
                extensionsLabel.IsVisible = isFileMove;
                extensionsTextBox.IsVisible = isFileMove;
                timestampCheckBox.IsVisible = isFileMove;
                backupCheckBox.IsVisible = isFileMove;
            };


            saveButton.Click += async (s, e) =>
            {
                try
                {
                    var ruleNameTextBox = this.FindControl<TextBox>("RuleNameTextBox");
                    if (string.IsNullOrEmpty(ruleNameTextBox.Text))
                    {
                        await MessageBox.Show(this, "Rule name is required.", "Error", MessageBox.MessageBoxButtons.Ok);
                        return;
                    }

                    object rule;
                    if (ruleTypeComboBox.SelectedIndex == 0) // FileMoveRule
                    {
                        if (string.IsNullOrEmpty(sourceTextBox.Text) || string.IsNullOrEmpty(targetTextBox.Text))
                        {
                            await MessageBox.Show(this, "Source and target directories are required.", "Error", MessageBox.MessageBoxButtons.Ok);
                            return;
                        }

                        var extensions = string.IsNullOrEmpty(extensionsTextBox.Text)
                            ? Array.Empty<string>()
                            : extensionsTextBox.Text.Split(',').Select(x => x.Trim()).ToArray();

                        rule = new
                        {
                            type = "FileMoveRule",
                            name = ruleNameTextBox.Text,
                            source = sourceTextBox.Text,
                            target = targetTextBox.Text,
                            supportedExtensions = extensions,
                            addTimestamp = timestampCheckBox.IsChecked ?? false,
                            backupFiles = backupCheckBox.IsChecked ?? false
                        };
                    }
                    else if (ruleTypeComboBox.SelectedIndex == 1)
                    {
                        var csvPath = await InputDialog.Show(this, "Enter CSV file path:", "BulkEmailRule Configuration");
                        if (string.IsNullOrEmpty(csvPath) || !File.Exists(csvPath))
                        {
                            await MessageBox.Show(this, "Invalid CSV file path.", "Error", MessageBox.MessageBoxButtons.Ok);
                            return;
                        }

                        rule = new
                        {
                            type = "BulkEmailRule",
                            name = ruleNameTextBox.Text,
                            csvPath
                        };
                    }
                    else
                    {
                        var filePicker = StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                        {
                            Title = "Select CSV or Excel File",
                            FileTypeFilter = new[]
                            {
                                new FilePickerFileType("CSV and Excel Files") { Patterns = new[] { "*.csv", "*.xlsx", "*.xls" } },
                                new FilePickerFileType("All Files") { Patterns = new[] { "*" } }
                            },
                            AllowMultiple = false
                        });

                        var files = await filePicker;
                        var filePath = files.FirstOrDefault()?.Path.LocalPath;

                        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                        {
                            await MessageBox.Show(this, "Please select a valid CSV or Excel file.", "Error", MessageBox.MessageBoxButtons.Ok);
                            return;
                        }

                        var columnsInput = await InputDialog.Show(this, "Enter required columns (comma-separated, e.g., id,name):", "DataProcessingRule Configuration");
                        var requiredColumns = columnsInput.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(c => c.Trim()).ToArray();

                        if (!requiredColumns.Any())
                        {
                            await MessageBox.Show(this, "At least one required column must be specified.", "Error", MessageBox.MessageBoxButtons.Ok);
                            return;
                        }

                        rule = new
                        {
                            type = "DataProcessingRule",
                            name = ruleNameTextBox.Text,
                            filePath,
                            requiredColumns
                        };
                    }

                    var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.rules.json");
                    List<object> rules = [];

                    if (File.Exists(configPath))
                    {
                        var json = await File.ReadAllTextAsync(configPath);
                        rules = JsonSerializer.Deserialize<List<object>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
                    }

                    rules.Add(rule);
                    var options = new JsonSerializerOptions { WriteIndented = true };
                    var updatedJson = JsonSerializer.Serialize(rules, options);
                    await File.WriteAllTextAsync(configPath, updatedJson);

                    await MessageBox.Show(this, "Rule saved successfully!", "Success", MessageBox.MessageBoxButtons.Ok);
                    _logger.LogInfo($"Rule '{ruleNameTextBox.Text}' saved to {configPath}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to save rule");
                    await MessageBox.Show(this, "Failed to save rule. Check logs for details.", "Error", MessageBox.MessageBoxButtons.Ok);
                }
            };
        }
    }

    // Helper for MessageBox
    public static class MessageBox
    {
        public enum MessageBoxButtons
        {
            Ok
        }

        public static async Task Show(Window owner, string message, string title, MessageBoxButtons buttons)
        {
            var dialog = new Window
            {
                Title = title,
                Width = 300,
                Height = 150,
                CanResize = false,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            var stackPanel = new StackPanel { Margin = new Thickness(10) };
            stackPanel.Children.Add(new TextBlock { Text = message });
            var button = new Button { Content = "OK", HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center, Margin = new Thickness(0, 10, 0, 0) };
            button.Click += (s, e) => dialog.Close();
            stackPanel.Children.Add(button);
            dialog.Content = stackPanel;
            await dialog.ShowDialog(owner);
        }
    }

    // Helper for InputDialog
    public static class InputDialog
    {
        public static async Task<string> Show(Window owner, string prompt, string title)
        {
            var dialog = new Window
            {
                Title = title,
                Width = 400,
                Height = 200,
                CanResize = false,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            var stackPanel = new StackPanel { Margin = new Thickness(10) };
            stackPanel.Children.Add(new TextBlock { Text = prompt });
            var textBox = new TextBox { Margin = new Thickness(0, 10, 0, 0) };
            stackPanel.Children.Add(textBox);
            var button = new Button { Content = "OK", HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center, Margin = new Thickness(0, 10, 0, 0) };
            string result = "";
            button.Click += (s, e) => { result = textBox.Text; dialog.Close(); };
            stackPanel.Children.Add(button);
            dialog.Content = stackPanel;
            await dialog.ShowDialog(owner);
            return result;
        }
    }
}