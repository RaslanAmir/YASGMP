using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Xunit;
using YasGMP.Models;
using YasGMP.Wpf.Services;
using YasGMP.Wpf.ViewModels;

namespace YasGMP.Wpf.Tests
{
    public class LoginViewModelTests
    {
        [Fact]
        public async Task LoginCommand_WhenAuthenticationSucceeds_UpdatesSessionAndCloses()
        {
            // Arrange
            var authenticator = new StubAuthenticator
            {
                UserToReturn = new User { Id = 42, Username = "operator", FullName = "Operator" },
                SessionId = "session-123"
            };
            var configuration = new ConfigurationBuilder().Build();
            var session = new UserSession(configuration);
            var viewModel = new LoginViewModel(authenticator, session)
            {
                Username = "operator"
            };
            viewModel.SetPassword("password");

            bool? closedResult = null;
            viewModel.RequestClose += (_, accepted) => closedResult = accepted;

            // Act
            await viewModel.LoginCommand.ExecuteAsync(null);

            // Assert
            Assert.True(closedResult);
            Assert.NotNull(session.CurrentUser);
            Assert.Equal(42, session.UserId);
            Assert.Equal("session-123", session.SessionId);
        }

        [Fact]
        public async Task LoginCommand_WhenAuthenticationFails_ShowsErrorAndRemainsOpen()
        {
            // Arrange
            var authenticator = new StubAuthenticator
            {
                UserToReturn = null,
                SessionId = null
            };
            var configuration = new ConfigurationBuilder().Build();
            var session = new UserSession(configuration);
            var viewModel = new LoginViewModel(authenticator, session)
            {
                Username = "unknown"
            };
            viewModel.SetPassword("bad");

            bool closeCalled = false;
            viewModel.RequestClose += (_, _) => closeCalled = true;

            // Act
            await viewModel.LoginCommand.ExecuteAsync(null);

            // Assert
            Assert.False(closeCalled);
            Assert.Null(session.CurrentUser);
            Assert.False(string.IsNullOrWhiteSpace(viewModel.ErrorMessage));
        }

        private sealed class StubAuthenticator : IAuthenticator
        {
            public User? UserToReturn { get; set; }

            public string? SessionId { get; set; }

            public User? CurrentUser { get; private set; }

            public string? CurrentSessionId => SessionId;

            public Task<User?> AuthenticateAsync(string username, string password)
            {
                CurrentUser = UserToReturn;
                return Task.FromResult(UserToReturn);
            }
        }
    }
}
