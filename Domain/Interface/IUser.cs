using System.Threading.Tasks;
using Domain.DTO.Request;
using Domain.DTO.Response;

namespace Domain.Interface
{
    public interface IUser
    { 
        Task<AuthenticateResponse> Authenticate(AuthenticateRequest model, string ipAddress);
        Task Register(RegisterRequest model, string origin);
        Task VerifyEmail(VerifyEmailRequest verifyEmailRequest);

    }
}