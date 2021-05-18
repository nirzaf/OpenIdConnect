namespace idsserver
{
    public class LoginWith2faRequest
    {
        public string ReturnUrl { get; set; }
        public bool RememberMe { get; set; }
        public string TwoFactorCode { get; set; }
        public bool RememberMachine { get; set; }
    }
}