using FluentAssertions;

namespace Qafi.Test;

public class Tests
{
    [Test]
    public void RegistrationTest()
    {
        var user = new User("User A");

        var userService = new UserService();
        userService.Register(user);

        userService.Users.Count.Should().Be(1);
    }
    
    [Test]
    public void DepositTest()
    {
        var user = new User("User A");

        user.Ledger?.Balance.Should().Be(0);
        
        var transferService = new TransferService();
        var qafiBankUser = new User("BankIn", new Ledger(new List<LedgerEntry>
        {
            new(LedgerEntryType.Credit, 100_000, string.Empty)
        }));
        
        transferService.Transfer(qafiBankUser, user, 100);
        
        user.Ledger?.Balance.Should().Be(100);
        user.Ledger?.Entries?.Count.Should().Be(1);
        user.Ledger?.Entries?[0].EntryType.Should().Be(LedgerEntryType.Credit);
        user.Ledger?.Entries?[0].Meta.Should().Be("Deposit");

        qafiBankUser.Ledger?.Balance.Should().Be(99_900);
        qafiBankUser.Ledger?.Entries?.Count.Should().Be(2);
        qafiBankUser.Ledger?.Entries?[1].EntryType.Should().Be(LedgerEntryType.Debit);
        qafiBankUser.Ledger?.Entries?[1].Meta.Should().Be("Deposit");
    }

    [Test]
    public void TransferTest()
    {
        var userA = new User("User A");
        var userB = new User("User B");
        
        var transferService = new TransferService();
        var qafiBankUser = new User("BankIn", new Ledger(new List<LedgerEntry>
        {
            new(LedgerEntryType.Credit, 100_000, string.Empty)
        }));
        
        transferService.Transfer(qafiBankUser, userA, 100);
        transferService.Transfer(qafiBankUser, userB, 100);
        
        transferService.Transfer(userA, userB, 15);
        
        userA.Ledger?.Balance.Should().Be(85);
        userA.Ledger?.Entries?.Count.Should().Be(2);
        userA.Ledger?.Entries?[1].EntryType.Should().Be(LedgerEntryType.Debit);
        userA.Ledger?.Entries?[1].Meta.Should().Be("Transfer between User A and User B");
        
        userB.Ledger?.Balance.Should().Be(115);
        userB.Ledger?.Entries?.Count.Should().Be(2);
        userB.Ledger?.Entries?[1].EntryType.Should().Be(LedgerEntryType.Credit);
        userB.Ledger?.Entries?[1].Meta.Should().StartWith("Transfer between User A and User B");
    }

    [Test]
    public void WithdrawTest()
    {
        var transferService = new TransferService();
        var customerBankOutUser = new User("BankOut", new Ledger(new List<LedgerEntry>
        {
            new(LedgerEntryType.Credit, 100_000)
        }));
        var user = new User("User", new Ledger(new List<LedgerEntry>
        {
            new(LedgerEntryType.Credit, 100)
        }));
        
        transferService.Transfer(user, customerBankOutUser, 40);
        
        user.Ledger?.Balance.Should().Be(60);
        user.Ledger?.Entries?.Count.Should().Be(2);
        user.Ledger?.Entries?[1].EntryType.Should().Be(LedgerEntryType.Debit);
        user.Ledger?.Entries?[1].Meta.Should().Be("Withdrawal");
    }

    [Test]
    public void CannotTransferAboveUserBalanceTest()
    {
        var transferService = new TransferService();
        var act = () =>
            transferService.Transfer(
                new User("A", new Ledger()),
                new User("B", new Ledger()),
                100,
                "Transfer"
            ); 

        act.Should().Throw<Exception>().WithMessage("Source user balance is not enough");
    }
     
    [Test]
    public void AllTasks()
    {
        var userService = new UserService();
        var transferService = new TransferService();
        
        // Wafi's bank account where money is deposited into by users so they show up in the service
        // is modelled as a user too in the system
        // this keeps the API consistent for transfers and deposits
        var qafiBankUser = new User("BankIn", new Ledger(new List<LedgerEntry>
        {
            new(LedgerEntryType.Credit, 100_000)
        }));
        
        // The customer's withdrawal bank account where their cash will go to at their preferred bank
        // is modelled as a user too
        var customerABankUser = new User("BankOut");

        // user A is added
        var userA = new User("User A");
        userService.Register(userA);
        
        // user A deposits $10
        transferService.Transfer(qafiBankUser, userA, 10);

        // assert user A has $10 as their balance
        userA.Ledger?.Balance.Should().Be(10);

        // user B is registered
        var userB = new User("User B");
        userService.Register(userB);
        
        transferService.Transfer(qafiBankUser, userB, 20);
        
        // asser userB has $20 as their balance
        userB.Ledger?.Balance.Should().Be(20);
        
        // user B sends $15 to user A
        transferService.Transfer(userB, userA, 15);
        
        // user A has $25 dollars as their account balance
        userA.Ledger?.Balance.Should().Be(25);
        
        // user B has $5 as their account balance
        userB.Ledger?.Balance.Should().Be(5);
        
        // user A transfers $25 from their account to 
        transferService.Transfer(userA, customerABankUser, 25);

        userA.Ledger?.Balance.Should().Be(0);
    }
}