using System.Security.Claims;
using System.Text.Encodings.Web;
using CocoCarbon.Domain.Entities;
using CocoCarbon.Domain.Enums;
using CocoCarbon.Infrastructure.Auth;
using CocoCarbon.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CocoCarbon.Api.Authentication;

public static class CocoClaimTypes
{
    public const string InternalUserId = "cococarbon:user_id";
    public const string FirebaseUid = "cococarbon:firebase_uid";
}

public sealed class FirebaseAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IFirebaseTokenVerifier tokenVerifier,
    CocoCarbonDbContext db,
    IConfiguration config)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "FirebaseBearer";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("Authorization", out var authHeaders))
        {
            return AuthenticateResult.NoResult();
        }

        var header = authHeaders.ToString();
        if (!header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return AuthenticateResult.NoResult();
        }

        var token = header["Bearer ".Length..].Trim();
        if (string.IsNullOrWhiteSpace(token))
        {
            return AuthenticateResult.Fail("Empty bearer token.");
        }

        VerifiedFirebaseIdentity identity;
        try
        {
            identity = await tokenVerifier.VerifyIdTokenAsync(token, Context.RequestAborted);
        }
        catch (TokenVerificationException ex)
        {
            Context.Items["AuthErrorCode"] = ex.ErrorCode;
            Context.Items["AuthErrorMessage"] = ex.Message;
            return AuthenticateResult.Fail(ex.Message);
        }

        var user = await db.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.FirebaseUid == identity.FirebaseUid, Context.RequestAborted);

        if (user is null)
        {
            var farmerRole = await db.Roles.FirstOrDefaultAsync(r => r.Code == RoleCode.Farmer, Context.RequestAborted);
            if (farmerRole is null)
            {
                return AuthenticateResult.Fail("System roles not initialized.");
            }

            user = new User
            {
                FirebaseUid = identity.FirebaseUid,
                Email = identity.Email,
                DisplayName = identity.DisplayName,
                Status = "Active"
            };
            user.UserRoles.Add(new UserRole
            {
                RoleId = farmerRole.Id,
                RoleCode = RoleCode.Farmer,
                ScopeType = "Global",
                ApprovalStatus = "Approved"
            });

            // Optional server-configured bootstrap admin email allowlist
            var bootstrapAdmins = (config["ADMIN_BOOTSTRAP_EMAILS"] ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (identity.EmailVerified &&
                bootstrapAdmins.Contains(identity.Email, StringComparer.OrdinalIgnoreCase))
            {
                var adminRole = await db.Roles.FirstOrDefaultAsync(r => r.Code == RoleCode.Admin, Context.RequestAborted);
                if (adminRole is not null)
                {
                    user.UserRoles.Add(new UserRole
                    {
                        RoleId = adminRole.Id,
                        RoleCode = RoleCode.Admin,
                        ScopeType = "Global",
                        ApprovalStatus = "Approved"
                    });
                }
            }

            db.Users.Add(user);
            await db.SaveChangesAsync(Context.RequestAborted);
        }

        if (!string.Equals(user.Status, "Active", StringComparison.OrdinalIgnoreCase))
        {
            return AuthenticateResult.Fail("Tài khoản đã bị khóa hoặc tạm ngưng.");
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(CocoClaimTypes.InternalUserId, user.Id.ToString()),
            new(CocoClaimTypes.FirebaseUid, user.FirebaseUid),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.DisplayName)
        };

        // ONLY add roles whose ApprovalStatus == "Approved"
        foreach (var ur in user.UserRoles.Where(r => string.Equals(r.ApprovalStatus, "Approved", StringComparison.OrdinalIgnoreCase)))
        {
            claims.Add(new Claim(ClaimTypes.Role, ur.RoleCode.ToString()));
        }

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, SchemeName));
        var ticket = new AuthenticationTicket(principal, SchemeName);
        return AuthenticateResult.Success(ticket);
    }

    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status401Unauthorized;
        Response.ContentType = "application/json";
        var code = Context.Items["AuthErrorCode"]?.ToString() ?? "UNAUTHORIZED";
        var msg = Context.Items["AuthErrorMessage"]?.ToString() ?? "Yêu cầu xác thực Firebase ID token hợp lệ.";
        await Response.WriteAsJsonAsync(new { errorCode = code, message = msg });
    }

    protected override async Task HandleForbiddenAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        Response.ContentType = "application/json";
        await Response.WriteAsJsonAsync(new
        {
            errorCode = "FORBIDDEN",
            message = "Bạn không có quyền thực hiện thao tác hoặc truy cập tài nguyên này."
        });
    }
}
