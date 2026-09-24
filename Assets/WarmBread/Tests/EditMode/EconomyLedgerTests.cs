using NUnit.Framework;

namespace WarmBread.Tests
{
    public sealed class EconomyLedgerTests
    {
        [Test]
        public void DuplicateTransactionCannotPayTwice()
        {
            var ledger = new EconomyLedger(100);
            Assert.That(ledger.TryApply("sale-1", 50, LedgerReason.Sale, out _), Is.True);
            Assert.That(ledger.TryApply("sale-1", 50, LedgerReason.Sale, out var error), Is.False);
            Assert.That(error, Is.EqualTo("duplicate_transaction"));
            Assert.That(ledger.Balance, Is.EqualTo(150));
        }

        [Test]
        public void BalanceCannotBecomeNegative()
        {
            var ledger = new EconomyLedger(100);
            Assert.That(ledger.TryApply("rent", -101, LedgerReason.Rent, out _), Is.False);
            Assert.That(ledger.Balance, Is.EqualTo(100));
        }
    }
}
