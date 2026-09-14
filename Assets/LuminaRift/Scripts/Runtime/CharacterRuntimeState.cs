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

        public CharacterRuntimeState(CharacterData definition, bool owned) { Definition = definition; IsOwned = owned; Level = 1; }
        public void Unlock() { IsOwned = true; }
        public void GainLevel() { Level++; }
        public void AddAffinity(int amount) { AffinityXp += Math.Max(0, amount); }
        public void ResetLevel(int level) { Level = Math.Max(1, level); }
        public void Restore(bool owned, int level, int affinityXp) { IsOwned = owned; Level = Math.Max(1, level); AffinityXp = Math.Max(0, affinityXp); }
    }
}
