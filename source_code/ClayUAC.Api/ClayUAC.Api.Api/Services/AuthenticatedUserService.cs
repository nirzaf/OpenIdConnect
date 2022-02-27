using System.Security.Claims;
using ClayUAC.Api.Application.Interfaces.Shared;
using Microsoft.AspNetCore.Http;

namespace ClayUAC.Api.Api.Services
{
    public class AuthenticatedUserService : IAuthenticatedUserService
    {
        public AuthenticatedUserService(IHttpContextAccessor httpContextAccessor)
        {
            UserId = httpContextAccessor.HttpContext?.User?.FindFirstValue("uid");
        }

        public string UserId { get; }
        public string Username { get; }
    }
}