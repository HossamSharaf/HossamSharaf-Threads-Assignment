using System;
using System.Collections.Generic;
using System.Text;

namespace ConsoleApp1.NewFolder
{
    public interface IEmailService
    {
        Task SendConfirmationAsync(Order order, Customer customer, CancellationToken ct = default);
    }
    public class EmailRecord
    {
        public int OrderId { get; set; }
        public string CustomerEmail { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }
        public int Attempts { get; set; } // تمت الإضافة من أجل Task 9.4
    }

    // Models/EmailRetryRecord.cs
    public class EmailRetryRecord
    {
        public int OrderId { get; set; }
        public string CustomerEmail { get; set; } = string.Empty;
        public int Attempts { get; set; }
        public string LastError { get; set; } = string.Empty;
    }
}
