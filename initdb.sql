-- =============================================
-- initdb.sql - starter data for CryptoSimulator (PostgreSQL)
-- Holds only the cryptos and one current price each. There are no sample users:
-- the first admin is created by the API at startup (ADMIN_* in .env), and other
-- users register through the UI.
-- Run AFTER the EF Core migration, on an empty database:
--   docker exec -i crypto-postgres psql -U admin-user -d crypto-db -v ON_ERROR_STOP=1 < initdb.sql
-- =============================================

BEGIN;
SET TIME ZONE 'UTC';

-- Stop if data already exists, so ids and keys never clash.
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM "Cryptos") THEN
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

-- CryptoLogs: one current price per crypto, valid from now. "To" = 9999-12-31 marks the current price.
-- The price updater starts from this price; without a row it would start from 0.
INSERT INTO "CryptoLogs" ("CryptoId", "CurrentValue", "From", "To") VALUES
(1, 69550.25, now(), '9999-12-31 23:59:59'),
(2, 0.16, now(), '9999-12-31 23:59:59'),
(3, 3580.75, now(), '9999-12-31 23:59:59'),
(4, 1.05, now(), '9999-12-31 23:59:59'),
(5, 0.60, now(), '9999-12-31 23:59:59'),
(6, 595.50, now(), '9999-12-31 23:59:59'),
(7, 1.00, now(), '9999-12-31 23:59:59'),
(8, 195.25, now(), '9999-12-31 23:59:59'),
(9, 0.18, now(), '9999-12-31 23:59:59'),
(10, 0.56, now(), '9999-12-31 23:59:59'),
(11, 3550.50, now(), '9999-12-31 23:59:59'),
(12, 69500.00, now(), '9999-12-31 23:59:59'),
(13, 6.85, now(), '9999-12-31 23:59:59'),
(14, 0.16, now(), '9999-12-31 23:59:59'),
(15, 53.50, now(), '9999-12-31 23:59:59');

COMMIT;
