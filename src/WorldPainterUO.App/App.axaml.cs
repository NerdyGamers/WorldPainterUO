using System;
using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.Logging;
using WorldPainterUO.App.Configuration;
using WorldPainterUO.App.Logging;
using WorldPainterUO.Core;

namespace WorldPainterUO.App;

public partial class App : Application
{
    private ILoggerFactory? _loggerFactory;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        InitializeLogging();
        InitializeTheme();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
            desktop.Exit += OnExit;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void OnExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        _loggerFactory?.Dispose();
    }

    private void InitializeLogging()
    {
        var logDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "WorldPainterUO", "logs");

        Directory.CreateDirectory(logDir);
        var logPath = Path.Combine(logDir, "worldpainteruo.log");

        _loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddProvider(new FileLoggerProvider(logPath));
            builder.SetMinimumLevel(LogLevel.Trace);
        });

        Log.SetFactory(_loggerFactory);
        var logger = Log.For<App>();
        logger.LogInformation("WorldPainterUO started");
    }

    private void InitializeTheme()
    {
        var prefs = AppPreferences.Load();
        RequestedThemeVariant = prefs.Theme switch
        {
            "Light" => Avalonia.Styling.ThemeVariant.Light,
            _ => Avalonia.Styling.ThemeVariant.Dark,
        };
    }
}
