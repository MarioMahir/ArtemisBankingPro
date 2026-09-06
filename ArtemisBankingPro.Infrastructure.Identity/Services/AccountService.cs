using ArtemisBankingPro.Core.Application.Common;
using ArtemisBankingPro.Core.Application.Dtos.Email;
using ArtemisBankingPro.Core.Application.Dtos.Users;
using ArtemisBankingPro.Core.Application.Interfaces.Services;
using ArtemisBankingPro.Core.Domain.Constants;
using ArtemisBankingPro.Infrastructure.Identity.Contexts;
using ArtemisBankingPro.Infrastructure.Identity.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ArtemisBankingPro.Infrastructure.Identity.Services;

public class AccountService(
    UserManager<ApplicationUser> userManager,
    IdentityContext context,
    IEmailService emailService,
    IConfiguration configuration) : IAccountService
{
    // ---------------- Autenticación ----------------

    public async Task<ServiceResult<UserDto>> AuthenticateAsync(string userName, string password, string[] allowedRoles)
    {
        var user = await userManager.FindByNameAsync(userName);
        if (user is null || !await userManager.CheckPasswordAsync(user, password))
            return ServiceResult<UserDto>.Fail(Mensajes.CredencialesInvalidas);

        if (!user.IsActive)
            return ServiceResult<UserDto>.Fail(Mensajes.CuentaInactiva);

        var role = await GetRoleAsync(user);
        if (!allowedRoles.Contains(role))
            return ServiceResult<UserDto>.Fail(Mensajes.SinPermisosWeb);

        return ServiceResult<UserDto>.Ok(ToDto(user, role));
    }

    // ---------------- Activación ----------------

    public async Task<ServiceResult> ConfirmAccountAsync(string token)
    {
        var record = await context.VerificationTokens
            .FirstOrDefaultAsync(t => t.Token == token && t.Type == VerificationTokenType.Activation);

        if (record is null)
            return ServiceResult.Fail(Mensajes.EnlaceActivacionInvalido);

        if (record.IsUsed)
            return ServiceResult.Fail(Mensajes.EnlaceActivacionUsado);

        var user = await userManager.FindByIdAsync(record.UserId);
        if (user is null)
            return ServiceResult.Fail(Mensajes.EnlaceActivacionInvalido);

        user.IsActive = true;
        user.EmailConfirmed = true;
        await userManager.UpdateAsync(user);

        record.IsUsed = true;
        await context.SaveChangesAsync();

        return ServiceResult.Ok(Mensajes.CuentaActivada);
    }

    // ---------------- Restablecimiento de contraseña ----------------

    public async Task<ServiceResult> RequestPasswordResetAsync(string userName, bool tokenInBody, string[] allowedRoles)
    {
        var user = await userManager.FindByNameAsync(userName);
        if (user is null)
            return ServiceResult.Fail(Mensajes.UsuarioNoExiste);

        if (string.IsNullOrWhiteSpace(user.Email))
            return ServiceResult.Fail(Mensajes.UsuarioSinCorreo);

        var role = await GetRoleAsync(user);
        if (!allowedRoles.Contains(role))
            return ServiceResult.Fail(Mensajes.SinPermisosWeb);

        // Orden exacto del spec: 1) desactivar temporalmente, 2) generar token,
        // 3) asociarlo, 4) guardar fecha de generación, 5) enviar correo.
        user.IsActive = false;
        await userManager.UpdateAsync(user);

        var token = Guid.NewGuid().ToString("N");
        context.VerificationTokens.Add(new VerificationToken
        {
            UserId = user.Id,
            Token = token,
            Type = VerificationTokenType.PasswordReset,
            CreatedAt = DateTime.UtcNow,
            IsUsed = false
        });
        await context.SaveChangesAsync();

        var body = tokenInBody
            ? $"""
               <h2>Restablecimiento de contraseña</h2>
               <p>Utilice el siguiente token para restablecer su contraseña (vigencia: {AppConstants.ResetTokenExpirationMinutes} minutos):</p>
               <p><strong>{token}</strong></p>
               <p>Identificador de usuario: <strong>{user.Id}</strong></p>
               """
            : $"""
               <h2>Restablecimiento de contraseña</h2>
               <p>Para restablecer su contraseña haga clic en el siguiente enlace (vigencia: {AppConstants.ResetTokenExpirationMinutes} minutos):</p>
               <p><a href="{BuildLink("Reset", user.Id, token)}">Restablecer contraseña</a></p>
               """;

        await emailService.SendAsync(new EmailRequest
        {
            To = user.Email,
            Subject = "Restablecimiento de contraseña",
            HtmlBody = body
        });

        return ServiceResult.Ok(Mensajes.ResetEnviado);
    }

    public async Task<ServiceResult> ResetPasswordAsync(string userId, string token, string password, string confirmPassword)
    {
        var record = await context.VerificationTokens
            .FirstOrDefaultAsync(t => t.Token == token
                && t.UserId == userId
                && t.Type == VerificationTokenType.PasswordReset);

        if (record is null)
            return ServiceResult.Fail(Mensajes.EnlaceResetInvalido);

        if (record.IsUsed)
            return ServiceResult.Fail(Mensajes.EnlaceResetUsado);

        if (record.CreatedAt.AddMinutes(AppConstants.ResetTokenExpirationMinutes) < DateTime.UtcNow)
            return ServiceResult.Fail(Mensajes.EnlaceResetExpirado);

        if (password != confirmPassword)
            return ServiceResult.Fail(Mensajes.ContrasenasNoCoinciden);

        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
            return ServiceResult.Fail(Mensajes.EnlaceResetInvalido);

        var identityToken = await userManager.GeneratePasswordResetTokenAsync(user);
        var result = await userManager.ResetPasswordAsync(user, identityToken, password);
        if (!result.Succeeded)
            return ServiceResult.Fail(string.Join(" ", result.Errors.Select(e => e.Description)));

        record.IsUsed = true;
        await context.SaveChangesAsync();

        // Al completar el restablecimiento la cuenta se reactiva.
        user.IsActive = true;
        await userManager.UpdateAsync(user);

        return ServiceResult.Ok(Mensajes.ContrasenaRestablecida);
    }

    // ---------------- CRUD de usuarios ----------------

    public async Task<ServiceResult<UserDto>> CreateUserAsync(CreateUserRequest request)
    {
        if (await context.Users.AnyAsync(u => u.Identification == request.Identification))
            return ServiceResult<UserDto>.Fail(Mensajes.CedulaDuplicada);

        if (await userManager.FindByEmailAsync(request.Email) is not null)
            return ServiceResult<UserDto>.Fail(Mensajes.CorreoDuplicado);

        if (await userManager.FindByNameAsync(request.UserName) is not null)
            return ServiceResult<UserDto>.Fail(Mensajes.UsuarioDuplicado);

        if (request.Password != request.ConfirmPassword)
            return ServiceResult<UserDto>.Fail(Mensajes.ContrasenasNoCoinciden);

        if (request.InitialAmount is < 0)
            return ServiceResult<UserDto>.Fail(Mensajes.MontoInicialNegativo);

        var user = new ApplicationUser
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Identification = request.Identification,
            Email = request.Email,
            UserName = request.UserName,
            CommerceId = request.CommerceId,
            IsActive = false, // los usuarios creados por el sistema nacen inactivos
            CreatedAt = DateTime.UtcNow
        };

        var creation = await userManager.CreateAsync(user, request.Password);
        if (!creation.Succeeded)
            return ServiceResult<UserDto>.Fail(string.Join(" ", creation.Errors.Select(e => e.Description)));

        await userManager.AddToRoleAsync(user, request.Role);

        var token = Guid.NewGuid().ToString("N");
        context.VerificationTokens.Add(new VerificationToken
        {
            UserId = user.Id,
            Token = token,
            Type = VerificationTokenType.Activation,
            CreatedAt = DateTime.UtcNow,
            IsUsed = false
        });
        await context.SaveChangesAsync();

        var body = request.TokenInBody
            ? $"""
               <h2>Activación de cuenta</h2>
               <p>Bienvenido a Artemis Banking Pro. Utilice el siguiente token para activar su cuenta:</p>
               <p><strong>{token}</strong></p>
               """
            : $"""
               <h2>Activación de cuenta</h2>
               <p>Bienvenido a Artemis Banking Pro. Para activar su cuenta haga clic en el siguiente enlace:</p>
               <p><a href="{BuildLink("Activation", user.Id, token)}">Activar mi cuenta</a></p>
               """;

        var emailSent = await emailService.SendAsync(new EmailRequest
        {
            To = user.Email!,
            Subject = "Activación de cuenta",
            HtmlBody = body
        });

        var dto = ToDto(user, request.Role);
        return emailSent
            ? ServiceResult<UserDto>.Ok(dto)
            : ServiceResult<UserDto>.Ok(dto, Mensajes.CorreoActivacionFallido);
    }

    public async Task<ServiceResult> UpdateUserAsync(UpdateUserRequest request)
    {
        var user = await userManager.FindByIdAsync(request.Id);
        if (user is null)
            return ServiceResult.Fail(Mensajes.UsuarioSeleccionadoNoExiste);

        if (await context.Users.AnyAsync(u => u.Identification == request.Identification && u.Id != user.Id))
            return ServiceResult.Fail(Mensajes.CedulaDuplicada);

        var byEmail = await userManager.FindByEmailAsync(request.Email);
        if (byEmail is not null && byEmail.Id != user.Id)
            return ServiceResult.Fail(Mensajes.CorreoDuplicado);

        var byName = await userManager.FindByNameAsync(request.UserName);
        if (byName is not null && byName.Id != user.Id)
            return ServiceResult.Fail(Mensajes.UsuarioDuplicado);

        if (!string.IsNullOrWhiteSpace(request.Password) && request.Password != request.ConfirmPassword)
            return ServiceResult.Fail(Mensajes.ContrasenasNoCoinciden);

        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.Identification = request.Identification;
        user.Email = request.Email;
        user.UserName = request.UserName;

        var update = await userManager.UpdateAsync(user);
        if (!update.Succeeded)
            return ServiceResult.Fail(string.Join(" ", update.Errors.Select(e => e.Description)));

        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            var reset = await userManager.ResetPasswordAsync(user, token, request.Password);
            if (!reset.Succeeded)
                return ServiceResult.Fail(string.Join(" ", reset.Errors.Select(e => e.Description)));
        }

        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> SetUserStatusAsync(string userId, bool active, string actingUserId)
    {
        if (userId == actingUserId)
            return ServiceResult.Fail(Mensajes.NoModificarPropioEstado);

        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
            return ServiceResult.Fail(Mensajes.UsuarioSeleccionadoNoExiste);

        user.IsActive = active;
        await userManager.UpdateAsync(user);
        return ServiceResult.Ok();
    }

    // ---------------- Consultas ----------------

    public async Task<UserDto?> GetByIdAsync(string id)
    {
        var user = await userManager.FindByIdAsync(id);
        return user is null ? null : ToDto(user, await GetRoleAsync(user));
    }

    public async Task<UserDto?> GetByUserNameAsync(string userName)
    {
        var user = await userManager.FindByNameAsync(userName);
        return user is null ? null : ToDto(user, await GetRoleAsync(user));
    }

    public async Task<UserDto?> GetByIdentificationAsync(string identification)
    {
        var user = await context.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Identification == identification);
        return user is null ? null : ToDto(user, await GetRoleAsync(user));
    }

    public async Task<UserDto?> GetByCommerceIdAsync(int commerceId)
    {
        var user = await context.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.CommerceId == commerceId);
        return user is null ? null : ToDto(user, await GetRoleAsync(user));
    }

    public async Task<PagedResult<UserDto>> GetPagedAsync(int page, int pageSize, string? role, bool onlyCommerce)
    {
        var query =
            from user in context.Users.AsNoTracking()
            join userRole in context.UserRoles on user.Id equals userRole.UserId
            join roleRow in context.Roles on userRole.RoleId equals roleRow.Id
            select new { user, RoleName = roleRow.Name! };

        query = onlyCommerce
            ? query.Where(x => x.RoleName == Roles.Comercio)
            : query.Where(x => x.RoleName != Roles.Comercio);

        if (!string.IsNullOrWhiteSpace(role))
            query = query.Where(x => x.RoleName == role);

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(x => x.user.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new UserDto
            {
                Id = x.user.Id,
                FirstName = x.user.FirstName,
                LastName = x.user.LastName,
                Identification = x.user.Identification,
                Email = x.user.Email!,
                UserName = x.user.UserName!,
                Role = x.RoleName,
                IsActive = x.user.IsActive,
                CommerceId = x.user.CommerceId,
                CreatedAt = x.user.CreatedAt
            })
            .ToListAsync();

        return new PagedResult<UserDto> { Items = items, Page = page, PageSize = pageSize, TotalCount = total };
    }

    public async Task<List<UserDto>> GetActiveClientsAsync()
    {
        var query =
            from user in context.Users.AsNoTracking()
            join userRole in context.UserRoles on user.Id equals userRole.UserId
            join roleRow in context.Roles on userRole.RoleId equals roleRow.Id
            where roleRow.Name == Roles.Cliente && user.IsActive
            select user;

        var users = await query.OrderBy(u => u.FirstName).ToListAsync();
        return users.Select(u => ToDto(u, Roles.Cliente)).ToList();
    }

    public async Task<Dictionary<string, UserDto>> GetByIdsAsync(IEnumerable<string> ids)
    {
        var idList = ids.Distinct().ToList();
        var users = await context.Users.AsNoTracking()
            .Where(u => idList.Contains(u.Id))
            .ToListAsync();

        var result = new Dictionary<string, UserDto>();
        foreach (var user in users)
            result[user.Id] = ToDto(user, await GetRoleAsync(user));
        return result;
    }

    public async Task<int> CountByRoleAndStatusAsync(string role, bool active)
    {
        var query =
            from user in context.Users.AsNoTracking()
            join userRole in context.UserRoles on user.Id equals userRole.UserId
            join roleRow in context.Roles on userRole.RoleId equals roleRow.Id
            where roleRow.Name == role && user.IsActive == active
            select user.Id;

        return await query.CountAsync();
    }

    public async Task DeactivateUsersByCommerceAsync(int commerceId)
    {
        var users = await context.Users
            .Where(u => u.CommerceId == commerceId && u.IsActive)
            .ToListAsync();

        foreach (var user in users)
            user.IsActive = false;

        await context.SaveChangesAsync();
    }

    public async Task<ServiceResult> AssignCommerceAsync(string userId, int commerceId)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
            return ServiceResult.Fail(Mensajes.UsuarioSeleccionadoNoExiste);

        user.CommerceId = commerceId;
        await userManager.UpdateAsync(user);
        return ServiceResult.Ok();
    }

    // ---------------- Helpers ----------------

    private async Task<string> GetRoleAsync(ApplicationUser user) =>
        (await userManager.GetRolesAsync(user)).FirstOrDefault() ?? string.Empty;

    private string BuildLink(string kind, string userId, string token)
    {
        var template = configuration[$"AccountLinks:{kind}"]
            ?? throw new InvalidOperationException($"Falta la configuración AccountLinks:{kind}.");
        return string.Format(template, userId, token);
    }

    private static UserDto ToDto(ApplicationUser user, string role) => new()
    {
        Id = user.Id,
        FirstName = user.FirstName,
        LastName = user.LastName,
        Identification = user.Identification,
        Email = user.Email!,
        UserName = user.UserName!,
        Role = role,
        IsActive = user.IsActive,
        CommerceId = user.CommerceId,
        CreatedAt = user.CreatedAt
    };
}
