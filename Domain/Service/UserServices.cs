using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Data.Entites;
using Domain.DTO.Request;
using Domain.DTO.Response;
using Domain.Helper;
using Domain.Interface;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Domain.Service
{
   public class UserServices:IUser
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly AppSettings _appSettings;
        private readonly IEmailService _emailService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;

        public UserServices(IMapper mapper,
            IOptions<AppSettings> appSettings,
            IEmailService emailService, ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager)
        {
            _mapper = mapper;
            _appSettings = appSettings.Value;
            _emailService = emailService;
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<AuthenticateResponse> Authenticate(AuthenticateRequest model, string ipAddress)
        {
            // Find user
            var user = await _userManager.FindByEmailAsync(model.Email);
            //var userHasValidPassword = await _userManager.CheckPasswordAsync(user, model.Password);

            // Verify account and password with the hashed password in the database
            // install BCrypt.Net-Next from the nuget package
            if (user == null || !user.IsVerified || !await _userManager.CheckPasswordAsync(user, model.Password))
            {
                throw new AppException("Email or password is incorrect");
            }

            // authentication successful so generate jwt and refresh tokens
            var jwtToken = generateJwtToken(user);
            var refreshToken = generateRefreshToken(ipAddress);
            user.RefreshTokens.Add(refreshToken);

            // remove old refresh tokens from account
            removeOldRefreshTokens(user);

            // save changes to db
             _context.Update(user);
            await _context.SaveChangesAsync();

            var response = _mapper.Map<AuthenticateResponse>(user);
            response.JwtToken = jwtToken;
            response.RefreshToken = refreshToken.Token;
            var roles = await _userManager.GetRolesAsync(user);
            response.Role = roles.First() ?? null;
            return response;

        }

        public async Task Register(RegisterRequest model, string origin)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            // validate
            if (user != null)
                throw new AppException($"Email '{model.Email}' is already registered");
            // map model to new account object
            var applicationUser = _mapper.Map<ApplicationUser>(model);
            applicationUser.UserName = applicationUser.LastName + applicationUser.FirstName;
            applicationUser.CreationDate = DateTime.UtcNow;
          //  applicationUser.VerificationToken = randomTokenString();


            // hash password
            // applicationUser.PasswordHash = BC.HashPassword(model.Password);

            IdentityResult Created = await _userManager.CreateAsync(applicationUser, model.Password);
            if (!Created.Succeeded)
            {
                var error = Created.Errors.First().Description ?? "Error, User not created";
               throw new AppException(error);
            }

            applicationUser.VerificationToken = await _userManager.GenerateEmailConfirmationTokenAsync(applicationUser);
            _context.Update(applicationUser);

            var roleExits = await _roleManager.RoleExistsAsync(model.Role);
            if (!roleExits)
            {
                await _roleManager.CreateAsync(new ApplicationRole()
                {
                    Name =model.Role,
                    IsActive = true
                });
            }
            await _userManager.AddToRoleAsync(applicationUser, model.Role);
            await  _context.SaveChangesAsync();

            sendVerificationEmail(applicationUser, origin);

        }

        public async Task VerifyEmail(VerifyEmailRequest verifyEmailRequest)
        {

            var user = await _userManager.FindByEmailAsync(verifyEmailRequest.Email);
            if (user == null) throw new AppException("Verification failed");

            var res = await _userManager.ConfirmEmailAsync(user, verifyEmailRequest.Token);
            if (!res.Succeeded) throw new AppException("Verification failed. " + res.Errors.First().Description);

            
            user.Verified = DateTime.UtcNow;
            user.VerificationToken = null;

            _context.Update(user);
            await _context.SaveChangesAsync();
            sendEmailVerifiedSuccessfully(user);
        }

        //###################################################  HELPERS ###########################################
        private string generateJwtToken(ApplicationUser user)
        {
            //Create Claim
            var Claims = new[]
            {
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Gender, user.Gender),
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim("id", user.Id),
                new Claim("fullName", $"{user.LastName} {user.FirstName}")
            };


            // Create a JWT
            var tokenHandler = new JwtSecurityTokenHandler();

            // Get key from app-settings and encode it
            var key = Encoding.ASCII.GetBytes(_appSettings.Secret);

            // Description of the JWT
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(Claims),
                Expires = DateTime.UtcNow.AddMinutes(2),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
                Issuer = _appSettings.Issuer,
                Audience = null
            };

            // create the JWT with the description
            var token = tokenHandler.CreateToken(tokenDescriptor);

            // write the JWT and return it
            return tokenHandler.WriteToken(token);
        }
        private RefreshToken generateRefreshToken(string ipAddress)
        {
            return new RefreshToken
            {
                Token = randomTokenString(),
                Expires = DateTime.UtcNow.AddDays(7),
                Created = DateTime.UtcNow,
                CreatedByIp = ipAddress
            };
        }
        private string randomTokenString()
        {
            using var rngCryptoServiceProvider = new RNGCryptoServiceProvider();
            var randomBytes = new byte[40];
            rngCryptoServiceProvider.GetBytes(randomBytes);
            // convert random bytes to hex string
            return BitConverter.ToString(randomBytes).Replace("-", "");
        }
        private void removeOldRefreshTokens(ApplicationUser user)
        {
            user.RefreshTokens.RemoveAll(x =>
                !x.IsActive &&
                x.Created.AddDays(_appSettings.RefreshTokenTTL) <= DateTime.UtcNow);
        }
        private void sendVerificationEmail(ApplicationUser user, string origin)
        {
            string message;
            if (!string.IsNullOrEmpty(origin))
            {
                var verifyUrl = $"{origin}/account/verify-email?token={user.VerificationToken}";
                message = $@"<p>Please click the below link to verify your email address:</p>
                             <p><a href=""{verifyUrl}"">{verifyUrl}</a></p>";
            }
            else
            {
                message = $@"<p>Please use the below token to verify your email address with the <code>/accounts/verify-email</code> api route:</p>
                             <p><code>{user.VerificationToken}</code></p>";
            }

            _emailService.Send(
                to: user.Email,
                subject: "Sign-up Verification API - Verify Email",
                html: $@"<h4>Verify Email</h4>
                         <p>Thanks for registering!</p>
                         {message}"
            );
        }
        private void sendEmailVerifiedSuccessfully(ApplicationUser user)
        {
           
          var  message = $@"<p>Email Verification Successful</p>
                             <p>Thank you for supporting us</p>";

            _emailService.Send(
                to: user.Email,
                subject: "Verify Email",
                html: $@"<h4>Verify Email</h4>
                         <p>Thanks for registering!</p>
                         {message}"
            );
        }
        //################################################### END #################################################
    }
}
