namespace TableBooking;

public sealed class FridayNightBooking
{
    public int TablesLeft { get; private set; } = 5;
    public int Booked { get; private set; }
    private readonly object _locker = new object();
    public void Book(string customerName)
    {
        lock (_locker)
        {
            if (TablesLeft <= 0)
                return;

            Thread.Sleep(20);

            TablesLeft--;
            Booked++;
            Console.WriteLine($"{customerName} booked a table. Left={TablesLeft}");
        }
    }
}
