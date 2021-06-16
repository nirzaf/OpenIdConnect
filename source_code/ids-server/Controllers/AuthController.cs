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
using Microsoft.Extensions.Logging;
using Duende.IdentityServer.Extensions;
using System.Security.Claims;

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
        private readonly IPersistedGrantService grantService;
        private readonly IPersistedGrantStore grantStore;
        private readonly ILogger<AuthController> logger;

        public AuthController(
            IIdentityServerInteractionService interaction,
            IClientStore clientStore,
            IAuthenticationSchemeProvider schemeProvider,
            IEventService events,
            SignInManager<IdentityUser> manager,
            UserManager<IdentityUser> usermanager,
            IPersistedGrantService grantService,
            IPersistedGrantStore grantStore,
            ILogger<AuthController> logger)
        {
            _interaction = interaction;
            _clientStore = clientStore;
            _schemeProvider = schemeProvider;
            _events = events;
            _manager = manager;
            _usermanager = usermanager;
            this.grantService = grantService;
            this.grantStore = grantStore;
            this.logger = logger;
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

        /// <summary>
        /// End all refresh tokens for the logged in user, accept the current on
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> EndAllOtherSessions()
        {
            if (User?.Identity.IsAuthenticated == true)
            {
                // get user id (which is the SubjectID)
                var subjectId = User.Identity.GetSubjectId();
                var user = await this._usermanager.FindByIdAsync(subjectId);
                this.logger.LogInformation($"end all other session for user {user.Email} with subject Id {subjectId}");

                // get the current SessionID for this user
                var result = await HttpContext.AuthenticateAsync();
                var sid = result.Properties.Items.FirstOrDefault(x => x.Key == "session_id").Value;
                this.logger.LogInformation($"current Session ID is {sid}");

                // get all for this user
                var allSessions = await this.grantStore.GetAllAsync(new PersistedGrantFilter
                {
                    SubjectId = subjectId
                });

                this.logger.LogInformation($"this user has {allSessions.Count()} sessions");
                foreach (var s in allSessions)
                {
                    var data = s.Data;
                    if (s.SessionId != sid)
                    {
                        // remove the session 
                        // when we hook this into DB, it will result in 1 call to the DB
                        await this.grantService.RemoveAllGrantsAsync(subjectId, s.ClientId, s.SessionId);
                        this.logger.LogInformation($"killed session Id {s.SessionId}, client ID: {s.ClientId}");
                    }
                }

                //  this will make sure that all other sessions are killed

                await this._usermanager.UpdateSecurityStampAsync(user);
                this.logger.LogInformation($"update Security Stampt");
            }
            return Ok();
        }


        /// <summary>
        /// End all refresh tokens for the logged in user
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> EndAllSessions()
        {
            if (User?.Identity.IsAuthenticated == true)
            {
                // get user id (which is the SubjectID)
                var subjectId = User.Identity.GetSubjectId();
                var user = await this._usermanager.FindByIdAsync(subjectId);
                this.logger.LogInformation($"end all other session for user {user.Email} with subject Id {subjectId}");
                await this.grantService.RemoveAllGrantsAsync(subjectId);
                
                //  this will make sure that all other sessions are killed
                await this._usermanager.UpdateSecurityStampAsync(user);
            }
            return Ok();
        }

        [HttpGet]
        public async Task<IActionResult> ValidateSecurityStamp()
        {
            if (User?.Identity.IsAuthenticated == true)
            {
                // get user id (which is the SubjectID)
                var validationResponse = await this._manager.ValidateSecurityStampAsync(User);
                return Ok(validationResponse == null ? "empty" : "not_empty");
            }
            return Ok("nothing");
        }

    }
}
