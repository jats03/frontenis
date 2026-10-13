using Frontenis.Application.Abstractions;
using Frontenis.Application.Dtos;
using Frontenis.Application.Services;
using Frontenis.Domain.Entities;
using Frontenis.Domain.Exceptions;
using Xunit;

namespace Frontenis.Tests;

public class AuthTests
{
    private sealed class FakeTime : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = new(2026, 10, 13, 12, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Now;
    }

    // Hash trivial: "h:" + contraseña. "old:" simula un hash con parámetros antiguos.
    private sealed class FakeHasher : IPasswordHasher
    {
        public string Hash(string password) => "h:" + password;
        public bool Verify(string password, string hash) => hash == "h:" + password || hash == "old:" + password;
        public bool NeedsRehash(string hash) => hash.StartsWith("old:");
    }

    private sealed class FakeTokens : ITokenService
    {
        public (string Token, DateTime ExpiresAtUtc) Create(User user) => ("token-" + user.Username, DateTime.UtcNow.AddHours(1));
    }

    private sealed class FakeUsers : IUserRepository
    {
        public List<User> Users = new();
        public string? LastHashSet;
        public Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default) =>
            Task.FromResult(Users.FirstOrDefault(u => u.Username == username));
        public Task<User?> GetByIdAsync(int id, CancellationToken ct = default) =>
            Task.FromResult(Users.FirstOrDefault(u => u.Id == id));
        public Task SetPasswordHashAsync(int id, string passwordHash, CancellationToken ct = default)
        {
            LastHashSet = passwordHash;
            var u = Users.First(x => x.Id == id);
            Users[Users.IndexOf(u)] = new User(u.Username, passwordHash, u.Role).WithId(id);
            return Task.CompletedTask;
        }
    }

    private static (AuthService Auth, FakeUsers Users, FakeTime Time) Build(string storedHash = "h:Correcta-Clave-2026")
    {
        var users = new FakeUsers();
        users.Users.Add(new User("admin", storedHash, Roles.Admin).WithId(1));
        var time = new FakeTime();
        return (new AuthService(users, new FakeHasher(), new FakeTokens(), new LoginThrottle(time)), users, time);
    }

    private static Task<LoginResponse> Login(AuthService a, string user, string pass, string ip = "1.1.1.1") =>
        a.LoginAsync(new LoginRequest(user, pass), ip);

    [Fact]
    public async Task Correct_login_returns_a_token()
    {
        var (auth, _, _) = Build();
        var r = await Login(auth, "admin", "Correcta-Clave-2026");
        Assert.Equal("token-admin", r.Token);
    }

    [Fact]
    public async Task Wrong_password_and_unknown_user_give_the_same_message()
    {
        var (auth, _, _) = Build();
        var a = await Assert.ThrowsAsync<UnauthorizedAppException>(() => Login(auth, "admin", "mala"));
        var b = await Assert.ThrowsAsync<UnauthorizedAppException>(() => Login(auth, "nadie", "mala"));
        Assert.Equal(a.Message, b.Message);
    }

    [Fact]
    public async Task Five_failures_lock_the_user_even_with_the_right_password_then_unlock_after_15_minutes()
    {
        var (auth, _, time) = Build();
        for (var i = 0; i < 5; i++)
            await Assert.ThrowsAsync<UnauthorizedAppException>(() => Login(auth, "admin", "mala", "9.9.9." + i));

        await Assert.ThrowsAsync<TooManyAttemptsException>(() => Login(auth, "admin", "Correcta-Clave-2026", "8.8.8.8"));

        time.Now = time.Now.AddMinutes(16);
        var ok = await Login(auth, "admin", "Correcta-Clave-2026", "8.8.8.8");
        Assert.Equal("token-admin", ok.Token);
    }

    [Fact]
    public async Task Twenty_failures_from_one_ip_lock_that_ip()
    {
        var (auth, _, _) = Build();
        for (var i = 0; i < 20; i++)
            await Assert.ThrowsAsync<UnauthorizedAppException>(() => Login(auth, "usuario" + i, "mala", "5.5.5.5"));
        await Assert.ThrowsAsync<TooManyAttemptsException>(() => Login(auth, "admin", "Correcta-Clave-2026", "5.5.5.5"));
    }

    [Fact]
    public async Task Old_hash_is_upgraded_after_successful_login()
    {
        var (auth, users, _) = Build("old:Correcta-Clave-2026");
        await Login(auth, "admin", "Correcta-Clave-2026");
        Assert.Equal("h:Correcta-Clave-2026", users.LastHashSet);
    }

    [Fact]
    public async Task Change_password_requires_current_and_a_strong_new_one()
    {
        var (auth, users, _) = Build();
        await Assert.ThrowsAsync<DomainException>(() =>
            auth.ChangePasswordAsync(1, new ChangePasswordRequest("equivocada", "Nueva-Clave-Segura-9"), "1.1.1.1"));
        await Assert.ThrowsAsync<DomainException>(() =>
            auth.ChangePasswordAsync(1, new ChangePasswordRequest("Correcta-Clave-2026", "corta"), "1.1.1.1"));

        await auth.ChangePasswordAsync(1, new ChangePasswordRequest("Correcta-Clave-2026", "Nueva-Clave-Segura-9"), "1.1.1.1");
        Assert.Equal("h:Nueva-Clave-Segura-9", users.LastHashSet);
        await Login(auth, "admin", "Nueva-Clave-Segura-9");
    }

    [Theory]
    [InlineData("corta", false)]
    [InlineData("aaaaaaaaaaaaaaaa", false)]
    [InlineData("admin-admin-admin1", false)]
    [InlineData("soloMinusculasLargas", true)]
    [InlineData("Frase larga de contraseña 2026", true)]
    [InlineData("Abc12345!xyz", true)]
    public void Password_policy(string password, bool accepted)
    {
        Assert.Equal(accepted, PasswordPolicy.Validate(password, "admin") is null);
    }
}
