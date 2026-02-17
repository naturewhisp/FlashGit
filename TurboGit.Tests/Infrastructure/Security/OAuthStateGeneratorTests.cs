using Xunit;
using TurboGit.Infrastructure.Security;

namespace TurboGit.Tests.Infrastructure.Security
{
    public class OAuthStateGeneratorTests
    {
        [Fact]
        public void GenerateState_ShouldReturnNonEmptyString()
        {
            var state = OAuthStateGenerator.GenerateState();
            Assert.False(string.IsNullOrEmpty(state));
        }

        [Fact]
        public void GenerateState_ShouldReturnUniqueValues()
        {
            var state1 = OAuthStateGenerator.GenerateState();
            var state2 = OAuthStateGenerator.GenerateState();
            Assert.NotEqual(state1, state2);
        }
    }
}
