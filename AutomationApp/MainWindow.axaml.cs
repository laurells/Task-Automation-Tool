using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using AutomationApp.Core;
using AutomationApp.Models;
using AutomationApp.Interfaces;
using AutomationApp.Rules;
using AutomationApp.Services;

namespace AutomationApp
{
    /// <summary>
    /// The main application window for configuring and saving automation rules.
    /// </summary>
    /// <remarks>
    /// Provides a user interface for creating rules such as <see cref="FileMoveRule"/>, <see cref="BulkEmailRule"/>,
    /// and <see cref="DataProcessingRule"/>. Saves rules to a JSON configuration file using <see cref="RuleConfig"/>.
    /// Integrates with <see cref="AutomationEngine"/> and <see cref="ILoggerService"/> for rule management and logging.
    /// </remarks>
    public partial class MainWindow : Window
    {
        private readonly AutomationEngine _engine;          // Engine for managing automation rules
        private readonly ILoggerService _logger;            // Logger for operation details
        private readonly AppConfiguration _appConfig;       // Application configuration settings

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow"/> class.
        /// </summary>
        /// <param name="engine">The automation engine for managing rules. Cannot be null.</param>
        /// <param name="logger">The logger for recording operation details. Cannot be null.</param>
        /// <param name="appConfig">The application configuration. If null, a default configuration is used.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="engine"/> or <paramref name="logger"/> is null.</exception>
        public MainWindow(AutomationEngine engine, ILoggerService logger, AppConfiguration? appConfig = null)
        {
            _engine = engine ?? throw new ArgumentNullException(nameof(engine));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _appConfig = appConfig ?? throw new ArgumentNullException(nameof(appConfig));
            InitializeComponent();
            InitializeEventHandlers();
            _logger.LogInfo("MainWindow initialized");
        }

        /// <summary>
        /// Loads the XAML layout for the window.
        /// </summary>
        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        /// <summary>
        /// Sets up event handlers for UI controls.
        /// </summary>
        /// <remarks>
        /// Configures the <c>RuleTypeComboBox</c> to toggle visibility of file-move-specific controls and the
        /// <c>SaveButton</c> to handle rule saving.
        /// </remarks>
        private void InitializeEventHandlers()
        {
            // Find UI controls
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

            // Handle rule type selection
            ruleTypeComboBox.SelectionChanged += (s, e) =>
            {
                bool isFileMove = ruleTypeComboBox.SelectedIndex == 0; // FileMoveRule
                // Toggle visibility of file-move-specific controls
                sourceLabel.IsVisible = isFileMove;
                sourceTextBox.IsVisible = isFileMove;
                targetLabel.IsVisible = isFileMove;
                targetTextBox.IsVisible = isFileMove;
                extensionsLabel.IsVisible = isFileMove;
                extensionsTextBox.IsVisible = isFileMove;
                timestampCheckBox.IsVisible = isFileMove;
                backupCheckBox.IsVisible = isFileMove;
                _logger.LogDebug($"Rule type changed to {(isFileMove ? "FileMoveRule" : ruleTypeComboBox.SelectedIndex == 1 ? "BulkEmailRule" : "DataProcessingRule")}");
            };

            // Handle save button click
            saveButton.Click += async (s, e) => await SaveRuleAsync();
        }

        /// <summary>
        /// Asynchronously saves a new rule to the configuration file.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <remarks>
        /// Validates user input, creates a <see cref="RuleConfig"/>, and appends it to <c>config.rules.json</c>.
        /// Displays a message box on success or failure.
        /// </remarks>
        private async Task SaveRuleAsync()
        {
            try
            {
                var ruleNameTextBox = this.FindControl<TextBox>("RuleNameTextBox");
                var ruleTypeComboBox = this.FindControl<ComboBox>("RuleTypeComboBox");

                // Validate rule name
                if (string.IsNullOrEmpty(ruleNameTextBox.Text))
                {
                    await MessageBox.Show(this, "Rule name is required.", "Error", MessageBox.MessageBoxButtons.Ok);
                    _logger.LogWarning("Rule save failed: Rule name is empty");
                    return;
                }

                RuleConfig rule;
                switch (ruleTypeComboBox.SelectedIndex)
                {
                    case 0: // FileMoveRule
                        rule = await CreateFileMoveRuleAsync(ruleNameTextBox.Text);
                        break;
                    case 1: // BulkEmailRule
                        rule = await CreateBulkEmailRuleAsync(ruleNameTextBox.Text);
                        break;
                    case 2: // DataProcessingRule
                        rule = await CreateDataProcessingRuleAsync(ruleNameTextBox.Text);
                        break;
                    default:
                        await MessageBox.Show(this, "Invalid rule type selected.", "Error", MessageBox.MessageBoxButtons.Ok);
                        _logger.LogWarning("Rule save failed: Invalid rule type selected");
                        return;
                }

                if (rule == null)
                    return; // Error message already shown in Create methods

                // Save rule to config file
                var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _appConfig.RulesConfigPath);
                List<RuleConfig> rules = [];

                if (File.Exists(configPath))
                {
                    var json = await File.ReadAllTextAsync(configPath);
                    rules = JsonSerializer.Deserialize<List<RuleConfig>>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }) ?? [];
                }

                rules.Add(rule);
                var options = new JsonSerializerOptions { WriteIndented = true };
                var updatedJson = JsonSerializer.Serialize(rules, options);
                await File.WriteAllTextAsync(configPath, updatedJson);

                await MessageBox.Show(this, "Rule saved successfully!", "Success", MessageBox.MessageBoxButtons.Ok);
                _logger.LogSuccess($"Rule '{ruleNameTextBox.Text}' saved to {configPath}");

                // Notify engine to reload rules (optional, if engine supports dynamic reloading)
                // await _engine.ReloadRulesAsync(configPath);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to serialize/deserialize rules");
                await MessageBox.Show(this, "Failed to save rule: Invalid configuration format.", "Error", MessageBox.MessageBoxButtons.Ok);
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, $"Failed to access configuration file");
                await MessageBox.Show(this, "Failed to save rule: Unable to write to configuration file.", "Error", MessageBox.MessageBoxButtons.Ok);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error saving rule");
                await MessageBox.Show(this, "Failed to save rule. Check logs for details.", "Error", MessageBox.MessageBoxButtons.Ok);
            }
        }

        /// <summary>
        /// Creates a <see cref="FileMoveRule"/> configuration from user input.
        /// </summary>
        /// <param name="ruleName">The name of the rule.</param>
        /// <returns>A task that resolves to the <see cref="RuleConfig"/> or null if validation fails.</returns>
        private async Task<RuleConfig?> CreateFileMoveRuleAsync(string ruleName)
        {
            var sourceTextBox = this.FindControl<TextBox>("SourceTextBox");
            var targetTextBox = this.FindControl<TextBox>("TargetTextBox");
            var extensionsTextBox = this.FindControl<TextBox>("ExtensionsTextBox");
            var timestampCheckBox = this.FindControl<CheckBox>("TimestampCheckBox");
            var backupCheckBox = this.FindControl<CheckBox>("BackupCheckBox");

            // Validate inputs
            if (string.IsNullOrEmpty(sourceTextBox.Text) || string.IsNullOrEmpty(targetTextBox.Text))
            {
                await MessageBox.Show(this, "Source and target directories are required.", "Error", MessageBox.MessageBoxButtons.Ok);
                _logger.LogWarning($"FileMoveRule '{ruleName}' save failed: Source or target directory is empty");
                return null;
            }

            if (!Directory.Exists(sourceTextBox.Text) || !Directory.Exists(targetTextBox.Text))
            {
                await MessageBox.Show(this, "Source or target directory does not exist.", "Error", MessageBox.MessageBoxButtons.Ok);
                _logger.LogWarning($"FileMoveRule '{ruleName}' save failed: Source or target directory does not exist");
                return null;
            }

            var extensions = string.IsNullOrEmpty(extensionsTextBox.Text)
                ? Array.Empty<string>()
                : extensionsTextBox.Text.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim())
                    .Where(x => !string.IsNullOrEmpty(x))
                    .ToArray();

            return new RuleConfig
            {
                Type = "filemoverule",
                Name = ruleName,
                Source = sourceTextBox.Text,
                Target = targetTextBox.Text,
                SupportedExtensions = extensions,
                AddTimestamp = timestampCheckBox.IsChecked ?? false,
                BackupFiles = backupCheckBox.IsChecked ?? false
            };
        }

        /// <summary>
        /// Creates a <see cref="BulkEmailRule"/> configuration from user input.
        /// </summary>
        /// <param name="ruleName">The name of the rule.</param>
        /// <returns>A task that resolves to the <see cref="RuleConfig"/> or null if validation fails.</returns>
        private async Task<RuleConfig?> CreateBulkEmailRuleAsync(string ruleName)
        {
            var csvPath = await InputDialog.Show(this, "Enter CSV file path:", "BulkEmailRule Configuration");
            if (string.IsNullOrEmpty(csvPath))
            {
                await MessageBox.Show(this, "CSV file path is required.", "Error", MessageBox.MessageBoxButtons.Ok);
                _logger.LogWarning($"BulkEmailRule '{ruleName}' save failed: CSV path is empty");
                return null;
            }

            if (!File.Exists(csvPath))
            {
                await MessageBox.Show(this, "Invalid CSV file path.", "Error", MessageBox.MessageBoxButtons.Ok);
                _logger.LogWarning($"BulkEmailRule '{ruleName}' save failed: CSV file does not exist: {csvPath}");
                return null;
            }

            return new RuleConfig
            {
                Type = "bulkemailrule",
                Name = ruleName,
                CsvPath = csvPath
            };
        }

        /// <summary>
        /// Creates a <see cref="DataProcessingRule"/> configuration from user input.
        /// </summary>
        /// <param name="ruleName">The name of the rule.</param>
        /// <returns>A task that resolves to the <see cref="RuleConfig"/> or null if validation fails.</returns>
        private async Task<RuleConfig?> CreateDataProcessingRuleAsync(string ruleName)
        {
            var filePicker = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Select CSV or Excel File",
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("CSV and Excel Files") { Patterns = new[] { "*.csv", "*.xlsx", "*.xls" } },
                    new FilePickerFileType("All Files") { Patterns = new[] { "*" } }
                },
                AllowMultiple = false
            });

            var filePath = filePicker.FirstOrDefault()?.Path.LocalPath;
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                await MessageBox.Show(this, "Please select a valid CSV or Excel file.", "Error", MessageBox.MessageBoxButtons.Ok);
                _logger.LogWarning($"DataProcessingRule '{ruleName}' save failed: Invalid file path: {filePath}");
                return null;
            }

            var columnsInput = await InputDialog.Show(this, "Enter required columns (comma-separated, e.g., id,name):", "DataProcessingRule Configuration");
            var requiredColumns = columnsInput.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(c => c.Trim())
                .Where(c => !string.IsNullOrEmpty(c))
                .ToArray();

            if (!requiredColumns.Any())
            {
                await MessageBox.Show(this, "At least one required column must be specified.", "Error", MessageBox.MessageBoxButtons.Ok);
                _logger.LogWarning($"DataProcessingRule '{ruleName}' save failed: No required columns specified");
                return null;
            }

            return new RuleConfig
            {
                Type = "dataprocessingrule",
                Name = ruleName,
                DataPath = filePath,
                RequiredColumns = requiredColumns
            };
        }
    }

    /// <summary>
    /// Provides a simple message box dialog for displaying messages to the user.
    /// </summary>
    public static class MessageBox
    {
        /// <summary>
        /// Defines the buttons available in the message box.
        /// </summary>
        public enum MessageBoxButtons
        {
            /// <summary>
            /// Displays an OK button.
            /// </summary>
            Ok
        }

        /// <summary>
        /// Displays a message box dialog.
        /// </summary>
        /// <param name="owner">The owner window for the dialog.</param>
        /// <param name="message">The message to display.</param>
        /// <param name="title">The dialog title.</param>
        /// <param name="buttons">The buttons to display.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <remarks>
        /// Currently supports only an OK button. The dialog is centered on the owner window.
        /// </remarks>
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
            var button = new Button
            {
                Content = "OK",
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                Margin = new Thickness(0, 10, 0, 0)
            };
            button.Click += (s, e) => dialog.Close();
            stackPanel.Children.Add(button);
            dialog.Content = stackPanel;
            await dialog.ShowDialog(owner);
        }
    }

    /// <summary>
    /// Provides an input dialog for collecting text input from the user.
    /// </summary>
    public static class InputDialog
    {
        /// <summary>
        /// Displays an input dialog with a text box.
        /// </summary>
        /// <param name="owner">The owner window for the dialog.</param>
        /// <param name="prompt">The prompt to display.</param>
        /// <param name="title">The dialog title.</param>
        /// <returns>A task that resolves to the user’s input or an empty string if cancelled.</returns>
        /// <remarks>
        /// The dialog includes a text box and an OK button. The dialog is centered on the owner window.
        /// </remarks>
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
            var button = new Button
            {
                Content = "OK",
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                Margin = new Thickness(0, 10, 0, 0)
            };
            string result = string.Empty;
            button.Click += (s, e) =>
            {
                result = textBox.Text ?? string.Empty;
                dialog.Close();
            };
            stackPanel.Children.Add(button);
            dialog.Content = stackPanel;
            await dialog.ShowDialog(owner);
            return result;
        }
    }
}