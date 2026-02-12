using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using TurboGit.Core.Models;

namespace TurboGit.Services
{
    public class RepositoryService : IRepositoryService
    {
        private readonly string _configPath;
        private List<LocalRepository> _repositories;

        public RepositoryService()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var turboGitPath = Path.Combine(appDataPath, "TurboGit");
            if (!Directory.Exists(turboGitPath))
            {
                Directory.CreateDirectory(turboGitPath);
            }
            _configPath = Path.Combine(turboGitPath, "repositories.json");
            _repositories = new List<LocalRepository>();
        }

        public async Task<IEnumerable<LocalRepository>> GetRepositoriesAsync()
        {
            if (_repositories.Count == 0 && File.Exists(_configPath))
            {
                try
                {
                    var json = await File.ReadAllTextAsync(_configPath);
                    _repositories = JsonSerializer.Deserialize<List<LocalRepository>>(json) ?? new List<LocalRepository>();
                }
                catch (JsonException ex)
                {
                     Console.WriteLine($"Error deserializing repositories: {ex.Message}");
                     // If the file is corrupted (invalid JSON), rename it to .bak and start with an empty list.
                     // This prevents the app from crashing or getting stuck in a broken state.
                     try
                     {
                        var backupPath = _configPath + ".bak";
                        if (File.Exists(backupPath)) File.Delete(backupPath);
                        File.Move(_configPath, backupPath);
                     }
                     catch(Exception moveEx)
                     {
                         Console.WriteLine($"Failed to rename corrupted config file: {moveEx.Message}");
                     }
                     _repositories = new List<LocalRepository>();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading repositories: {ex.Message}");
                    _repositories = new List<LocalRepository>();
                }
            }

            return _repositories;
        }

        public async Task AddRepositoryAsync(LocalRepository repository)
        {
            if (!_repositories.Any(r => r.Path == repository.Path))
            {
                _repositories.Add(repository);
                await SaveRepositoriesAsync();
            }
        }

        public async Task RemoveRepositoryAsync(LocalRepository repository)
        {
            var existing = _repositories.FirstOrDefault(r => r.Path == repository.Path);
            if (existing != null)
            {
                _repositories.Remove(existing);
                await SaveRepositoriesAsync();
            }
        }

        private async Task SaveRepositoriesAsync()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(_repositories, options);
                await File.WriteAllTextAsync(_configPath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving repositories: {ex.Message}");
            }
        }
    }
}
