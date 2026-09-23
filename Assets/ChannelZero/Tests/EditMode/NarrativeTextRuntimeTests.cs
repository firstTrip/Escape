using System.Collections.Generic;
using System.Linq;
using ChannelZero.Runtime.Core;
using NUnit.Framework;
using UnityEngine;

namespace ChannelZero.Tests.EditMode
{
    public sealed class NarrativeTextRuntimeTests
    {
        private static NarrativeTextCatalog LoadCatalog()
        {
            TextAsset asset = Resources.Load<TextAsset>("ChannelZero/Data/narrative_ko-KR.v1");
            Assert.That(asset, Is.Not.Null);
            return NarrativeTextCatalog.FromJson(asset.text);
        }

        [Test]
        public void AuthoredJson_DeserializesWith83UniqueIdsAndRequiredTargets()
        {
            NarrativeTextCatalog catalog = LoadCatalog();

            Assert.That(catalog.SchemaVersion, Is.EqualTo(1));
            Assert.That(catalog.Locale, Is.EqualTo("ko-KR"));
            Assert.That(catalog.Entries, Has.Count.EqualTo(83));
            Assert.That(catalog.Entries.Select(entry => entry.id).Distinct().Count(), Is.EqualTo(83));
            string[] targets = catalog.Entries.Select(entry => entry.target).Distinct().ToArray();
            Assert.That(targets, Is.SupersetOf(new[]
            {
                "Living_CRTRear", "Living_TubeCase", "Living_JinwooHand",
                "Living_Lockbox", "Living_REC", "Living_Mina",
            }));
        }

        [Test]
        public void Conditions_SupportFlagItemSlotResultAndAndOperator()
        {
            ChannelZeroSessionState state = ChannelZeroSessionState.CreateNew();
            NarrativeTextContext context = new NarrativeTextContext(state)
                .SetFlag("Ready", true)
                .SetItem("Tube")
                .SetSlot("Deck", "loaded")
                .SetResult("Test", "normal");

            Assert.That(NarrativeConditionEvaluator.Evaluate(
                "flag:Ready=true&item:Tube=true&slot:Deck=loaded&result:Test=normal", context), Is.True);
            Assert.That(NarrativeConditionEvaluator.Evaluate("flag:Ready=false", context), Is.False);
        }

        [Test]
        public void Resolver_UsesHighestMatchingPriorityAndFallsBack()
        {
            const string json = "{\"schemaVersion\":1,\"locale\":\"ko-KR\",\"entries\":[" +
                "{\"id\":\"low\",\"room\":\"LivingRoom\",\"target\":\"T\",\"era\":\"ANY\",\"type\":\"description\",\"trigger\":\"inspect\",\"condition\":\"\",\"priority\":0,\"once\":false,\"text\":\"low\"}," +
                "{\"id\":\"high\",\"room\":\"LivingRoom\",\"target\":\"T\",\"era\":\"2001\",\"type\":\"description\",\"trigger\":\"inspect\",\"condition\":\"flag:Open=true\",\"priority\":20,\"once\":false,\"text\":\"high\"}]}";
            NarrativeTextResolver resolver = new NarrativeTextResolver(NarrativeTextCatalog.FromJson(json));
            ChannelZeroSessionState state = ChannelZeroSessionState.CreateNew();

            var fallback = resolver.Resolve("LivingRoom", "T", ChannelEra.Year2001, "inspect",
                new NarrativeTextContext(state).SetFlag("Open", false));
            var priority = resolver.Resolve("LivingRoom", "T", ChannelEra.Year2001, "inspect",
                new NarrativeTextContext(state).SetFlag("Open", true));

            Assert.That(fallback.Single().id, Is.EqualTo("low"));
            Assert.That(priority.Single().id, Is.EqualTo("high"));
        }

        [Test]
        public void OnceText_SurvivesSaveLoadAndRewind()
        {
            MemoryStore store = new();
            ChannelZeroSaveService saves = new ChannelZeroSaveService(store);
            ChannelZeroSessionState state = ChannelZeroSessionState.CreateNew();
            state.MarkTextSeen("CH1.LIVING.CRT.MONO.UNPLUGGED");
            state.RewindPhysicalState(null);
            saves.Save(state);

            Assert.That(saves.TryLoad(out ChannelZeroSessionState restored), Is.True);
            Assert.That(restored.HasSeenText("CH1.LIVING.CRT.MONO.UNPLUGGED"), Is.True);

            NarrativeTextResolver resolver = new NarrativeTextResolver(LoadCatalog());
            var entries = resolver.Resolve("LivingRoom", "Living_CRT", ChannelEra.Year2001, "inspect",
                new NarrativeTextContext(restored).SetFlag("CrtPowered", false).SetFlag("CrtNoiseSeen", true));
            Assert.That(entries.Select(entry => entry.id), Does.Not.Contain("CH1.LIVING.CRT.MONO.UNPLUGGED"));
            Assert.That(entries.Select(entry => entry.id), Does.Contain("CH1.LIVING.CRT.DESC.UNPLUGGED"));
        }

        private sealed class MemoryStore : IChannelZeroSaveStore
        {
            private readonly Dictionary<string, string> values = new();
            public bool HasKey(string key) => values.ContainsKey(key);
            public string Read(string key) => values.TryGetValue(key, out string value) ? value : string.Empty;
            public void Write(string key, string value) => values[key] = value;
            public void Delete(string key) => values.Remove(key);
        }
    }
}
