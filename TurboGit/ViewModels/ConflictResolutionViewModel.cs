// TurboGit/ViewModels/ConflictResolutionViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Text;
using System.Threading.Tasks;
using System.IO;
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
        private string _preConflictContent = string.Empty;
        private string _postConflictContent = string.Empty;
        
        [ObservableProperty]
        private string _filePath = string.Empty;

        [ObservableProperty]
        private string _currentChanges = string.Empty;
        
        [ObservableProperty]
        private string _incomingChanges = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNotBusy))]
        private bool _isBusy = false;
        
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsResolved))]
        private string _resolvedContent = string.Empty;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        public bool IsNotBusy => !IsBusy;
        public bool IsResolved => !string.IsNullOrEmpty(ResolvedContent) && !IsBusy;

        // In a real app, this would be injected via DI.
        public ConflictResolutionViewModel() : this(new GeminiProResolver(new AiServiceConfig
            {
                ApiKey = Environment.GetEnvironmentVariable("TURBOGIT_GEMINI_API_KEY") ?? string.Empty
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
            var preBuilder = new StringBuilder();
            var currentBuilder = new StringBuilder();
            var incomingBuilder = new StringBuilder();
            var postBuilder = new StringBuilder();

            var lines = content.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            
            // simple state machine
            // 0: pre-conflict
            // 1: inside current (HEAD)
            // 2: inside incoming
            // 3: post-conflict (after first conflict)
            int state = 0;

            foreach (var line in lines)
            {
                if (state == 0)
                {
                    if (line.StartsWith("<<<<<<<"))
                    {
                        state = 1;
                    }
                    else
                    {
                        preBuilder.AppendLine(line);
                    }
                }
                else if (state == 1)
                {
                    if (line.StartsWith("======="))
                    {
                        state = 2;
                    }
                    else
                    {
                        currentBuilder.AppendLine(line);
                    }
                }
                else if (state == 2)
                {
                    if (line.StartsWith(">>>>>>>"))
                    {
                        state = 3;
                    }
                    else
                    {
                        incomingBuilder.AppendLine(line);
                    }
                }
                else if (state == 3)
                {
                    postBuilder.AppendLine(line);
                }
            }

            _preConflictContent = preBuilder.ToString();
            CurrentChanges = currentBuilder.ToString();
            IncomingChanges = incomingBuilder.ToString();
            _postConflictContent = postBuilder.ToString();
        }

        [RelayCommand]
        private async Task ResolveWithAi()
        {
            IsBusy = true;
            ErrorMessage = string.Empty;
            ResolvedContent = string.Empty;

            try
            {
                var fullConflictText = $"<<<<<<< HEAD\n{CurrentChanges}=======\n{IncomingChanges}>>>>>>> INCOMING";

                // Assume file extension gives language hint. e.g., "main.cs" -> "csharp"
                var languageHint = System.IO.Path.GetExtension(FilePath).TrimStart('.');

                ResolvedContent = await _aiResolver.ResolveConflictAsync(fullConflictText, languageHint);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"AI Resolution Failed: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private void AcceptSolution()
        {
            try
            {
                if (string.IsNullOrEmpty(FilePath)) return;

                // Reconstruct the file content
                // Note: ResolvedContent should not include markers.
                // We use TrimEnd on PreConflictContent because AppendLine adds a newline that might not be needed if Pre was empty?
                // Actually, ParseConflictedContent adds AppendLine for every line.
                // So _preConflictContent has a trailing newline.
                // _postConflictContent has a trailing newline.
                // _resolvedContent comes from AI, might or might not have newline.

                // Simplistic reconstruction:
                var newContent = _preConflictContent + ResolvedContent + _postConflictContent;

                // Write to file
                File.WriteAllText(FilePath, newContent);

                Console.WriteLine($"Conflict for {FilePath} resolved and saved.");

                // Reset/Clear? Or Close View?
                // In a real VM, we might request Close via an event or service.
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to save solution: {ex.Message}";
            }
        }
    }
}
