using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;
using Duende.IdentityServer.Events;
using Duende.IdentityServer.Services;
using Duende.IdentityServer.Stores;
using Microsoft.AspNetCore.Identity;
using IdentityServerHost.Quickstart.UI;

namespace idsserver
{
    [AllowAnonymous]
    public class AuthController : Controller
    {
        private readonly IIdentityServerInteractionService _interaction;
        private readonly IClientStore _clientStore;
        private readonly IAuthenticationSchemeProvider _schemeProvider;
        private readonly IEventService _events;
        private readonly SignInManager<IdentityUser> _manager;

        public AuthController(
            IIdentityServerInteractionService interaction,
            IClientStore clientStore,
            IAuthenticationSchemeProvider schemeProvider,
            IEventService events,
            SignInManager<IdentityUser> manager)
        {
            _interaction = interaction;
            _clientStore = clientStore;
            _schemeProvider = schemeProvider;
            _events = events;
            _manager = manager;
        }

        /// <summary>
        /// Handle postback from username/password login
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Login([FromBody] LoginInputModel model)
        {
            var context = await _interaction.GetAuthorizationContextAsync(model.ReturnUrl);

            if (string.IsNullOrEmpty(model?.Username) || string.IsNullOrEmpty(model?.Password))
                return BadRequest("invalid request payload");

            var user = await _manager.UserManager.FindByNameAsync(model.Username);

            if (user != null && await _manager.CheckPasswordSignInAsync(user, model.Password, true) == Microsoft.AspNetCore.Identity.SignInResult.Success)
            {
                await _events.RaiseAsync(new UserLoginSuccessEvent(user.UserName, user.Id, user.UserName, clientId: context?.Client.ClientId));

                // only set explicit expiration here if user chooses "remember me". 
                // otherwise we rely upon expiration configured in cookie middleware.
                AuthenticationProperties props = null;
                if (AccountOptions.AllowRememberLogin && model.RememberLogin)
                {
                    props = new AuthenticationProperties
                    {
                        IsPersistent = true,
                        ExpiresUtc = DateTimeOffset.UtcNow.Add(AccountOptions.RememberMeLoginDuration)
                    };
                };

                await _manager.SignInAsync(user, props);

                if (context != null)
                {
                    if (context.IsNativeClient())
                    {
                        // The client is native, so this change in how to
                        // return the response is for better UX for the end user.
                        return Ok(new
                        {
                            ReturnUrl = model.ReturnUrl
                        });
                    }

                    // we can trust model.ReturnUrl since GetAuthorizationContextAsync returned non-null
                    return Ok(new
                    {
                        ReturnUrl = model.ReturnUrl
                    });
                }

                // request for a local page
                if (Url.IsLocalUrl(model.ReturnUrl))
                {
                    return Ok(new
                    {
                        ReturnUrl = model.ReturnUrl
                    });
                }
                else if (string.IsNullOrEmpty(model.ReturnUrl))
                {
                    return Ok(new
                    {
                        ReturnUrl = "/"
                    });
                }
                else
                {
                    // user might have clicked on a malicious link - should be logged
                    return BadRequest("invalid return URL");
                }
            }

            await _events.RaiseAsync(new UserLoginFailureEvent(model.Username, "invalid credentials", clientId: context?.Client.ClientId));
            ModelState.AddModelError(string.Empty, AccountOptions.InvalidCredentialsErrorMessage);
            return BadRequest("Something went wrong");
        }
    }
}
