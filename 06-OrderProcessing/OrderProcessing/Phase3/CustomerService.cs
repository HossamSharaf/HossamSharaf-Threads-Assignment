using System;
using System.Collections.Generic;
using System.Text;

namespace ConsoleApp1.NewFolder
{
    public class CustomerService
    {
        private readonly CustomerRepository _customerRepo;

        public CustomerService(CustomerRepository customerRepo)
        {
            _customerRepo = customerRepo;
        }

        public async Task<Customer> GetCustomerAsync(int id, CancellationToken ct = default)
        {
            var customer = await _customerRepo.GetByIdAsync(id, ct);
            if (customer == null)
            {
                throw new CustomerNotFoundException($"Customer #{id} not found in the database.");
            }
            return customer;
        }
    }
}
