namespace Commons.Auth.BearerToken;

public interface ITokenService
{
    Task<string> GetTokenAsync();
}
