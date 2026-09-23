using System;
using ChannelZero.Runtime.Core;
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
        [SerializeField] private Text statusLabel;
        [SerializeField] private Button backButton;
        [SerializeField] private Text channelReadout;
        [SerializeField] private Text operationReadout;
        [SerializeField] private bool restoreSaveOnStart;
        [Header("Audio / direction extension points")]
        [SerializeField] private StringEvent onRoomChanged;
        [SerializeField] private StringEvent onEraChanged;
        [SerializeField] private StringEvent onHotspotActivated;
        [SerializeField] private StringEvent onOperationChanged;

        private ChannelZeroSaveService saveService;
        private NarrativeTextResolver narrativeResolver;
        private NarrativePresenter narrativePresenter;
        private string activeNarrativeTarget = string.Empty;

        public ChannelZeroSessionState State { get; private set; }

        private void Awake()
        {
            saveService = new ChannelZeroSaveService(new PlayerPrefsChannelZeroSaveStore());
            State = restoreSaveOnStart && saveService.TryLoad(out ChannelZeroSessionState loaded)
                ? loaded
                : ChannelZeroSessionState.CreateNew();

            narrativePresenter = GetComponent<NarrativePresenter>();
            if (narrativePresenter == null)
                narrativePresenter = gameObject.AddComponent<NarrativePresenter>();
            narrativePresenter.Configure(statusLabel);

            TextAsset narrativeJson = Resources.Load<TextAsset>("ChannelZero/Data/narrative_ko-KR.v1");
            if (narrativeJson != null)
            {
                try
                {
                    narrativeResolver = new NarrativeTextResolver(NarrativeTextCatalog.FromJson(narrativeJson.text));
                }
                catch (Exception exception)
                {
                    Debug.LogError($"CHANNEL_ZERO narrative load failed: {exception.Message}");
                }
            }
            else
            {
                Debug.LogError("CHANNEL_ZERO narrative resource is missing.");
            }
        }

        private void OnEnable()
        {
            if (presenter != null)
                presenter.HotspotClicked += HandleHotspot;
            if (closeupCanvas != null)
                closeupCanvas.Closed += HandleCloseupClosed;
        }

        private void Start()
        {
            Refresh("2001년 현관. 거실문을 선택하세요.");
            if (!string.IsNullOrWhiteSpace(State.activeCloseupId) && closeupCanvas != null)
            {
                activeNarrativeTarget = NarrativeTargetForCloseup(State.activeCloseupId);
                closeupCanvas.Restore(State, ResolveCloseupText(activeNarrativeTarget));
            }
        }

        private void OnDisable()
        {
            if (presenter != null)
                presenter.HotspotClicked -= HandleHotspot;
            if (closeupCanvas != null)
                closeupCanvas.Closed -= HandleCloseupClosed;
        }

        public void Tune1961() { if (CanAcceptWorldInput()) Tune(ChannelEra.Year1961); }
        public void Tune1981() { if (CanAcceptWorldInput()) Tune(ChannelEra.Year1981); }
        public void Tune2001() { if (CanAcceptWorldInput()) Tune(ChannelEra.Year2001); }
        public void Tune2021() { if (CanAcceptWorldInput()) Tune(ChannelEra.Year2021); }
        public void TunePast() { if (CanAcceptWorldInput()) ShiftEra(-1); }
        public void TuneFuture() { if (CanAcceptWorldInput()) ShiftEra(1); }

        public void SelectRec() { if (CanAcceptWorldInput()) SelectOperation(ChannelOperation.Rec); }
        public void SelectPlay() { if (CanAcceptWorldInput()) SelectOperation(ChannelOperation.Play); }
        public void SelectLoad() { if (CanAcceptWorldInput()) SelectOperation(ChannelOperation.Load); }
        public void SelectRew() { if (CanAcceptWorldInput()) SelectOperation(ChannelOperation.Rew); }
        public void SelectHold() { if (CanAcceptWorldInput()) SelectOperation(ChannelOperation.Hold); }

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
            if (!CanAcceptWorldInput())
                return;
            ChannelOperation next = State.operation switch
            {
                ChannelOperation.Rec => ChannelOperation.Play,
                ChannelOperation.Play => ChannelOperation.Load,
                ChannelOperation.Load => ChannelOperation.Rew,
                ChannelOperation.Rew => ChannelOperation.Hold,
                _ => ChannelOperation.Rec,
            };
            SelectOperation(next);
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
            Refresh("저장을 지우고 2001년 현관으로 돌아왔습니다.");
        }

        public void GoBack()
        {
            if (!CanAcceptWorldInput())
                return;
            if (!State.TryGoBack())
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

            State.Tune(era);
            saveService.Save(State);
            presenter.Present(State);
            onEraChanged?.Invoke(((int)era).ToString());
            UpdateReadouts();
            SetStatus($"{(int)era}년 / {State.roomId}");
        }

        private void SelectOperation(ChannelOperation operation)
        {
            State.operation = operation;
            saveService.Save(State);
            onOperationChanged?.Invoke(operation.ToString());
            UpdateReadouts();
            SetStatus($"{operation.ToString().ToUpperInvariant()} 선택 — 기능 연결 지점 준비됨");
        }

        private void HandleHotspot(string logicalId)
        {
            if (narrativePresenter != null && narrativePresenter.IsPresenting)
            {
                narrativePresenter.Advance();
                return;
            }

            State.Observe(logicalId);
            onHotspotActivated?.Invoke(logicalId);

            switch (logicalId)
            {
                case ChannelZeroIds.EntryMail:
                    PlayNarrative(ChannelZeroIds.EntryMail, "inspect",
                        () => OpenCloseup(ChannelZeroIds.PrologueServiceRequestCloseup, ChannelZeroIds.EntryMail));
                    break;
                case "Entry_ExteriorDoor":
                    PlayNarrative("Entry_ExteriorDoor", "interact");
                    break;
                case ChannelZeroIds.EntryLivingDoor:
                    PlayNarrative(ChannelZeroIds.EntryLivingDoor, "inspect", EnterLivingRoom);
                    break;
                case ChannelZeroIds.LivingWorkshopDoor:
                    PlayNarrative(ChannelZeroIds.LivingWorkshopDoor, "interact", EnterWorkshop);
                    break;
                case "Workshop_LivingDoor":
                    GoBack();
                    break;
                case "Living_CRT":
                    if (State.operation == ChannelOperation.Hold)
                        OpenCloseup(ChannelZeroIds.LivingCrtRearCloseup, "Living_CRTRear");
                    else
                        PlayNarrative("Living_CRT", "inspect",
                            () => OpenCloseup(ChannelZeroIds.LivingCrtFrontCloseup, "Living_CRT"));
                    break;
                case "Living_Toolbox":
                case "Living_TubeCase":
                    PlayNarrative("Living_TubeCase", "inspect",
                        () => OpenCloseup(ChannelZeroIds.LivingTubeStorageCloseup, "Living_TubeCase"));
                    break;
                case "Living_NumberRug":
                    OpenCloseup(ChannelZeroIds.LivingNumberRugCloseup);
                    break;
                case "Living_DisplayMedical":
                    OpenCloseup(ChannelZeroIds.LivingMedicalCabinetCloseup);
                    break;
                case "Living_Armchair":
                case "Living_JinwooHand":
                    OpenCloseup(ChannelZeroIds.LivingHandTreatmentCloseup, "Living_JinwooHand");
                    break;
                case "Living_CoffeeTable":
                    string tableTarget = State.operation is ChannelOperation.Rec or ChannelOperation.Load or ChannelOperation.Rew
                        ? "Living_REC"
                        : "Living_Lockbox";
                    OpenCloseup(State.operation switch
                    {
                        ChannelOperation.Rec => ChannelZeroIds.LivingRecPanelCloseup,
                        ChannelOperation.Load => ChannelZeroIds.LivingRecSlotsCloseup,
                        ChannelOperation.Rew => ChannelZeroIds.LivingWiringDiagramCloseup,
                        _ => ChannelZeroIds.LivingLockboxCloseup,
                    }, tableTarget);
                    break;
                case "Living_Lockbox":
                    PlayNarrative("Living_Lockbox", "inspect",
                        () => OpenCloseup(ChannelZeroIds.LivingLockboxCloseup, "Living_Lockbox"));
                    break;
                case "Living_REC":
                    PlayNarrative("Living_REC", "inspect");
                    break;
                case "Living_Mina":
                    PlayNarrative("Living_Mina", "inspect");
                    break;
                case "Living_Photos":
                    OpenCloseup(ChannelZeroIds.LivingFamilyPhotosCloseup);
                    break;
                case "Living_Clock":
                    OpenCloseup(ChannelZeroIds.LivingClockCloseup);
                    break;
                case "Workshop_TubeTester":
                    OpenCloseup(ChannelZeroIds.WorkshopTubeTesterCloseup);
                    break;
                case "Workshop_PartsDrawer":
                    OpenCloseup(ChannelZeroIds.WorkshopPartsDrawerCloseup);
                    break;
                case ChannelZeroIds.WorkshopFloorPlan:
                    OpenCloseup(ChannelZeroIds.WorkshopFloorPlanCloseup);
                    break;
                case "Workshop_Workbench":
                    OpenCloseup(ChannelZeroIds.WorkshopWiringDiagramCloseup);
                    break;
                case "Workshop_RepairLog":
                    OpenCloseup(ChannelZeroIds.WorkshopRepairLogCloseup);
                    break;
                case "Workshop_KeyCutter":
                    OpenCloseup(ChannelZeroIds.WorkshopKeyCutterCloseup);
                    break;
                case ChannelZeroIds.WorkshopFoldingCrank:
                    OpenCloseup(ChannelZeroIds.WorkshopFoldingCrankInspect);
                    break;
                default:
                    PlayNarrative(NarrativeTargetForHotspot(logicalId), "inspect",
                        () => SetStatus($"조사: {logicalId}"));
                    break;
            }
        }

        private void OpenCloseup(string closeupId, string narrativeTarget = null)
        {
            State.MarkRecordRead(closeupId);
            activeNarrativeTarget = string.IsNullOrWhiteSpace(narrativeTarget)
                ? NarrativeTargetForCloseup(closeupId)
                : narrativeTarget;
            string exactText = ResolveCloseupText(activeNarrativeTarget);
            closeupCanvas?.Open(State, closeupId, ChannelZeroIds.DefaultVisualState, exactText);
            saveService.Save(State);
            SetStatus($"클로즈업: {closeupId}");
        }

        private void EnterLivingRoom()
        {
            State.MoveTo(ChannelZeroIds.LivingRoom, StoryChapter.Chapter1);
            State.Tune(ChannelEra.Year2001);
            onRoomChanged?.Invoke(State.roomId);
            Refresh("2001년 거실에 들어왔습니다. 오른쪽 큰 다이얼로 연도를 전환할 수 있습니다.");
        }

        private void EnterWorkshop()
        {
            State.MoveTo(ChannelZeroIds.WorkshopRoom, StoryChapter.Chapter2);
            onRoomChanged?.Invoke(State.roomId);
            Refresh($"{(int)State.era}년 작업실에 들어왔습니다. 2막 오브젝트를 조사할 수 있습니다.");
        }

        private void HandleCloseupClosed()
        {
            presenter.Present(State);
            saveService.Save(State);
            string target = activeNarrativeTarget;
            activeNarrativeTarget = string.Empty;
            PlayNarrative(target, "close_closeup",
                () => SetStatus($"{(int)State.era}년 / {State.roomId}로 복귀"));
        }

        private bool PlayNarrative(string target, string trigger, Action completed = null)
        {
            if (narrativeResolver == null || string.IsNullOrWhiteSpace(target))
            {
                completed?.Invoke();
                return false;
            }

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

        private string ResolveCloseupText(string target)
        {
            if (narrativeResolver == null || string.IsNullOrWhiteSpace(target))
                return string.Empty;

            var entries = narrativeResolver.Resolve(NarrativeRoomId(), target, State.era,
                "open_closeup", BuildNarrativeContext());
            foreach (NarrativeTextEntry entry in entries)
            {
                if (entry.once)
                    State.MarkTextSeen(entry.id);
                if (string.Equals(entry.type, "document", StringComparison.OrdinalIgnoreCase))
                    State.MarkRecordRead(entry.id);
            }
            return entries.Count > 0 ? entries[0].text : string.Empty;
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

        private static string NarrativeTargetForHotspot(string logicalId)
        {
            return logicalId switch
            {
                "Living_Toolbox" => "Living_TubeCase",
                "Living_Armchair" => "Living_JinwooHand",
                "Living_CoffeeTable" => "Living_Lockbox",
                "Workshop_PartsDrawer" => "Workshop_PartsDrawers",
                "Workshop_Workbench" => "Workshop_Bench",
                "Workshop_RepairLog" => "Workshop_Records",
                _ => logicalId,
            };
        }

        private static string NarrativeTargetForCloseup(string closeupId)
        {
            return closeupId switch
            {
                ChannelZeroIds.PrologueServiceRequestCloseup => ChannelZeroIds.EntryMail,
                ChannelZeroIds.LivingCrtFrontCloseup => "Living_CRT",
                ChannelZeroIds.LivingCrtRearCloseup => "Living_CRTRear",
                ChannelZeroIds.LivingTubeStorageCloseup => "Living_TubeCase",
                ChannelZeroIds.LivingHandTreatmentCloseup => "Living_JinwooHand",
                ChannelZeroIds.LivingLockboxCloseup => "Living_Lockbox",
                ChannelZeroIds.LivingRecPanelCloseup => "Living_REC",
                ChannelZeroIds.LivingRecSlotsCloseup => "Living_REC",
                _ => string.Empty,
            };
        }

        private bool CanAcceptWorldInput()
        {
            return narrativePresenter == null || !narrativePresenter.IsPresenting;
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
                operationReadout.text = State.operation == ChannelOperation.None
                    ? "READY"
                    : State.operation.ToString().ToUpperInvariant();
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
            ChannelZeroCloseupCanvasController closeups, Text label, Button back,
            Text channel, Text operation, bool restore)
        {
            presenter = roomPresenter;
            closeupCanvas = closeups;
            statusLabel = label;
            backButton = back;
            channelReadout = channel;
            operationReadout = operation;
            restoreSaveOnStart = restore;
        }
#endif
    }
}
