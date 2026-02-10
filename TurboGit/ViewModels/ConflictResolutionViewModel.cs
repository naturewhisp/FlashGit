// TurboGit/ViewModels/ConflictResolutionViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Text;
using System.Threading.Tasks;
using TurboGit.Services;

namespace TurboGit.ViewModels
{
    /// <summary>
    /// ViewModel for the ConflictResolutionView dialog.
    /// Manages the logic for parsing, resolving, and applying AI-powered conflict resolutions.
    /// </summary>
    public partial class ConflictResolutionViewModel : ObservableObject
    {
        private readonly IAiResolverService _aiResolver;
        
        [ObservableProperty]
        private string _filePath;

        [ObservableProperty]
        private string _currentChanges;
        
        [ObservableProperty]
        private string _incomingChanges;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNotBusy))]
        private bool _isBusy = false;
        
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsResolved))]
        private string _resolvedContent;

        public bool IsNotBusy => !IsBusy;
        public bool IsResolved => !string.IsNullOrEmpty(ResolvedContent) && !IsBusy;

        // In a real app, this would be injected via DI.
        public ConflictResolutionViewModel() : this(new GeminiProResolver(new AiServiceConfig
            {
                ApiKey = Environment.GetEnvironmentVariable("TURBOGIT_GEMINI_API_KEY")
            }))
        {
        }

        public ConflictResolutionViewModel(IAiResolverService aiResolver)
        {
            _aiResolver = aiResolver;
        }

        /// <summary>
        /// Loads the conflicted file content and parses it into the respective views.
        /// </summary>
        /// <param name="filePath">The path to the conflicted file.</param>
        /// <param name="fullContent">The full content of the file with conflict markers.</param>
        public void LoadConflict(string filePath, string fullContent)
        {
            FilePath = filePath;
            ParseConflictedContent(fullContent);
        }

        private void ParseConflictedContent(string content)
        {
            var current = new StringBuilder();
            var incoming = new StringBuilder();
            var activeBuilder = new StringBuilder(); // A temporary builder

            var lines = content.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            
            foreach (var line in lines)
            {
                if (line.StartsWith("<<<<<<<"))
                {
                    activeBuilder = current; // Start capturing current changes
                }
                else if (line.StartsWith("======="))
                {
                    activeBuilder = incoming; // Switch to capturing incoming changes
                }
                else if (line.StartsWith(">>>>>>>"))
                {
                    activeBuilder = new StringBuilder(); // Stop capturing
                }
                else
                {
                    activeBuilder.AppendLine(line);
                }
            }

            CurrentChanges = current.ToString();
            IncomingChanges = incoming.ToString();
        }

        [RelayCommand]
        private async Task ResolveWithAi()
        {
            IsBusy = true;
            ResolvedContent = string.Empty;

            var fullConflictText = $"<<<<<<< HEAD\n{CurrentChanges}=======\n{IncomingChanges}>>>>>>> INCOMING";
            
            // Assume file extension gives language hint. e.g., "main.cs" -> "csharp"
            var languageHint = System.IO.Path.GetExtension(FilePath).TrimStart('.');

            ResolvedContent = await _aiResolver.ResolveConflictAsync(fullConflictText, languageHint);
            
            IsBusy = false;
        }

        [RelayCommand]
        private void AcceptSolution()
        {
            // In a real implementation, this method would:
            // 1. Write the content of `ResolvedContent` back to the file on disk.
            // 2. Use LibGit2Sharp to `git add` the file, marking the conflict as resolved.
            // 3. Close the dialog window.
            Console.WriteLine($"Conflict for {FilePath} resolved. New content would be saved.");
        }
    }
}
