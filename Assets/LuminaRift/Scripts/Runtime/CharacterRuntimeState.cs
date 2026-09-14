using System;

namespace LuminaRift
{
    [Serializable]
    public sealed class CharacterRuntimeState
    {
        public CharacterData Definition { get; private set; }
        public bool IsOwned { get; private set; }
        public int Level { get; private set; }
        public int AffinityXp { get; private set; }

        public CharacterRuntimeState(CharacterData definition, bool isOwned)
        {
            Definition = definition;
            IsOwned = isOwned;
            Level = 1;
            AffinityXp = 0;
        }

        public void Unlock() { IsOwned = true; }
        public void GainLevel() { Level++; }
        public void AddAffinity(int amount) { AffinityXp += Math.Max(0, amount); }
        public void ResetLevel() { Level = 1; }
    }
}
