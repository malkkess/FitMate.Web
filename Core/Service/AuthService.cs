using System.Security.Cryptography;
using BCrypt.Net;
using DomainLayer.Contracts;
using DomainLayer.Models;
using Microsoft.Extensions.Logging;
using ServiceAbstraction;
using Shared.DataTransferObject;
using Shared.Enums;

namespace Service
{
    public class AuthService : IAuthService
    {
        private const int MinPasswordLength = 8;
        private const int ResetCodeLength = 6;
        private static readonly TimeSpan ResetCodeLifetime = TimeSpan.FromMinutes(15);

        private readonly IUnitOfWork _uow;
        private readonly ITokenService _tokenService;
        private readonly IEmailService _emailService;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            IUnitOfWork uow,
            ITokenService tokenService,
            IEmailService emailService,
            ILogger<AuthService> logger)
        {
            _uow = uow;
            _tokenService = tokenService;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request)
        {
            ValidateRegistration(request);

            var normalizedEmail = NormalizeEmail(request.Email);
            var users = await _uow.GetRepository<User, int>().GetAllAsync();
            if (users.Any(u => u.Email.Equals(normalizedEmail, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException("An account with this email already exists.");
            }

            var user = new User
            {
                FullName = request.FullName.Trim(),
                Email = normalizedEmail,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Age = request.Age,
                Height = request.Height,
                CurrentWeight = request.Weight,
                Gender = request.Gender,
                ActivityLevel = request.ActivityLevel,
                Goal = request.Goal,
            };

            var healthProfile = new HealthProfile { User = user };
            HealthProfileMapper.ApplyHealthProfile(healthProfile, request);

            await _uow.GetRepository<User, int>().AddAsync(user);
            await _uow.GetRepository<HealthProfile, int>().AddAsync(healthProfile);
            await _uow.SaveChangesAsync();

            return BuildAuthResponse(user, healthProfile);
        }

        public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                throw new InvalidOperationException("Email and password are required.");
            }

            var normalizedEmail = NormalizeEmail(request.Email);
            var users = await _uow.GetRepository<User, int>().GetAllAsync();
            var user = users.FirstOrDefault(u =>
                u.Email.Equals(normalizedEmail, StringComparison.OrdinalIgnoreCase));

            if (user is null)
            {
                throw new InvalidOperationException("No account found with this email.");
            }

            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                throw new InvalidOperationException("Incorrect password.");
            }

            var health = await GetHealthProfileAsync(user.Id);
            return BuildAuthResponse(user, health);
        }

        public async Task<MessageResponseDto> ForgotPasswordAsync(ForgotPasswordRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Email))
            {
                throw new InvalidOperationException("Email is required.");
            }

            var normalizedEmail = NormalizeEmail(request.Email);
            var users = await _uow.GetRepository<User, int>().GetAllAsync();
            var user = users.FirstOrDefault(u =>
                u.Email.Equals(normalizedEmail, StringComparison.OrdinalIgnoreCase));

            if (user is null)
            {
                throw new InvalidOperationException("No account found with this email.");
            }

            var code = GenerateResetCode();
            var resetRepo = _uow.GetRepository<PasswordResetCode, int>();
            var existingCodes = (await resetRepo.GetAllAsync())
                .Where(c => c.Email.Equals(normalizedEmail, StringComparison.OrdinalIgnoreCase) && !c.IsUsed)
                .ToList();

            foreach (var existing in existingCodes)
            {
                existing.IsUsed = true;
                existing.UpdatedAt = DateTime.UtcNow;
                resetRepo.Update(existing);
            }

            await resetRepo.AddAsync(new PasswordResetCode
            {
                Email = normalizedEmail,
                CodeHash = BCrypt.Net.BCrypt.HashPassword(code),
                ExpiresAt = DateTime.UtcNow.Add(ResetCodeLifetime),
            });

            await _uow.SaveChangesAsync();
            await _emailService.SendPasswordResetCodeAsync(normalizedEmail, code);

            _logger.LogInformation("Password reset code generated for {Email}", normalizedEmail);

            return new MessageResponseDto
            {
                Message = "A verification code has been sent to your email.",
            };
        }

        public async Task<MessageResponseDto> ResetPasswordAsync(ResetPasswordRequestDto request)
        {
            ValidateResetPassword(request);

            var normalizedEmail = NormalizeEmail(request.Email);
            var users = await _uow.GetRepository<User, int>().GetAllAsync();
            var user = users.FirstOrDefault(u =>
                u.Email.Equals(normalizedEmail, StringComparison.OrdinalIgnoreCase));

            if (user is null)
            {
                throw new InvalidOperationException("No account found with this email.");
            }

            var resetRepo = _uow.GetRepository<PasswordResetCode, int>();
            var activeCode = (await resetRepo.GetAllAsync())
                .Where(c =>
                    c.Email.Equals(normalizedEmail, StringComparison.OrdinalIgnoreCase) &&
                    !c.IsUsed &&
                    c.ExpiresAt >= DateTime.UtcNow)
                .OrderByDescending(c => c.CreatedAt)
                .FirstOrDefault();

            if (activeCode is null || !BCrypt.Net.BCrypt.Verify(request.Code.Trim(), activeCode.CodeHash))
            {
                throw new InvalidOperationException("Invalid or expired verification code.");
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;
            _uow.GetRepository<User, int>().Update(user);

            activeCode.IsUsed = true;
            activeCode.UpdatedAt = DateTime.UtcNow;
            resetRepo.Update(activeCode);

            await _uow.SaveChangesAsync();

            return new MessageResponseDto
            {
                Message = "Password has been reset successfully. You can now log in.",
            };
        }

        private AuthResponseDto BuildAuthResponse(User user, HealthProfile? health)
        {
            var token = _tokenService.GenerateToken(user.Id, user.Email, user.FullName, out var expiresAt);
            return new AuthResponseDto
            {
                Token = token,
                ExpiresAt = expiresAt,
                User = HealthProfileMapper.MapToProfileDto(user, health),
            };
        }

        private async Task<HealthProfile?> GetHealthProfileAsync(int userId)
        {
            var profiles = await _uow.GetRepository<HealthProfile, int>().GetAllAsync();
            return profiles.FirstOrDefault(h => h.UserId == userId);
        }

        private static void ValidateRegistration(RegisterRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.FullName))
                throw new InvalidOperationException("Full name is required.");

            if (string.IsNullOrWhiteSpace(request.Email))
                throw new InvalidOperationException("Email is required.");

            if (!request.Email.Contains('@'))
                throw new InvalidOperationException("Email format is invalid.");

            if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < MinPasswordLength)
                throw new InvalidOperationException($"Password must be at least {MinPasswordLength} characters.");

            if (request.Age < 13 || request.Age > 120)
                throw new InvalidOperationException("Age must be between 13 and 120.");

            if (request.Height <= 0 || request.Weight <= 0)
                throw new InvalidOperationException("Height and weight must be greater than zero.");

            if (!Enum.IsDefined(typeof(Gender), request.Gender))
                throw new InvalidOperationException("Invalid gender value.");

            if (!Enum.IsDefined(typeof(ActivityLevel), request.ActivityLevel))
                throw new InvalidOperationException("Invalid activity level value.");

            if (!Enum.IsDefined(typeof(Goal), request.Goal))
                throw new InvalidOperationException("Invalid goal value.");

            if (!Enum.IsDefined(typeof(DiabetesStatus), request.DiabetesStatus))
                throw new InvalidOperationException("Invalid diabetes status value.");
        }

        private static void ValidateResetPassword(ResetPasswordRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Email))
                throw new InvalidOperationException("Email is required.");

            if (string.IsNullOrWhiteSpace(request.Code))
                throw new InvalidOperationException("Verification code is required.");

            if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < MinPasswordLength)
                throw new InvalidOperationException($"New password must be at least {MinPasswordLength} characters.");
        }

        private static string NormalizeEmail(string email) =>
            email.Trim().ToLowerInvariant();

        private static string GenerateResetCode()
        {
            var value = RandomNumberGenerator.GetInt32(0, 1_000_000);
            return value.ToString("D6");
        }
    }
}
