using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Iyu.Windows;
using Iyu.Windows.Mvvm;
using System;
using System.Runtime.CompilerServices;
using System.Text.Json;
using WebPageInspector.Models;
using WebPageInspector.Services;

namespace WebPageInspector.Pages
{
    public sealed partial class WebCrawlerPage : IView
    {
        public WebCrawlerPage()
        {
            this.InitializeComponent();

            Refresh();
        }

        private void Refresh()
        {
            this.InvokeInBusy(RefreshAsync);
        }

        private async Task RefreshAsync()
        {
            this.Tasks = await AppResolver.TaskService.GetTemplatesAsync();
        }

        [ObservableProperty] private IEnumerable<WebCrawlingTask> tasks;
        [ObservableProperty] private WebCrawlingTask? selectedTask;
        [ObservableProperty] private string? taskJson;
        [ObservableProperty] private int progress;
        [ObservableProperty] private string? logs;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(StartCommand), nameof(StopCommand))]
        private WebCrawlingTask? task;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(StartCommand), nameof(StopCommand))]
        private bool isProcess;

        partial void OnSelectedTaskChanged(WebCrawlingTask? value)
        {
            this.TaskJson = value?.BuildJson();
        }

        partial void OnTaskJsonChanged(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                this.Task = null;
            }
            else
            {
                this.Task = WebCrawlingTask.FromJson(value);
            }
        }

        [RelayCommand]
        private void ClearSelectedTask()
        {
            this.SelectedTask = null;
        }

        [RelayCommand(CanExecute = nameof(CanStart))]
        private async Task StartAsync()
        {
            if (Task == null) return;
            this.IsProcess = true;

            var crawler = new Crawler(this.wv);
            var result = await crawler.RunAsync(Task);

            Logs = JsonHelper.Serialize(result);
            Stop();
        }

        private bool CanStart() => this.Task is not null && this.IsProcess is false;

        [RelayCommand(CanExecute = nameof(CanStop))]
        private void Stop()
        {
            this.IsProcess = false;
        }

        private bool CanStop() => this.IsProcess is true;
    }
}