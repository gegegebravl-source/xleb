using System;
using System.Collections.Generic;

namespace WarmBread
{
    public sealed class EconomyLedger
    {
        private readonly List<LedgerEntry> entries = new List<LedgerEntry>();
        private readonly HashSet<string> transactionIds = new HashSet<string>(StringComparer.Ordinal);

        public EconomyLedger(int openingBalance)
        {
            if (openingBalance < 0) throw new ArgumentOutOfRangeException(nameof(openingBalance));
            Balance = openingBalance;
        }

        public int Balance { get; private set; }
        public IReadOnlyList<LedgerEntry> Entries => entries;

        public bool TryApply(string transactionId, int amount, LedgerReason reason, out string error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(transactionId)) { error = "transaction_id_empty"; return false; }
            if (amount == 0) { error = "amount_zero"; return false; }
            if (transactionIds.Contains(transactionId)) { error = "duplicate_transaction"; return false; }
            var next = (long)Balance + amount;
            if (next < 0 || next > int.MaxValue) { error = "balance_out_of_range"; return false; }

            Balance = (int)next;
            transactionIds.Add(transactionId);
            entries.Add(new LedgerEntry(transactionId, amount, Balance, reason, DateTime.UtcNow));
            return true;
        }
    }

    public enum LedgerReason { Sale, Purchase, Rent, Penalty, Refund }

    public readonly struct LedgerEntry
    {
        public LedgerEntry(string id, int amount, int balanceAfter, LedgerReason reason, DateTime createdUtc)
        { Id = id; Amount = amount; BalanceAfter = balanceAfter; Reason = reason; CreatedUtc = createdUtc; }
        public string Id { get; }
        public int Amount { get; }
        public int BalanceAfter { get; }
        public LedgerReason Reason { get; }
        public DateTime CreatedUtc { get; }
    }
}
