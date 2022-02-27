using ClayUAC.Api.Application.Interfaces.Shared;
using System;

namespace ClayUAC.Api.Infrastructure.Shared.Services
{
    public class SystemDateTimeService : IDateTimeService
    {
        public DateTime NowUtc => DateTime.UtcNow;
    }
}