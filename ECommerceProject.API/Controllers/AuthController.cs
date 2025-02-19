using ECommerceProject.Core.DTOs;
using ECommerceProject.Core.Entities.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ECommerceProject.Core.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Logging;

namespace ECommerceProject.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly JwtConfig _jwtConfig;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            UserManager<ApplicationUser> userManager, 
            SignInManager<ApplicationUser> signInManager,
            IOptions<JwtConfig> jwtConfig,
            ILogger<AuthController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _jwtConfig = jwtConfig.Value;
            _logger = logger;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto model)
        {
            var user = new ApplicationUser
            {
                UserName = model.UserName,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                return Ok(new { message = "Kullanıcı başarıyla oluşturuldu" });
            }

            return BadRequest(result.Errors);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                return BadRequest("Kullanıcı bulunamadı");
            }

            var result = await _signInManager.PasswordSignInAsync(user, model.Password, false, false);

            if (result.Succeeded)
            {
                // JWT token oluştur
                var token = await GenerateJwtToken(user);
                
                return Ok(new { 
                    token = token,
                    message = "Giriş başarılı" 
                });
            }

            return BadRequest("Giriş başarısız");
        }

        [HttpPost("register-seller")]
        public async Task<ActionResult<ApiResponse<AuthResponseDto>>> RegisterSeller(RegisterDto registerDto)
        {
            try
            {
                // Kullanıcıyı oluştur
                var user = new ApplicationUser
                {
                    UserName = registerDto.Email,
                    Email = registerDto.Email,
                    FirstName = registerDto.FirstName,
                    LastName = registerDto.LastName
                };

                var result = await _userManager.CreateAsync(user, registerDto.Password);

                if (result.Succeeded)
                {
                    // Seller rolünü ekle
                    await _userManager.AddToRoleAsync(user, "Seller");

                    // Token oluştur
                    var token = await GenerateJwtToken(user);

                    return Ok(new ApiResponse<AuthResponseDto>
                    {
                        Data = new AuthResponseDto
                        {
                            Token = token,
                            Email = user.Email,
                            FirstName = user.FirstName,
                            LastName = user.LastName
                        }
                    });
                }

                return BadRequest(new ApiResponse<AuthResponseDto>
                {
                    Error = "Kullanıcı oluşturulurken hata oluştu: " + string.Join(", ", result.Errors.Select(e => e.Description))
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Satıcı kaydı sırasında hata oluştu");
                return StatusCode(500, new ApiResponse<AuthResponseDto>
                {
                    Error = "Satıcı kaydı sırasında bir hata oluştu"
                });
            }
        }

        private async Task<string> GenerateJwtToken(ApplicationUser user)
        {
            // Token içinde taşınacak bilgiler (claims)
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email, user.Email ?? ""),
                new Claim(ClaimTypes.Name, user.UserName ?? ""),
                new Claim("FirstName", user.FirstName ?? ""),
                new Claim("LastName", user.LastName ?? "")
            };

            // Kullanıcının rollerini ekle
            var roles = await _userManager.GetRolesAsync(user);
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            // Token'ı imzalamak için kullanılacak key
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtConfig.Key));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // Token'ı oluştur
            var token = new JwtSecurityToken(
                issuer: _jwtConfig.Issuer,
                audience: _jwtConfig.Audience,
                claims: claims,
                expires: DateTime.Now.AddMinutes(_jwtConfig.DurationInMinutes),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
} 