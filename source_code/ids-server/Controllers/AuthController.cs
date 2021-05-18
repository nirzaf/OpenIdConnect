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
        private readonly UserManager<IdentityUser> _usermanager;

        public AuthController(
            IIdentityServerInteractionService interaction,
            IClientStore clientStore,
            IAuthenticationSchemeProvider schemeProvider,
            IEventService events,
            SignInManager<IdentityUser> manager,
            UserManager<IdentityUser> usermanager)
        {
            _interaction = interaction;
            _clientStore = clientStore;
            _schemeProvider = schemeProvider;
            _events = events;
            _manager = manager;
            _usermanager = usermanager;
        }

        /// <summary>
        /// Handle postback from username/password login
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Login([FromBody] LoginRequest model)
        {
            var context = await _interaction.GetAuthorizationContextAsync(model.ReturnUrl);

            if (string.IsNullOrEmpty(model?.Username) || string.IsNullOrEmpty(model?.Password))
                return BadRequest("invalid request payload");

            var user = await _manager.UserManager.FindByNameAsync(model.Username);

            if (user != null)
            {
                var result = await _manager.PasswordSignInAsync(user, model.Password, model.RememberLogin, lockoutOnFailure: false);
                if (result.Succeeded)
                {
                    await _events.RaiseAsync(new UserLoginSuccessEvent(user.UserName, user.Id, user.UserName, clientId: context?.Client.ClientId));

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
                            // when user navigate directly to Identity Server, we just take them to home after login
                            ReturnUrl = "/home"
                        });
                    }
                    else
                    {
                        // user might have clicked on a malicious link - should be logged
                        return BadRequest("invalid return URL");
                    }
                }
                if (result.RequiresTwoFactor)
                {
                    return Unauthorized(new
                    {
                        Require2fa = true,
                        ReturnUrl = model.ReturnUrl,
                        RememberMe = model.RememberLogin
                    });
                }
                if (result.IsLockedOut)
                {

                    return Unauthorized(new
                    {
                        Lockeout = true,
                        ReturnUrl = model.ReturnUrl,
                        RememberMe = model.RememberLogin
                    });
                }
                else
                {
                    return BadRequest("Invalid login attempt.");
                }
            }

            await _events.RaiseAsync(new UserLoginFailureEvent(model.Username, "invalid credentials", clientId: context?.Client.ClientId));
            ModelState.AddModelError(string.Empty, AccountOptions.InvalidCredentialsErrorMessage);
            return BadRequest("Something went wrong");
        }



        [HttpPost]
        public async Task<IActionResult> LoginWith2fa([FromBody] LoginWith2faRequest model)
        {
            model.ReturnUrl = model.ReturnUrl ?? Url.Content("/home");

            var user = await _manager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
                return BadRequest("User not logged in");

            var authenticatorCode = model.TwoFactorCode.Replace(" ", string.Empty).Replace("-", string.Empty);

            var result = await _manager.TwoFactorAuthenticatorSignInAsync(authenticatorCode, model.RememberMe, model.RememberMachine);

            if (result.Succeeded)
            {
                return Ok(new
                {
                    ReturnUrl = model.ReturnUrl
                });
            }
            else if (result.IsLockedOut)
            {
                return Unauthorized(new
                {
                    Lockeout = true,
                    ReturnUrl = model.ReturnUrl,
                    RememberMe = model.RememberMe
                });
            }
            else
            {
                return BadRequest("Invalid authenticator code.");
            }
        }
    }
}
