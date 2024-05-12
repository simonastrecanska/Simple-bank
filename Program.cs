using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Transactions;

public class FileManager

{

    // Cesta k súboru, do ktorého ukladáme dáta

    private const string BasePath = "accounts.json";

    // Maximálny počet verzií súborov, ktoré ukladáme

    private const int MaxVersions = 3;

    // Uloženie dát do súboru

    public void Save(Dictionary<int, Account> accounts)

    {
        // Získame názov nového súboru

        var newFilePath = GetNewFilePath();

        // Serializujeme dáta do JSON formátu

        var jsonString = JsonSerializer.Serialize(accounts);

        // Ak počet existujúcich verzií prekročí maximálny limit, odstráni sa najstaršia verzia

        if (GetExistingVersions().Count >= MaxVersions)

        {

            var oldestVersionPath = GetOldestVersionPath();

            if (oldestVersionPath != null)

            {

                File.Delete(oldestVersionPath);

            }

        }

        // Uložíme JSON reťazec do súboru

        File.WriteAllText(newFilePath, jsonString);

    }

    // Metóda na načítanie dát zo súboru

    public Dictionary<int, Account>? Load()
    {
        // Získame zoznam existujúcich verzií súborov
        var versions = GetExistingVersions().OrderByDescending(f => f).ToList();
        Dictionary<int, Account>? accounts = null;

        // Ak neexistujú žiadne súbory na načítanie, vrátime prázdny slovník
        if (versions.Count == 0)
        {
            Console.WriteLine("No data files could be loaded.");
            return new Dictionary<int, Account>();
        }

        // Prechádzame cez všetky verzie a pokúšame sa načítať dáta
        foreach (var version in versions)
        {
            try
            {
                // Načítať JSON reťazec zo súboru a deserializovať ho do slovníka účtov
                var jsonString = File.ReadAllText(version);
                accounts = JsonSerializer.Deserialize<Dictionary<int, Account>>(jsonString);

                // Ak všetky ID kľúčov a účtov sú zhodné, prerušíme cyklus a vrátime načítané dáta
                if (accounts != null && accounts.All(kv => kv.Key == kv.Value.ID))
                {
                    break;
                }
                else
                {
                    accounts = null;
                    throw new Exception("ID does not match dictionary key.");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"An error occurred while loading accounts from {version}: {e.Message}");
            }
        }

        // Ak sa nám nepodarilo načítať žiadne dáta, vrátime prázdny slovník
        return accounts ?? new Dictionary<int, Account>();
    }

    // Pomocná metóda na generovanie cesty pre nový súbor

    private string GetNewFilePath()

    {

        var versionNumber = GetExistingVersions().Count;

        return $"{BasePath}.{versionNumber}";

    }

    // Pomocná metóda na získanie existujúcich verzií súborov v aktuálnom adresári
    private List<string> GetExistingVersions()

    {
        var filePaths = Directory.GetFiles(Directory.GetCurrentDirectory(), $"{BasePath}.*");
        return filePaths.OrderBy(f => f).ToList();

    }

    // Pomocná metóda na získanie najstaršej verzie súboru

    private string GetOldestVersionPath()

    {

        var versions = GetExistingVersions();

        if (!versions.Any())

        {

            throw new InvalidOperationException("No versions exist.");

        }

        return versions.First();

    }

}

// Trieda reprezentujúca bankový účet

public class Account

{
    // ID bankového účtu

    public int ID { get; set; }

    // Meno majiteľa účtu
    public string Name { get; set; }

    // Aktuálny zostatok na účte
    public double Balance { get; set; }

    // Overdraft limit, ktorý určuje, o koľko môže byť účet v mínuse
    public double OverdraftLimit { get; set; }

    // Konštruktor pre vytvorenie nového účtu
    public Account(int id, string name)

    {

        ID = id;

        Name = name;

        Balance = 0.0;  // Zostatok je na začiatku nulový

    }

    // Vklad peňazí na účet

    public void Deposit(double amount)

    {
        // Zostatok na účte sa zvýši o vkladanú sumu
        Balance += amount;
    }

    // Výber peňazí z účtu

    public bool Withdraw(double amount)

    {

        // Ak je na účte dostatok peňazí alebo je povolený overdraft

        if (Balance + OverdraftLimit >= amount)

        {

            // Zostatok na účte sa zníži o vyberanú sumu

            Balance -= amount;

            return true;  // Operácia bola úspešná

        }

        return false;  // Operácia bola neúspešná

    }



    // Textová reprezentácia účtu

    public override string ToString()

    {

        // Vracia detaily o účte ako textový reťazec

        return $"Account ID: {ID}, Name: {Name}, Balance: {Balance}, Overdraft Limit: {OverdraftLimit}";

    }

}



// Enumerácia definujúca možné výsledky operácie výberu peňazí

public enum WithdrawResult

{
    Success,          // Operácia bola úspešná

    AccountNotFound,  // Účet nebol nájdený

    InsufficientBalance  // Na účte nie je dostatok peňazí

}



// Trieda BankSystem na správu bankových účtov a operácií

public class BankSystem : IDisposable, IEnumerable<Account>

{

    public Dictionary<int, Account> accounts;  // Zoznam účtov v banke

    private FileManager fileManager;  // Správca súborov na načítanie a uloženie účtov

    private int nextId;  // Ďalšie dostupné ID pre vytvorenie účtu

    private bool disposed = false;

    public List<Account> Accounts { get; set; }



    // Konštruktor pre triedu BankSystem

    public BankSystem()

    {

        // Inicializácia súborov a načítanie účtov

        fileManager = new FileManager();

        accounts = fileManager.Load() ?? new Dictionary<int, Account>();

        // Nastavenie ďalšieho ID účtu

        nextId = accounts.Count > 0 ? accounts.Keys.Max() + 1 : 0;

        Accounts = new List<Account>();

    }



    // Odstránenie všetkých účtov

    public void DeleteAllAccounts()

    {
        accounts.Clear();
    }

    // Vytvorenie účtu s daným menom a limitom

    public int CreateAccount(string name, double overdraftLimit)

    {

        var account = new Account(nextId++, name);

        account.OverdraftLimit = overdraftLimit;

        accounts[account.ID] = account;

        return account.ID;

    }



    // Výpočet priemernej bilancie všetkých účtov

    public double GetAverageBalance()

    {

        if (accounts.Count == 0)

        {

            return 0;

        }

        else

        {

            return Math.Round(CalculateTotalBalance() / accounts.Count, 2);

        }

    }



    // Pridanie bonusu na všetky účty, ktoré majú zostatok nad určitým limitom

    public void ApplyBonus(double bonusAmount, double minBalanceForBonus)

    {

        foreach (var account in accounts.Values)

        {

            if (account.Balance >= minBalanceForBonus)

            {

                account.Deposit(bonusAmount);

            }

        }

    }



    // Odstránenie účtu s daným ID

    public bool DeleteAccount(int id)

    {

        return accounts.Remove(id);

    }



    // Prevod peňazí z jedného účtu na druhý

    public bool Transfer(int fromId, int toId, double amount)

    {

        if (accounts.ContainsKey(fromId) && accounts.ContainsKey(toId))

        {

            if (accounts[fromId].Withdraw(amount))

            {

                accounts[toId].Deposit(amount);

                return true;

            }

        }

        return false;

    }



    // Vklad peňazí na účet s daným ID

    public bool Deposit(int id, double amount)

    {

        if (accounts.ContainsKey(id))

        {

            accounts[id].Deposit(amount);

            return true;

        }

        return false;

    }



    // Výber peňazí z účtu s daným ID

    public WithdrawResult Withdraw(int id, double amount)

    {

        if (accounts.ContainsKey(id))

        {

            if (accounts[id].Balance >= amount)

            {

                accounts[id].Withdraw(amount);

                return WithdrawResult.Success;

            }

            else

            {

                return WithdrawResult.InsufficientBalance;

            }

        }

        return WithdrawResult.AccountNotFound;

    }



    // Zmenu limitu prečerpania

    public bool ChangeOverdraftLimit(int accountId, double newOverdraftLimit)

    {

        if (accounts.TryGetValue(accountId, out Account? account))

        {

            if (account != null)

            {

                account.OverdraftLimit = newOverdraftLimit;

                return true;

            }

        }

        return false;

    }



    // Výpis detailov účtu

    public void PrintAccount(int id)

    {

        if (accounts.ContainsKey(id))

        {

            Console.WriteLine(accounts[id]);

        }

        else

        {

            Console.WriteLine("Account not found.");

        }

    }

    // Hľadanie účtov podľa mena

    public List<Account> SearchAccountsByName(string name)

    {

        return accounts.Values.Where(a => a.Name == name).ToList();

    }



    // Hromadná operácia - pridanie úrokov na všetky účty

    public void AddInterest(double interestRate)

    {

        foreach (var account in accounts.Values)

        {

            double interest = account.Balance * interestRate;

            account.Deposit(interest);

        }

    }



    // Implementácia rozhrania IEnumerable<Account>

    public IEnumerator<Account> GetEnumerator()

    {

        return accounts.Values.GetEnumerator();

    }



    IEnumerator IEnumerable.GetEnumerator()

    {

        return GetEnumerator();

    }



    // Implementácia rozhrania IDisposable.

    public void Dispose()

    {

        // Volanie metódy Dispose(true)

        Dispose(true);

        GC.SuppressFinalize(this);

    }



    // Rozdelenie peňazí z jedného účtu na viacero ostatných

    public void SplitMoney(int sourceAccountId, List<int> targetAccountIds, double totalAmount)

    {

        // overenie počtu cielových účtov

        if (targetAccountIds.Count == 0)

        {

            Console.WriteLine("No target accounts provided.");

            return;

        }



        double splitAmount = totalAmount / targetAccountIds.Count;



        // pokus o výber peňazí zo zdrojového účtu

        if (!Withdraw(sourceAccountId, totalAmount).Equals(WithdrawResult.Success))

        {

            Console.WriteLine("Withdrawal from source account failed. Check balance and account ID.");

            return;

        }



        // vklad rozdelených peňazí na cielové účty

        foreach (int targetAccountId in targetAccountIds)

        {

            if (!Deposit(targetAccountId, splitAmount))

            {

                Console.WriteLine($"Deposit to account ID: {targetAccountId} failed. Check account ID.");

            }

        }

    }



    // Výpočet celkového zostatku všetkých účtov

    public double CalculateTotalBalance()

    {

        double totalBalance = 0;



        foreach (var account in accounts.Values)

        {

            totalBalance += account.Balance;

        }



        return totalBalance;

    }



    // Získanie účtu s najvyšším zostatkom

    public Account? GetAccountWithHighestBalance()

    {

        Account? highestBalanceAccount = null;



        foreach (var account in this.accounts)

        {

            if (highestBalanceAccount == null || account.Value.Balance > highestBalanceAccount.Balance)

            {

                highestBalanceAccount = account.Value;

            }

        }



        return highestBalanceAccount;

    }



    // Získanie účtu s najnižším zostatkom

    public Account? GetAccountWithLowestBalance()

    {

        Account? lowestBalanceAccount = null;



        foreach (var account in this.accounts)

        {

            if (lowestBalanceAccount == null || account.Value.Balance < lowestBalanceAccount.Balance)

            {

                lowestBalanceAccount = account.Value;

            }

        }



        return lowestBalanceAccount;

    }



    // Získanie zoznamu účtov s konkrétnym zostatkom

    public List<Account> GetAccountsWithBalance(double limit)

    {

        var result = new List<Account>();

        foreach (var account in accounts.Values)

        {

            if (account.Balance == limit)

            {

                result.Add(account);

            }

        }

        return result;

    }



    // Zoznam účtov zoradených podľa zostatku

    public List<Account> GetAccountsSortedByBalance()

    {

        return accounts.Values.OrderBy(a => a.Balance).ToList();

    }



    // Zoznamu účtov so zostatkom vyšším ako limit

    public List<Account> GetAccountsWithBalanceAboveLimit(double limit)

    {

        return accounts.Values.Where(a => a.Balance > limit).ToList();

    }



    // Celkovž počet účtov

    public int GetTotalAccountsCount()

    {

        return accounts.Count;

    }



    // Zmenu mena majiteľa účtu

    public void UpdateAccountHolderName(int accountId, string newName)

    {

        if (accounts.TryGetValue(accountId, out Account? account))

        {

            if (account != null)

            {

                account.Name = newName;

            }

        }

        else

        {

            Console.WriteLine("Account not found.");

        }

    }

    public Account? GetNewestAccount()

    {

        if (accounts.Count == 0)

            return null;



        var newestAccount = accounts.Values.OrderByDescending(a => a.ID).First();

        return newestAccount;

    }



    public void ApplyFee(double feeAmount, double balanceThreshold)

    {

        foreach (var account in accounts.Values)

        {

            if (account.Balance < balanceThreshold)

            {

                account.Withdraw(feeAmount);

            }

        }

    }



    // Dispose pre správu zdrojov

    protected virtual void Dispose(bool disposing)

    {

        if (!disposed)

        {

            if (disposing)

            {

                // Uloženie databázy pri ukončení

                fileManager.Save(accounts);

            }

            disposed = true;

        }

    }

}



public class Program

{

    public static void Main()

    {

        using var bankSystem = new BankSystem();

        {

            while (true)

            {

                // Zobrazenie ponuky

                Console.WriteLine("Bank System Menu:");

                Console.WriteLine("1. Create an account");

                Console.WriteLine("2. Deposit money");

                Console.WriteLine("3. Withdraw money");

                Console.WriteLine("4. Transfer money");

                Console.WriteLine("5. View account details");

                Console.WriteLine("6. Change Overdraft Limit");

                Console.WriteLine("7. Search for accounts by name");

                Console.WriteLine("8. Add interest to all accounts");

                Console.WriteLine("9. Bulk deposit");

                Console.WriteLine("10. Bulk withdrawal");

                Console.WriteLine("11. Get accounts with specific balance");

                Console.WriteLine("12. Show all accounts");

                Console.WriteLine("13. Create multiple accounts");

                Console.WriteLine("14. Show the newest account");

                Console.WriteLine("15. Print the average accounts balance");

                Console.WriteLine("16. Apply a bonus to all accounts with a balance above a certain threshold");

                Console.WriteLine("17. Apply a fee to all accounts with a balance below a certain threshold");

                Console.WriteLine("18. Show accounts with negative balance");

                Console.WriteLine("19. Show accounts with positive balance");

                Console.WriteLine("20. Split money between multiple accounts");

                Console.WriteLine("21. Get account with the highest balance");

                Console.WriteLine("22. Get account with the lowest balance");

                Console.WriteLine("23. Calculate total balance in the system");

                Console.WriteLine("24. List accounts with balance above certain limit");

                Console.WriteLine("25. Get total accounts count");

                Console.WriteLine("26. Update account holder's name");

                Console.WriteLine("27. Get accounts sorted by balance");

                Console.WriteLine("28. Delete an account");

                Console.WriteLine("29. Delete all accounts");

                Console.WriteLine("30. Exit");

                Console.Write("Enter your choice (1-30): ");



                string? input = Console.ReadLine();

                // Validace vstupu

                if (!int.TryParse(input, out int choice))

                {

                    Console.WriteLine("Invalid input. Please enter a number.");

                    continue;

                }



                switch (choice)

                {

                    case 1:

                        // Vytvorenie účtu

                        Console.Write("Enter account holder's name: ");

                        string? name = Console.ReadLine();

                        if (name == null)

                        {

                            Console.WriteLine("Unexpected end of input stream.");

                            break;

                        }



                        Console.Write("Enter overdraft limit: ");

                        if (double.TryParse(Console.ReadLine(), out double overdraftLimit))

                        {

                            int? newAccountId = bankSystem.CreateAccount(name, overdraftLimit);

                            if (newAccountId.HasValue)

                            {

                                Console.WriteLine($"Account created with ID: {newAccountId.Value} and overdraft limit: {overdraftLimit}");

                            }

                            else

                            {

                                Console.WriteLine("Failed to create account");

                            }

                        }

                        else

                        {

                            Console.WriteLine("Invalid input. Please enter a number for the overdraft limit.");

                        }

                        break;



                    case 2:

                        // Vklad peňazí na účet

                        Console.Write("Enter account ID: ");

                        string? depositAccountIdInput = Console.ReadLine();

                        if (!int.TryParse(depositAccountIdInput, out int depositAccountId))

                        {

                            Console.WriteLine("Invalid input! Please enter a valid number for the account ID.");

                            break;

                        }

                        Console.Write("Enter deposit amount: ");

                        string? depositAmountInput = Console.ReadLine();

                        if (!double.TryParse(depositAmountInput, out double depositAmount))

                        {

                            Console.WriteLine("Invalid input! Please enter a valid number for the deposit amount.");

                            break;

                        }

                        bankSystem.Deposit(depositAccountId, depositAmount);

                        Console.WriteLine($"Deposited {depositAmount} to account ID: {depositAccountId}");

                        break;



                    case 3:

                        // Výber peňazí z účtu

                        Console.Write("Enter account ID: ");

                        string? withdrawAccountIdInput = Console.ReadLine();

                        if (!int.TryParse(withdrawAccountIdInput, out int withdrawAccountId))

                        {

                            Console.WriteLine("Invalid input! Please enter a valid number for the account ID.");

                            break;

                        }

                        Console.Write("Enter withdrawal amount: ");

                        string? withdrawAmountInput = Console.ReadLine();

                        if (!double.TryParse(withdrawAmountInput, out double withdrawAmount))

                        {

                            Console.WriteLine("Invalid input! Please enter a valid number for the withdrawal amount.");

                            break;

                        }

                        var withdrawResult = bankSystem.Withdraw(withdrawAccountId, withdrawAmount);

                        switch (withdrawResult)

                        {

                            case WithdrawResult.Success:

                                Console.WriteLine($"Withdrew {withdrawAmount} from account ID: {withdrawAccountId}");

                                break;

                            case WithdrawResult.AccountNotFound:

                                Console.WriteLine($"Account ID: {withdrawAccountId} not found.");

                                break;

                            case WithdrawResult.InsufficientBalance:

                                Console.WriteLine($"Insufficient balance for withdrawal in account ID: {withdrawAccountId}");

                                break;

                        }

                        break;



                    case 4:

                        // Prevod peňazí medzi účtami

                        Console.Write("Enter sender's account ID: ");

                        string? fromIdStr = Console.ReadLine();

                        if (fromIdStr == null || !int.TryParse(fromIdStr, out int fromId))

                        {

                            Console.WriteLine("Invalid input. Please enter a number for the account ID.");

                            break;

                        }



                        Console.Write("Enter recipient's account ID: ");

                        string? toIdStr = Console.ReadLine();

                        if (toIdStr == null || !int.TryParse(toIdStr, out int toId))

                        {

                            Console.WriteLine("Invalid input. Please enter a number for the account ID.");

                            break;

                        }



                        Console.Write("Enter transfer amount: ");

                        string? transferAmountStr = Console.ReadLine();

                        if (transferAmountStr == null || !double.TryParse(transferAmountStr, out double transferAmount))

                        {

                            Console.WriteLine("Invalid input. Please enter a valid amount to transfer.");

                            break;

                        }



                        if (bankSystem.Transfer(fromId, toId, transferAmount))

                        {

                            Console.WriteLine($"Transferred {transferAmount} from account ID: {fromId} to account ID: {toId}");

                        }

                        else

                        {

                            Console.WriteLine("Transfer failed. Check account IDs and balance.");

                        }

                        break;



                    case 5:

                        // Zobrazenie detailov účtu

                        Console.Write("Enter account ID: ");

                        string? viewAccountIdStr = Console.ReadLine();

                        if (viewAccountIdStr == null || !int.TryParse(viewAccountIdStr, out int viewAccountId))

                        {

                            Console.WriteLine("Invalid input. Please enter a number for the account ID.");

                            break;

                        }



                        bankSystem.PrintAccount(viewAccountId);

                        break;



                    case 6:

                        // Zmeniť overdraft limit

                        Console.Write("Enter account ID: ");

                        string? accountIdStr = Console.ReadLine();

                        if (accountIdStr == null || !int.TryParse(accountIdStr, out int accountId))

                        {

                            Console.WriteLine("Invalid input. Please enter a number for the account ID.");

                            break;

                        }



                        Console.Write("Enter new overdraft limit: ");

                        string? newOverdraftLimitStr = Console.ReadLine();

                        if (newOverdraftLimitStr == null || !double.TryParse(newOverdraftLimitStr, out double newOverdraftLimit))

                        {

                            Console.WriteLine("Invalid input. Please enter a number for the new overdraft limit.");

                            break;

                        }



                        if (bankSystem.ChangeOverdraftLimit(accountId, newOverdraftLimit))

                        {

                            Console.WriteLine($"Changed overdraft limit to {newOverdraftLimit} for account ID: {accountId}");

                        }

                        else

                        {

                            Console.WriteLine("Change overdraft limit failed. Check account ID.");

                        }

                        break;



                    case 7:

                        // Hľadanie účtu podľa mena

                        Console.Write("Enter account holder's name: ");

                        string? searchName = Console.ReadLine();

                        if (searchName == null)

                        {

                            Console.WriteLine("Unexpected end of input stream.");

                            break;

                        }



                        var foundAccounts = bankSystem.SearchAccountsByName(searchName);

                        if (foundAccounts.Count > 0)

                        {

                            foreach (var account in foundAccounts)

                            {

                                Console.WriteLine(account);

                            }

                        }

                        else

                        {

                            Console.WriteLine("No accounts found with this name.");

                        }

                        break;



                    case 8:

                        // Pripočítanie úroku všetkým účtom

                        Console.Write("Enter the interest rate (as a decimal, e.g., 0.01 for 1%): ");

                        string? interestRateInput = Console.ReadLine();

                        if (!double.TryParse(interestRateInput, out double interestRate))

                        {

                            Console.WriteLine("Invalid input! Please enter a valid number for the interest rate.");

                            break;

                        }

                        bankSystem.AddInterest(interestRate);

                        Console.WriteLine($"Added interest to all accounts at a rate of {interestRate * 100}%");

                        break;



                    case 9:

                        // Hromadný vklad

                        Console.Write("Enter the deposit amount for all accounts: ");

                        string? bulkDepositInput = Console.ReadLine();

                        if (!double.TryParse(bulkDepositInput, out double bulkDepositAmount))

                        {

                            Console.WriteLine("Invalid input! Please enter a valid number for the deposit amount.");

                            break;

                        }

                        foreach (var account in bankSystem)

                        {

                            bankSystem.Deposit(account.ID, bulkDepositAmount);

                        }

                        Console.WriteLine($"Deposited {bulkDepositAmount} to all accounts");

                        break;



                    case 10:

                        // Hromadné výbery

                        Console.Write("Enter the withdrawal amount for all accounts: ");

                        string? bulkWithdrawalInput = Console.ReadLine();

                        if (!double.TryParse(bulkWithdrawalInput, out double bulkWithdrawalAmount))

                        {

                            Console.WriteLine("Invalid input! Please enter a valid number for the withdrawal amount.");

                            break;

                        }

                        foreach (var account in bankSystem)

                        {

                            bankSystem.Withdraw(account.ID, bulkWithdrawalAmount);

                        }

                        Console.WriteLine($"Withdrew {bulkWithdrawalAmount} from all accounts");

                        break;



                    case 11:

                        // Vypíše účet s špecifickým zostatkom

                        Console.Write("Enter the balance amount: ");

                        string? balanceAmountInput = Console.ReadLine();

                        if (!double.TryParse(balanceAmountInput, out double balanceAmount))

                        {

                            Console.WriteLine("Invalid input! Please enter a valid number.");

                            break;

                        }



                        List<Account> accounts = bankSystem.GetAccountsWithBalance(balanceAmount);



                        if (accounts.Count == 0)

                        {

                            Console.WriteLine($"No accounts found with a balance {balanceAmount}");

                        }

                        else

                        {

                            Console.WriteLine($"Accounts with balance {balanceAmount}:");

                            foreach (var account in accounts)

                            {

                                Console.WriteLine(account);

                            }

                        }

                        break;



                    case 12:

                        // Zobrazenie všetkých účtov

                        Console.WriteLine("Current accounts:");

                        foreach (var account in bankSystem)

                        {

                            Console.WriteLine(account);

                        }

                        break;



                    case 13:

                        // Vytvorenie viacerých účtov

                        Console.Write("Enter number of accounts to be created: ");

                        string? numAccountsStr = Console.ReadLine();

                        if (numAccountsStr == null)

                        {

                            Console.WriteLine("Unexpected end of input stream.");

                            break;

                        }

                        if (!int.TryParse(numAccountsStr, out int numAccounts))

                        {

                            Console.WriteLine("Invalid input. Please enter a number.");

                            break;

                        }

                        for (int i = 0; i < numAccounts; i++)

                        {

                            Console.Write("Enter account holder's name for account {0}: ", i + 1);

                            string? accName = Console.ReadLine();

                            if (accName == null)

                            {

                                Console.WriteLine("Unexpected end of input stream.");

                                break;

                            }



                            Console.Write("Enter overdraft limit for account {0}: ", i + 1);

                            string? overdraftLimitStr = Console.ReadLine();

                            if (overdraftLimitStr == null)

                            {

                                Console.WriteLine("Unexpected end of input stream.");

                                break;

                            }

                            if (!double.TryParse(overdraftLimitStr, out double accountOverdraftLimit))

                            {

                                Console.WriteLine("Invalid input for overdraft limit. Please enter a valid number.");

                                continue;

                            }



                            int newAccId = bankSystem.CreateAccount(accName, accountOverdraftLimit);

                            Console.WriteLine($"Account created with ID: {newAccId} and overdraft limit: {accountOverdraftLimit}");

                        }

                        break;



                    case 14:

                        // Vypíše najnovšie vytvorený účet

                        var newestAccount = bankSystem.GetNewestAccount();

                        if (newestAccount != null)

                        {

                            Console.WriteLine($"Newest Account ID: {newestAccount.ID}, Name: {newestAccount.Name}, Balance: {newestAccount.Balance}");

                        }

                        else

                        {

                            Console.WriteLine("No accounts found.");

                        }

                        break;



                    case 15:

                        // Vypísať priemerný zostatok účtov v systéme

                        double averageBalance = bankSystem.GetAverageBalance();

                        Console.WriteLine($"The average account balance is: {averageBalance:F2}");

                        break;



                    case 16:

                        // Pridanie bonusu na všetky účty so zostatkom nad určitou hranicou

                        Console.Write("Enter the bonus amount: ");

                        string? bonusAmountInput = Console.ReadLine();

                        if (!double.TryParse(bonusAmountInput, out double bonusAmount))

                        {

                            Console.WriteLine("Invalid input! Please enter a valid number.");

                            break;

                        }



                        Console.Write("Enter the minimum balance to qualify for the bonus: ");

                        string? minBalanceForBonusInput = Console.ReadLine();

                        if (!double.TryParse(minBalanceForBonusInput, out double minBalanceForBonus))

                        {

                            Console.WriteLine("Invalid input! Please enter a valid number.");

                            break;

                        }



                        bankSystem.ApplyBonus(bonusAmount, minBalanceForBonus);

                        Console.WriteLine($"A bonus of {bonusAmount} has been applied to all accounts with a balance of at least {minBalanceForBonus}.");

                        break;



                    case 17:

                        // Poplatok pre všetky účty so zostatkom pod určitou hranicou

                        Console.Write("Enter the fee amount: ");

                        string? feeInput = Console.ReadLine();

                        double feeAmount = 0;

                        if (!string.IsNullOrEmpty(feeInput))

                        {

                            feeAmount = double.Parse(feeInput);

                        }

                        Console.Write("Enter the balance threshold: ");

                        string? balanceInput = Console.ReadLine();

                        double balanceThreshold = 0;

                        if (!string.IsNullOrEmpty(balanceInput))

                        {

                            balanceThreshold = double.Parse(balanceInput);

                        }

                        bankSystem.ApplyFee(feeAmount, balanceThreshold);

                        Console.WriteLine($"Applied a fee of {feeAmount} to all accounts with a balance below {balanceThreshold}");

                        break;



                    case 18:

                        // Zobrazenie účtov s negatívnym zostatkom

                        Console.WriteLine("Accounts with negative balance:");

                        foreach (var account in bankSystem)

                        {

                            if (account.Balance < 0)

                            {

                                Console.WriteLine(account);

                            }

                        }

                        break;



                    case 19:

                        // Zobrazenie účtov s pozitívnym zostatkom

                        Console.WriteLine("Accounts with positive balance:");

                        foreach (var account in bankSystem)

                        {

                            if (account.Balance > 0)

                            {

                                Console.WriteLine(account);

                            }

                        }

                        break;



                    case 20:

                        // Rozdelenie peňazí medzi viaceré účty

                        Console.Write("Enter source account ID: ");

                        string? sourceAccountIdStr = Console.ReadLine();

                        if (sourceAccountIdStr == null || !int.TryParse(sourceAccountIdStr, out int sourceAccountId))

                        {

                            Console.WriteLine("Invalid input. Please enter a number for the source account ID.");

                            break;

                        }



                        Console.Write("Enter total amount to split: ");

                        string? totalAmountStr = Console.ReadLine();

                        if (totalAmountStr == null || !double.TryParse(totalAmountStr, out double totalAmount))

                        {

                            Console.WriteLine("Invalid input. Please enter a number for the total amount to split.");

                            break;

                        }



                        Console.Write("Enter number of target accounts: ");

                        string? numTargetsStr = Console.ReadLine();

                        if (numTargetsStr == null || !int.TryParse(numTargetsStr, out int numTargets))

                        {

                            Console.WriteLine("Invalid input. Please enter a number for the number of target accounts.");

                            break;

                        }



                        var targetAccountIds = new List<int>();

                        for (int i = 0; i < numTargets; i++)

                        {

                            Console.Write($"Enter target account ID {i + 1}: ");

                            string? targetAccountIdStr = Console.ReadLine();

                            if (targetAccountIdStr == null || !int.TryParse(targetAccountIdStr, out int targetAccountId))

                            {

                                Console.WriteLine("Invalid input. Please enter a number for the target account ID.");

                                break;

                            }

                            targetAccountIds.Add(targetAccountId);

                        }



                        bankSystem.SplitMoney(sourceAccountId, targetAccountIds, totalAmount);

                        break;



                    case 21:

                        // Vypíše účet s najvyšším zostatkom

                        Account? highestBalanceAccount = bankSystem.GetAccountWithHighestBalance();

                        if (highestBalanceAccount != null)

                        {

                            Console.WriteLine($"Account with the highest balance: ID = {highestBalanceAccount.ID}, Balance = {highestBalanceAccount.Balance}");

                        }

                        else

                        {

                            Console.WriteLine("No accounts found.");

                        }

                        break;



                    case 22:

                        // Vypíše účet s najnižším zostatkom

                        Account? lowestBalanceAccount = bankSystem.GetAccountWithLowestBalance();

                        if (lowestBalanceAccount != null)

                        {

                            Console.WriteLine($"Account with the lowest balance: ID = {lowestBalanceAccount.ID}, Balance = {lowestBalanceAccount.Balance}");

                        }

                        else

                        {

                            Console.WriteLine("No accounts found.");

                        }

                        break;



                    case 23:

                        // Vypočítať celkový zostatok v systéme

                        double totalBalance = bankSystem.CalculateTotalBalance();

                        Console.WriteLine("Total balance in the system: " + totalBalance);

                        break;



                    case 24:

                        // Zoznam účtov so zostatkom nad určitým limitom

                        Console.Write("Enter balance limit: ");

                        string? balanceLimitInput = Console.ReadLine();

                        if (!double.TryParse(balanceLimitInput, out double balanceLimit))

                        {

                            Console.WriteLine("Invalid input! Please enter a valid number.");

                            break;

                        }



                        var accountsWithBalanceAboveLimit = bankSystem.GetAccountsWithBalanceAboveLimit(balanceLimit);

                        if (accountsWithBalanceAboveLimit.Count > 0)

                        {

                            Console.WriteLine($"Accounts with balance above {balanceLimit}:");

                            foreach (var account in accountsWithBalanceAboveLimit)

                            {

                                Console.WriteLine(account);

                            }

                        }

                        else

                        {

                            Console.WriteLine("No accounts with balance above the specified limit.");

                        }

                        break;



                    case 25:

                        // Počet účtov v systéme

                        int totalAccounts = bankSystem.GetTotalAccountsCount();

                        Console.WriteLine($"Total number of accounts in the system is: {totalAccounts}");

                        break;



                    case 26:

                        // Aktualizácia mena majiteľa účtu

                        Console.Write("Enter account ID: ");

                        if (int.TryParse(Console.ReadLine(), out int updateNameAccountId))

                        {

                            Console.Write("Enter new account holder's name: ");

                            string? newName = Console.ReadLine();

                            if (newName == null)

                            {

                                Console.WriteLine("Unexpected end of input stream.");

                                return;

                            }

                            bankSystem.UpdateAccountHolderName(updateNameAccountId, newName);

                            Console.WriteLine($"Updated account holder's name for account ID: {updateNameAccountId}");

                        }

                        else

                        {

                            Console.WriteLine("Invalid input. Please enter a valid account ID.");

                        }

                        break;



                    case 27:

                        // Zoradenie kont poďľa zostatkov

                        List<Account> sortedAccounts = bankSystem.GetAccountsSortedByBalance();



                        Console.WriteLine("Accounts sorted by balance:");

                        foreach (var account in sortedAccounts)

                        {

                            Console.WriteLine(account);

                        }

                        break;



                    case 28:

                        // Odstránenie účtu

                        Console.Write("Enter account ID: ");

                        string? deleteAccountIdStr = Console.ReadLine();

                        if (deleteAccountIdStr == null || !int.TryParse(deleteAccountIdStr, out int deleteAccountId))

                        {

                            Console.WriteLine("Invalid input. Please enter a number for the account ID.");

                            break;

                        }



                        if (bankSystem.DeleteAccount(deleteAccountId))

                        {

                            Console.WriteLine($"Account ID: {deleteAccountId} deleted.");

                        }

                        else

                        {

                            Console.WriteLine("Account deletion failed. Check account ID.");

                        }

                        break;

                    case 29:
                        // Zmazať všetky účty
                        bankSystem.DeleteAllAccounts();
                        Console.WriteLine("All accounts have been deleted");
                        break;
                    case 30:
                        // Ukončenie programu
                        return;

                    default:
                        Console.WriteLine("Invalid choice.");
                        break;
                }
            }
        }
    }
}