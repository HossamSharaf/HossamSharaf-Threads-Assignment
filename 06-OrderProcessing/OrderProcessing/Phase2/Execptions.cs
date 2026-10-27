using System;
using System.Collections.Generic;
using System.Text;

namespace ConsoleApp1
{
    public class ShippingProviderUnavailableException : Exception
    {
        public ShippingProviderUnavailableException(string message) : base(message) { }
    }
}
