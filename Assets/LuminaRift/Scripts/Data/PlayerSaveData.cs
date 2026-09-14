using System;
using System.Collections.Generic;

namespace LuminaRift
{
    [Serializable]
    public sealed class CharacterSaveRecord
    {
        public string characterId;
        public bool isOwned;
        public int level;
        public int affinityXp;
    }

    [Serializable]
    public sealed class PitySaveRecord
    {
        public string bannerId;
        public int pullsSinceFiveStar;
    }

    [Serializable]
    public sealed class TelemetrySaveData
    {
        public double lifetimePlaySeconds;
        public double currentRunSeconds;
        public double lastAscensionRunSeconds;
        public int highestLevelReached = 1;
        public long totalPulls;
        public long fiveStarPulls;
        public long totalClicks;
        public double lifetimeCreditsEarned;
    }

    [Serializable]
    public sealed class PlayerSaveData
    {
        public int saveVersion = 1;
        public long lastSaveUtcTicks;
        public double pendingOfflineCredits;
        public double pendingOfflineSeconds;
        public double credits;
        public int standardTickets;
        public int lumina;
        public int ascensionPower;
        public int ascensionCount;
        public bool autoLevelEnabled;
        public string activeCharacterId;
        public List<CharacterSaveRecord> characters = new List<CharacterSaveRecord>();
        public List<PitySaveRecord> pity = new List<PitySaveRecord>();
        public List<string> claimedRunMilestones = new List<string>();
        public TelemetrySaveData telemetry = new TelemetrySaveData();
    }
}
