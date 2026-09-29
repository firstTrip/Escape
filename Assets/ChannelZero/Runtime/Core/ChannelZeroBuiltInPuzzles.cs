using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ChannelZero.Runtime.Core
{
    public sealed class ChannelZeroBuiltInPuzzleRegistry : IChannelZeroPuzzleRegistry
    {
        private readonly Dictionary<string, IChannelZeroPuzzle> puzzles = new(StringComparer.Ordinal);

        public PuzzleDefinitionCatalog Catalog { get; }

        public ChannelZeroBuiltInPuzzleRegistry(string localeCode = "ko-KR")
        {
            Catalog = PuzzleDefinitionCatalog.Load(localeCode);
            foreach (PuzzleDefinition definition in Catalog.Puzzles)
                Register(CreatePuzzle(definition));
        }

        public bool TryGet(string closeupId, out IChannelZeroPuzzle puzzle) => puzzles.TryGetValue(closeupId, out puzzle);
        public bool TryGetDefinition(string closeupId, out PuzzleDefinition definition) =>
            Catalog.TryGetPuzzle(closeupId, out definition);
        private void Register(IChannelZeroPuzzle puzzle) => puzzles[puzzle.CloseupId] = puzzle;

        private static IChannelZeroPuzzle CreatePuzzle(PuzzleDefinition definition)
        {
            (string, string)[] loot = definition.lootItems
                .Select((item, index) => (item, index < definition.lootLabels.Length ? definition.lootLabels[index] : item))
                .ToArray();
            return definition.ruleType switch
            {
                "signal" => new SignalPuzzle(),
                "crt_rear" => new CrtRearPuzzle(),
                "loot" or "parts_drawer" => new LootPuzzle(definition.id, definition.closeupId, definition.title,
                    loot, definition.openArtwork, definition.completeArtwork, definition.solvedFlag),
                "sequence" => new SequencePuzzle(definition.id, definition.closeupId, definition.title,
                    definition.instruction, definition.answer, definition.clueFlag,
                    definition.reviewArtworkStateByEra),
                "treatment" => new HandTreatmentPuzzle(),
                "jinwoo_causality" => new Chapter1JinwooPuzzle(),
                "rec" => new RecPuzzle(definition.closeupId),
                "return_circuit" => new Chapter1ReturnCircuitPuzzle(definition.closeupId),
                "vase_causality" => new Chapter1VasePuzzle(definition.closeupId),
                "child_trace" => new Chapter1ChildTracePuzzle(definition.closeupId),
                "signal_stabilizer" => new Chapter1SignalStabilizerPuzzle(definition.closeupId),
                "clock_knot" => new Chapter1ClockKnotPuzzle(definition.closeupId),
                "era_evidence" => new EraEvidencePuzzle(definition.id, definition.closeupId, definition.title,
                    definition.instruction, definition.clueFlag, definition.completeFlag, definition.chapterTwo),
                "inspect" => new InspectPuzzle(definition.id, definition.closeupId, definition.title,
                    definition.instruction, definition.completeFlag),
                "inspect_plan" => new InspectPuzzle(definition.id, definition.closeupId, definition.title,
                    definition.instruction, definition.completeFlag, true),
                "tester" => new TubeTesterPuzzle(),
                "crank" => new FoldingCrankPuzzle(),
                _ => throw new NotSupportedException($"Unknown puzzle rule type: {definition.ruleType}"),
            };
        }
    }

    internal abstract class PuzzleBase : IChannelZeroPuzzle
    {
        public string PuzzleId { get; }
        public string CloseupId { get; }
        protected string Title { get; }

        protected PuzzleBase(string puzzleId, string closeupId, string title)
        {
            PuzzleId = puzzleId;
            CloseupId = closeupId;
            Title = title;
        }

        public abstract PuzzleView BuildView(ChannelZeroPuzzleContext context);
        public abstract PuzzleActionResult Execute(string actionId, ChannelZeroPuzzleContext context);

        protected PuzzleView View(string body, string artworkState = "default", bool completed = false,
            params PuzzleActionView[] actions)
        {
            PuzzleView view = new()
            {
                puzzleId = PuzzleId,
                closeupId = CloseupId,
                title = Title,
                body = body,
                titleKey = $"puzzle.{PuzzleId}.title",
                bodyKey = $"puzzle.{PuzzleId}.body.{artworkState}",
                artworkStateId = artworkState,
                completed = completed,
                actions = actions.ToList(),
            };
            foreach (PuzzleActionView action in view.actions)
                if (string.IsNullOrWhiteSpace(action.labelKey))
                    action.labelKey = $"puzzle.{PuzzleId}.action.{action.actionId}";
            return view;
        }

        protected PuzzleActionResult Result(ChannelZeroPuzzleContext context, string trigger = null)
        {
            return new PuzzleActionResult { view = BuildView(context), narrativeTrigger = trigger, stateChanged = true };
        }
    }

    internal sealed class SequencePuzzle : PuzzleBase
    {
        private readonly string hint;
        private readonly string answer;
        private readonly string clueFlag;
        private readonly EraResultStateEntry[] artworkStates;

        public SequencePuzzle(string id, string closeup, string title, string hint, string answer,
            string clueFlag, EraResultStateEntry[] artworkStates)
            : base(id, closeup, title)
        {
            this.hint = hint;
            this.answer = answer;
            this.clueFlag = clueFlag;
            this.artworkStates = artworkStates ?? Array.Empty<EraResultStateEntry>();
        }

        public override PuzzleView BuildView(ChannelZeroPuzzleContext context)
        {
            bool solved = context.GetState(PuzzleId) == "solved";
            string input = context.GetState(PuzzleId + ".input", string.Empty);
            string body = solved ? $"해제 완료 — {answer}" : $"{(context.GetFlag(clueFlag) ? hint : "주변 단서를 더 확인해야 한다.")}\n입력: {input.PadRight(answer.Length, '·')}";
            string artworkState = Array.Find(artworkStates,
                entry => entry.era == (int)context.Era)?.stateId ?? ChannelZeroIds.DefaultVisualState;
            PuzzleView view = View(body, artworkState, solved);
            if (!solved)
            {
                for (int i = 0; i <= 9; i++) view.actions.Add(new PuzzleActionView("digit:" + i, i.ToString()));
                view.actions.Add(new PuzzleActionView("reset", "입력 지우기"));
            }
            return view;
        }

        public override PuzzleActionResult Execute(string actionId, ChannelZeroPuzzleContext context)
        {
            if (context.GetState(PuzzleId) == "solved") return Result(context);
            if (actionId == "reset") context.SetState(PuzzleId + ".input", string.Empty);
            else if (actionId.StartsWith("digit:", StringComparison.Ordinal))
            {
                string input = context.GetState(PuzzleId + ".input", string.Empty) + actionId[6..];
                if (!answer.StartsWith(input, StringComparison.Ordinal))
                {
                    context.SetState(PuzzleId + ".input", string.Empty);
                    return Result(context, "puzzle_wrong");
                }
                if (input == answer)
                {
                    context.SetState(PuzzleId, "solved");
                    context.SetFlag(PuzzleId == ChannelZeroPuzzleIds.Rug2749 ? "Rug2749Solved" : "Lockbox8888Solved");
                    return Result(context, "puzzle_success");
                }
                context.SetState(PuzzleId + ".input", input);
            }
            return Result(context);
        }
    }

    internal class LootPuzzle : PuzzleBase
    {
        private readonly (string item, string label)[] loot;
        private readonly string openArtwork;
        private readonly string completeArtwork;
        private readonly string solvedFlag;

        public LootPuzzle(string id, string closeup, string title, (string, string)[] loot,
            string openArtwork = "default", string completeArtwork = "default", string solvedFlag = "") : base(id, closeup, title)
        { this.loot = loot; this.openArtwork = openArtwork; this.completeArtwork = completeArtwork; this.solvedFlag = solvedFlag; }

        public override PuzzleView BuildView(ChannelZeroPuzzleContext context)
        {
            bool opened = context.GetState(PuzzleId) != "default";
            bool complete = loot.All(entry => WasCollected(entry.item, context));
            PuzzleView view = View(!opened ? "잠금쇠를 열어 내부를 확인한다." : complete ? "필요한 물품을 모두 챙겼다." : "사용할 물품을 고른다.",
                complete ? completeArtwork : opened ? openArtwork : "default", complete);
            if (!opened) view.actions.Add(new PuzzleActionView("open", "열기"));
            else foreach (var entry in loot.Where(entry => !WasCollected(entry.item, context)))
                view.actions.Add(new PuzzleActionView("take:" + entry.item, entry.label));
            return view;
        }

        public override PuzzleActionResult Execute(string actionId, ChannelZeroPuzzleContext context)
        {
            if (actionId == "open") context.SetState(PuzzleId, "open");
            else if (actionId.StartsWith("take:", StringComparison.Ordinal))
            {
                string itemId = actionId[5..];
                if (loot.Any(entry => entry.item == itemId) && !WasCollected(itemId, context))
                {
                    context.AddItem(itemId);
                    context.SetFlag(ClaimFlag(itemId));
                    context.SetState(PuzzleId, "open");
                    if (loot.All(entry => WasCollected(entry.item, context)) && !string.IsNullOrWhiteSpace(solvedFlag))
                        context.SetFlag(solvedFlag);
                }
            }
            return Result(context, actionId.StartsWith("take:") ? "item_acquired" : null);
        }

        private bool WasCollected(string itemId, ChannelZeroPuzzleContext context)
        {
            if (context.HasItem(itemId) || context.GetFlag(ClaimFlag(itemId)))
                return true;

            // Compatibility for saves created before loot claims were tracked separately.
            if (PuzzleId == ChannelZeroPuzzleIds.MedicalCabinet)
            {
                string treatment = context.GetState(ChannelZeroPuzzleIds.JinwooHand);
                if (itemId == ChannelZeroPuzzleIds.Disinfectant)
                    return treatment == "disinfected" || treatment == "treated" || context.GetFlag("JinwooTreated");
                if (itemId == ChannelZeroPuzzleIds.Bandage)
                    return treatment == "treated" || context.GetFlag("JinwooTreated");
            }

            if (PuzzleId == ChannelZeroPuzzleIds.TubeCase && itemId == ChannelZeroPuzzleIds.ReplacementTube)
            {
                string repair = context.GetState(ChannelZeroPuzzleIds.CrtRear);
                return repair == "replaced" || repair == "bracket_locked" ||
                    repair == "bracket_removed" || context.GetFlag("CrtRepaired");
            }

            return false;
        }

        private string ClaimFlag(string itemId) => $"LootClaimed:{PuzzleId}:{itemId}";
    }

    internal sealed class SignalPuzzle : PuzzleBase
    {
        public SignalPuzzle() : base(ChannelZeroPuzzleIds.CrtSignal, ChannelZeroIds.LivingCrtFrontCloseup, "무전원 CRT") { }
        public override PuzzleView BuildView(ChannelZeroPuzzleContext c)
        {
            bool stable = c.GetFlag(Chapter1Flags.ChildSofaIntroSeen);
            PuzzleView view = View(
                stable ? "아이의 모습은 사라졌고 후면에서 금속 팽창음이 난다." : "전원선은 빠져 있지만 화면에 백색 잡음이 흐른다.",
                "default", false);
            if (stable)
                view.actions.Add(new PuzzleActionView("inspect_rear", "TV 뒤쪽 확인"));
            return view;
        }
        public override PuzzleActionResult Execute(string actionId, ChannelZeroPuzzleContext c)
        {
            if (actionId == "inspect_rear")
            {
                PuzzleActionResult result = Result(c);
                result.requestedCloseupId = ChannelZeroIds.LivingCrtRearCloseup;
                return result;
            }
            return Result(c);
        }
    }

    internal sealed class CrtRearPuzzle : PuzzleBase, IInventoryItemPuzzle
    {
        public CrtRearPuzzle() : base(ChannelZeroPuzzleIds.CrtRear, ChannelZeroIds.LivingCrtRearCloseup, "CRT 후면 수리부") { }
        public override PuzzleView BuildView(ChannelZeroPuzzleContext c)
        {
            string stage = c.GetState(PuzzleId);
            return stage switch
            {
                "screws_loosened" => View("황동 나사를 모두 풀었다. 서비스 덮개를 분리할 수 있다.",
                    "screws_loosened", false, new PuzzleActionView("remove_cover", "서비스 덮개 분리")),
                "open" => View("그을린 진공관이 보인다.", "open", false, new PuzzleActionView("remove_tube", "손상 진공관 분리")),
                "tube_removed" => InventoryTarget(View("빈 소켓이 드러나 있다.", "tube_removed")),
                "replaced" => View("교체관을 12도 돌려 고정했다. 덧댄 황동 브래킷을 잠근다.", "replaced", false, new PuzzleActionView("lock_bracket", "브래킷 잠금")),
                "bracket_locked" => View("진공관 장착과 브래킷 고정이 끝났다.", "bracket_removed", true),
                _ => InventoryTarget(View("서비스 덮개가 나사로 고정되어 있다.", "default", false,
                    c.GetFlag("CrtRearInspected")
                        ? Array.Empty<PuzzleActionView>()
                        : new[] { new PuzzleActionView("inspect_screws", "후면 나사 조사") })),
            };
        }
        public override PuzzleActionResult Execute(string actionId, ChannelZeroPuzzleContext c)
        {
            if (actionId == "inspect_screws")
            {
                c.SetFlag("CrtRearInspected");
                c.SetFlag(Chapter1Flags.TVRearInspected);
                return Result(c, "interact_screw");
            }
            if (actionId == "open_cover") return UseItem(ChannelZeroPuzzleIds.Screwdriver, c);
            if (actionId == "install_tube") return UseItem(ChannelZeroPuzzleIds.ReplacementTube, c);
            else if (actionId == "remove_cover") c.SetState(PuzzleId, "open");
            else if (actionId == "remove_tube") c.SetState(PuzzleId, "tube_removed");
            else if (actionId == "lock_bracket")
            {
                c.SetState(PuzzleId, "bracket_locked");
                c.SetFlag("CrtRearOpen");
                c.SetFlag("CrtRepaired");
                c.SetFlag(Chapter1Flags.TVPhysicalRepairDone);
            }
            else return Result(c, "interact_screw");
            return Result(c, c.GetFlag("CrtRepaired") ? "puzzle_success" : "state_changed");
        }

        public PuzzleActionResult UseItem(string itemId, ChannelZeroPuzzleContext c)
        {
            string stage = c.GetState(PuzzleId);
            if (stage == "default" && c.GetFlag("CrtRearInspected") &&
                itemId == ChannelZeroPuzzleIds.Screwdriver && c.HasItem(itemId))
            {
                c.SetState(PuzzleId, "screws_loosened");
                return Result(c, "state_changed");
            }
            if (stage == "tube_removed" && itemId == ChannelZeroPuzzleIds.ReplacementTube && c.RemoveItem(itemId))
            {
                c.SetState(PuzzleId, "replaced");
                return Result(c, "state_changed");
            }
            return ItemRejected(c, "interact_screw");
        }

        private static PuzzleView InventoryTarget(PuzzleView view)
        {
            view.acceptsInventoryItems = true;
            return view;
        }

        private PuzzleActionResult ItemRejected(ChannelZeroPuzzleContext c, string trigger)
        {
            PuzzleActionResult result = Result(c, trigger);
            result.feedbackText = ChannelZeroPuzzleService.NoActionMessage;
            return result;
        }
    }

    internal sealed class HandTreatmentPuzzle : PuzzleBase, IInventoryItemPuzzle
    {
        public HandTreatmentPuzzle() : base(ChannelZeroPuzzleIds.JinwooHand, ChannelZeroIds.LivingHandTreatmentCloseup, "진우의 손 치료") { }
        public override PuzzleView BuildView(ChannelZeroPuzzleContext c)
        {
            if (!c.GetFlag("CrtRepaired"))
                return View(ChannelZeroPuzzleService.NoActionMessage, "default", true);

            string stage = c.GetState(PuzzleId);
            if (stage == "treated")
                return View("상처 치료는 끝났다. 수상기 아래 기록 장치와 거실의 남은 단서를 확인하자.",
                    "default", true);
            PuzzleView view = View(stage == "disinfected"
                ? "상처의 오염은 닦여 있다."
                : "진우의 손등에 깊은 상처가 있다.");
            view.acceptsInventoryItems = true;
            return view;
        }
        public override PuzzleActionResult Execute(string actionId, ChannelZeroPuzzleContext c)
        {
            if (actionId == "disinfect") return UseItem(ChannelZeroPuzzleIds.Disinfectant, c);
            if (actionId == "bandage") return UseItem(ChannelZeroPuzzleIds.Bandage, c);
            return ItemRejected(c);
        }

        public PuzzleActionResult UseItem(string itemId, ChannelZeroPuzzleContext c)
        {
            if (!c.GetFlag("CrtRepaired"))
                return ItemRejected(c);

            string stage = c.GetState(PuzzleId);
            if (itemId == ChannelZeroPuzzleIds.Disinfectant && stage == "default" && c.RemoveItem(itemId))
                c.SetState(PuzzleId, "disinfected");
            else if (itemId == ChannelZeroPuzzleIds.Bandage && stage == "disinfected" && c.RemoveItem(itemId))
            {
                c.SetState(PuzzleId, "treated");
                c.SetFlag("JinwooTreated");
            }
            else if (itemId == ChannelZeroPuzzleIds.Bandage && stage == "default")
                return Result(c, "use_bandage_before_disinfect");
            else
                return ItemRejected(c);
            return Result(c, c.GetFlag("JinwooTreated") ? "puzzle_success" : "state_changed");
        }

        private PuzzleActionResult ItemRejected(ChannelZeroPuzzleContext c)
        {
            PuzzleActionResult result = Result(c);
            result.feedbackText = ChannelZeroPuzzleService.NoActionMessage;
            return result;
        }
    }

    internal sealed class Chapter1ReturnCircuitPuzzle : PuzzleBase
    {
        public Chapter1ReturnCircuitPuzzle(string closeupId)
            : base(ChannelZeroPuzzleIds.ReturnCircuit, closeupId, "1961 귀환 회로") { }

        public override PuzzleView BuildView(ChannelZeroPuzzleContext c)
        {
            if (c.Era != ChannelEra.Year1961)
                return View(ChannelZeroPuzzleService.NoActionMessage, completed: true);
            bool wiring = c.GetFlag(Chapter1Flags.ReturnWiringSolved);
            bool tubes = c.GetFlag(Chapter1Flags.ReturnTubeSequenceSolved);
            int wireIndex = Parse(c.GetState(PuzzleId + ".wireIndex", "0"));
            int tubeIndex = Parse(c.GetState(PuzzleId + ".tubeIndex", "0"));
            PuzzleView view = View(tubes ? "RETURN 2001" : wiring
                ? $"배선 완료. 진공관 점등 순서를 되짚는다. ({tubeIndex}/4)"
                : $"라디오와 액자 뒤 대응에 맞춰 선을 연결한다. ({wireIndex}/4)",
                completed: tubes);
            if (!wiring)
            {
                string clueStage = c.GetState(PuzzleId + ".clueStage", "tuner");
                if (clueStage == "tuner")
                    view.actions.Add(new PuzzleActionView("inspect_frame", "가족사진 액자 뒤 확인"));
                else if (clueStage == "frame_back")
                    view.actions.Add(new PuzzleActionView("open_frame_back", "액자 뒤판 열기"));
                else if (clueStage == "frame_open")
                    view.actions.Add(new PuzzleActionView("inspect_tone_trace", "음·색 흔적 대조"));
                else
                    foreach (string color in new[] { "red", "blue", "yellow", "green" })
                        view.actions.Add(new PuzzleActionView("wire:" + color, color switch
                        { "red" => "빨강", "blue" => "파랑", "yellow" => "노랑", _ => "초록" }));
            }
            else if (!tubes)
                for (int i = 1; i <= 4; i++) view.actions.Add(new PuzzleActionView("tube:" + i, $"진공관 {i}"));
            return view;
        }

        public override PuzzleActionResult Execute(string actionId, ChannelZeroPuzzleContext c)
        {
            if (actionId == "inspect_frame")
            {
                c.SetState(PuzzleId + ".clueStage", "frame_back");
                return Result(c, "frame_back");
            }
            if (actionId == "open_frame_back")
            {
                c.SetState(PuzzleId + ".clueStage", "frame_open");
                return Result(c, "state_changed");
            }
            if (actionId == "inspect_tone_trace")
            {
                c.SetState(PuzzleId + ".clueStage", "trace_seen");
                return Result(c, "radio");
            }
            ReturnCircuitPuzzle puzzle = new(c.State);
            bool success = actionId.StartsWith("wire:", StringComparison.Ordinal)
                ? puzzle.ConnectWire(actionId[5..])
                : actionId.StartsWith("tube:", StringComparison.Ordinal) &&
                  puzzle.SelectTube(Parse(actionId[5..]));
            PuzzleActionResult result = Result(c, success ? "state_changed" : "puzzle_wrong");
            result.feedbackText = success ? string.Empty : "순서가 맞지 않는다. 확인한 연결은 유지된다.";
            return result;
        }

        private static int Parse(string value) => int.TryParse(value, out int parsed) ? parsed : 0;
    }

    internal sealed class Chapter1VasePuzzle : PuzzleBase
    {
        public Chapter1VasePuzzle(string closeupId)
            : base(ChannelZeroPuzzleIds.VaseCausality, closeupId, "화병 인과 사건") { }
        public override PuzzleView BuildView(ChannelZeroPuzzleContext c)
        {
            PuzzleView view;
            if (c.Era == ChannelEra.Year1961)
            {
                bool corrected = c.GetFlag(Chapter1Flags.VasePreserved1961);
                view = View(corrected ? "화병을 선반 안쪽 안전 영역으로 옮겼다." :
                    "화병이 선반 끝에서 떨어지기 직전이다.", completed: corrected);
                if (!corrected)
                {
                    view.actions.Add(new PuzzleActionView("leave", "그대로 둔다"));
                    if (c.GetFlag(Chapter1Flags.VaseBroken1961))
                        view.actions.Add(new PuzzleActionView("move", "안쪽으로 옮긴다"));
                }
            }
            else if (c.Era == ChannelEra.Year1981)
            {
                bool preserved = c.GetFlag(Chapter1Flags.VasePreserved1961);
                view = View(preserved ? "화병과 장식장이 온전하다." :
                    "화병은 사라졌고 장식장 옆면에 오래된 균열이 남았다.", completed: preserved);
                view.actions.Add(new PuzzleActionView(preserved ? "confirm" : "observe_broken", "결과 확인"));
            }
            else view = View(ChannelZeroPuzzleService.NoActionMessage, completed: true);
            return view;
        }
        public override PuzzleActionResult Execute(string actionId, ChannelZeroPuzzleContext c)
        {
            VaseCausalityPuzzle puzzle = new(c.State);
            if (actionId == "leave") puzzle.LeaveVase();
            else if (actionId == "observe_broken") puzzle.ObserveBrokenFuture();
            else if (actionId == "move") puzzle.MoveVase();
            else if (actionId == "confirm") puzzle.ConfirmPreservedFuture();
            return Result(c, "state_changed");
        }
    }

    internal sealed class Chapter1ChildTracePuzzle : PuzzleBase
    {
        public Chapter1ChildTracePuzzle(string closeupId)
            : base(ChannelZeroPuzzleIds.FamilyPhotos, closeupId, "아이의 시대별 흔적") { }
        public override PuzzleView BuildView(ChannelZeroPuzzleContext c)
        {
            if (!c.GetFlag(Chapter1Flags.ManualDialUnlocked))
                return View("같은 거실에서 찍은 가족사진. 가족만 있고 아이는 없다.");
            string flag = TraceFlag(c.Era);
            bool seen = c.GetFlag(flag);
            bool complete = c.GetFlag(Chapter1Flags.ChildTrace2749Known);
            PuzzleView view = View(complete ? "자동 기록: 2 · 7 · 4 · 9" :
                $"{(int)c.Era}년의 사진과 반사면을 비교한다.", completed: complete);
            if (!seen) view.actions.Add(new PuzzleActionView("observe", "흔적 조사"));
            return view;
        }
        public override PuzzleActionResult Execute(string actionId, ChannelZeroPuzzleContext c)
        {
            if (actionId == "observe") new ChildTraceStabilizerPuzzle(c.State).ObserveTrace(c.Era);
            return Result(c, "state_changed");
        }
        private static string TraceFlag(ChannelEra era) => era switch
        {
            ChannelEra.Year1961 => Chapter1Flags.ChildTrace1961Seen,
            ChannelEra.Year1981 => Chapter1Flags.ChildTrace1981Seen,
            ChannelEra.Year2001 => Chapter1Flags.ChildTrace2001Seen,
            _ => Chapter1Flags.ChildTrace2021Seen,
        };
    }

    internal sealed class Chapter1SignalStabilizerPuzzle : PuzzleBase
    {
        public Chapter1SignalStabilizerPuzzle(string closeupId)
            : base(ChannelZeroPuzzleIds.ChildTraceStabilizer, closeupId, "카펫 아래 신호 안정기") { }
        public override PuzzleView BuildView(ChannelZeroPuzzleContext c)
        {
            bool solved = c.GetFlag(Chapter1Flags.SignalStabilizerSolved);
            string input = c.GetState(PuzzleId + ".input", string.Empty);
            PuzzleView view = View(solved ? "네 시대 신호가 정렬됐다. 공통 시각은 08:08이다."
                : $"네 자리 황동 다이얼: {input.PadRight(4, '·')}", completed: solved);
            if (!solved)
            {
                for (int i = 0; i <= 9; i++) view.actions.Add(new PuzzleActionView("digit:" + i, i.ToString()));
                view.actions.Add(new PuzzleActionView("reset", "입력 지우기"));
            }
            return view;
        }
        public override PuzzleActionResult Execute(string actionId, ChannelZeroPuzzleContext c)
        {
            if (actionId == "reset") c.SetState(PuzzleId + ".input", string.Empty);
            else if (actionId.StartsWith("digit:", StringComparison.Ordinal))
            {
                string input = (c.GetState(PuzzleId + ".input", string.Empty) + actionId[6..]);
                if (input.Length == 4)
                {
                    bool solved = new ChildTraceStabilizerPuzzle(c.State).Submit(input);
                    c.SetState(PuzzleId + ".input", string.Empty);
                    return Result(c, solved ? "puzzle_success" : "puzzle_wrong");
                }
                c.SetState(PuzzleId + ".input", input);
            }
            return Result(c);
        }
    }

    internal sealed class Chapter1JinwooPuzzle : PuzzleBase, IInventoryItemPuzzle
    {
        public Chapter1JinwooPuzzle()
            : base(ChannelZeroPuzzleIds.JinwooHand, ChannelZeroIds.LivingHandTreatmentCloseup, "진우의 오른손") { }
        public override PuzzleView BuildView(ChannelZeroPuzzleContext c)
        {
            JinwooHandCausalityPuzzle puzzle = new(c.State);
            if (c.Era == ChannelEra.Year2001 && c.GetFlag(Chapter1Flags.JinwooInjurySeen))
            {
                bool preserved = c.GetFlag(Chapter1Flags.JinwooDisinfected) && c.GetFlag(Chapter1Flags.JinwooBandaged);
                PuzzleView future = View(preserved ? "오른손이 보존된 사진과 치료 기록이다."
                    : "빈 오른쪽 소매와 우측 전완 절단 진료 기록이다.", completed: c.GetFlag(Chapter1Flags.JinwooHandPreserved));
                future.actions.Add(new PuzzleActionView(preserved ? "confirm_preserved" : "observe_amputation", "기록 확인"));
                return future;
            }
            if (c.Era != ChannelEra.Year1961) return View(ChannelZeroPuzzleService.NoActionMessage, completed: true);
            if (!c.GetFlag(Chapter1Flags.JinwooInjurySeen)) puzzle.ObserveInjury();
            PuzzleView view = View(c.GetFlag(Chapter1Flags.JinwooBandaged) ? "상처 세척과 압박 고정이 끝났다."
                : c.GetFlag(Chapter1Flags.JinwooDisinfected) ? "오염을 닦았다. 이제 붕대를 감을 수 있다."
                : "오른손목에 깊은 상처와 고전압 화상이 겹쳤다.", completed: c.GetFlag(Chapter1Flags.JinwooBandaged));
            view.acceptsInventoryItems = !c.GetFlag(Chapter1Flags.JinwooBandaged);
            return view;
        }
        public override PuzzleActionResult Execute(string actionId, ChannelZeroPuzzleContext c)
        {
            JinwooHandCausalityPuzzle puzzle = new(c.State);
            if (actionId == "observe_amputation") puzzle.ObserveAmputationFuture();
            else if (actionId == "confirm_preserved") puzzle.ConfirmPreservedFuture();
            return Result(c, "state_changed");
        }
        public PuzzleActionResult UseItem(string itemId, ChannelZeroPuzzleContext c)
        {
            bool accepted = new JinwooHandCausalityPuzzle(c.State).Use(itemId);
            if (accepted) c.RemoveItem(itemId);
            PuzzleActionResult result = Result(c, accepted ? "state_changed" : "use_bandage_before_disinfect");
            if (!accepted) result.feedbackText = "먼저 상처를 씻어야 한다.";
            return result;
        }
    }

    internal sealed class Chapter1ClockKnotPuzzle : PuzzleBase
    {
        public Chapter1ClockKnotPuzzle(string closeupId)
            : base(ChannelZeroPuzzleIds.ClockKnot, closeupId, "08:08 괘종시계") { }
        public override PuzzleView BuildView(ChannelZeroPuzzleContext c)
        {
            if (c.Era != ChannelEra.Year1981)
                return View($"{(int)c.Era}년 시계 후면의 수리 흔적이다.", completed: true);
            bool solved = c.GetFlag(Chapter1Flags.ClockKnotSolved);
            PuzzleView view = View(solved ? "08:08:01. 숨은 칸이 열렸다." :
                "초침이 08:07:59와 08:08:00 사이를 반복한다.", completed: solved);
            if (!solved)
            {
                if (!c.GetFlag(Chapter1Flags.ClockKnotEntered)) view.actions.Add(new PuzzleActionView("enter", "08:08 눈금 고정"));
                else if (c.GetState(PuzzleId + ".step", "key") == "key") view.actions.Add(new PuzzleActionView("insert_key", "태엽 열쇠 삽입"));
                else if (c.GetState(PuzzleId + ".step") == "gear") view.actions.Add(new PuzzleActionView("fix_gear", "공통 황동 톱니 고정"));
                else if (c.GetState(PuzzleId + ".step") == "time") view.actions.Add(new PuzzleActionView("set_0808", "시침·분침 08:08"));
                else view.actions.Add(new PuzzleActionView("release_second", "멈춘 1초 흘려보내기"));
            }
            return view;
        }
        public override PuzzleActionResult Execute(string actionId, ChannelZeroPuzzleContext c)
        {
            ClockKnotPuzzle puzzle = new(c.State);
            if (actionId == "enter") puzzle.Enter();
            else if (actionId == "insert_key" && c.HasItem(ChannelZeroPuzzleIds.ClockKey)) c.SetState(PuzzleId + ".step", "gear");
            else if (actionId == "fix_gear") c.SetState(PuzzleId + ".step", "time");
            else if (actionId == "set_0808") c.SetState(PuzzleId + ".step", "tick");
            else if (actionId == "release_second") puzzle.Solve(true, true, 8, 8);
            return Result(c, c.GetFlag(Chapter1Flags.ClockKnotSolved) ? "puzzle_success" : "state_changed");
        }
    }

    internal sealed class RecPuzzle : PuzzleBase
    {
        public RecPuzzle(string closeupId) : base(ChannelZeroPuzzleIds.RecMaster, closeupId, "REC 기록 장치") { }
        public override PuzzleView BuildView(ChannelZeroPuzzleContext c)
        {
            bool complete = c.HasItem(ChannelZeroPuzzleIds.MasterTape);
            ChannelEra[] sourceEras = { ChannelEra.Year1961, ChannelEra.Year1981, ChannelEra.Year2021 };
            string captured = string.Join(", ", sourceEras
                .Where(era => HasRecording(c, era)).Select(era => ((int)era).ToString()));
            bool liveReferenceSeen = c.GetFlag("LiveReference2001Seen");
            List<PuzzleActionView> actions = new();
            if (!complete)
            {
                if (c.Era == ChannelEra.Year2001)
                    actions.Add(new PuzzleActionView("observe_live", "2001년 실시간 기준 확인",
                        !liveReferenceSeen));
                else
                    actions.Add(new PuzzleActionView("record", $"{(int)c.Era}년 REC",
                        c.Recording != null));
                if (c.Era == ChannelEra.Year2021)
                    actions.Add(new PuzzleActionView("mix", "MASTER 합성"));
            }
            string live = liveReferenceSeen ? "확인됨" : "미확인";
            return View(complete ? "세 시대의 기록을 2001년 실시간 화면에 맞춰 MASTER 테이프를 만들었다."
                    : $"REC 소스: {(captured.Length == 0 ? "없음" : captured)} / 2001 실시간 기준: {live}",
                "default", complete, actions.ToArray());
        }
        public override PuzzleActionResult Execute(string actionId, ChannelZeroPuzzleContext c)
        {
            if (actionId == "record" && c.GetFlag("CrtRepaired") && c.Era != ChannelEra.Year2001 &&
                c.Recording != null)
                c.Recording.RecordCurrent();
            else if (actionId == "observe_live" && c.Era == ChannelEra.Year2001)
                c.SetFlag("LiveReference2001Seen");
            else if (actionId == "mix" && c.Era == ChannelEra.Year2021 &&
                new[] { ChannelEra.Year1961, ChannelEra.Year1981, ChannelEra.Year2021 }
                    .All(era => HasRecording(c, era)) && c.GetFlag("LiveReference2001Seen"))
            { c.AddItem(ChannelZeroPuzzleIds.MasterTape); c.SetState(PuzzleId, "master_complete"); c.SetFlag("RecMasterComplete"); }
            else return Result(c, "press_play");
            return Result(c, c.HasItem(ChannelZeroPuzzleIds.MasterTape) ? "puzzle_success" : "state_changed");
        }

        private static bool HasRecording(ChannelZeroPuzzleContext c, ChannelEra era) =>
            c.Recording?.HasRecording(ChannelZeroIds.LivingRoom, era) ??
            ChannelZeroRecordingState.HasRecording(c.State, ChannelZeroIds.LivingRoom, era);
    }

    internal sealed class EraEvidencePuzzle : PuzzleBase
    {
        private readonly string actionLabel, flagPrefix, completeFlag;
        private readonly bool chapterTwo;
        public EraEvidencePuzzle(string id, string closeup, string title, string actionLabel, string flagPrefix, string completeFlag, bool chapterTwo)
            : base(id, closeup, title) { this.actionLabel = actionLabel; this.flagPrefix = flagPrefix; this.completeFlag = completeFlag; this.chapterTwo = chapterTwo; }
        public override PuzzleView BuildView(ChannelZeroPuzzleContext c)
        {
            int[] years = { 1961, 1981, 2001, 2021 };
            string seen = string.Join(", ", years.Where(year => c.GetFlag(flagPrefix + year)));
            bool complete = years.All(year => c.GetFlag(flagPrefix + year));
            bool finalized = c.GetFlag(completeFlag);
            List<PuzzleActionView> actions = new();
            if (!c.GetFlag(flagPrefix + (int)c.Era))
                actions.Add(new PuzzleActionView("observe", $"{(int)c.Era}년 {actionLabel}"));
            if (complete && !finalized)
                actions.Add(new PuzzleActionView("finalize", chapterTwo ? "기록장 복원" : "사진 비교 완료"));
            return View($"확인한 연도: {(seen.Length == 0 ? "없음" : seen)}", "default", finalized,
                actions.ToArray());
        }
        public override PuzzleActionResult Execute(string actionId, ChannelZeroPuzzleContext c)
        {
            if (actionId == "observe") c.SetFlag(flagPrefix + (int)c.Era);
            bool all = new[] { 1961, 1981, 2001, 2021 }.All(year => c.GetFlag(flagPrefix + year));
            if (actionId == "finalize" && all)
            {
                c.SetFlag(completeFlag);
                if (!chapterTwo) { c.SetFlag("FamilyPhotoClueSeen"); c.SetFlag("MinaAllErasConfirmed"); }
            }
            return Result(c, actionId == "finalize" && all ? "compare_complete" : "state_changed");
        }
    }

    internal sealed class InspectPuzzle : PuzzleBase
    {
        private readonly string inspectedText, flag;
        private readonly bool lockPlan;
        public InspectPuzzle(string id, string closeup, string title, string inspectedText, string flag, bool lockPlan = false)
            : base(id, closeup, title) { this.inspectedText = inspectedText; this.flag = flag; this.lockPlan = lockPlan; }
        public override PuzzleView BuildView(ChannelZeroPuzzleContext c) => View(c.GetFlag(flag) ? inspectedText : "세부 내용을 조사할 수 있다.", "default", c.GetFlag(flag), c.GetFlag(flag) ? Array.Empty<PuzzleActionView>() : new[] { new PuzzleActionView("inspect", "자세히 조사") });
        public override PuzzleActionResult Execute(string actionId, ChannelZeroPuzzleContext c)
        { if (actionId == "inspect") { c.SetFlag(flag); if (lockPlan) c.State.ForeshadowDisappearingStair(true); } return Result(c, "state_changed"); }
    }

    internal sealed class PartsDrawerPuzzle : LootPuzzle
    {
        public PartsDrawerPuzzle() : base(ChannelZeroPuzzleIds.WorkshopDrawer, ChannelZeroIds.WorkshopPartsDrawerCloseup,
            "부품 서랍", new[] { (ChannelZeroPuzzleIds.NormalTube, "정상 후보 진공관"), (ChannelZeroPuzzleIds.FaultyTube, "그을린 후보 진공관") }) { }
    }

    internal sealed class TubeTesterPuzzle : PuzzleBase
    {
        public TubeTesterPuzzle() : base(ChannelZeroPuzzleIds.TubeTester, ChannelZeroIds.WorkshopTubeTesterCloseup, "진공관 시험기") { }
        public override PuzzleView BuildView(ChannelZeroPuzzleContext c)
        {
            string loaded = c.GetState(PuzzleId + ".loaded", "empty");
            string result = c.GetState(PuzzleId, "empty");
            PuzzleView view = View(result == "normal" ? "바늘이 정상 범위에 멈췄다." : result == "faulty" ? "바늘이 오르지 않는다." : $"장착: {loaded}", "default", result == "normal");
            if (c.HasItem(ChannelZeroPuzzleIds.NormalTube)) view.actions.Add(new PuzzleActionView("load:normal", "정상 후보 장착"));
            if (c.HasItem(ChannelZeroPuzzleIds.FaultyTube)) view.actions.Add(new PuzzleActionView("load:faulty", "그을린 후보 장착"));
            view.actions.Add(new PuzzleActionView("test", "측정 시작", loaded != "empty"));
            return view;
        }
        public override PuzzleActionResult Execute(string actionId, ChannelZeroPuzzleContext c)
        {
            if (actionId.StartsWith("load:")) c.SetState(PuzzleId + ".loaded", actionId[5..]);
            else if (actionId == "test")
            {
                string loaded = c.GetState(PuzzleId + ".loaded", "empty");
                if (loaded == "empty") return Result(c, "start_test");
                c.SetState(PuzzleId, loaded);
                c.State.SetPuzzleState("result:TubeTest", loaded);
                if (loaded == "normal") c.SetFlag("TubeTestNormal");
                return Result(c, "test_complete");
            }
            return Result(c);
        }
    }

    internal sealed class FoldingCrankPuzzle : PuzzleBase
    {
        public FoldingCrankPuzzle() : base(ChannelZeroPuzzleIds.FoldingCrank, ChannelZeroIds.WorkshopFoldingCrankInspect, "접이식 크랭크") { }
        public override PuzzleView BuildView(ChannelZeroPuzzleContext c)
        {
            bool owned = c.HasItem(ChannelZeroIds.FoldingCrankItem);
            bool held = c.State.heldItemId == ChannelZeroIds.FoldingCrankItem;
            return View(held ? "크랭크를 HOLD 슬롯에 고정했다." : owned ? "후반 장치에 사용할 크랭크를 확보했다." : c.Era == ChannelEra.Year1961 ? "도면의 회전 소켓과 규격이 같다." : "이 시대에는 크랭크가 보이지 않는다.", "default", held,
                held ? Array.Empty<PuzzleActionView>() : owned ? new[] { new PuzzleActionView("hold", "HOLD로 고정") } : c.Era == ChannelEra.Year1961 ? new[] { new PuzzleActionView("take", "크랭크 획득") } : Array.Empty<PuzzleActionView>());
        }
        public override PuzzleActionResult Execute(string actionId, ChannelZeroPuzzleContext c)
        {
            if (actionId == "take" && c.Era == ChannelEra.Year1961) { c.AddItem(ChannelZeroIds.FoldingCrankItem); c.SetFlag("FoldingCrank"); }
            else if (actionId == "hold" && c.State.TryHold(ChannelZeroIds.FoldingCrankItem)) c.SetFlag("FoldingCrankHeld");
            return Result(c, actionId == "take" ? "item_acquired" : "state_changed");
        }
    }

}
