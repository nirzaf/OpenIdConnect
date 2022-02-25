using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace idsserver
{
    /// <summary>
    /// REST API endpoints for managing 2FA
    /// </summary>
    [Authorize]
    public class UserController : Controller
    {
        private readonly ILogger<UserController> _logger;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UrlEncoder _urlEncoder;
        private readonly UserManager<IdentityUser> _userManager;

        public UserController(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            ILogger<UserController> logger,
            UrlEncoder urlEncoder
        )
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _urlEncoder = urlEncoder;
        }

        [HttpGet]
        public async Task<IActionResult> IsTwoFactorEnabled()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();
            bool isTwoFactorEnabled = await _userManager.GetTwoFactorEnabledAsync(user);
            return Ok(isTwoFactorEnabled);
        }

        [HttpPut]
        public async Task<IActionResult> EnableTwoFactor([FromBody] TwoFactorCodeRequest code)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();
            _logger.LogInformation($"turning 2FA for user with code {code.Code}");

            bool is2FaTokenValid = await _userManager.VerifyTwoFactorTokenAsync(user,
                _userManager.Options.Tokens.AuthenticatorTokenProvider, code.Code);
            if (!is2FaTokenValid) return BadRequest("Invalid Token");
            await _userManager.SetTwoFactorEnabledAsync(user, true);

            return Ok();
        }

        /// <summary>
        /// return shared key and URI for QR code
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> AuthenticatorUri()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();
            var unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
            if (string.IsNullOrEmpty(unformattedKey))
            {
                await _userManager.ResetAuthenticatorKeyAsync(user);
                unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
            }

            var sharedKey = HelperClass.FormatKey(unformattedKey);
            var email = await _userManager.GetEmailAsync(user);
            var authenticatorUri = HelperClass.GenerateQrCodeUri(email, unformattedKey);

            return Ok(new
            {
                SharedKey = sharedKey,
                AuthenticatorUri = authenticatorUri
            });
        }

        /// <summary>
        /// Disable user 2FA
        /// </summary>
        /// <returns></returns>
        [HttpPut]
        public async Task<IActionResult> DisableTwoFactor()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();
            await _userManager.SetTwoFactorEnabledAsync(user, false);
            return Ok();
        }
    }
}