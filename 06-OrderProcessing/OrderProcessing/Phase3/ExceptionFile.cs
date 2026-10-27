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
}
