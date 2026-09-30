using System;
using ChannelZero.Runtime.Core;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ChannelZero.Runtime.Presentation
{
    [Serializable]
    public sealed class StringEvent : UnityEvent<string> { }

    [DisallowMultipleComponent]
    public sealed class ChannelZeroVerticalSliceController : MonoBehaviour
    {
        [SerializeField] private ChannelZeroRoomPresenter presenter;
        [SerializeField] private ChannelZeroCloseupCanvasController closeupCanvas;
        [SerializeField] private ChannelZeroHudController hudController;
        [SerializeField] private TMP_Text statusLabel;
        [SerializeField] private Button backButton;
        [SerializeField] private TMP_Text channelReadout;
        [SerializeField] private TMP_Text operationReadout;
        [SerializeField] private bool restoreSaveOnStart;
        [SerializeField] private string locale = "ko-KR";
        [SerializeField] private Chapter1GlitchFrameSequencePlayer chapterOneFrameSequence;
        [Header("Audio / direction extension points")]
        [SerializeField] private StringEvent onRoomChanged;
        [SerializeField] private StringEvent onEraChanged;
        [SerializeField] private StringEvent onHotspotActivated;
        [SerializeField] private StringEvent onPuzzleActivated = new();
        [SerializeField] private StringEvent onOperationChanged;
        [SerializeField] private StringEvent onChapterCompleted = new();

        private ChannelZeroSaveService saveService;
        private IChannelZeroPuzzleView puzzleView;
        private NarrativeTextResolver narrativeResolver;
        private NarrativePresenter narrativePresenter;
        private ChannelZeroPuzzleService puzzleService;
        private ChannelZeroInventoryService inventoryService;
        private ChannelZeroRoomNavigationService navigationService;
        private ChannelZeroChapterProgressionService progressionService;
        private ChannelZeroScenarioProgressionService scenarioProgressionService;
        private ChannelZeroProgressGuidanceService guidanceService;
        private ChannelZeroOperationService operationService;
        private ChannelZeroComparisonOverlayService comparisonService;
        private ChannelZeroGameContext gameContext;
        private ChannelZeroInteractionDispatcher interactionDispatcher;
        private string activeNarrativeTarget = string.Empty;
        private string pendingHighlightHotspotId = string.Empty;
        private string pendingHighlightMarker = string.Empty;
        private string pendingChapterCompletion = string.Empty;
        private string pendingGuidanceTextId = string.Empty;
        private AudioSource completionAudioSource;
        private Chapter1SequenceDirector chapterOneDirector;
        private YearDialController yearDial;
        private CausalEventService causalEvents;
        private bool chapterSequenceInProgress;

        public ChannelZeroSessionState State { get; private set; }

        private void Awake()
        {
            puzzleView = closeupCanvas;
            saveService = new ChannelZeroSaveService(ChannelZeroRuntimeEnvironment.CreateSaveStore());
            State = restoreSaveOnStart && saveService.TryLoad(out ChannelZeroSessionState loaded)
                ? loaded
                : ChannelZeroSessionState.CreateNew();
            ConfigureGameServices();
            DisableLegacyTimeUi();

            narrativePresenter = GetComponent<NarrativePresenter>();
            if (narrativePresenter == null)
                narrativePresenter = gameObject.AddComponent<NarrativePresenter>();
            narrativePresenter.Configure(statusLabel);

        }

        private void OnEnable()
        {
            if (presenter != null)
                presenter.HotspotClicked += HandleHotspot;
            if (puzzleView != null)
            {
                puzzleView.Closed += HandleCloseupClosed;
                puzzleView.PuzzleActionRequested += HandlePuzzleAction;
            }
        }

        private void Start()
        {
            Refresh("2001년 현관. 거실문을 선택하세요.");
            if (State.GetFlag(Chapter1Flags.LegacyMigrationNoticePending))
            {
                State.SetFlag(Chapter1Flags.LegacyMigrationNoticePending, false);
                SetStatus("시간 조작이 연도 다이얼 하나로 변경되었습니다. 제1막의 안전한 지점에서 다시 시작합니다.");
                saveService.Save(State);
            }
            if (!string.IsNullOrWhiteSpace(State.activeCloseupId) && puzzleView != null)
            {
                activeNarrativeTarget = gameContext.InteractionCatalog.NarrativeTargetForCloseup(State.activeCloseupId);
                puzzleView.Restore(State, ResolveCloseupText(activeNarrativeTarget));
                PresentActivePuzzle();
            }
        }

        private void OnDisable()
        {
            if (presenter != null)
                presenter.HotspotClicked -= HandleHotspot;
            if (puzzleView != null)
            {
                puzzleView.Closed -= HandleCloseupClosed;
                puzzleView.PuzzleActionRequested -= HandlePuzzleAction;
            }
        }

        public void Tune1961() { if (CanAcceptWorldInput()) Tune(ChannelEra.Year1961); }
        public void Tune1981() { if (CanAcceptWorldInput()) Tune(ChannelEra.Year1981); }
        public void Tune2001() { if (CanAcceptWorldInput()) Tune(ChannelEra.Year2001); }
        public void Tune2021() { if (CanAcceptWorldInput()) Tune(ChannelEra.Year2021); }
        public void TunePast() { if (CanAcceptWorldInput()) ShiftEra(-1); }
        public void TuneFuture() { if (CanAcceptWorldInput()) ShiftEra(1); }

        [Obsolete("Chapter 1 uses the year dial only.")]
        public void SelectRec() => SetStatus("기록 기능은 연도 다이얼로 통합되었습니다.");
        [Obsolete("Chapter 1 uses the year dial only.")]
        public void SelectPlay() => SetStatus("재생 기능은 자동 연출로 통합되었습니다.");
        [Obsolete("Chapter 1 uses the year dial only.")]
        public void SelectLoad() => SetStatus("비교는 필요한 시점에 자동으로 실행됩니다.");
        [Obsolete("Chapter 1 uses local causal checkpoints.")]
        public void SelectRew() => SetStatus("같은 연도를 다시 선택하면 해당 인과 사건만 복구됩니다.");
        [Obsolete("Chapter 1 no longer exposes HOLD.")]
        public void SelectHold() => SetStatus("아이템은 인벤토리에서 직접 선택해 사용하세요.");

        public void AdvanceNarrative() => narrativePresenter?.Advance();
        public void SkipNarrative() => narrativePresenter?.Skip();

        public void CycleEraForward()
        {
            if (!CanAcceptWorldInput())
                return;
            ChannelEra next = State.era switch
            {
                ChannelEra.Year1961 => ChannelEra.Year1981,
                ChannelEra.Year1981 => ChannelEra.Year2001,
                ChannelEra.Year2001 => ChannelEra.Year2021,
                _ => ChannelEra.Year1961,
            };
            Tune(next);
        }

        public void CycleOperationForward()
        {
            SetStatus("제1막의 시간 조작은 연도 다이얼 하나만 사용합니다.");
        }

        private void ShiftEra(int direction)
        {
            ChannelEra target = State.era;
            if (direction < 0)
            {
                target = State.era switch
                {
                    ChannelEra.Year2021 => ChannelEra.Year2001,
                    ChannelEra.Year2001 => ChannelEra.Year1981,
                    ChannelEra.Year1981 => ChannelEra.Year1961,
                    _ => ChannelEra.Year1961,
                };
            }
            else if (direction > 0)
            {
                target = State.era switch
                {
                    ChannelEra.Year1961 => ChannelEra.Year1981,
                    ChannelEra.Year1981 => ChannelEra.Year2001,
                    ChannelEra.Year2001 => ChannelEra.Year2021,
                    _ => ChannelEra.Year2021,
                };
            }

            if (target == State.era)
            {
                SetStatus(direction < 0 ? "가장 오래된 시점입니다." : "가장 먼 미래 시점입니다.");
                return;
            }

            Tune(target);
        }

        public void GoForward()
        {
            if (!CanAcceptWorldInput())
                return;
            if (State.roomId == ChannelZeroIds.EntryRoom)
            {
                EnterLivingRoom();
                return;
            }

            if (State.roomId == ChannelZeroIds.LivingRoom)
            {
                EnterWorkshop();
                return;
            }

            SetStatus("현재 연결된 다음 방이 없습니다.");
        }

        public void ClearSaveAndRestart()
        {
            narrativePresenter?.Skip();
            saveService.Clear();
            State = ChannelZeroSessionState.CreateNew();
            ConfigureGameServices();
            Refresh("저장을 지우고 2001년 현관으로 돌아왔습니다.");
        }

        public void GoBack()
        {
            if (!CanAcceptWorldInput())
                return;
            if (!navigationService.TryBack())
            {
                SetStatus("더 이상 돌아갈 방이 없습니다.");
                return;
            }

            onRoomChanged?.Invoke(State.roomId);
            Refresh($"{(int)State.era}년 {State.roomId}로 돌아왔습니다.");
        }

        private void Tune(ChannelEra era)
        {
            if (State.roomId == ChannelZeroIds.EntryRoom && era != ChannelEra.Year2001)
            {
                SetStatus("프롤로그 현관은 2001년에 고정되어 있습니다.");
                return;
            }

            if (State.roomId == ChannelZeroIds.LivingRoom && !yearDial.TryTune(era))
            {
                SetStatus("첫 귀환을 마치기 전에는 연도 다이얼을 수동으로 돌릴 수 없습니다.");
                return;
            }
            if (State.roomId != ChannelZeroIds.LivingRoom)
                State.Tune(era);
            causalEvents?.OnYearEntered(era);
            saveService.Save(State);
            presenter.Present(State);
            onEraChanged?.Invoke(((int)era).ToString());
            NotifyNewlyActivePuzzles();
            UpdateReadouts();
            SetStatus($"{(int)era}년 / {State.roomId}");
        }

        private void SelectOperation(ChannelOperation operation)
        {
            string message = operationService.Execute(operation);
            presenter.Present(State);
            hudController?.RefreshInventory();
            saveService.Save(State);
            onOperationChanged?.Invoke(operation.ToString());
            UpdateReadouts();
            SetStatus(message);
        }

        private void HandleHotspot(string logicalId)
        {
            if (chapterSequenceInProgress)
                return;
            if (narrativePresenter != null && narrativePresenter.IsPresenting)
            {
                narrativePresenter.Advance();
                return;
            }

            logicalId = ResolveChapterOneHotspotId(logicalId);
            if (!chapterOneDirector.CanUseWorldHotspot(logicalId))
            {
                SetStatus(chapterOneDirector.Phase == Chapter1SequencePhase.BaselinePhoto
                    ? "먼저 가족사진을 확인해야 한다."
                    : "지금은 TV에서 일어나는 일을 끝까지 확인해야 한다.");
                return;
            }
            if (logicalId == "Living_Child" &&
                State.GetFlag(Chapter1Flags.SofaChildAppeared) &&
                !State.GetFlag(Chapter1Flags.ChildSofaIntroSeen))
            {
                PlaySofaChildEncounter();
                return;
            }
            if (logicalId == ChannelZeroIds.LivingWorkshopDoor && State.HasItem(ChannelZeroPuzzleIds.WorkshopKey))
            {
                PlayNarrative(ChannelZeroIds.LivingWorkshopDoor, "interact", EnterWorkshop);
                return;
            }
            logicalId = ResolveProgressiveHotspotId(logicalId);
            InteractionRouteDefinition route = gameContext.Interactions.Resolve(logicalId, State.operation);
            string scenarioInteractionId = string.IsNullOrWhiteSpace(route.unlockId)
                ? logicalId
                : route.unlockId;
            ScenarioAccessResult access = scenarioProgressionService.Evaluate(scenarioInteractionId);
            if (!access.IsUnlocked)
            {
                string lockedMessage = scenarioInteractionId == ChannelZeroIds.LivingWorkshopDoor
                    ? progressionService.DescribeChapterOneBlockers()
                    : access.LockedMessage;
                PlayNarrative(string.IsNullOrWhiteSpace(route.narrativeTarget)
                        ? logicalId
                        : route.narrativeTarget,
                    "interact",
                    () => SetStatus(lockedMessage));
                return;
            }

            State.Observe(logicalId);
            onHotspotActivated?.Invoke(logicalId);

            if (!interactionDispatcher.Dispatch(route))
                SetStatus(ChannelZeroPuzzleService.NoActionMessage);
        }

        private static string ResolveChapterOneHotspotId(string logicalId) => logicalId switch
        {
            "Living_Mina" => "Living_Child",
            "Living_Lockbox" => "Living_Vase",
            _ => logicalId,
        };

        private string ResolveProgressiveHotspotId(string logicalId)
        {
            if (logicalId == "Living_CRT" && State.GetFlag(Chapter1Flags.TVRearInspected) &&
                !State.GetFlag(Chapter1Flags.TVPhysicalRepairDone))
                return "Living_CRTRear";
            if (logicalId == "Living_Toolbox" && State.GetFlag(Chapter1Flags.ScrewdriverOwned))
                return "Living_TubeCase";
            return logicalId;
        }

        private void ExecuteOpenCloseup(InteractionRouteDefinition route)
        {
            if (State.operation == ChannelOperation.Load && comparisonService != null &&
                comparisonService.IsOpen && route.narrativeTarget == "Living_REC")
            {
                OpenComparisonOverlay(route.closeupId);
                return;
            }
            if (TryHandleNonActivePuzzle(route.closeupId, route.narrativeTarget,
                route.closeupNarrativeTarget))
                return;

            string closeupTarget = string.IsNullOrWhiteSpace(route.closeupNarrativeTarget)
                ? route.narrativeTarget
                : route.closeupNarrativeTarget;
            Action open = () => OpenCloseup(route.closeupId, closeupTarget, true);
            if (route.playNarrativeBeforeAction)
                PlayNarrative(route.narrativeTarget, route.narrativeTrigger, open);
            else
                open();
        }

        private void ExecuteNarrative(InteractionRouteDefinition route) =>
            PlayNarrative(route.narrativeTarget, route.narrativeTrigger);

        private void ExecuteEnterLiving(InteractionRouteDefinition route)
        {
            if (route.playNarrativeBeforeAction)
                PlayNarrative(route.narrativeTarget, route.narrativeTrigger, EnterLivingRoom);
            else
                EnterLivingRoom();
        }

        private void ExecuteEnterWorkshop(InteractionRouteDefinition route)
        {
            if (route.playNarrativeBeforeAction)
                PlayNarrative(route.narrativeTarget, route.narrativeTrigger, EnterWorkshop);
            else
                EnterWorkshop();
        }

        private void ExecuteNoAction(InteractionRouteDefinition route)
        {
            string message = string.IsNullOrWhiteSpace(route.statusMessage)
                ? ChannelZeroPuzzleService.NoActionMessage
                : route.statusMessage;
            PlayNarrative(route.narrativeTarget, route.narrativeTrigger, () => SetStatus(message));
        }

        private void OpenCloseup(string closeupId, string narrativeTarget = null,
            bool accessAlreadyEvaluated = false)
        {
            if (!accessAlreadyEvaluated &&
                TryHandleNonActivePuzzle(closeupId, narrativeTarget, narrativeTarget))
                return;

            State.MarkRecordRead(closeupId);
            activeNarrativeTarget = string.IsNullOrWhiteSpace(narrativeTarget)
                ? gameContext.InteractionCatalog.NarrativeTargetForCloseup(closeupId)
                : narrativeTarget;
            ChannelZeroLocalizedText exactText = ResolveCloseupTextReference(activeNarrativeTarget);
            puzzleView?.OpenLocalized(State, closeupId, ChannelZeroIds.DefaultVisualState, exactText);
            PresentActivePuzzle();
            saveService.Save(State);
            SetStatus($"클로즈업: {closeupId}");
        }

        private void EnterLivingRoom()
        {
            if (!navigationService.TryEnterLiving())
            {
                SetStatus("우편함의 서비스 요청서를 먼저 확인해야 한다.");
                return;
            }
            chapterOneDirector.MarkServiceRequestSeen();
            onRoomChanged?.Invoke(State.roomId);
            Refresh("2001년 거실에 들어왔습니다. 먼저 가족사진을 확인하세요.");
            QueueHotspotHighlightOnce("Living_Photos", "tutorial:baseline_photo");
        }

        private void EnterWorkshop()
        {
            if (State.roomId == ChannelZeroIds.LivingRoom)
            {
                if (!State.HasItem(ChannelZeroPuzzleIds.WorkshopKey))
                {
                    SetStatus("작업실 문에 맞는 열쇠가 필요하다.");
                    return;
                }
                State.SetFlag(Chapter1Flags.WorkshopKeyUsed);
                chapterOneDirector.RefreshCompletion();
            }
            if (!navigationService.TryEnterWorkshop())
            {
                PlayNarrative(ChannelZeroIds.LivingWorkshopDoor, "interact");
                return;
            }
            onRoomChanged?.Invoke(State.roomId);
            Refresh($"{(int)State.era}년 작업실에 들어왔습니다.");
            State.SetFlag(Chapter1Flags.EndingRecognitionActive);
            activeNarrativeTarget = "Chapter1Ending";
            puzzleView?.OpenLocalized(State, ChannelZeroIds.LivingCrtFrontCloseup,
                ChannelZeroIds.DefaultVisualState,
                new ChannelZeroLocalizedText("CH1.ENDING.PLAYER.DIALOGUE.01", "처음 보는 것 같은데… 이름이 뭐니?"));
            PlayChapterOneEnding();
        }

        private void PlayChapterOneEnding()
        {
            PlayNarrativeById("CH1.ENDING.PLAYER.DIALOGUE.01", () =>
                PlayNarrativeById("CH1.ENDING.CHILD.DIALOGUE.02", () =>
                    PlayNarrativeById("CH1.ENDING.PLAYER.DIALOGUE.03", () =>
                        PlayNarrativeById("CH1.ENDING.CHILD.DIALOGUE.04", () =>
                            SetStatus("제1장 완료 — 아이는 나를 이미 알고 있었다.")))));
        }

        private void HandleCloseupClosed()
        {
            comparisonService?.Close();
            presenter.Present(State);
            if (!string.IsNullOrWhiteSpace(pendingHighlightHotspotId))
            {
                HighlightHotspotOnce(pendingHighlightHotspotId, pendingHighlightMarker);
                pendingHighlightHotspotId = string.Empty;
                pendingHighlightMarker = string.Empty;
            }
            saveService.Save(State);
            string target = activeNarrativeTarget;
            if (target == "Chapter1Ending")
                State.SetFlag(Chapter1Flags.EndingRecognitionActive, false);
            if (target == "Living_Photos" && !State.GetFlag(Chapter1Flags.Photo2001BaselineSeen))
            {
                chapterOneDirector.MarkBaselinePhotoSeen();
                QueueHotspotHighlightOnce("Living_CRT", "tutorial:first_tv");
            }
            else if (target == "Living_CRT" && !State.GetFlag(Chapter1Flags.ChildSofaIntroSeen))
            {
                if (chapterOneDirector.BeginSofaChildEncounter())
                {
                    chapterSequenceInProgress = true;
                    Action revealChild = () =>
                    {
                        chapterSequenceInProgress = false;
                        if (!chapterOneDirector.RevealSofaChildAfterGlitch()) return;
                        chapterOneFrameSequence?.ShowSofaChild();
                        presenter.Present(State);
                        saveService.Save(State);
                        SetStatus("비어 있던 소파에 아이가 앉아 있다. 지금은 아이만 조사할 수 있다.");
                        QueueHotspotHighlightOnce("Living_Child", "tutorial:sofa_child");
                    };
                    if (chapterOneFrameSequence != null)
                        chapterOneFrameSequence.PlayFirstTvRoomGlitch(revealChild);
                    else
                        revealChild();
                }
            }
            string completedChapter = pendingChapterCompletion;
            pendingChapterCompletion = string.Empty;
            activeNarrativeTarget = string.Empty;
            PlayNarrative(target, "close_closeup",
                () =>
                {
                    SetStatus($"{(int)State.era}년 / {State.roomId}로 복귀");
                    if (!string.IsNullOrWhiteSpace(completedChapter))
                    {
                        pendingGuidanceTextId = string.Empty;
                        PresentChapterCompletion(completedChapter);
                        return;
                    }
                    if (!string.IsNullOrWhiteSpace(pendingGuidanceTextId))
                    {
                        PresentGuidanceNonBlocking(pendingGuidanceTextId);
                        pendingGuidanceTextId = string.Empty;
                    }
                });
        }

        private void PresentActivePuzzle()
        {
            if (puzzleView == null || puzzleService == null || State == null)
                return;
            if (puzzleService.TryOpen(State.activeCloseupId, out PuzzleView view))
                puzzleView.PresentPuzzle(view);
        }

        private void HandlePuzzleAction(string actionId)
        {
            if (State == null || puzzleService == null || string.IsNullOrWhiteSpace(State.activeCloseupId))
                return;
            if (!puzzleService.TryExecute(State.activeCloseupId, actionId, out PuzzleActionResult result))
                return;

            ApplyPuzzleResult(result);
        }

        private void ApplyPuzzleResult(PuzzleActionResult result)
        {
            if (result == null)
                return;

            if (SynchronizeChapterOneState())
                return;

            foreach (string chapterId in result.completedChapterIds)
                QueueChapterCompletion(chapterId);
            if (!string.IsNullOrWhiteSpace(result.requestedCloseupId))
            {
                OpenCloseup(result.requestedCloseupId,
                    gameContext.InteractionCatalog.NarrativeTargetForCloseup(result.requestedCloseupId));
                return;
            }
            puzzleView.PresentPuzzle(result.view);
            hudController?.RefreshInventory();
            if (!string.IsNullOrWhiteSpace(result.feedbackText))
                puzzleView.ShowFeedback(result.feedbackText);
            saveService.Save(State);
            UpdateReadouts();
            NotifyNewlyActivePuzzles();

            if (!string.IsNullOrWhiteSpace(result.narrativeTrigger))
            {
                puzzleView.SetInputLocked(true);
                PlayNarrative(activeNarrativeTarget, result.narrativeTrigger,
                    () =>
                    {
                        puzzleView.SetInputLocked(false);
                        PresentActivePuzzle();
                        TryPlayProgressGuidance();
                    });
            }
            else
                TryPlayProgressGuidance();

            if (State.GetFlag(Chapter1Flags.TVRearInspected) && !State.HasItem(ChannelZeroPuzzleIds.Screwdriver))
                QueueHotspotHighlightOnce("Living_Toolbox", "tutorial:toolbox_screwdriver");
            if (State.GetFlag(Chapter1Flags.ScrewdriverOwned) && !State.GetFlag(Chapter1Flags.ReplacementTubeOwned))
                QueueHotspotHighlightOnce("Living_Toolbox", "transition:toolbox_tube_case");

            if (State.GetFlag("Chapter2Complete"))
                SetStatus("제2장 완료 — 도면과 크랭크, 네 시대 수리 기록을 확보했습니다.");
            else if (State.GetFlag("Chapter1Complete"))
                SetStatus("제1장 완료 — 작업실 문 잠금이 풀렸습니다.");
        }

        private bool SynchronizeChapterOneState()
        {
            if (State.GetFlag("CrtRearInspected")) State.SetFlag(Chapter1Flags.TVRearInspected);
            if (State.HasItem(ChannelZeroPuzzleIds.Screwdriver)) State.SetFlag(Chapter1Flags.ScrewdriverOwned);
            if (State.HasItem(ChannelZeroPuzzleIds.ReplacementTube)) State.SetFlag(Chapter1Flags.ReplacementTubeOwned);
            if ((State.GetFlag("CrtRepaired") || State.GetFlag(Chapter1Flags.TVPhysicalRepairDone)) &&
                !State.GetFlag(Chapter1Flags.FirstSlip1961Done) && !chapterSequenceInProgress)
            {
                chapterSequenceInProgress = true;
                puzzleView?.DismissForSequence();
                Action completeSlip = () =>
                {
                    chapterOneDirector.CompletePhysicalRepair();
                    chapterSequenceInProgress = false;
                    presenter.Present(State);
                    saveService.Save(State);
                    onEraChanged?.Invoke("1961");
                    SetStatus("다이얼이 1961에 고정됐다. 귀환 회로를 복구해야 한다.");
                };
                if (chapterOneFrameSequence != null)
                    chapterOneFrameSequence.PlayEraTransition(completeSlip);
                else
                    completeSlip();
                return true;
            }
            if (State.GetFlag(Chapter1Flags.ReturnWiringSolved) &&
                State.GetFlag(Chapter1Flags.ReturnTubeSequenceSolved) &&
                !State.GetFlag(Chapter1Flags.ReturnCircuitSolved) && !chapterSequenceInProgress)
            {
                chapterSequenceInProgress = true;
                puzzleView?.DismissForSequence();
                Action completeReturn = () =>
                {
                    chapterOneDirector.CompleteReturnCircuit();
                    chapterSequenceInProgress = false;
                    presenter.Present(State);
                    saveService.Save(State);
                    onEraChanged?.Invoke("2001");
                    SetStatus("2001년으로 돌아왔다. 사진에 아이가 새로 생겼고 연도 다이얼이 열렸다.");
                };
                if (chapterOneFrameSequence != null)
                    chapterOneFrameSequence.PlayEraTransitionReverse(completeReturn);
                else
                    completeReturn();
                return true;
            }
            chapterOneDirector.RefreshCompletion();
            return false;
        }

        private void HandleInventorySelection(string itemId)
        {
            if (State == null || State.closeupInputLocked ||
                (narrativePresenter != null && narrativePresenter.IsPresenting))
                return;

            if (!inventoryService.Select(itemId))
                return;

            if (puzzleView != null && puzzleView.IsOpen
                && puzzleService.TryUseItem(State.activeCloseupId, itemId, out PuzzleActionResult result))
            {
                ApplyPuzzleResult(result);
                return;
            }

            hudController?.RefreshInventory();
            puzzleView?.RefreshInventory();
            string message = $"선택: {inventoryService.DisplayName(itemId)} — {inventoryService.Description(itemId)}";
            if (puzzleView != null && puzzleView.IsOpen)
                puzzleView.ShowFeedback(message);
            else
                SetStatus(message);
            saveService.Save(State);
        }

        private void ConfigureGameServices()
        {
            gameContext = ChannelZeroGameContext.Create(State, saveService, locale);
            inventoryService = gameContext.Inventory;
            navigationService = gameContext.Navigation;
            progressionService = gameContext.Progression;
            scenarioProgressionService = gameContext.Scenario;
            guidanceService = gameContext.Guidance;
            puzzleService = gameContext.Puzzles;
            operationService = gameContext.Operations;
            comparisonService = gameContext.Comparison;
            narrativeResolver = gameContext.Narrative;
            chapterOneDirector = new Chapter1SequenceDirector(State);
            yearDial = new YearDialController(State);
            causalEvents = new CausalEventService(State);
            chapterOneFrameSequence ??= GetComponent<Chapter1GlitchFrameSequencePlayer>();
            if (chapterOneFrameSequence == null)
                chapterOneFrameSequence = gameObject.AddComponent<Chapter1GlitchFrameSequencePlayer>();
            interactionDispatcher ??= CreateInteractionDispatcher();
            if (puzzleView != null)
            {
                puzzleView.ConfigureInventory(inventoryService.Description);
                puzzleView.ConfigureLocalization(narrativeResolver.Locale,
                    narrativeResolver.ResolveLocalizedText);
            }
            hudController ??= FindFirstObjectByType<ChannelZeroHudController>(FindObjectsInactive.Include);
            hudController?.BindInventory(State, inventoryService.DisplayName, HandleInventorySelection);
        }

        private void PlaySofaChildEncounter()
        {
            PlayNarrativeById("CH1.LIVING.CHILD.DIALOGUE.INTRO_01", () =>
                PlayNarrativeById("CH1.LIVING.PLAYER.DIALOGUE.INTRO_02", () =>
                    PlayNarrativeById("CH1.LIVING.CHILD.DIALOGUE.INTRO_03", () =>
                        chapterOneFrameSequence.PlaySofaApproachAndDisappear(() =>
                        {
                            chapterOneDirector.CompleteSofaChildEncounter();
                            presenter.Present(State);
                            saveService.Save(State);
                            QueueHotspotHighlightOnce("Living_CRT", "tutorial:tv_rear_after_child");
                            SetStatus("아이의 접근 동작이 끊긴 뒤 빈 소파만 남았다. TV 후면을 확인하세요.");
                        }))));
        }

        private void DisableLegacyTimeUi()
        {
            foreach (string objectName in new[]
            {
                "Operation_REC", "Operation_PLAY", "Operation_LOAD", "Operation_REW",
                "Operation_HOLD", "RecButton", "PlayButton", "LoadButton", "RewButton",
                "HoldButton", "RecordSlots", "TapeSlots", "MasterSlot",
            })
            {
                GameObject legacy = GameObject.Find(objectName);
                if (legacy != null) legacy.SetActive(false);
            }
            Transform chrome = GameObject.Find("CrtHud")?.transform;
            if (chrome != null && chrome.Find("RetiredControlsMask") == null)
            {
                GameObject mask = new("RetiredControlsMask", typeof(RectTransform),
                    typeof(CanvasRenderer), typeof(Image));
                mask.transform.SetParent(chrome, false);
                Image image = mask.GetComponent<Image>();
                image.color = new Color(0.015f, 0.012f, 0.01f, 0.98f);
                image.raycastTarget = false;
                RectTransform rect = image.rectTransform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.zero;
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector2(612f, 100f);
                rect.sizeDelta = new Vector2(1080f, 84f);
                mask.transform.SetSiblingIndex(1);
            }
            if (operationReadout != null)
            {
                operationReadout.text = "YEAR DIAL";
                operationReadout.gameObject.SetActive(false);
            }
            State.operation = ChannelOperation.None;
        }

        private ChannelZeroInteractionDispatcher CreateInteractionDispatcher()
        {
            ChannelZeroInteractionDispatcher dispatcher = new();
            dispatcher.Register(InteractionActionType.OpenCloseup, ExecuteOpenCloseup);
            dispatcher.Register(InteractionActionType.PlayNarrative, ExecuteNarrative);
            dispatcher.Register(InteractionActionType.EnterLivingRoom, ExecuteEnterLiving);
            dispatcher.Register(InteractionActionType.EnterWorkshop, ExecuteEnterWorkshop);
            dispatcher.Register(InteractionActionType.GoBack, _ => GoBack());
            dispatcher.Register(InteractionActionType.NoAction, ExecuteNoAction);
            return dispatcher;
        }

        private void OpenComparisonOverlay(string closeupId)
        {
            ChannelZeroComparisonOverlay overlay = comparisonService.Current;
            if (overlay == null || puzzleView == null)
                return;
            activeNarrativeTarget = "Living_REC";
            puzzleView.Open(State, closeupId, ChannelZeroIds.DefaultVisualState,
                "LOAD 비교 화면 — 기록은 현재 세계에 영향을 주지 않는다.");
            puzzleView.PresentPuzzle(new PuzzleView
            {
                puzzleId = "system.load_comparison",
                closeupId = closeupId,
                title = $"LOAD {(int)overlay.Record.era} → {(int)overlay.CurrentEra}",
                body = overlay.Summary,
                completed = true,
            });
            saveService.Save(State);
            SetStatus("LOAD 비교 중 — BACK 또는 닫기로 돌아갈 수 있습니다.");
        }

        private bool PlayNarrative(string target, string trigger, Action completed = null)
        {
            if (narrativeResolver == null || string.IsNullOrWhiteSpace(target))
            {
                completed?.Invoke();
                return false;
            }

            TMP_Text output = puzzleView != null && puzzleView.IsOpen
                ? puzzleView.NarrativeLabel
                : statusLabel;
            narrativePresenter.Configure(output);
            NarrativeTextContext context = BuildNarrativeContext();
            var entries = narrativeResolver.Resolve(NarrativeRoomId(), target, State.era, trigger, context);
            bool started = narrativePresenter.Begin(entries, State, () =>
            {
                saveService.Save(State);
                completed?.Invoke();
            });
            if (started)
                saveService.Save(State);
            return started;
        }

        private bool PlayNarrativeById(string textId, Action completed = null)
        {
            if (narrativeResolver == null || string.IsNullOrWhiteSpace(textId))
            {
                completed?.Invoke();
                return false;
            }

            TMP_Text output = puzzleView != null && puzzleView.IsOpen
                ? puzzleView.NarrativeLabel
                : statusLabel;
            narrativePresenter.Configure(output);
            var entries = narrativeResolver.ResolveById(textId, BuildNarrativeContext());
            bool started = narrativePresenter.Begin(entries, State, () =>
            {
                saveService.Save(State);
                completed?.Invoke();
            });
            if (started)
                saveService.Save(State);
            return started;
        }

        private bool TryHandleNonActivePuzzle(string closeupId, string narrativeTarget,
            string closeupNarrativeTarget)
        {
            if (puzzleService == null || string.IsNullOrWhiteSpace(closeupId))
                return false;

            PuzzleInteractionAccess access = puzzleService.EvaluateAccess(closeupId);
            bool textOnlyEvidence = access.Definition?.ruleType == "era_evidence" &&
                !access.ReviewUiEnabled &&
                (access.Mode == PuzzleInteractionMode.PassiveObservation ||
                 access.Mode == PuzzleInteractionMode.ResultOnly);
            if (textOnlyEvidence)
            {
                if (access.Mode == PuzzleInteractionMode.PassiveObservation)
                {
                    puzzleService.RecordPassiveObservation(closeupId);
                    progressionService.Refresh();
                    saveService.Save(State);
                }
                string target = string.IsNullOrWhiteSpace(closeupNarrativeTarget)
                    ? narrativeTarget
                    : closeupNarrativeTarget;
                string trigger = target == "Workshop_Records" ? "open_page" : "open_closeup";
                if (!PlayNarrative(target, trigger))
                    PlayNarrativeById(access.NarrativeTextId,
                        () => SetStatus("시대별 상태를 조사 기록에 남겼다."));
                return true;
            }

            if (access.Mode == PuzzleInteractionMode.Missing || access.OpensPuzzleUi)
            {
                if (access.Mode == PuzzleInteractionMode.PassiveObservation &&
                    access.Definition?.ruleType == "era_evidence")
                {
                    puzzleService.RecordPassiveObservation(closeupId);
                    progressionService.Refresh();
                    saveService.Save(State);
                }
                return false;
            }

            if (access.Mode == PuzzleInteractionMode.PassiveObservation)
            {
                puzzleService.RecordPassiveObservation(closeupId);
                progressionService.Refresh();
                saveService.Save(State);
                string target = string.IsNullOrWhiteSpace(closeupNarrativeTarget)
                    ? narrativeTarget
                    : closeupNarrativeTarget;
                string trigger = target == "Workshop_Records" ? "open_page" : "inspect";
                if (!PlayNarrative(target, trigger))
                    PlayNarrativeById(access.NarrativeTextId,
                        () => SetStatus("시대별 상태를 조사 기록에 남겼다."));
                return true;
            }

            if (!PlayNarrativeById(access.NarrativeTextId,
                () => SetStatus("이 시기에는 상태와 흔적만 확인할 수 있다.")))
                SetStatus("이 시기에는 상태와 흔적만 확인할 수 있다.");
            return true;
        }

        private void NotifyNewlyActivePuzzles()
        {
            if (puzzleService == null)
                return;
            foreach (PuzzleDefinition definition in puzzleService.NewlyActiveDefinitions())
            {
                if (!HotspotBelongsToCurrentRoom(definition.hotspotId))
                    continue;
                QueueHotspotHighlightOnce(definition.hotspotId,
                    $"puzzle_active:{definition.RuntimePuzzleId}:{definition.activeEra}");
            }
        }

        private void HighlightHotspotOnce(string hotspotId, string marker)
        {
            if (string.IsNullOrWhiteSpace(hotspotId) || State.HasSeenText(marker))
                return;
            State.MarkTextSeen(marker);
            int pulses = marker != null && marker.StartsWith("transition:", StringComparison.Ordinal) ? 2 : 1;
            presenter?.PulseHotspot(hotspotId, pulses);
            onPuzzleActivated?.Invoke(hotspotId);
        }

        private void TryPlayProgressGuidance()
        {
            if (guidanceService == null || !guidanceService.TryGetNext(out string textId))
                return;
            if (puzzleView != null && puzzleView.IsOpen)
            {
                pendingGuidanceTextId = textId;
                return;
            }
            PresentGuidanceNonBlocking(textId);
        }

        private void PresentGuidanceNonBlocking(string textId)
        {
            var entries = narrativeResolver.ResolveById(textId, BuildNarrativeContext());
            if (entries.Count == 0)
                return;
            NarrativeTextEntry entry = entries[0];
            if (entry.once)
                State.MarkTextSeen(entry.id);
            SetStatus(entry.text);
            saveService.Save(State);
        }

        private void QueueChapterCompletion(string chapterId)
        {
            pendingChapterCompletion = chapterId;
            State.SetVisualState(chapterId == "CH1" ? "chapter1_complete" : "chapter2_complete");
        }

        private void PresentChapterCompletion(string chapterId)
        {
            bool chapterOne = chapterId == "CH1";
            presenter?.FlashCompletion(chapterOne
                ? new Color(0.92f, 0.63f, 0.20f, 0f)
                : new Color(0.25f, 0.75f, 0.90f, 0f));
            presenter?.PulseHotspot(chapterOne ? ChannelZeroIds.LivingWorkshopDoor : "Workshop_LivingDoor", 2);
            PlayCompletionTone(chapterOne ? 523.25f : 659.25f);
            onChapterCompleted?.Invoke(chapterId);
            PlayNarrativeById(chapterOne ? "SYS.CH1.COMPLETE" : "SYS.CH2.COMPLETE");
        }

        private void PlayCompletionTone(float baseFrequency)
        {
            completionAudioSource ??= gameObject.AddComponent<AudioSource>();
            completionAudioSource.playOnAwake = false;
            completionAudioSource.spatialBlend = 0f;
            const int sampleRate = 22050;
            const float duration = 0.34f;
            int sampleCount = Mathf.CeilToInt(sampleRate * duration);
            float[] samples = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float time = i / (float)sampleRate;
                float envelope = Mathf.Sin(Mathf.PI * i / Mathf.Max(1, sampleCount - 1));
                float frequency = i < sampleCount / 2 ? baseFrequency : baseFrequency * 1.25f;
                samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * time) * envelope * 0.12f;
            }
            AudioClip clip = AudioClip.Create("ChapterCompletionTone", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            completionAudioSource.PlayOneShot(clip);
            Destroy(clip, duration + 0.1f);
        }

        private void QueueHotspotHighlightOnce(string hotspotId, string marker)
        {
            if (State.HasSeenText(marker))
                return;
            if (puzzleView != null && puzzleView.IsOpen)
            {
                pendingHighlightHotspotId = hotspotId;
                pendingHighlightMarker = marker;
                return;
            }
            HighlightHotspotOnce(hotspotId, marker);
        }

        private bool HotspotBelongsToCurrentRoom(string hotspotId)
        {
            if (State.roomId == ChannelZeroIds.LivingRoom)
                return hotspotId.StartsWith("Living_", StringComparison.Ordinal);
            if (State.roomId == ChannelZeroIds.WorkshopRoom)
                return hotspotId.StartsWith("Workshop_", StringComparison.Ordinal);
            return false;
        }

        private ChannelZeroLocalizedText ResolveCloseupTextReference(string target)
        {
            if (narrativeResolver == null || string.IsNullOrWhiteSpace(target))
                return ChannelZeroLocalizedText.Literal(string.Empty);

            string trigger = target == "Workshop_Records" ? "open_page" : "open_closeup";
            var entries = narrativeResolver.Resolve(NarrativeRoomId(), target, State.era,
                trigger, BuildNarrativeContext());
            foreach (NarrativeTextEntry entry in entries)
            {
                if (entry.once)
                    State.MarkTextSeen(entry.id);
                if (string.Equals(entry.type, "document", StringComparison.OrdinalIgnoreCase))
                    State.MarkRecordRead(entry.id);
            }
            return entries.Count > 0
                ? new ChannelZeroLocalizedText(entries[0].id, entries[0].text)
                : ChannelZeroLocalizedText.Literal(string.Empty);
        }

        private string ResolveCloseupText(string target)
        {
            ChannelZeroLocalizedText localized = ResolveCloseupTextReference(target);
            return narrativeResolver?.ResolveLocalizedText(localized.Key, localized.Fallback) ?? localized.Fallback;
        }

        private NarrativeTextContext BuildNarrativeContext()
        {
            NarrativeTextContext context = new NarrativeTextContext(State)
                .SetFlag("EnteredHouse", State.visitedRoomIds.Contains(ChannelZeroIds.LivingRoom))
                .SetFlag("FrontDoorLocked", State.visitedRoomIds.Contains(ChannelZeroIds.LivingRoom))
                .SetFlag("ReadServiceRequest", State.recordIds.Contains("PRO.ENTRY.MAIL.DOC.REQUEST") ||
                    State.recordIds.Contains(ChannelZeroIds.PrologueServiceRequestCloseup))
                .SetFlag("CrtPowered", false)
                .SetFlag("CrtNoiseSeen", State.observedHotspotIds.Contains("Living_CRT"))
                .SetFlag("MinaFirstSeen", State.HasSeenText("CH1.LIVING.CRT.DIALOGUE.MINA_2001"));
            return context;
        }

        private string NarrativeRoomId() => State.roomId == ChannelZeroIds.EntryRoom ? "EntryHall" : State.roomId;

        private bool CanAcceptWorldInput()
        {
            bool narrativeOpen = narrativePresenter != null && narrativePresenter.IsPresenting;
            bool closeupOpen = puzzleView != null && puzzleView.IsOpen;
            return !chapterSequenceInProgress && !narrativeOpen && !closeupOpen;
        }

        private void Refresh(string message)
        {
            if (!presenter.Present(State))
                message = $"배경 누락: {State.roomId}/{(int)State.era}/{State.roomVisualStateId}";
            saveService.Save(State);
            UpdateBackButton();
            UpdateReadouts();
            SetStatus(message);
        }

        private void UpdateReadouts()
        {
            if (State == null)
                return;

            if (channelReadout != null)
                channelReadout.text = $"CH {(int)State.era}";
            if (operationReadout != null)
                operationReadout.text = "YEAR DIAL";
        }

        private void UpdateBackButton()
        {
            if (backButton != null)
                backButton.interactable = State != null && State.CanGoBack;
        }

        private void SetStatus(string message)
        {
            if (statusLabel != null)
                statusLabel.text = message;
            Debug.Log($"CHANNEL_ZERO: {message}");
        }

#if UNITY_EDITOR
        public void EditorConfigure(ChannelZeroRoomPresenter roomPresenter,
            ChannelZeroCloseupCanvasController closeups, ChannelZeroHudController hud,
            TMP_Text label, Button back,
            TMP_Text channel, TMP_Text operation, bool restore)
        {
            presenter = roomPresenter;
            closeupCanvas = closeups;
            hudController = hud;
            statusLabel = label;
            backButton = back;
            channelReadout = channel;
            operationReadout = operation;
            restoreSaveOnStart = restore;
        }
#endif
    }
}
