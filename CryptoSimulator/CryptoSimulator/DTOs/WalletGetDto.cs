﻿namespace CryptoSimulator.DTOs
{
    public class WalletGetDto
    {
        public int Id { get; set; }
        public decimal Balance { get; set; }
        public int UserId { get; set; }
    }

    public class WalletPutDto
    {
        public decimal Balance { get; set; }
    }

    public class WalletPostDto
    {
        public decimal Balance { get; set; }
        public int UserId { get; set; }
    }
}
