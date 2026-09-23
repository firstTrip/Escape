using UnityEngine;

namespace ChannelZero.Runtime.Core
{
    public interface IChannelZeroSaveStore
    {
        bool HasKey(string key);
        string Read(string key);
        void Write(string key, string value);
        void Delete(string key);
    }

    public sealed class PlayerPrefsChannelZeroSaveStore : IChannelZeroSaveStore
    {
        public bool HasKey(string key) => PlayerPrefs.HasKey(key);
        public string Read(string key) => PlayerPrefs.GetString(key, string.Empty);

        public void Write(string key, string value)
        {
            PlayerPrefs.SetString(key, value);
            PlayerPrefs.Save();
        }

        public void Delete(string key)
        {
            PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
        }
    }

    public sealed class ChannelZeroSaveService
    {
        public const string DefaultSaveKey = "ChannelZero.Session.v1";

        private readonly IChannelZeroSaveStore store;
        private readonly string saveKey;

        public ChannelZeroSaveService(IChannelZeroSaveStore store, string saveKey = DefaultSaveKey)
        {
            this.store = store;
            this.saveKey = saveKey;
        }

        public void Save(ChannelZeroSessionState state)
        {
            if (state == null)
                return;

            store.Write(saveKey, JsonUtility.ToJson(state));
        }

        public bool TryLoad(out ChannelZeroSessionState state)
        {
            state = null;
            if (!store.HasKey(saveKey))
                return false;

            string json = store.Read(saveKey);
            if (string.IsNullOrWhiteSpace(json))
                return false;

            try
            {
                state = JsonUtility.FromJson<ChannelZeroSessionState>(json);
                if (state == null || state.saveVersion > ChannelZeroSessionState.CurrentSaveVersion)
                {
                    state = null;
                    return false;
                }

                state.NormalizeAfterLoad();
                return true;
            }
            catch (System.ArgumentException)
            {
                state = null;
                return false;
            }
        }

        public void Clear() => store.Delete(saveKey);
    }
}
