-- =============================================
-- initdb.sql - sample data for CryptoSimulator (PostgreSQL)
-- Run AFTER the EF Core migration, on an empty database:
--   docker exec -i crypto-postgres psql -U admin-user -d crypto-db -v ON_ERROR_STOP=1 < initdb.sql
-- =============================================

BEGIN;
SET TIME ZONE 'UTC';

-- Stop if data already exists, so ids and keys never clash.
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "Cryptos") OR EXISTS (SELECT 1 FROM "Users") THEN
        RAISE EXCEPTION 'Seed data already present, aborting.';
    END IF;
END $$;

-- Tables are empty here; reset ids so the foreign keys below (1, 2, ...) match.
TRUNCATE "PriceAlerts", "Transactions", "MyCryptos", "CryptoLogs", "Wallets", "Users", "Cryptos" RESTART IDENTITY;

-- Cryptos (ids 1-15 in insert order)
INSERT INTO "Cryptos" ("Name") VALUES
('Bitcoin'), ('Dogecoin'), ('Ethereum'), ('Tether'), ('XRP'),
('BNB'), ('USD Coin'), ('Solana'), ('TRON'), ('Cardano'),
('Lido Staked Ether'), ('Wrapped Bitcoin'), ('TON'), ('Stellar'), ('Avalanche');

-- Users (Role: 0 = Admin, 1 = User). Passwords are placeholders, not real hashes.
INSERT INTO "Users" ("Role", "Username", "FirstName", "LastName", "Email", "Password") VALUES
(0, 'admin',          'Alice',   'Admin',    'admin@cryptotracker.io', 'hashed_admin_pass'),
(1, 'bob_the_trader', 'Bob',     'Smith',    'bob.smith@email.com',    'hashed_bob_pass'),
(1, 'charlie_crypto', 'Charlie', 'Jones',    'charlie.j@webmail.org',  'hashed_charlie_pass'),
(1, 'diana_hodl',     'Diana',   'Williams', 'diana.w@sample.net',     'hashed_diana_pass');

-- Wallets (wallet id = user id here)
INSERT INTO "Wallets" ("Balance", "UserId") VALUES
(10000.00, 1),
(2500.50,  2),
(5800.75,  3),
(1250.00,  4);

-- CryptoLogs: price history. "To" = 9999-12-31 marks the current price.
-- (Tether dips to 0.09 on purpose: price alert 6 was triggered by it.)
-- Bitcoin (ID 1)
INSERT INTO "CryptoLogs" ("CryptoId", "CurrentValue", "From", "To") VALUES
(1, 68500.00, '2025-04-08 10:00:00', '2025-04-09 15:30:00'),
(1, 69100.50, '2025-04-09 15:30:00', '2025-04-11 08:45:00'),
(1, 69550.25, '2025-04-11 08:45:00', '9999-12-31 23:59:59'); -- Current Price

-- Dogecoin (ID 2)
INSERT INTO "CryptoLogs" ("CryptoId", "CurrentValue", "From", "To") VALUES
(2, 0.18, '2025-04-09 11:00:00', '2025-04-10 09:00:00'),
(2, 0.15, '2025-04-10 09:00:00', '2025-04-11 11:30:00'),
(2, 0.16, '2025-04-11 11:30:00', '9999-12-31 23:59:59'); -- Current Price

-- Ethereum (ID 3)
INSERT INTO "CryptoLogs" ("CryptoId", "CurrentValue", "From", "To") VALUES
(3, 3500.00, '2025-04-09 12:00:00', '2025-04-10 16:45:00'),
(3, 3580.75, '2025-04-10 16:45:00', '9999-12-31 23:59:59'); -- Current Price

-- Tether (ID 4) - Stablecoin
INSERT INTO "CryptoLogs" ("CryptoId", "CurrentValue", "From", "To") VALUES
(4, 1.00, '2025-04-01 00:00:00', '2025-04-10 08:00:00'),
(4, 0.09, '2025-04-10 08:00:00', '2025-04-11 10:00:00'),
(4, 1.05, '2025-04-11 10:00:00', '9999-12-31 23:59:59'); -- Current Price

-- XRP (ID 5)
INSERT INTO "CryptoLogs" ("CryptoId", "CurrentValue", "From", "To") VALUES
(5, 0.58, '2025-04-09 14:00:00', '2025-04-10 18:00:00'),
(5, 0.60, '2025-04-10 18:00:00', '9999-12-31 23:59:59'); -- Current Price

-- BNB (ID 6)
INSERT INTO "CryptoLogs" ("CryptoId", "CurrentValue", "From", "To") VALUES
(6, 580.00, '2025-04-10 09:30:00', '2025-04-11 07:00:00'),
(6, 595.50, '2025-04-11 07:00:00', '9999-12-31 23:59:59'); -- Current Price

-- USD Coin (ID 7) - Stablecoin
INSERT INTO "CryptoLogs" ("CryptoId", "CurrentValue", "From", "To") VALUES
(7, 0.99, '2025-04-05 00:00:00', '2025-04-11 00:00:00'),
(7, 1.00, '2025-04-11 00:00:00', '9999-12-31 23:59:59'); -- Current Price

-- Solana (ID 8)
INSERT INTO "CryptoLogs" ("CryptoId", "CurrentValue", "From", "To") VALUES
(8, 190.50, '2025-04-09 17:00:00', '2025-04-10 20:15:00'),
(8, 195.25, '2025-04-10 20:15:00', '9999-12-31 23:59:59'); -- Current Price

-- TRON (ID 9)
INSERT INTO "CryptoLogs" ("CryptoId", "CurrentValue", "From", "To") VALUES
(9, 0.15, '2025-04-10 10:00:00', '2025-04-11 09:00:00'),
(9, 0.18, '2025-04-11 09:00:00', '9999-12-31 23:59:59'); -- Current Price

-- Cardano (ID 10)
INSERT INTO "CryptoLogs" ("CryptoId", "CurrentValue", "From", "To") VALUES
(10, 0.55, '2025-04-09 18:00:00', '2025-04-11 06:30:00'),
(10, 0.56, '2025-04-11 06:30:00', '9999-12-31 23:59:59'); -- Current Price

-- Lido Staked Ether (ID 11)
INSERT INTO "CryptoLogs" ("CryptoId", "CurrentValue", "From", "To") VALUES
(11, 3480.00, '2025-04-09 12:30:00', '2025-04-10 17:00:00'),
(11, 3550.50, '2025-04-10 17:00:00', '9999-12-31 23:59:59'); -- Current Price

-- Wrapped Bitcoin (ID 12)
INSERT INTO "CryptoLogs" ("CryptoId", "CurrentValue", "From", "To") VALUES
(12, 68450.00, '2025-04-08 11:00:00', '2025-04-09 16:00:00'),
(12, 69050.00, '2025-04-09 16:00:00', '2025-04-11 09:00:00'),
(12, 69500.00, '2025-04-11 09:00:00', '9999-12-31 23:59:59'); -- Current Price

-- TON (ID 13)
INSERT INTO "CryptoLogs" ("CryptoId", "CurrentValue", "From", "To") VALUES
(13, 6.50, '2025-04-10 11:00:00', '2025-04-11 10:30:00'),
(13, 6.85, '2025-04-11 10:30:00', '9999-12-31 23:59:59'); -- Current Price

-- Stellar (ID 14)
INSERT INTO "CryptoLogs" ("CryptoId", "CurrentValue", "From", "To") VALUES
(14, 0.12, '2025-04-09 19:00:00', '2025-04-10 22:00:00'),
(14, 0.16, '2025-04-10 22:00:00', '9999-12-31 23:59:59'); -- Current Price

-- Avalanche (ID 15)
INSERT INTO "CryptoLogs" ("CryptoId", "CurrentValue", "From", "To") VALUES
(15, 52.00, '2025-04-10 14:00:00', '2025-04-11 11:45:00'),
(15, 53.50, '2025-04-11 11:45:00', '9999-12-31 23:59:59'); -- Current Price


-- MyCryptos: current holdings (WalletId, CryptoId, Amount)
INSERT INTO "MyCryptos" ("WalletId", "CryptoId", "Amount") VALUES
(2, 1, 0.02),    -- Bob: Bitcoin
(2, 2, 5000.0),  -- Bob: Dogecoin
(3, 3, 1.5),     -- Charlie: Ethereum
(3, 8, 25.0),    -- Charlie: Solana
(3, 10, 1000.0), -- Charlie: Cardano
(4, 4, 750.0),   -- Diana: Tether
(4, 15, 10.0);   -- Diana: Avalanche

-- Transactions: ExchangeRate = price per unit at trade time
INSERT INTO "Transactions" ("WalletId", "CryptoId", "Amount", "ExchangeRate", "Date", "IsPurchase") VALUES
(2, 1,  0.02,   68550.00, '2025-04-08 14:00:00', true),
(2, 2,  6000.0, 0.18,     '2025-04-09 11:30:00', true),
(2, 2,  1000.0, 0.175,    '2025-04-10 10:15:00', false),
(3, 3,  1.0,    3510.00,  '2025-04-09 13:00:00', true),
(3, 8,  25.0,   191.00,   '2025-04-09 17:30:00', true),
(3, 10, 1000.0, 0.55,     '2025-04-10 09:00:00', true),
(3, 3,  0.5,    3580.00,  '2025-04-10 17:00:00', true),
(4, 15, 10.0,   52.10,    '2025-04-10 14:30:00', true),
(4, 1,  0.01,   69200.00, '2025-04-10 15:00:00', true),
(4, 1,  0.01,   69500.00, '2025-04-11 09:30:00', false),
(4, 4,  750.0,  1.0001,   '2025-04-11 10:05:00', true);

-- PriceAlerts (AlertType: 0 = below target, 1 = above target)
INSERT INTO "PriceAlerts" ("UserId", "CryptoId", "TargetPrice", "AlertType", "IsActive", "HasBeenNotifiedForCurrentState", "CreatedAt", "LastTriggeredAt") VALUES
-- Bob: BTC below 69000 (current 69550.25, not triggered)
(2, 1, 69000.00000000, 0, true,  false, '2025-04-11 12:00:00', NULL),
-- Bob: DOGE above 0.20 (current 0.16, not triggered)
(2, 2, 0.20000000,     1, true,  false, '2025-04-11 12:05:00', NULL),
-- Charlie: ETH below 3590 (current 3580.75, triggered, already notified)
(3, 3, 3590.00000000,  0, true,  true,  '2025-04-10 14:00:00', '2025-04-10 16:45:00'),
-- Diana: SOL above 192 (current 195.25, triggered, later switched off)
(4, 8, 192.00000000,   1, false, true,  '2025-04-10 10:00:00', '2025-04-10 20:15:00'),
-- Bob: BTC above 75000 (far from current price)
(2, 1, 75000.00000000, 1, true,  false, '2025-04-11 12:10:00', NULL),
-- Charlie: USDT below 0.50 (triggered by the dip, price recovered, can fire again)
(3, 4, 0.50000000,     0, true,  false, '2025-04-09 10:00:00', '2025-04-10 08:00:00');

COMMIT;
