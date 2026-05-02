public interface IAuthService
{
    Task<AuthResponseDto> LoginAsync(LoginDto dto);
}