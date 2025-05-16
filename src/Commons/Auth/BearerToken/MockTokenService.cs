using Microsoft.Extensions.Configuration;

namespace Commons.Auth.BearerToken
{
    public class MockTokenService : ITokenService
    {
        private readonly string _token;

        public MockTokenService()
        {
            string token = File.ReadAllText("token");
            _token = token;
        }

        public Task<string> GetTokenAsync() => Task.FromResult(_token);
    }
}
