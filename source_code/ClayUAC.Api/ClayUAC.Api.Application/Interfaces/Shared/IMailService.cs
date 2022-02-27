using ClayUAC.Api.Application.DTOs.Mail;
using System.Threading.Tasks;

namespace ClayUAC.Api.Application.Interfaces.Shared
{
    public interface IMailService
    {
        Task SendAsync(MailRequest request);
    }
}