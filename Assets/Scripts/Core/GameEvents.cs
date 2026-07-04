using System;
using UnityEngine;

namespace ViceBayEmpire.Core
{
    /// <summary>
    /// Central static event bus. Systems raise and subscribe here instead of
    /// referencing each other directly, keeping managers decoupled.
    /// Subscribe in OnEnable, unsubscribe in OnDisable.
    /// </summary>
    public static class GameEvents
    {
        // ---- economy -------------------------------------------------------
        public static event Action<long> MoneyChanged;               // new cash balance
        public static event Action<double> CryptoChanged;            // new crypto balance
        public static void RaiseMoneyChanged(long v) => MoneyChanged?.Invoke(v);
        public static void RaiseCryptoChanged(double v) => CryptoChanged?.Invoke(v);

        // ---- wanted / heat -------------------------------------------------
        public static event Action<int> WantedChanged;              // 0..5 stars
        public static event Action<float, Vector3> CrimeCommitted;  // severity, position
        public static void RaiseWantedChanged(int stars) => WantedChanged?.Invoke(stars);
        public static void RaiseCrimeCommitted(float sev, Vector3 pos) => CrimeCommitted?.Invoke(sev, pos);

        // ---- player state --------------------------------------------------
        public static event Action<float, float> HealthChanged;     // hp, maxHp
        public static event Action PlayerDied;
        public static event Action<bool> VehicleStateChanged;       // inVehicle
        public static void RaiseHealthChanged(float hp, float max) => HealthChanged?.Invoke(hp, max);
        public static void RaisePlayerDied() => PlayerDied?.Invoke();
        public static void RaiseVehicleStateChanged(bool inVehicle) => VehicleStateChanged?.Invoke(inVehicle);

        // ---- interaction / UI ---------------------------------------------
        public static event Action<string> InteractionPrompt;       // text or null to hide
        public static event Action<string, NotifyType> Notify;
        public static void RaiseInteractionPrompt(string text) => InteractionPrompt?.Invoke(text);
        public static void RaiseNotify(string text, NotifyType type = NotifyType.Info) => Notify?.Invoke(text, type);

        // ---- business / property ------------------------------------------
        public static event Action<string, long> BusinessIncome;    // businessId, amount
        public static event Action<string> PropertyPurchased;
        public static void RaiseBusinessIncome(string id, long amt) => BusinessIncome?.Invoke(id, amt);
        public static void RaisePropertyPurchased(string id) => PropertyPurchased?.Invoke(id);

        // ---- world time ----------------------------------------------------
        public static event Action<float> HourChanged;             // 0..24
        public static event Action<int> NewDay;                    // day index
        public static void RaiseHourChanged(float hour) => HourChanged?.Invoke(hour);
        public static void RaiseNewDay(int day) => NewDay?.Invoke(day);
    }

    public enum NotifyType { Info, Success, Warning, Danger, Money }
}
