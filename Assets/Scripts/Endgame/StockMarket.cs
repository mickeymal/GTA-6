using System;
using System.Collections.Generic;
using UnityEngine;
using ViceBayEmpire.Core;

namespace ViceBayEmpire.Endgame
{
    /// <summary>
    /// Two-exchange stock market (a legit "VBX" and a shadier "DarkPool"). Prices do a
    /// random walk; the player can hold positions. Manipulation: assassinating a rival
    /// CEO (a Dark Web contract) or raiding a competitor tanks/pumps a specific stock,
    /// letting the player profit — GTA V's Lester-assassination loop generalized.
    /// </summary>
    public class StockMarket : MonoBehaviour
    {
        public static StockMarket Instance { get; private set; }

        [Serializable]
        public class Stock
        {
            public string symbol;
            public string company;
            public float price = 100f;
            public float volatility = 0.02f;
            public bool darkPool;
            [NonSerialized] public float targetBias;   // manipulation nudges price
        }

        public List<Stock> stocks = new();
        readonly Dictionary<string, int> holdings = new();   // symbol -> shares
        public float tickInterval = 3f;
        float tickTimer;

        EconomyManager Economy => GameManager.Instance.economy;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
            if (stocks.Count == 0) SeedDefaults();
        }

        void SeedDefaults()
        {
            stocks.Add(new Stock { symbol = "BAWS", company = "Bawsaq Holdings", price = 120, volatility = 0.03f });
            stocks.Add(new Stock { symbol = "VICE", company = "Vice Media", price = 64, volatility = 0.04f });
            stocks.Add(new Stock { symbol = "CANE", company = "Sugar Cane Corp", price = 210, volatility = 0.02f });
            stocks.Add(new Stock { symbol = "GRND", company = "Grand Pharma", price = 88, volatility = 0.05f, darkPool = true });
        }

        void Update()
        {
            tickTimer -= Time.deltaTime;
            if (tickTimer > 0f) return;
            tickTimer = tickInterval;
            foreach (var s in stocks)
            {
                float drift = s.targetBias;
                float noise = UnityEngine.Random.Range(-1f, 1f) * s.volatility;
                s.price = Mathf.Max(1f, s.price * (1f + drift + noise));
                s.targetBias = Mathf.MoveTowards(s.targetBias, 0f, 0.01f);  // manipulation decays
            }
        }

        public Stock Get(string symbol) => stocks.Find(s => s.symbol == symbol);
        public int Shares(string symbol) => holdings.GetValueOrDefault(symbol, 0);

        public bool Buy(string symbol, int shares)
        {
            var s = Get(symbol); if (s == null || shares <= 0) return false;
            long cost = (long)(s.price * shares);
            if (!Economy.Spend(cost)) { GameEvents.RaiseNotify("Insufficient funds.", NotifyType.Warning); return false; }
            holdings[symbol] = Shares(symbol) + shares;
            GameEvents.RaiseNotify($"Bought {shares} {symbol} @ ${s.price:F2}", NotifyType.Info);
            return true;
        }

        public bool Sell(string symbol, int shares)
        {
            var s = Get(symbol); if (s == null || Shares(symbol) < shares) return false;
            holdings[symbol] -= shares;
            Economy.AddCash((long)(s.price * shares));
            GameEvents.RaiseNotify($"Sold {shares} {symbol} @ ${s.price:F2}", NotifyType.Money);
            return true;
        }

        /// <summary>Manipulate a stock: positive bias pumps, negative dumps. Called by
        /// assassination/raid missions targeting a company.</summary>
        public void Manipulate(string symbol, float biasPerTick)
        {
            var s = Get(symbol);
            if (s != null) { s.targetBias = biasPerTick; GameEvents.RaiseNotify($"{symbol} is moving...", NotifyType.Info); }
        }

        public long PortfolioValue()
        {
            long total = 0;
            foreach (var kv in holdings) { var s = Get(kv.Key); if (s != null) total += (long)(s.price * kv.Value); }
            return total;
        }
    }
}
