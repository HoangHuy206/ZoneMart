using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Fido2NetLib;
using Fido2NetLib.Objects;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using MongoDB.Driver;
using ZoneMart.Server.Models;
using ZoneMart.Server.Services;

namespace ZoneMart.Server.Controllers;

[ApiController]
[Route("api/auth/webauthn")]
public class WebAuthnController : ControllerBase
{
    private readonly IFido2 _fido2;
    private readonly MongoDbService _mongoService;
    private readonly IMemoryCache _cache;
    private readonly Fido2Configuration _fidoConfig;

    public WebAuthnController(
        IFido2 fido2,
        MongoDbService mongoService,
        IMemoryCache cache,
        Fido2Configuration fidoConfig)
    {
        _fido2 = fido2;
        _mongoService = mongoService;
        _cache = cache;
        _fidoConfig = fidoConfig;
    }

    /// <summary>
    /// Tự động cập nhật RPID và Origins theo domain hiện tại (hỗ trợ localhost, ngrok, devtunnels, production)
    /// </summary>
    private void UpdateFidoDomainAndOrigin(string? clientHost = null, string? clientOrigin = null)
    {
        string host = clientHost?.Trim() ?? "";
        string origin = clientOrigin?.Trim() ?? "";

        // Nếu client không gửi lên thì tự động trích xuất từ các HTTP Header tiêu chuẩn
        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(origin))
        {
            string reqOrigin = Request.Headers["Origin"].FirstOrDefault() ?? "";
            string forwardedHost = Request.Headers["X-Forwarded-Host"].FirstOrDefault() ?? "";
            string forwardedProto = Request.Headers["X-Forwarded-Proto"].FirstOrDefault() ?? "https";
            string referer = Request.Headers.Referer.FirstOrDefault() ?? "";

            if (!string.IsNullOrEmpty(reqOrigin) && Uri.TryCreate(reqOrigin, UriKind.Absolute, out var uriOrigin))
            {
                if (string.IsNullOrWhiteSpace(host)) host = uriOrigin.Host;
                if (string.IsNullOrWhiteSpace(origin)) origin = $"{uriOrigin.Scheme}://{uriOrigin.Authority}";
            }
            else if (!string.IsNullOrEmpty(forwardedHost))
            {
                var splitted = forwardedHost.Split(':')[0];
                if (string.IsNullOrWhiteSpace(host)) host = splitted;
                if (string.IsNullOrWhiteSpace(origin)) origin = $"{forwardedProto}://{forwardedHost}";
            }
            else if (!string.IsNullOrEmpty(referer) && Uri.TryCreate(referer, UriKind.Absolute, out var uriRef))
            {
                if (string.IsNullOrWhiteSpace(host)) host = uriRef.Host;
                if (string.IsNullOrWhiteSpace(origin)) origin = $"{uriRef.Scheme}://{uriRef.Authority}";
            }
            else
            {
                if (string.IsNullOrWhiteSpace(host)) host = Request.Host.Host;
                if (string.IsNullOrWhiteSpace(origin)) origin = $"{Request.Scheme}://{Request.Host.Value}";
            }
        }

        _fidoConfig.RPID = host;
        lock (_fidoConfig)
        {
            var updatedOrigins = new HashSet<string>(_fidoConfig.Origins ?? new HashSet<string>(), StringComparer.OrdinalIgnoreCase)
            {
                origin,
                $"https://{host}",
                $"http://{host}",
                "http://localhost:5173",
                "http://127.0.0.1:5173",
                "http://localhost:5000",
                "http://127.0.0.1:5000"
            };
            if (origin.Contains("://"))
            {
                var schemeAndHost = origin.Substring(0, origin.IndexOf("://") + 3) + host;
                updatedOrigins.Add(schemeAndHost);
            }
            _fidoConfig.Origins = updatedOrigins;
        }
    }

    [HttpGet("check-passkey")]
    public async Task<IActionResult> CheckPasskey([FromQuery] string username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return BadRequest(new { success = false, message = "Vui lòng cung cấp username!" });
        }

        var clean = username.Trim().ToLowerInvariant();
        var user = await _mongoService.Users.Find(u =>
            !u.IsDeleted &&
            (u.PhoneEmail.ToLower() == clean || (u.Phone != null && u.Phone == clean) || u.UserCode == clean)
        ).FirstOrDefaultAsync();

        if (user == null)
        {
            return Ok(new { success = true, hasPasskey = false, registered = false });
        }

        bool hasPasskey = user.PasskeyCredentials != null && user.PasskeyCredentials.Count > 0;
        return Ok(new
        {
            success = true,
            hasPasskey,
            registered = true,
            fullName = user.FullName ?? "",
            devices = user.PasskeyCredentials?.Select(c => c.DeviceName).ToList() ?? new List<string>()
        });
    }
    [HttpPost("clear-passkeys")]
    public async Task<IActionResult> ClearPasskeys([FromQuery] string username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return BadRequest(new { success = false, message = "Vui lòng cung cấp username!" });
        }

        var clean = username.Trim().ToLowerInvariant();
        var update = Builders<User>.Update.Set(u => u.PasskeyCredentials, new List<PasskeyCredential>());
        await _mongoService.Users.UpdateOneAsync(
            u => !u.IsDeleted && (u.PhoneEmail.ToLower() == clean || (u.Phone != null && u.Phone == clean) || u.UserCode == clean),
            update
        );

        return Ok(new { success = true, message = "Đã xóa toàn bộ khóa sinh trắc học Face ID của tài khoản!" });
    }


    [HttpPost("register-options")]
    public async Task<IActionResult> RegisterOptions([FromBody] WebAuthnRegisterOptionsRequest request)
    {
        try
        {
            UpdateFidoDomainAndOrigin(request.ClientHost, request.ClientOrigin);

            if (string.IsNullOrWhiteSpace(request.Username))
            {
                return BadRequest(new { success = false, message = "Vui lòng cung cấp tên đăng nhập hoặc số điện thoại / email!" });
            }

            var cleanUser = request.Username.Trim().ToLowerInvariant();
            var user = await _mongoService.Users.Find(u =>
                !u.IsDeleted &&
                (u.PhoneEmail.ToLower() == cleanUser || (u.Phone != null && u.Phone == cleanUser) || u.UserCode == cleanUser)
            ).FirstOrDefaultAsync();

            if (user == null)
            {
                return NotFound(new { success = false, message = "Không tìm thấy tài khoản người dùng để kích hoạt sinh trắc học!" });
            }

            var fidoUser = new Fido2User
            {
                DisplayName = user.FullName ?? "Người Dùng ZoneMart",
                Name = user.PhoneEmail,
                Id = Encoding.UTF8.GetBytes(user.Id!)
            };

            var existingKeys = (user.PasskeyCredentials ?? new List<PasskeyCredential>())
                .Select(c => new PublicKeyCredentialDescriptor(c.Id))
                .ToList();

            var authenticatorSelection = new AuthenticatorSelection
            {
                AuthenticatorAttachment = AuthenticatorAttachment.Platform,
                UserVerification = UserVerificationRequirement.Preferred,
                ResidentKey = ResidentKeyRequirement.Preferred
            };

            var options = _fido2.RequestNewCredential(new RequestNewCredentialParams
            {
                User = fidoUser,
                ExcludeCredentials = existingKeys,
                AuthenticatorSelection = authenticatorSelection,
                AttestationPreference = AttestationConveyancePreference.None
            });

            // Ghi đè rpId theo đúng host chuẩn
            options.Rp.Id = _fidoConfig.RPID;

            // Lưu options vào cache để verify
            var cacheKey = $"fido2_reg_{user.Id}";
            _cache.Set(cacheKey, options, TimeSpan.FromMinutes(5));

            return Content(options.ToJson(), "application/json");
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Lỗi khi tạo tùy chọn đăng ký sinh trắc học: " + ex.Message });
        }
    }

    [HttpPost("register-verify")]
    public async Task<IActionResult> RegisterVerify([FromBody] WebAuthnRegisterVerifyRequest request)
    {
        try
        {
            UpdateFidoDomainAndOrigin(request.ClientHost, request.ClientOrigin);

            if (string.IsNullOrWhiteSpace(request.Username) || request.AttestationResponse == null)
            {
                return BadRequest(new { success = false, message = "Dữ liệu xác minh không hợp lệ!" });
            }

            var cleanUser = request.Username.Trim().ToLowerInvariant();
            var user = await _mongoService.Users.Find(u =>
                !u.IsDeleted &&
                (u.PhoneEmail.ToLower() == cleanUser || (u.Phone != null && u.Phone == cleanUser) || u.UserCode == cleanUser)
            ).FirstOrDefaultAsync();

            if (user == null)
            {
                return NotFound(new { success = false, message = "Không tìm thấy tài khoản người dùng!" });
            }

            var cacheKey = $"fido2_reg_{user.Id}";
            if (!_cache.TryGetValue(cacheKey, out CredentialCreateOptions? origOptions) || origOptions == null)
            {
                return BadRequest(new { success = false, message = "Phiên đăng ký đã hết hạn. Vui lòng thử lại!" });
            }

            var success = await _fido2.MakeNewCredentialAsync(new MakeNewCredentialParams
            {
                AttestationResponse = request.AttestationResponse,
                OriginalOptions = origOptions,
                IsCredentialIdUniqueToUserCallback = async (param, cancellationToken) =>
                {
                    var existingUser = await _mongoService.Users.Find(u =>
                        !u.IsDeleted && u.PasskeyCredentials.Any(c => c.Id == param.CredentialId)
                    ).FirstOrDefaultAsync(cancellationToken);
                    // Hợp lệ nếu chưa ai sở hữu khóa này HOẶC khóa này thuộc chính tài khoản hiện tại (quét cập nhật lại)
                    return existingUser == null || existingUser.Id == user.Id;
                }
            });

            _cache.Remove(cacheKey);

            var newCred = new PasskeyCredential
            {
                Id = success.Id,
                PublicKey = success.PublicKey,
                UserHandle = success.User.Id,
                SignatureCounter = success.SignCount,
                AaGuid = success.AaGuid.ToString(),
                DeviceName = request.DeviceName ?? "Thiết bị sinh trắc học",
                BiometricType = request.BiometricType ?? "biometric",
                RegDate = DateTime.UtcNow
            };

            user.PasskeyCredentials ??= new List<PasskeyCredential>();
            // Nếu đã từng có bản ghi cùng CredentialId trên tài khoản này thì thay thế bản cũ, tránh trùng lặp danh sách
            user.PasskeyCredentials.RemoveAll(c => c.Id.SequenceEqual(newCred.Id));
            user.PasskeyCredentials.Add(newCred);

            var update = Builders<User>.Update.Set(u => u.PasskeyCredentials, user.PasskeyCredentials);
            await _mongoService.Users.UpdateOneAsync(u => u.Id == user.Id, update);

            return Ok(new
            {
                success = true,
                message = $"Đã liên kết thành công Face ID / Vân tay của thiết bị với tài khoản ZoneMart!",
                credentialId = Convert.ToBase64String(newCred.Id)
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Lỗi khi lưu xác thực sinh trắc học: " + ex.Message });
        }
    }

    [HttpPost("login-options")]
    public async Task<IActionResult> LoginOptions([FromBody] WebAuthnLoginOptionsRequest request)
    {
        try
        {
            UpdateFidoDomainAndOrigin(request.ClientHost, request.ClientOrigin);

            List<PublicKeyCredentialDescriptor>? allowedCredentials = null;

            if (!string.IsNullOrWhiteSpace(request.Username))
            {
                var cleanUser = request.Username.Trim().ToLowerInvariant();
                var user = await _mongoService.Users.Find(u =>
                    !u.IsDeleted &&
                    (u.PhoneEmail.ToLower() == cleanUser || (u.Phone != null && u.Phone == cleanUser) || u.UserCode == cleanUser)
                ).FirstOrDefaultAsync();

                if (user != null && (user.PasskeyCredentials == null || user.PasskeyCredentials.Count == 0))
                {
                    return Ok(new { success = false, hasPasskey = false, message = "Tài khoản này chưa từng lưu Face ID trên thiết bị!" });
                }

                if (user != null && user.PasskeyCredentials != null && user.PasskeyCredentials.Count > 0)
                {
                    allowedCredentials = user.PasskeyCredentials
                        .Select(c => new PublicKeyCredentialDescriptor(c.Id))
                        .ToList();
                }
            }

            var options = _fido2.GetAssertionOptions(new GetAssertionOptionsParams
            {
                AllowedCredentials = allowedCredentials,
                UserVerification = UserVerificationRequirement.Preferred
            });

            // Ghi đè rpId theo đúng host chuẩn
            options.RpId = _fidoConfig.RPID;

            // Cache options theo Challenge
            var challengeKey = $"fido2_login_{Convert.ToBase64String(options.Challenge)}";
            _cache.Set(challengeKey, options, TimeSpan.FromMinutes(5));

            return Content(options.ToJson(), "application/json");
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Lỗi khi tạo tùy chọn đăng nhập sinh trắc học: " + ex.Message });
        }
    }

    [HttpPost("login-verify")]
    public async Task<IActionResult> LoginVerify([FromBody] WebAuthnLoginVerifyRequest request)
    {
        try
        {
            UpdateFidoDomainAndOrigin(request.ClientHost, request.ClientOrigin);

            if (request.AssertionResponse == null || request.AssertionResponse.RawId == null)
            {
                return BadRequest(new { success = false, message = "Dữ liệu xác thực không hợp lệ!" });
            }

            var clientDataJson = Encoding.UTF8.GetString(request.AssertionResponse.Response.ClientDataJson);
            using var doc = JsonDocument.Parse(clientDataJson);
            if (!doc.RootElement.TryGetProperty("challenge", out var challengeProp))
            {
                return BadRequest(new { success = false, message = "Dữ liệu sinh trắc học không có challenge!" });
            }

            var challengeStr = challengeProp.GetString();
            byte[] challengeBytes;
            try
            {
                challengeBytes = Base64UrlDecode(challengeStr!);
            }
            catch
            {
                return BadRequest(new { success = false, message = "Challenge không đúng định dạng!" });
            }

            var challengeKey = $"fido2_login_{Convert.ToBase64String(challengeBytes)}";
            if (!_cache.TryGetValue(challengeKey, out AssertionOptions? origOptions) || origOptions == null)
            {
                return BadRequest(new { success = false, message = "Phiên đăng nhập sinh trắc học đã hết hạn. Vui lòng bấm quét lại!" });
            }

            byte[] rawCredId = request.AssertionResponse.RawId;
            var allUsersWithPasskeys = await _mongoService.Users.Find(u =>
                !u.IsDeleted && u.PasskeyCredentials != null && u.PasskeyCredentials.Count > 0
            ).ToListAsync();

            User? matchedUser = null;
            PasskeyCredential? matchedCred = null;

            foreach (var u in allUsersWithPasskeys)
            {
                var cred = u.PasskeyCredentials.FirstOrDefault(c => c.Id.SequenceEqual(rawCredId));
                if (cred != null)
                {
                    matchedUser = u;
                    matchedCred = cred;
                    break;
                }
            }

            if (matchedUser == null || matchedCred == null)
            {
                return NotFound(new { success = false, message = "Face ID của thiết bị này chưa được liên kết với bất kỳ tài khoản nào trên ZoneMart!" });
            }

            if (matchedUser.AccountStatus == "banned")
            {
                return StatusCode(403, new { success = false, message = "Tài khoản của bạn đã bị khóa vĩnh viễn do vi phạm quy định sàn ZoneMart!" });
            }

            if (matchedUser.AccountStatus == "locked_10_days" && matchedUser.LockUntil.HasValue && matchedUser.LockUntil.Value > DateTime.UtcNow)
            {
                return StatusCode(403, new { success = false, message = $"Tài khoản đang bị tạm khóa đến {matchedUser.LockUntil.Value.ToLocalTime():dd/MM/yyyy HH:mm}!" });
            }

            var res = await _fido2.MakeAssertionAsync(new MakeAssertionParams
            {
                AssertionResponse = request.AssertionResponse,
                OriginalOptions = origOptions,
                StoredPublicKey = matchedCred.PublicKey,
                StoredSignatureCounter = matchedCred.SignatureCounter,
                IsUserHandleOwnerOfCredentialIdCallback = (param, cancellationToken) =>
                {
                    return Task.FromResult(true);
                }
            });

            _cache.Remove(challengeKey);

            matchedCred.SignatureCounter = res.SignCount;
            var update = Builders<User>.Update.Set(u => u.PasskeyCredentials, matchedUser.PasskeyCredentials);
            await _mongoService.Users.UpdateOneAsync(u => u.Id == matchedUser.Id, update);

            string determinedRole = "buyer";
            if (matchedUser.IsAdmin) determinedRole = "admin";
            else if (matchedUser.IsManager) determinedRole = "manager";
            else if (matchedUser.IsSeller) determinedRole = "seller";
            else if (matchedUser.IsShipper) determinedRole = "shipper";

            var userStore = await _mongoService.Stores.Find(s => s.UserId == matchedUser.Id).FirstOrDefaultAsync();
            string sellerStatus = userStore?.Status ?? "";
            string storeName = userStore?.StoreName ?? "";
            string rejectReason = userStore?.RejectReason ?? "";

            return Ok(new
            {
                success = true,
                message = $"Đăng nhập Face ID thành công vào tài khoản '{matchedUser.FullName}'!",
                user = new
                {
                    id = matchedUser.Id ?? "",
                    userCode = matchedUser.UserCode ?? "",
                    phoneEmail = matchedUser.PhoneEmail ?? "",
                    phone = matchedUser.Phone ?? "",
                    fullName = matchedUser.FullName ?? "Người Dùng ZoneMart",
                    avatarUrl = matchedUser.AvatarUrl ?? "",
                    role = determinedRole,
                    isAdmin = matchedUser.IsAdmin,
                    isManager = matchedUser.IsManager,
                    walletBalance = matchedUser.WalletBalance,
                    storeName = storeName,
                    sellerStatus = sellerStatus,
                    rejectReason = rejectReason,
                    faceAuthEnabled = true
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Lỗi khi xác minh sinh trắc học: " + ex.Message });
        }
    }

    private static byte[] Base64UrlDecode(string input)
    {
        string output = input;
        output = output.Replace('-', '+').Replace('_', '/');
        switch (output.Length % 4)
        {
            case 0: break;
            case 2: output += "=="; break;
            case 3: output += "="; break;
            default: throw new FormatException("Chuỗi Base64Url không hợp lệ");
        }
        return Convert.FromBase64String(output);
    }
}

public class WebAuthnRegisterOptionsRequest
{
    public string Username { get; set; } = string.Empty;
    public string? DeviceName { get; set; }
    public string? ClientHost { get; set; }
    public string? ClientOrigin { get; set; }
}

public class WebAuthnRegisterVerifyRequest
{
    public string Username { get; set; } = string.Empty;
    public AuthenticatorAttestationRawResponse? AttestationResponse { get; set; }
    public string? DeviceName { get; set; }
    public string? BiometricType { get; set; }
    public string? ClientHost { get; set; }
    public string? ClientOrigin { get; set; }
}

public class WebAuthnLoginOptionsRequest
{
    public string? Username { get; set; }
    public string? ClientHost { get; set; }
    public string? ClientOrigin { get; set; }
}

public class WebAuthnLoginVerifyRequest
{
    public AuthenticatorAssertionRawResponse? AssertionResponse { get; set; }
    public string? ClientHost { get; set; }
    public string? ClientOrigin { get; set; }
}
