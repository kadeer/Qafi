// See https://aka.ms/new-console-template for more information
Console.WriteLine();

public record User
{
    public string Name { get; }

    public Ledger? Ledger { get; }

    public User(string name, Ledger? ledger = default)
    {
        Name = name;
        Ledger = ledger ?? new Ledger();
    }
}

public record Ledger
{
    public List<LedgerEntry>? Entries { get; }

    public Ledger(List<LedgerEntry>? entries = null)
    {
        Entries = entries ?? new List<LedgerEntry>();
    }

    public double Balance =>
        Entries?.Aggregate(
            (double)0,
            (acc, e) =>
                acc
                + e.EntryType switch
                {
                    LedgerEntryType.Debit => -e.Amount,
                    LedgerEntryType.Credit => e.Amount,
                    _ => throw new ArgumentOutOfRangeException()
                }
        ) ?? 0;
};

public record LedgerEntry(LedgerEntryType EntryType, double Amount, string Meta = "");

public class UserService
{
    public readonly List<User> Users = new();

    public void Register(User user)
    {
        Users.Add(user);
    }
}

public class TransferService
{
    private static readonly Dictionary<User, object> TransactionLock = new();

    public void Transfer(User source, User destination, double amount, string? meta = null)
    {
        // Poor man's implementation of a transaction coordinator so multiple threads do not deplete the balance below 0
        if (!TransactionLock.ContainsKey(source))
        {
            TransactionLock[source] = new { };
        }

        lock (TransactionLock[source])
        {
            if (source.Ledger?.Balance < amount)
            {
                throw new Exception("Source user balance is not enough");
            }

            meta = meta switch
            {
                null
                    => (source.Name, destination.Name) switch
                    {
                        ("BankIn", _) => "Deposit",
                        (_, "BankOut") => "Withdrawal",
                        (string s, string d) when s.Contains("User") || d.Contains("User")
                            => $"Transfer between {s} and {d}"
                    },
                _ => meta
            };

            source.Ledger?.Entries?.Add(new LedgerEntry(LedgerEntryType.Debit, amount, meta));
            destination.Ledger?.Entries?.Add(new LedgerEntry(LedgerEntryType.Credit, amount, meta));
        }
    }
}

public enum LedgerEntryType
{
    Debit,
    Credit
}
