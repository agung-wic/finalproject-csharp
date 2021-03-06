using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PaymentAPI.Configuration;
using PaymentAPI.Models.DTOs.Requests;
using PaymentAPI.Models.DTOs.Responses;

using PaymentAPI.Data;
using Microsoft.EntityFrameworkCore;
using PaymentAPI.Models;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;

namespace PaymentAPI.Controllers
{
    [Route("api/[controller]")]
    // [ApiController]
    public class AuthManagementController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly JwtConfig _jwtConfig;
        private readonly TokenValidationParameters _tokenValidationParams;
        private readonly ApiDbContext _apiDbContext;

        public AuthManagementController(
            UserManager<IdentityUser> userManager,
            IOptionsMonitor<JwtConfig> optionsMonitor,
            TokenValidationParameters tokenValidationParams,
            ApiDbContext apiDbContext
        )
        {
            _userManager = userManager;
            _jwtConfig = optionsMonitor.CurrentValue;
            _tokenValidationParams = tokenValidationParams;
            _apiDbContext = apiDbContext;
        }

        [HttpPost]
        [Route("Register")]
        public async Task<IActionResult> Register([FromBody] UserRegistrationDto user)
        {
            if (ModelState.IsValid)
            {
                var existingUser = await _userManager.FindByEmailAsync(user.Email);
                if (existingUser != null)
                {
                    return BadRequest(new RegistrationResponse()
                    {
                        Errors = new List<string>() {
                            "Email already in use"
                        },
                        Success = false
                    });
                }

                var newUser = new IdentityUser() { Email = user.Email, UserName = user.Username };
                var isCreated = await _userManager.CreateAsync(newUser, user.Password);
                if (isCreated.Succeeded)
                {
                    // var jwtToken = GenerateJwtToken(newUser);

                    return Ok(new RegistrationResponse()
                    {
                        Success = true,
                        Messages = "Register Success"
                    });
                }
                else
                {
                    return BadRequest(new RegistrationResponse()
                    {
                        Errors = isCreated.Errors.Select(x => x.Description).ToList(),
                        Success = false
                    });
                }
            }

            return BadRequest(new RegistrationResponse()
            {
                Errors = new List<string>() {
                    "Invalid payload"
                },
                Success = false
            });
        }

        private NewRefreshToken GenerateJwtToken(IdentityUser user)
        {
            var jwtTokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtConfig.Secret);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim("Id", user.Id),
                    new Claim(JwtRegisteredClaimNames.Email, user.Email),
                    new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                }),
                Expires = DateTime.UtcNow.AddHours(1),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = jwtTokenHandler.CreateToken(tokenDescriptor);
            var jwtToken = jwtTokenHandler.WriteToken(token);

            return new NewRefreshToken()
            {
                JwtId = token.Id,
                IsUsed = false,
                IsRevorked = false,
                UserId = user.Id,
                AddedDate = DateTime.UtcNow,
                ExpiryDate = DateTime.UtcNow.AddMonths(6),
                refreshToken = RandomString(35) + Guid.NewGuid(),
                Token = jwtToken
            };

            // await _apiDbContext.RefreshTokens.AddAsync(refreshToken);
            // await _apiDbContext.SaveChangesAsync();

            // return new AuthResult()
            // {
            //     Token = jwtToken,
            //     Success = true,
            //     RefreshToken = refreshToken.Token,
            //     UserId = refreshToken.UserId
            // };
            // return jwtToken;
        }

        private string RandomString(int length)
        {
            var random = new Random();
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(x => x[random.Next(x.Length)]).ToArray());
        }
        [HttpPost]
        [Route("Login")]
        public async Task<IActionResult> Login([FromBody] UserLoginRequest user)
        {
            if (ModelState.IsValid)
            {
                var existingUser = await _userManager.FindByEmailAsync(user.Email);

                if (existingUser == null)
                {
                    return BadRequest(new RegistrationResponse()
                    {
                        Errors = new List<string>() {
                            "Invalid login request"
                        },
                        Success = false
                    });
                }

                var isCorrect = await _userManager.CheckPasswordAsync(existingUser, user.Password);

                if (!isCorrect)
                {
                    return BadRequest(new RegistrationResponse()
                    {
                        Errors = new List<string>() {
                            "Invalid login request"
                        },
                        Success = false
                    });
                }

                var everLoggedIn = await _apiDbContext.RefreshTokens.FirstOrDefaultAsync(x => x.UserId == existingUser.Id);

                var jwtToken = GenerateJwtToken(existingUser);

                if (everLoggedIn != null)
                {

                    everLoggedIn.Token = jwtToken.refreshToken;
                    everLoggedIn.IsRevorked = false;

                    await _apiDbContext.SaveChangesAsync();

                    return Ok(new AuthResult()
                    {
                        Token = jwtToken.Token,
                        RefreshToken = jwtToken.refreshToken,
                        Success = true,
                        Messages = "Successfully Re-Login",
                        UserId = jwtToken.UserId
                    });
                }
                else
                {
                    var newData = new RefreshToken()
                    {
                        JwtId = jwtToken.JwtId,
                        IsUsed = false,
                        IsRevorked = false,
                        UserId = jwtToken.UserId,
                        AddedDate = jwtToken.AddedDate,
                        ExpiryDate = jwtToken.ExpiryDate,
                        Token = jwtToken.refreshToken,

                    };

                    await _apiDbContext.RefreshTokens.AddAsync(newData);
                    await _apiDbContext.SaveChangesAsync();

                    return Ok(new AuthResult{
                        Success = true,
                        Messages = "Successfully Login",
                        Token = jwtToken.Token,
                        RefreshToken = jwtToken.refreshToken,
                        UserId = jwtToken.UserId
                    });
                }
            }
            return BadRequest(new RegistrationResponse()
            {
                Errors = new List<string>() {
                    "Invalid payload"
                },
                Success = false
            });
        }

        [HttpDelete]
        [Route("Logout/{id}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> Logout(string id)
        {
            var storedToken = await _apiDbContext.RefreshTokens.FirstOrDefaultAsync(x => x.UserId == id);

            if (storedToken == null)
            {
                return NotFound(new AuthResult()
                {
                    Success = false,
                    Errors = new List<string>() { storedToken.UserId + "Not Found" }
                });
            }

            _apiDbContext.RefreshTokens.Remove(storedToken);

            await _apiDbContext.SaveChangesAsync();

            return Ok(new AuthResult()
            {
                Success = true,
                Messages = "Successfully Logout"
            });



        }

        // [HttpPost]
        // [Route("RefreshToken")]
        public async Task<IActionResult> RefreshToken([FromBody] TokenRequest tokenRequest)
        {
            if (ModelState.IsValid)
            {
                var result = await VerifyAndGenerateToken(tokenRequest);

                if (result == null)
                {
                    return BadRequest(new RegistrationResponse()
                    {
                        Errors = new List<string>() {
                            "Invalid tokens"
                        },
                        Success = false
                    });
                }
                return Ok(result);
            }

            return BadRequest(new RegistrationResponse()
            {
                Errors = new List<string>() {
                    "Invalid payload"
                },
                Success = false
            });
        }

        private async Task<AuthResult> VerifyAndGenerateToken(TokenRequest tokenRequest)
        {
            var jwtTokenHandler = new JwtSecurityTokenHandler();

            var storedToken = await _apiDbContext.RefreshTokens.FirstOrDefaultAsync(x => x.Token == tokenRequest.RefreshToken);

            try
            {
                // validation 1 - validation jwt token format
                var tokenInVerification = jwtTokenHandler.ValidateToken(tokenRequest.Token, _tokenValidationParams, out var validatedToken);

                // validation 2 - validate encryption algorithm
                if (validatedToken is JwtSecurityToken jwtSecurityToken)
                {
                    var result = jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase);
                    if (result == false)
                    {
                        return null;
                    }
                }

                // validation 3 - validate expiry date
                var utcExpiryDate = long.Parse(tokenInVerification.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Exp).Value);
                var expiryDate = UnixTimeStampToDateTime(utcExpiryDate);

                if (expiryDate > DateTime.UtcNow)
                {
                    return new AuthResult()
                    {
                        Success = false,
                        Errors = new List<string>() {
                            "Token has not yet expired"
                        }
                    };
                }

                // validation 4 - validate existence of the token
                // var storedToken = await _apiDbContext.RefreshTokens.FirstOrDefaultAsync(x => x.Token == tokenRequest.RefreshToken);

                if (storedToken == null)
                {
                    return new AuthResult()
                    {
                        Success = false,
                        Errors = new List<string>() {
                            "Token does not exist"
                        }
                    };
                }

                // validation 5 - validate if used
                if (storedToken.IsUsed)
                {
                    return new AuthResult()
                    {
                        Success = false,
                        Errors = new List<string>() {
                            "Token has been used"
                        }
                    };
                }

                // validation 6 - validate if revoked
                if (storedToken.IsRevorked)
                {
                    return new AuthResult()
                    {
                        Success = false,
                        Errors = new List<string>() {
                            "Token has been revoked"
                        }
                    };
                }

                // validation 7 - validate the id
                var jti = tokenInVerification.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Jti).Value;

                if (storedToken.JwtId != jti)
                {
                    return new AuthResult()
                    {
                        Success = false,
                        Errors = new List<string>() {
                            "Token doesn't match"
                        }
                    };
                }

                // update current token
                storedToken.IsUsed = true;
                _apiDbContext.RefreshTokens.Update(storedToken);
                await _apiDbContext.SaveChangesAsync();

                // generate a new token
                var dbUser = await _userManager.FindByIdAsync(storedToken.UserId);
                var tokenResult = GenerateJwtToken(dbUser);
                return new AuthResult()
                {
                    Token = tokenResult.Token,
                    Success = true,
                    RefreshToken = tokenResult.refreshToken,
                    UserId = tokenResult.UserId
                };
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("Lifetime validation failed. The token is expired."))
                {
                    // return new AuthResult()
                    // {
                    //     Success = false,
                    //     Errors = new List<string>() {
                    //         "Token has expired please re-login"
                    //     }
                    // };
                    storedToken.IsUsed = true;
                    _apiDbContext.RefreshTokens.Update(storedToken);
                    await _apiDbContext.SaveChangesAsync();

                    // generate a new token
                    var dbUser = await _userManager.FindByIdAsync(storedToken.UserId);
                    var tokenResult = GenerateJwtToken(dbUser);
                    return new AuthResult()
                    {
                        Token = tokenResult.Token,
                        Success = true,
                        RefreshToken = tokenResult.refreshToken,
                        UserId = tokenResult.UserId
                    };
                }
                else
                {
                    return new AuthResult()
                    {
                        Success = false,
                        Errors = new List<string>() {
                            "Something went wrong."
                        }
                    };
                }
            }
        }

        private DateTime UnixTimeStampToDateTime(long UnixTimeStamp)
        {
            var dateTimeVal = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dateTimeVal = dateTimeVal.AddSeconds(UnixTimeStamp).ToUniversalTime();

            return dateTimeVal;
        }

    }
}