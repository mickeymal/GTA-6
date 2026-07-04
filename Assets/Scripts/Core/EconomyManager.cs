using UnityEngine;

namespace ViceBayEmpire.Core
{
    /// <summary>
    /// Single source of truth for the player's cash and cryptocurrency (ViceCoin).
    /// Crypto is used for Dark Web purchases; cash for legitimate shops. Includes a
    /// laundering path so dirty crypto can be converted to spendable cash at a fee.
    /// </summary>
    public class EconomyManager : MonoBehaviour
    {
        [SerializeField] long cash = 2500;
        [SerializeField] double crypto = 0;          // ViceCoin (VC)
        [SerializeField] long dirtyMoney = 0;        // proceeds needing laundering
        public double viceCoinRate = 850.0;          // 1 VC = $850 (fluctuates)

        public long Cash => cash;
        public double Crypto => crypto;
        public long DirtyMoney => dirtyMoney;

        public void ResetTo(long startCash, double startCrypto)
        {
            cash = startCash; crypto = startCrypto; dirtyMoney = 0;
            GameEvents.RaiseMoneyChanged(cash);
            GameEvents.RaiseCryptoChanged(crypto);
        }

        // ---- cash ----------------------------------------------------------
        public bool CanAfford(long amount) => cash >= amount;

        public bool Spend(long amount)
        {
            if (amount <= 0 || cash < amount) return false;
            cash -= amount;
            GameEvents.RaiseMoneyChanged(cash);
            return true;
        }

        /// <summary>Spend as much as available up to amount; returns amount actually taken.</summary>
        public long SpendUpTo(long amount)
        {
            if (amount < 0) amount = 0;
            long taken = amount < cash ? amount : cash;   // long math, no float overflow
            cash -= taken;
            GameEvents.RaiseMoneyChanged(cash);
            return taken;
        }

        public void AddCash(long amount, bool dirty = false)
        {
            if (amount <= 0) return;
            if (dirty) dirtyMoney += amount;
            cash += amount;
            GameEvents.RaiseMoneyChanged(cash);
            if (dirty) GameEvents.RaiseNotify($"+${amount:N0} (dirty)", NotifyType.Money);
        }

        // ---- crypto --------------------------------------------------------
        public bool SpendCrypto(double vc)
        {
            if (vc <= 0 || crypto < vc) return false;
            crypto -= vc;
            GameEvents.RaiseCryptoChanged(crypto);
            return true;
        }

        public void AddCrypto(double vc)
        {
            crypto += vc;
            GameEvents.RaiseCryptoChanged(crypto);
        }

        public bool BuyCryptoWithCash(long spend)
        {
            if (!Spend(spend)) return false;
            AddCrypto(spend / viceCoinRate);
            return true;
        }

        public bool SellCryptoForCash(double vc)
        {
            if (!SpendCrypto(vc)) return false;
            AddCash((long)(vc * viceCoinRate));
            return true;
        }

        // ---- laundering ----------------------------------------------------
        /// <summary>Convert dirty money to clean crypto through a business, taking a cut.</summary>
        public long Launder(long amount, float feeRate = 0.15f)
        {
            amount = Mathf.Min((int)amount, (int)dirtyMoney);
            if (amount <= 0) return 0;
            dirtyMoney -= amount;
            long clean = (long)(amount * (1f - feeRate));
            AddCrypto(clean / viceCoinRate);
            GameEvents.RaiseNotify($"Laundered ${amount:N0} → {clean / viceCoinRate:F2} VC", NotifyType.Success);
            return clean;
        }

        void Update()
        {
            // slow random walk of the crypto price for the stock-market / dark-web feel
            viceCoinRate = Mathf.Clamp((float)viceCoinRate + Random.Range(-4f, 4f) * Time.deltaTime, 400f, 1600f);
        }
    }
}
