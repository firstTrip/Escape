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

        public ChannelZeroSessionState State { get; private set; }

        private void Awake()
        {
            saveService = new ChannelZeroSaveService(new PlayerPrefsChannelZeroSaveStore());
            State = restoreSaveOnStart && saveService.TryLoad(out ChannelZeroSessionState loaded)
                ? loaded
                : ChannelZeroSessionState.CreateNew();
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
                closeupCanvas.Restore(State, ExactTextFor(State.activeCloseupId));
        }

        private void OnDisable()
        {
            if (presenter != null)
                presenter.HotspotClicked -= HandleHotspot;
            if (closeupCanvas != null)
                closeupCanvas.Closed -= HandleCloseupClosed;
        }

        public void Tune1961() => Tune(ChannelEra.Year1961);
        public void Tune1981() => Tune(ChannelEra.Year1981);
        public void Tune2001() => Tune(ChannelEra.Year2001);
        public void Tune2021() => Tune(ChannelEra.Year2021);
        public void TunePast() => ShiftEra(-1);
        public void TuneFuture() => ShiftEra(1);

        public void SelectRec() => SelectOperation(ChannelOperation.Rec);
        public void SelectPlay() => SelectOperation(ChannelOperation.Play);
        public void SelectLoad() => SelectOperation(ChannelOperation.Load);
        public void SelectRew() => SelectOperation(ChannelOperation.Rew);
        public void SelectHold() => SelectOperation(ChannelOperation.Hold);

        public void CycleEraForward()
        {
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
            saveService.Clear();
            State = ChannelZeroSessionState.CreateNew();
            Refresh("저장을 지우고 2001년 현관으로 돌아왔습니다.");
        }

        public void GoBack()
        {
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
            State.Observe(logicalId);
            onHotspotActivated?.Invoke(logicalId);

            switch (logicalId)
            {
                case ChannelZeroIds.EntryMail:
                    OpenCloseup(ChannelZeroIds.PrologueServiceRequestCloseup);
                    break;
                case ChannelZeroIds.EntryLivingDoor:
                    EnterLivingRoom();
                    break;
                case ChannelZeroIds.LivingWorkshopDoor:
                    EnterWorkshop();
                    break;
                case "Workshop_LivingDoor":
                    GoBack();
                    break;
                case "Living_CRT":
                    OpenCloseup(State.operation == ChannelOperation.Hold
                        ? ChannelZeroIds.LivingCrtRearCloseup
                        : ChannelZeroIds.LivingCrtFrontCloseup);
                    break;
                case "Living_Toolbox":
                    OpenCloseup(ChannelZeroIds.LivingTubeStorageCloseup);
                    break;
                case "Living_NumberRug":
                    OpenCloseup(ChannelZeroIds.LivingNumberRugCloseup);
                    break;
                case "Living_DisplayMedical":
                    OpenCloseup(ChannelZeroIds.LivingMedicalCabinetCloseup);
                    break;
                case "Living_Armchair":
                    OpenCloseup(ChannelZeroIds.LivingHandTreatmentCloseup);
                    break;
                case "Living_CoffeeTable":
                    OpenCloseup(State.operation switch
                    {
                        ChannelOperation.Rec => ChannelZeroIds.LivingRecPanelCloseup,
                        ChannelOperation.Load => ChannelZeroIds.LivingRecSlotsCloseup,
                        ChannelOperation.Rew => ChannelZeroIds.LivingWiringDiagramCloseup,
                        _ => ChannelZeroIds.LivingLockboxCloseup,
                    });
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
                    SetStatus($"조사: {logicalId}");
                    saveService.Save(State);
                    break;
            }
        }

        private void OpenCloseup(string closeupId)
        {
            State.MarkRecordRead(closeupId);
            closeupCanvas?.Open(State, closeupId, ChannelZeroIds.DefaultVisualState, ExactTextFor(closeupId));
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
            SetStatus($"{(int)State.era}년 / {State.roomId}로 복귀");
        }

        private static string ExactTextFor(string closeupId)
        {
            return closeupId == ChannelZeroIds.PrologueServiceRequestCloseup
                ? "접수일: 2001년 10월 10일\n방문 요청일: 2001년 10월 10일\n증상: 아이가 채널을 따라옵니다."
                : string.Empty;
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
