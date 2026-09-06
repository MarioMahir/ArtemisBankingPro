using ArtemisBankingPro.Infrastructure.Identity.Contexts;
using ArtemisBankingPro.Infrastructure.Identity.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ArtemisBankingPro.Tests.Integration.Identity;

/// <summary>Pruebas del contexto de Identity sobre SQLite en memoria (usuarios, roles y tokens).</summary>
public class IdentityContextTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly IdentityContext _context;

    public IdentityContextTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _context = CreateContext();
        _context.Database.EnsureCreated();
    }

    private IdentityContext CreateContext() => new(
        new DbContextOptionsBuilder<IdentityContext>()
            .UseSqlite(_connection)
            .Options);

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }

    private static ApplicationUser Usuario(
        string id, string userName, string cedula, string email) => new()
    {
        Id = id,
        UserName = userName,
        NormalizedUserName = userName.ToUpperInvariant(),
        Email = email,
        NormalizedEmail = email.ToUpperInvariant(),
        FirstName = "Ana",
        LastName = "Pérez",
        Identification = cedula,
        IsActive = true,
        CreatedAt = DateTime.Now
    };

    // ---------------- Usuarios ----------------

    [Fact]
    public async Task AddUsuario_SePersisteConCedulaComoTexto()
    {
        // Arrange: cédula con cero inicial (por eso es texto).
        _context.Users.Add(Usuario("u1", "ana", "00112345678", "ana@test.com"));

        // Act
        await _context.SaveChangesAsync();

        // Assert
        using var verificacion = CreateContext();
        var guardado = await verificacion.Users.SingleAsync();
        Assert.Equal("00112345678", guardado.Identification);
        Assert.True(guardado.IsActive);
    }

    [Fact]
    public async Task AddUsuario_CedulaDuplicada_LanzaPorIndiceUnico()
    {
        // Arrange
        _context.Users.Add(Usuario("u1", "ana", "00112345678", "ana@test.com"));
        await _context.SaveChangesAsync();

        // Act + Assert: la cédula es única en el sistema.
        _context.Users.Add(Usuario("u2", "otro", "00112345678", "otro@test.com"));
        await Assert.ThrowsAsync<DbUpdateException>(() => _context.SaveChangesAsync());
    }

    [Fact]
    public async Task AddUsuario_ConComercioAsociado_GuardaElCommerceId()
    {
        // Arrange
        var usuario = Usuario("u1", "comercio1", "00100000001", "c@test.com");
        usuario.CommerceId = 42;
        _context.Users.Add(usuario);

        // Act
        await _context.SaveChangesAsync();

        // Assert
        using var verificacion = CreateContext();
        Assert.Equal(42, (await verificacion.Users.SingleAsync()).CommerceId);
    }

    // ---------------- VerificationTokens ----------------

    [Fact]
    public async Task AddToken_SePersisteAsociadoAlUsuarioYSinUsar()
    {
        // Arrange
        _context.VerificationTokens.Add(new VerificationToken
        {
            UserId = "u1",
            Token = "token-activacion-123",
            Type = VerificationTokenType.Activation,
            CreatedAt = DateTime.Now,
            IsUsed = false
        });

        // Act
        await _context.SaveChangesAsync();

        // Assert
        using var verificacion = CreateContext();
        var guardado = await verificacion.VerificationTokens.SingleAsync();
        Assert.Equal("u1", guardado.UserId);
        Assert.Equal(VerificationTokenType.Activation, guardado.Type);
        Assert.False(guardado.IsUsed);
    }

    [Fact]
    public async Task AddToken_TokenDuplicado_LanzaPorIndiceUnico()
    {
        // Arrange
        _context.VerificationTokens.Add(new VerificationToken
        {
            UserId = "u1", Token = "mismo-token", Type = VerificationTokenType.PasswordReset,
            CreatedAt = DateTime.Now
        });
        await _context.SaveChangesAsync();

        // Act + Assert: el valor del token es único (un solo uso, sin colisiones).
        _context.VerificationTokens.Add(new VerificationToken
        {
            UserId = "u2", Token = "mismo-token", Type = VerificationTokenType.PasswordReset,
            CreatedAt = DateTime.Now
        });
        await Assert.ThrowsAsync<DbUpdateException>(() => _context.SaveChangesAsync());
    }

    [Fact]
    public async Task UpdateToken_MarcadoComoUsado_SePersiste()
    {
        // Arrange: un token de un solo uso se marca usado al consumirse.
        var token = new VerificationToken
        {
            UserId = "u1", Token = "token-1", Type = VerificationTokenType.PasswordReset,
            CreatedAt = DateTime.Now.AddMinutes(-5), IsUsed = false
        };
        _context.VerificationTokens.Add(token);
        await _context.SaveChangesAsync();

        // Act
        token.IsUsed = true;
        await _context.SaveChangesAsync();

        // Assert
        using var verificacion = CreateContext();
        Assert.True((await verificacion.VerificationTokens.SingleAsync()).IsUsed);
    }

    // ---------------- Roles ----------------

    [Fact]
    public async Task AddRoles_LosCuatroRolesDelSistemaSePersisten()
    {
        // Arrange
        foreach (var rol in Core.Domain.Constants.Roles.All)
        {
            _context.Roles.Add(new Microsoft.AspNetCore.Identity.IdentityRole(rol)
            {
                NormalizedName = rol.ToUpperInvariant()
            });
        }

        // Act
        await _context.SaveChangesAsync();

        // Assert
        using var verificacion = CreateContext();
        Assert.Equal(4, await verificacion.Roles.CountAsync());
        Assert.Contains(await verificacion.Roles.ToListAsync(),
            r => r.Name == Core.Domain.Constants.Roles.Comercio);
    }
}
