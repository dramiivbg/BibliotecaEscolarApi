using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MiApi.Services.SmsService
{
    public class SmsService : ISmsService
    {
        public Task SendAsync(string phoneNumber, string message)
        {
            Console.WriteLine($"Sending SMS to {phoneNumber}: {message}");
            return Task.CompletedTask;
        }
    }
}