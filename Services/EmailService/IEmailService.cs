using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace MiApi.Services.EmailService
{
    public interface IEmailService
    {
        Task SendAsync(string to, string subject, string body);

    }
}