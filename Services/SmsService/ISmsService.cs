using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MiApi.Services.SmsService
{
    public interface ISmsService
    {
        Task SendAsync(string phoneNumber, string message);
    }
}