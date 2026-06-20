using FileCraft.Services;
using FileCraft.Services.Interfaces;
using FileCraft.ViewModels;
using FileCraft.ViewModels.Functional;
using FileCraft.ViewModels.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Windows;

namespace FileCraft
{
    public partial class App : Application
    {
        private readonly IHost _host;

        public App()
        {
            _host = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    ConfigureServices(services);
                })
                .Build();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            await _host.StartAsync();

            var dbService = _host.Services.GetRequiredService<IDatabaseService>();
            dbService.Initialize();

            var saveService = _host.Services.GetRequiredService<ISaveService>();
            saveService.ImportLegacyPresets();

            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            var mainViewModel = _host.Services.GetRequiredService<MainViewModel>();

            mainWindow.Show();
            await mainViewModel.InitializeAsync();

            base.OnStartup(e);
        }

        private void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IAppPathProvider, AppPathProvider>();
            services.AddSingleton<IWindowFactory, WindowFactory>();
            services.AddSingleton<IDialogService, DialogService>();
            services.AddSingleton<IFileOperationService, FileOperationService>();
            services.AddSingleton<ICsvDelimiterDetector, CsvDelimiterDetector>();
            services.AddSingleton<ICsvParsingService, CsvParsingService>();
            services.AddSingleton<IClipboardService, ClipboardService>();
            services.AddSingleton<IFolderTreeService, FolderTreeService>();
            services.AddSingleton<ISaveService, SaveService>();
            services.AddSingleton<ISharedStateService, SharedStateService>();
            services.AddSingleton<IFileQueryService, FileQueryService>();
            services.AddSingleton<IAudioMetadataReaderService, AtlAudioMetadataReaderService>();
            services.AddSingleton<IUndoService, UndoService>();
            services.AddSingleton<IFolderTreeLinkService, FolderTreeLinkService>();
            services.AddSingleton<IPathPresetService, PathPresetService>();
            services.AddSingleton<IDatabaseService, DatabaseService>();

            services.AddTransient<FolderTreeManager>();
            services.AddTransient<PathPresetsViewModel>();

            services.AddSingleton<FileContentExportViewModel>();
            services.AddSingleton<TreeGeneratorViewModel>();
            services.AddSingleton<FolderContentExportViewModel>();
            services.AddSingleton<CsvViewerViewModel>();

            services.AddSingleton<OptionsPresetsViewModel>();
            services.AddSingleton<OptionsTreeToolsViewModel>();
            services.AddSingleton<OptionsGeneralViewModel>();
            services.AddSingleton<OptionsViewModel>();

            services.AddSingleton<PathSelectionViewModel>();
            services.AddSingleton<SessionHistoryViewModel>();

            services.AddSingleton<MainViewModel>();

            services.AddTransient<MainWindow>();
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            using (_host)
            {
                await _host.StopAsync(TimeSpan.FromSeconds(5));
            }

            base.OnExit(e);
        }
    }
}
