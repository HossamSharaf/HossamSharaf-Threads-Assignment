using System;
using System.Collections.Generic;
using System.Text;

namespace ConsoleApp1
{
    public class CustomerRepository
    {
        private const string SourceName = "CustomerRepository";

        // مثال دالة القراءة (DB Query)
        public async Task<Customer?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            // 1. محاكاة التأخير (Open Connection 200ms -> Execute Query 300-600ms)
            await SimulatedLatency.SimulateDbQueryAsync(SourceName, $"Customer #{id}", ct);

            // 2. قراءة البيانات الفعلية من JsonFileStore
            var customers = await JsonFileStore.ReadAsync<Customer>(DataFiles.GetFilePath(DataFiles.CustomersFile), ct);
            var customer = customers.FirstOrDefault(c => c.Id == id);

            if (customer != null)
            {
                Log.Success(SourceName, $"Customer #{id} found ({customer.Name})");
            }
            else
            {
                Log.Error(SourceName, $"Customer #{id} not found.");
            }

            return customer;
        }

        // مثال دالة الكتابة (DB Write)
        public async Task SaveCustomerAsync(Customer customer, CancellationToken ct = default)
        {
            // 1. خطوات ما قبل الكتابة (Open 200ms -> Begin Tx 100ms -> Insert/Update Delay 400ms)
            await SimulatedLatency.SimulateDbWriteOpenAndTransactionAsync(SourceName, ct);

            // 2. التعديل والكتابة الفعلية في الملف في خطوة الـ Insert/Update وقبل الـ Commit
            var customers = await JsonFileStore.ReadAsync<Customer>(DataFiles.GetFilePath(DataFiles.CustomersFile), ct);
            customers.RemoveAll(c => c.Id == customer.Id);
            customers.Add(customer);

            await JsonFileStore.WriteAsync(DataFiles.GetFilePath(DataFiles.CustomersFile), customers, ct);

            // 3. خطوة الـ Commit النهاية (200ms)
            await SimulatedLatency.SimulateDbWriteCommitAsync(SourceName, ct);

            Log.Success(SourceName, $"Customer #{customer.Id} saved successfully.");
        }
    }
}
