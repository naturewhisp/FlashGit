
// Namespace per i servizi dell'applicazione
namespace TurboGit.Services
{
    // Import delle librerie necessarie
    using LibGit2Sharp;
    using System;
    using System.IO;
    using System.IO.Compression;
    using System.Threading.Tasks;

    /// <summary>
    /// Fornisce la funzionalità per esportare un commit specifico come archivio Zip.
    /// L'archivio conterrà uno snapshot pulito del codice sorgente a quel punto,
    /// escludendo la directory .git.
    /// </summary>
    public class ZipExportService
    {
        /// <summary>
        /// Crea un archivio Zip in modo asincrono a partire da un commit specifico di un repository Git.
        /// </summary>
        /// <param name="repoPath">Il percorso del repository Git locale.</param>
        /// <param name="commitSha">L'hash SHA del commit da esportare.</param>
        /// <param name="outputPath">Il percorso completo dove salvare il file .zip risultante.</param>
        /// <returns>Un Task che rappresenta l'operazione asincrona.</returns>
        /// <exception cref="ArgumentException">Lanciata se uno dei parametri è nullo o vuoto.</exception>
        /// <exception cref="NotFoundException">Lanciata se il commit non viene trovato nel repository.</exception>
        public async Task CreateZipFromCommitAsync(string repoPath, string commitSha, string outputPath)
        {
            // Validazione degli input
            if (string.IsNullOrWhiteSpace(repoPath))
                throw new ArgumentException("Il percorso del repository non può essere nullo o vuoto.", nameof(repoPath));
            if (string.IsNullOrWhiteSpace(commitSha))
                throw new ArgumentException("Lo SHA del commit non può essere nullo o vuoto.", nameof(commitSha));
            if (string.IsNullOrWhiteSpace(outputPath))
                throw new ArgumentException("Il percorso di output non può essere nullo o vuoto.", nameof(outputPath));

            // Assicura che la directory di destinazione esista
            var outputDir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            // Esegue l'operazione su un thread in background per non bloccare la UI
            await Task.Run(() =>
            {
                // Apre il repository utilizzando LibGit2Sharp
                using (var repo = new Repository(repoPath))
                {
                    // Cerca il commit specifico tramite il suo SHA
                    var commit = repo.Lookup<Commit>(commitSha);
                    if (commit == null)
                    {
                        throw new NotFoundException($"Commit con SHA '{commitSha}' non trovato.");
                    }

                    // Crea il file Zip
                    using (var fileStream = new FileStream(outputPath, FileMode.Create))
                    using (var archive = new ZipArchive(fileStream, ZipArchiveMode.Create))
                    {
                        // Avvia il processo ricorsivo per aggiungere i file dall'albero del commit
                        AddTreeEntriesToArchive(commit.Tree, archive, "");
                    }
                }
            });
        }

        /// <summary>
        /// Metodo di supporto ricorsivo per navigare l'albero di Git e aggiungere file all'archivio.
        /// </summary>
        /// <param name="tree">L'oggetto Tree di Git da processare.</param>
        /// <param name="archive">L'istanza di ZipArchive a cui aggiungere i file.</param>
        /// <param name="currentPath">Il percorso della directory corrente all'interno dell'archivio.</param>
        private void AddTreeEntriesToArchive(Tree tree, ZipArchive archive, string currentPath)
        {
            foreach (var entry in tree)
            {
                // Costruisce il percorso completo del file o della cartella all'interno dello zip
                // Normalizza i separatori di percorso per la compatibilità con lo standard zip
                var entryPath = Path.Combine(currentPath, entry.Name).Replace('\\', '/');

                switch (entry.TargetType)
                {
                    case TreeEntryTargetType.Blob: // È un file
                        var blob = (Blob)entry.Target;
                        // Crea una nuova voce nell'archivio
                        var archiveEntry = archive.CreateEntry(entryPath, CompressionLevel.Optimal);
                        using (var entryStream = archiveEntry.Open())
                        using (var contentStream = blob.GetContentStream())
                        {
                            // Copia il contenuto del file nell'archivio
                            contentStream.CopyTo(entryStream);
                        }
                        break;

                    case TreeEntryTargetType.Tree: // È una directory
                        // Richiama ricorsivamente la funzione per la sotto-directory
                        AddTreeEntriesToArchive((Tree)entry.Target, archive, entryPath);
                        break;
                    
                    // I sottomoduli (GitLink) e altri tipi di oggetti vengono ignorati
                }
            }
        }
    }
}
