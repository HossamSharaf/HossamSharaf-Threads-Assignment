using System;
using System.Collections.Generic;
using System.Text;

namespace ConsoleApp1.NewFolder
{
    public class CustomerNotFoundException : Exception
    {
        public CustomerNotFoundException(string message) : base(message) { }
    }

    public class InvalidCouponException : Exception
    {
        public InvalidCouponException(string message) : base(message) { }
    }

    // الأخطاء الجديدة الخاصة بـ Task 9.2
    public class DatabaseWriteException : Exception
    {
        public DatabaseWriteException(string message) : base(message) { }
    }

    public class SmtpUnavailableException : Exception
    {
        public SmtpUnavailableException(string message) : base(message) { }
    }
}
