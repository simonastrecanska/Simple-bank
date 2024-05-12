# Simple Banking System

## Overview
The goal of this project was to create a simple banking system capable of creating, updating, and deleting accounts, as well as facilitating transactions between them. The system includes a persistent database, ensuring that data persists even after the program is terminated.

## Features
- **CreateAccount(name)**: Creates a new bank account with a specified name. A unique identifier (ID) is automatically assigned to each new account, which will have its name and balance.
- **DeleteAccount(id)**: Allows users to remove an account from the banking system.
- **Transfer(fromId, toId, amount)**: Transfers money from one account to another.
- **Deposit(id, amount)**: Deposits funds into a specified bank account.
- **Withdraw(id, amount)**: Withdraws funds from a bank account.
- **PrintAccount(id)**: Displays account information, including the account's ID, name, and current balance.

## Main Program Workflow
At the start of the program, a `BankSystem` class object is created to serve as our banking system. This initializes necessary variables and loads existing accounts from a file (if they exist). A menu of the banking system is then displayed in the console, listing various account operation options. Users input their choice (numbers from 1 to 7) based on which the corresponding operation is performed. If a new account is being created (choice 1), the user is prompted to enter the account holder's name, and a new account with an assigned ID is created. For other operations (deposit, withdrawal, transfer, account display, and deletion), users are prompted to enter the account number (ID) and other necessary details. After each operation, the account file is updated using the `FileManager` class, responsible for saving and loading data from the file.

## Testing Scenarios
When the program is launched, the system menu displays various options. Users can choose to create a new account by entering the necessary participant information. Subsequently, they can deposit and withdraw money, perform transfers between accounts, and view account information.

For example:
- At the start, the JSON file is empty.
- We create three accounts for Simona, Maroš, and Daniel with overdraft limits of 500, 700, and 1000 respectively. Here’s what the account listing looks like:
  ```
  Enter your choice (1-30): 12
  Current accounts:
    Account ID: 0, Name: Simona, Balance: 0, Overdraft Limit: 500
    Account ID: 1, Name: Maroš, Balance: 0, Overdraft Limit: 700
    Account ID: 2, Name: Daniel, Balance: 0, Overdraft Limit: 1000
  ```
- Accounts can be updated with deposits:
  ```
  Simona deposits 1000, Maroš deposits 2000, Daniel deposits 3000.
  ```
- Account balances after various transactions:
  ```
  Account ID: 0, Name: Simona, Balance: 800, Overdraft Limit: 500
  Account ID: 1, Name: Maroš, Balance: 1500, Overdraft Limit: 700
  Account ID: 2, Name: Daniel, Balance: 3500, Overdraft Limit: 1000
  ```
- Applying a bonus of 50 to all accounts with a balance over 1500:
  ```
  Account ID: 1, Name: Maroš, Balance: 1550, Overdraft Limit: 700
  Account ID: 2, Name: Daniel, Balance: 3550, Overdraft Limit: 1000
  ```
- At the end, viewing all accounts before terminating the program:
  ```
  JSON file should look as follows after closing:
  {"0":{"ID":0,"Name":"Simona","Balance":800,"OverdraftLimit":500},"1":{"ID":1,"Name":"Maroš","Balance":1550,"OverdraftLimit":700},"2":{"ID":2,"Name":"Daniel","Balance":3550,"OverdraftLimit":1000}}
  ```

---
This format should help you organize and present your project documentation clearly and professionally.
