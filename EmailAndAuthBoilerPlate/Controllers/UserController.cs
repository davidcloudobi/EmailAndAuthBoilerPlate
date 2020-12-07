using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Domain.DTO.Request;
using Domain.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EmailAndAuthBoilerPlate.Controllers
{
    /// <summary>
    /// user
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {   /// <summary>
    /// user service
    /// </summary>
        public IUser UserRepository { get; set; }
        /// <summary>
        /// ctor
        /// </summary>
        public UserController(IUser userRepository)
        {
            UserRepository = userRepository;
        }

      /// <summary>
      /// Register
      /// </summary>
      /// <param name="register"></param>
      /// <returns></returns>
        [HttpPost("register")]
        public async Task<IActionResult> Test([FromBody]RegisterRequest register)
        {
             await UserRepository.Register(register, Request.Headers["origin"]);
             return Ok(new { message = "Registration successful, please check your email for verification instructions" });
        }

      /// <summary>
      /// Authenticate
      /// </summary>
      /// <param name="model"></param>
      /// <returns></returns>
      [HttpPost("authenticate")]
      public async Task<IActionResult> Authenticate([FromBody] AuthenticateRequest model)
      {
          var res = await UserRepository.Authenticate(model, ipAddress());
          setTokenCookie(res.RefreshToken);
          return Ok(res);
      }

      /// <summary>
      /// Verify Email
      /// </summary>
      /// <param name="request"></param>
      /// <returns></returns>
      [HttpPost]
      public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request)
      {
        await  UserRepository.VerifyEmail(request);
        return Ok("Email Verification Successful");
      }



      //######################################### HELPERS ############################################################

      // helper methods

      private void setTokenCookie(string token)
      {
          var cookieOptions = new CookieOptions
          {
              HttpOnly = true,
              Expires = DateTime.UtcNow.AddDays(7)
          };
          Response.Cookies.Append("refreshToken", token, cookieOptions);
      }

      private string ipAddress()
      {
          if (Request.Headers.ContainsKey("X-Forwarded-For"))
              return Request.Headers["X-Forwarded-For"];
          else
              return HttpContext.Connection.RemoteIpAddress.MapToIPv4().ToString();
      }

      //######################################### HELPERS END ########################################################
    }
}
