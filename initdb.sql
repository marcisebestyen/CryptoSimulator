use CryptoDb;

-- =============================================
-- initdb.sql - Populate Database with Sample Data (MSSQL / T-SQL Version)
-- =============================================

-- Clear existing data (optional, uncomment if needed)
-- DELETE FROM CryptoLogs;
-- DELETE FROM Transactions;
-- DELETE FROM MyCryptos;
-- DELETE FROM Wallets;
-- DELETE FROM Users;
-- DELETE FROM Cryptos;
-- -- Reset auto-increment counters if necessary (use DBCC CHECKIDENT('YourTableName', RESEED, 0) for MSSQL)

-- ---------------------------------------------
-- Cryptos Table
-- Assuming Id is auto-increment (IDENTITY) starting from 1
-- ---------------------------------------------
PRINT 'Populating Cryptos...';
INSERT INTO Cryptos (Name) VALUES
('Bitcoin'),             -- ID 1
('Dogecoin'),            -- ID 2
('Ethereum'),            -- ID 3
('Tether'),              -- ID 4
('XRP'),                 -- ID 5
('BNB'),                 -- ID 6
('USD Coin'),            -- ID 7
('Solana'),              -- ID 8
('TRON'),                -- ID 9
('Cardano'),             -- ID 10
('Lido Staked Ether'),   -- ID 11
('Wrapped Bitcoin'),     -- ID 12
('TON'),                 -- ID 13
('Stellar'),             -- ID 14
('Avalanche');           -- ID 15
PRINT 'Cryptos populated.';

-- ---------------------------------------------
-- Users Table
-- Assuming Id is auto-increment (IDENTITY) starting from 1
-- Role: 0 = Admin, 1 = User
-- Note: Passwords should be hashed in a real application!
-- ---------------------------------------------
PRINT 'Populating Users...';
INSERT INTO Users (Role, Username, FirstName, LastName, Email, Password) VALUES
(0, 'admin', 'Alice', 'Admin', 'admin@cryptotracker.io', 'hashed_admin_pass'), -- ID 1 (Admin)
(1, 'bob_the_trader', 'Bob', 'Smith', 'bob.smith@email.com', 'hashed_bob_pass'), -- ID 2 (User)
(1, 'charlie_crypto', 'Charlie', 'Jones', 'charlie.j@webmail.org', 'hashed_charlie_pass'), -- ID 3 (User)
(1, 'diana_hodl', 'Diana', 'Williams', 'diana.w@sample.net', 'hashed_diana_pass'); -- ID 4 (User)
PRINT 'Users populated.';

-- ---------------------------------------------
-- Wallets Table
-- Assuming Id is auto-increment (IDENTITY) starting from 1
-- Linking users to their wallets
-- ---------------------------------------------
PRINT 'Populating Wallets...';
INSERT INTO Wallets (Balance, UserId) VALUES
(10000.00, 1), -- Wallet for Alice Admin (User ID 1)
(2500.50, 2),  -- Wallet for Bob Smith (User ID 2)
(5800.75, 3),  -- Wallet for Charlie Jones (User ID 3)
(1250.00, 4);  -- Wallet for Diana Williams (User ID 4)
PRINT 'Wallets populated.';

-- ---------------------------------------------
-- CryptoLogs Table
-- Historical and current prices for cryptos
-- Using T-SQL standard identifier quoting [] for reserved keywords [From] and [To]
-- Dates are examples leading up to roughly 2025-04-11
-- ---------------------------------------------
PRINT 'Populating CryptoLogs...';

-- Bitcoin (ID 1)
INSERT INTO CryptoLogs (CryptoId, CurrentValue, [From], [To]) VALUES
(1, 68500.00, '2025-04-08 10:00:00', '2025-04-09 15:30:00'),
(1, 69100.50, '2025-04-09 15:30:00', '2025-04-11 08:45:00'),
(1, 69550.25, '2025-04-11 08:45:00', '9999-12-31 23:59:59'); -- Current Price

-- Dogecoin (ID 2)
INSERT INTO CryptoLogs (CryptoId, CurrentValue, [From], [To]) VALUES
(2, 0.18, '2025-04-09 11:00:00', '2025-04-10 09:00:00'),
(2, 0.15, '2025-04-10 09:00:00', '2025-04-11 11:30:00'),
(2, 0.16, '2025-04-11 11:30:00', '9999-12-31 23:59:59'); -- Current Price

-- Ethereum (ID 3)
INSERT INTO CryptoLogs (CryptoId, CurrentValue, [From], [To]) VALUES
(3, 3500.00, '2025-04-09 12:00:00', '2025-04-10 16:45:00'),
(3, 3580.75, '2025-04-10 16:45:00', '9999-12-31 23:59:59'); -- Current Price

-- Tether (ID 4) - Stablecoin
INSERT INTO CryptoLogs (CryptoId, CurrentValue, [From], [To]) VALUES
(4, 1.00, '2025-04-01 00:00:00', '2025-04-10 08:00:00'),
(4, 0.09, '2025-04-10 08:00:00', '2025-04-11 10:00:00'),
(4, 1.05, '2025-04-11 10:00:00', '9999-12-31 23:59:59'); -- Current Price

-- XRP (ID 5)
INSERT INTO CryptoLogs (CryptoId, CurrentValue, [From], [To]) VALUES
(5, 0.58, '2025-04-09 14:00:00', '2025-04-10 18:00:00'),
(5, 0.60, '2025-04-10 18:00:00', '9999-12-31 23:59:59'); -- Current Price

-- BNB (ID 6)
INSERT INTO CryptoLogs (CryptoId, CurrentValue, [From], [To]) VALUES
(6, 580.00, '2025-04-10 09:30:00', '2025-04-11 07:00:00'),
(6, 595.50, '2025-04-11 07:00:00', '9999-12-31 23:59:59'); -- Current Price

-- USD Coin (ID 7) - Stablecoin
INSERT INTO CryptoLogs (CryptoId, CurrentValue, [From], [To]) VALUES
(7, 0.99, '2025-04-05 00:00:00', '2025-04-11 00:00:00'),
(7, 1.00, '2025-04-11 00:00:00', '9999-12-31 23:59:59'); -- Current Price

-- Solana (ID 8)
INSERT INTO CryptoLogs (CryptoId, CurrentValue, [From], [To]) VALUES
(8, 190.50, '2025-04-09 17:00:00', '2025-04-10 20:15:00'),
(8, 195.25, '2025-04-10 20:15:00', '9999-12-31 23:59:59'); -- Current Price

-- TRON (ID 9)
INSERT INTO CryptoLogs (CryptoId, CurrentValue, [From], [To]) VALUES
(9, 0.15, '2025-04-10 10:00:00', '2025-04-11 09:00:00'),
(9, 0.18, '2025-04-11 09:00:00', '9999-12-31 23:59:59'); -- Current Price

-- Cardano (ID 10)
INSERT INTO CryptoLogs (CryptoId, CurrentValue, [From], [To]) VALUES
(10, 0.55, '2025-04-09 18:00:00', '2025-04-11 06:30:00'),
(10, 0.56, '2025-04-11 06:30:00', '9999-12-31 23:59:59'); -- Current Price

-- Lido Staked Ether (ID 11)
INSERT INTO CryptoLogs (CryptoId, CurrentValue, [From], [To]) VALUES
(11, 3480.00, '2025-04-09 12:30:00', '2025-04-10 17:00:00'),
(11, 3550.50, '2025-04-10 17:00:00', '9999-12-31 23:59:59'); -- Current Price

-- Wrapped Bitcoin (ID 12)
INSERT INTO CryptoLogs (CryptoId, CurrentValue, [From], [To]) VALUES
(12, 68450.00, '2025-04-08 11:00:00', '2025-04-09 16:00:00'),
(12, 69050.00, '2025-04-09 16:00:00', '2025-04-11 09:00:00'),
(12, 69500.00, '2025-04-11 09:00:00', '9999-12-31 23:59:59'); -- Current Price

-- TON (ID 13)
INSERT INTO CryptoLogs (CryptoId, CurrentValue, [From], [To]) VALUES
(13, 6.50, '2025-04-10 11:00:00', '2025-04-11 10:30:00'),
(13, 6.85, '2025-04-11 10:30:00', '9999-12-31 23:59:59'); -- Current Price

-- Stellar (ID 14)
INSERT INTO CryptoLogs (CryptoId, CurrentValue, [From], [To]) VALUES
(14, 0.12, '2025-04-09 19:00:00', '2025-04-10 22:00:00'),
(14, 0.16, '2025-04-10 22:00:00', '9999-12-31 23:59:59'); -- Current Price

-- Avalanche (ID 15)
INSERT INTO CryptoLogs (CryptoId, CurrentValue, [From], [To]) VALUES
(15, 52.00, '2025-04-10 14:00:00', '2025-04-11 11:45:00'),
(15, 53.50, '2025-04-11 11:45:00', '9999-12-31 23:59:59'); -- Current Price

PRINT 'CryptoLogs populated.';

-- ---------------------------------------------
-- MyCryptos Table
-- Represents the current holdings of users in their wallets
-- Links WalletId to CryptoId with an Amount owned
-- These amounts should ideally reflect the net result of transactions
-- Note: Assumes composite primary key (WalletId, CryptoId)
-- ---------------------------------------------
PRINT 'Populating MyCryptos...';
INSERT INTO MyCryptos (WalletId, CryptoId, Amount) VALUES
-- Bob's Holdings (Wallet ID 2)
(2, 1, 0.02),    -- Bob owns 0.02 Bitcoin
(2, 2, 5000.0), -- Bob owns 5000 Dogecoin

-- Charlie's Holdings (Wallet ID 3)
(3, 3, 1.5),     -- Charlie owns 1.5 Ethereum
(3, 8, 25.0),    -- Charlie owns 25 Solana
(3, 10, 1000.0), -- Charlie owns 1000 Cardano

-- Diana's Holdings (Wallet ID 4)
(4, 4, 750.0),   -- Diana owns 750 Tether (perhaps from selling other crypto)
(4, 15, 10.0);   -- Diana owns 10 Avalanche
PRINT 'MyCryptos populated.';

-- ---------------------------------------------
-- Transactions Table
-- Assuming Id is auto-increment (IDENTITY) starting from 1
-- Recording buy (IsPurchase=1/true) and sell (IsPurchase=0/false) activities
-- Using BIT type for IsPurchase (1 for true, 0 for false)
-- ExchangeRate is the price per unit AT THE TIME of the transaction
-- ---------------------------------------------
PRINT 'Populating Transactions...';
INSERT INTO Transactions (WalletId, CryptoId, Amount, ExchangeRate, Date, IsPurchase) VALUES
-- Bob's Transactions (Wallet ID 2)
(2, 1, 0.02, 68550.00, '2025-04-08 14:00:00', 1), -- Bought 0.02 BTC
(2, 2, 6000.0, 0.18, '2025-04-09 11:30:00', 1), -- Bought 6000 DOGE
(2, 2, 1000.0, 0.175, '2025-04-10 10:15:00', 0), -- Sold 1000 DOGE

-- Charlie's Transactions (Wallet ID 3)
(3, 3, 1.0, 3510.00, '2025-04-09 13:00:00', 1), -- Bought 1.0 ETH
(3, 8, 25.0, 191.00, '2025-04-09 17:30:00', 1), -- Bought 25 SOL
(3, 10, 1000.0, 0.55, '2025-04-10 09:00:00', 1), -- Bought 1000 ADA
(3, 3, 0.5, 3580.00, '2025-04-10 17:00:00', 1), -- Bought 0.5 ETH

-- Diana's Transactions (Wallet ID 4)
(4, 15, 10.0, 52.10, '2025-04-10 14:30:00', 1), -- Bought 10 AVAX
(4, 1, 0.01, 69200.00, '2025-04-10 15:00:00', 1), -- Bought 0.01 BTC
(4, 1, 0.01, 69500.00, '2025-04-11 09:30:00', 0), -- Sold 0.01 BTC (Net result: Balance increases, BTC holding goes to 0)
(4, 4, 750.0, 1.0001, '2025-04-11 10:05:00', 1); -- Bought 750 USDT (maybe with proceeds from BTC sale)

PRINT 'Transactions populated.';

-- =============================================
-- End of Script
-- =============================================
PRINT 'Database initialization script completed.';
