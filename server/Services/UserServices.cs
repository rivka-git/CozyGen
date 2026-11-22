using AutoMapper;
using DTOs;
using Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.IdentityModel.Tokens;
using Repository;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
namespace Services
{


    public class UserServices : IUserServices
    {
        IUserRepository _r;
        IMapper _mapper;
        IPasswordService _passwordService;
        IDistributedCache _cache;
        IConfiguration _configuration;
        public UserServices(IUserRepository i, IMapper mapperr, IPasswordService passwordService, IDistributedCache cache, IConfiguration configuration)
        {
            _r = i;
            _mapper = mapperr;
            _passwordService = passwordService;
            _cache = cache;
            _configuration = configuration;
        }
        private const string CacheKey = "all_users_list";
        public async Task<IEnumerable<User>> GetUsers()
        {
            var cachedUsers = await _cache.GetStringAsync(CacheKey);
            if (!string.IsNullOrEmpty(cachedUsers))
            {
                return JsonSerializer.Deserialize<List<User>>(cachedUsers);
            }
            var users = await _r.GetUsers();

            var ttlMinutes = _configuration.GetValue<int>("RedisSettings:DefaultTTLInMinutes");
            var options = new DistributedCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(ttlMinutes));

            var serializedData = JsonSerializer.Serialize(users);
            await _cache.SetStringAsync(CacheKey, serializedData, options);

            return users;
        }
        public async Task<DtoUser_Name_Gmail_Role_Id?> GetUserById(int id)
        {
            var u = await _r.GetUserById(id);
            var r = _mapper.Map<User, DtoUser_Name_Gmail_Role_Id>(u);
            return r;
        }
        public async Task<DtoAuthResponse?> AddNewUser(DtoUser_All user)
        {
            int d = _passwordService.GetStrengthByPassword(user.PasswordHash);
            if (d >= 2)
            {
                var userEntity = _mapper.Map<DtoUser_All, User>(user);
                userEntity.Role = "Customer";
                userEntity.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);
                var res = await _r.AddNewUser(userEntity);
                await _cache.RemoveAsync(CacheKey);

                return new DtoAuthResponse
                {
                    User = _mapper.Map<User, DtoUser_Name_Gmail_Role_Id>(res),
                    Token = GenerateJwtToken(res)
                };
            }
            return null;
        }

        public async Task<DtoAuthResponse?> Login(DtoUser_Gmail_Password value)
        {
            var u = await _r.GetUserByEmail(value.Email);
            if (u == null) return null;

            if (!await VerifyPasswordAsync(u, value.PasswordHash)) return null;

            return new DtoAuthResponse
            {
                User = _mapper.Map<User, DtoUser_Name_Gmail_Role_Id>(u),
                Token = GenerateJwtToken(u)
            };
        }

        // Verifies a password against the stored BCrypt hash.
        // Legacy users stored before hashing was introduced are upgraded on first successful login.
        private async Task<bool> VerifyPasswordAsync(User user, string password)
        {
            if (IsBcryptHash(user.PasswordHash))
            {
                return BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
            }

            if (user.PasswordHash != password) return false;

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
            await _r.Update(user.UserId, user);
            await _cache.RemoveAsync(CacheKey);
            return true;
        }

        private static bool IsBcryptHash(string hash) =>
            hash.StartsWith("$2a$") || hash.StartsWith("$2b$") || hash.StartsWith("$2y$");

        public async Task<DtoUser_Name_Gmail_Role_Id> Update(int id, DtoUser_All userDto)
        {

            int d = _passwordService.GetStrengthByPassword(userDto.PasswordHash);
            if (d < 2) return null;

            var existingUser = await _r.GetUserById(id);
            if (existingUser == null) return null;

            _mapper.Map(userDto, existingUser);
            existingUser.UserId = id;
            existingUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(userDto.PasswordHash);
            var res = await _r.Update(id, existingUser);
            await _cache.RemoveAsync(CacheKey);

            return _mapper.Map<User, DtoUser_Name_Gmail_Role_Id>(res);
        }
      
        private string GenerateJwtToken(User user)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var key = jwtSettings["Key"] ?? throw new InvalidOperationException("JwtSettings:Key is missing.");
            var expiryMinutes = jwtSettings.GetValue<int>("ExpiryMinutes");

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
                new(JwtRegisteredClaimNames.Email, user.Email),
                new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
                new(ClaimTypes.Role, user.Role),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expiryMinutes <= 0 ? 60 : expiryMinutes),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

    }
}