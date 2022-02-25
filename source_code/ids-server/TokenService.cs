using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Duende.IdentityServer.AspNetIdentity;
using Duende.IdentityServer.Configuration;
using Duende.IdentityServer.Extensions;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using Duende.IdentityServer.Stores;
using IdentityModel;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace idsserver
{
    public class MyRefreshTokenService : DefaultRefreshTokenService
    {
        public MyRefreshTokenService(IRefreshTokenStore refreshTokenStore, IProfileService profile, ISystemClock clock, ILogger<DefaultRefreshTokenService> logger) : base(refreshTokenStore, profile, clock, logger)
        {
        }

        public override async Task<string> CreateRefreshTokenAsync(RefreshTokenCreationRequest request)
        {
            var identity = new ClaimsIdentity(request.Subject.Identity);
            // copy over the sid claim from AccessToken
            identity.AddClaim(new Claim(JwtClaimTypes.SessionId, request.AccessToken.SessionId));

            // save the information about the request session
            request.Subject = new ClaimsPrincipal(identity);
            return await base.CreateRefreshTokenAsync(request);
        }
    }

    public class CustomProfileService : IProfileService
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IUserClaimsPrincipalFactory<IdentityUser> _claimsFactory;
        private readonly ProfileService<IdentityUser> _builtInService;
        private readonly ILogger<CustomProfileService> logger;
        private readonly SignInManager<IdentityUser> signInManager;

        public CustomProfileService(
            UserManager<IdentityUser> userManager,
            IUserClaimsPrincipalFactory<IdentityUser> claimsFactory,
            ProfileService<IdentityUser> builtInService,
            ILogger<CustomProfileService> logger,
            SignInManager<IdentityUser> signInManager
            )
        {
            _userManager = userManager;
            _claimsFactory = claimsFactory;
            _builtInService = builtInService;
            this.logger = logger;
            this.signInManager = signInManager;
        }

        public async Task GetProfileDataAsync(ProfileDataRequestContext context)
        {
            // always include roles list and display name 
            // we could also check for requested scope by looking at context.RequestedResources.RawScopeValues
            // this is inside the access token, we always want to return name,userType and Permissions array
            logger.LogInformation($"always return Name and SID claims");

            // context.IssuedClaims.Add(context.Subject.FindFirst(JwtClaimTypes.Name));
            // context.IssuedClaims.Add(context.Subject.FindFirst(JwtClaimTypes.SessionId));

            await _builtInService.GetProfileDataAsync(context);
        }

        public async Task IsActiveAsync(IsActiveContext context)
        {
            if (context.Subject.Identity?.AuthenticationType == IdentityConstants.ApplicationScheme)
            {
                logger.LogInformation($"checking if user still valid");
                var validationResponse = await signInManager.ValidateSecurityStampAsync(context.Subject);

                if (validationResponse == null)
                {
                    context.IsActive = false;
                    return;
                }

                await _builtInService.IsActiveAsync(context);
            }
        }
    }

}