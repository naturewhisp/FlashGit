using System.Collections.Generic;
using System.Threading.Tasks;
using TurboGit.Core.Models;

namespace TurboGit.Services
{
    public interface IRepositoryService
    {
        Task<IEnumerable<LocalRepository>> GetRepositoriesAsync();
        Task AddRepositoryAsync(LocalRepository repository);
        Task RemoveRepositoryAsync(LocalRepository repository);
    }
}
