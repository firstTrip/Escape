using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChannelZero.Runtime.Core
{
    [Serializable]
    public sealed class NarrativeTextEntry
    {
        public string id;
        public string chapter;
        public string room;
        public string target;
        public string era;
        public string state;
        public string type;
        public string speaker;
        public string trigger;
        public string condition;
        public int priority;
        public bool once;
        [TextArea] public string text;
    }

    [Serializable]
    internal sealed class NarrativeTextFile
    {
        public int schemaVersion;
        public string locale;
        public List<NarrativeTextEntry> entries = new();
    }
}
