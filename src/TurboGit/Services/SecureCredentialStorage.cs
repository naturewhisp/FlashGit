using System;
using System.IO;

namespace TurboGit.Services
{
    /// <summary>
    /// Implementazione base per il salvataggio delle credenziali.
    /// ATTENZIONE: Questa implementazione salva il token in chiaro in una cartella utente.
    /// Per un'applicazione di produzione, si dovrebbe usare il Windows DataProtectionProvider
    /// o il Keychain di macOS per una maggiore sicurezza.
    /// </summary>
    public class SecureCredentialStorage : ICredentialStorage
    {
        private readonly string _filePath;

        public SecureCredentialStorage()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var turboGitFolder = Path.Combine(appDataPath, "TurboGit");
            Directory.CreateDirectory(turboGitFolder);
            _filePath = Path.Combine(turboGitFolder, "turbogit.token");
        }

        public void SaveToken(string token)
        {
            // WARNING: Plain text storage. For production, encrypt this data.
            File.WriteAllText(_filePath, token);
        }

        public string? GetToken()
        {
            if (File.Exists(_filePath))
            {
                return File.ReadAllText(_filePath);
            }
            return null;
        }

        public void ClearToken()
        {
            if (File.Exists(_filePath))
            {
                File.Delete(_filePath);
            }
        }
    }
}

