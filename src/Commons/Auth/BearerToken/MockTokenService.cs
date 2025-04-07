using Microsoft.Extensions.Configuration;

namespace Commons.Auth.BearerToken
{
    public class MockTokenService : ITokenService
    {
        private readonly string _token;

        public MockTokenService(IConfiguration config)
        {
            string? token = config.GetValue<string>("token");
            if (token is null)
                throw new ArgumentNullException("Mock tocken not set");
            _token = token;
        }

        public Task<string> GetTokenAsync() => Task.FromResult(_token);
    }
}
