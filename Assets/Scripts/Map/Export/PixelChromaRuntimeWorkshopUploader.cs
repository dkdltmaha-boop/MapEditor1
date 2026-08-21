// 런타임(인게임) Steam 창작마당 업로더.
// 씬의 GameObject 에 붙이고 mapEditor 를 연결한 뒤, UI 버튼에서 ValidateAndUpload() 를 호출한다.
// 에디터가 없는 실제 빌드에서도 동작한다(플레이어용).
//
// 요구사항: Steamworks.NET + STEAMWORKS_NET 정의 심볼.
// 없으면 아래 #else 스텁이 컴파일되어 프로젝트/씬 참조가 깨지지 않는다.

using System;
using UnityEngine;

#if STEAMWORKS_NET
using System.IO;
using Steamworks;

public sealed class PixelChromaRuntimeWorkshopUploader : MonoBehaviour
{
    private const int MaxCreateBusyRetries = 4;
    private const float CreateBusyRetryBaseDelaySeconds = 2f;

    [Tooltip("맵 데이터와 창작마당 메타데이터를 가진 매니저.")]
    public MapEditorManager mapEditor;

    [Tooltip("PixelChroma Steam App ID.")]
    public uint testAppId = 5023800u;

    [Tooltip("활성화하면 업로드 공개 범위를 강제로 미등록으로 설정합니다.")]
    public bool forceUnlistedForTest;

    [Tooltip("이 컴포넌트가 SteamAPI.Init/Shutdown/RunCallbacks 를 직접 관리한다. 이미 SteamManager 가 있으면 끈다.")]
    public bool manageSteamLifecycle = true;

    // UI 가 구독할 이벤트들. Steam 타입을 노출하지 않으려고 string/float/ulong 만 쓴다.
    // 왜: UI 바인더가 Steamworks 를 몰라도 되게 해서 STEAMWORKS_NET 없이도 컴파일되게.
    public event Action<string> StatusChanged;
    public event Action<float>  ProgressChanged;
    public event Action<ulong>  UploadSucceeded;
    public event Action<string> UploadFailed;

    private enum Phase { Idle, Creating, Submitting, Done, Error }
    private Phase phase = Phase.Idle;

    private bool   steamStartedByUs;
    private bool   usingExternalSteamManager;
    private string exportFolder = string.Empty;
    private string lastStatus = string.Empty;

    private PublishedFileId_t publishedFileId;
    private UGCUpdateHandle_t updateHandle;

    // 비동기 결과 수신기. 왜: CreateItem/SubmitItemUpdate 는 즉시 끝나지 않고
    // 나중에 콜백으로 결과가 오므로, 그 콜백을 받을 핸들러를 미리 만들어 둔다.
    private CallResult<CreateItemResult_t>        createItemCallResult;
    private CallResult<SubmitItemUpdateResult_t>  submitItemCallResult;
    private int createBusyRetryCount;
    private float createBusyRetryAt = -1f;
    private bool previewFallbackAttempted;

    public bool IsBusy => phase == Phase.Creating || phase == Phase.Submitting;
    public bool CompletedWithoutSteamPreview { get; private set; }
    public uint EffectiveAppId => mapEditor != null && mapEditor.steamAppId > 0
        ? mapEditor.steamAppId
        : (testAppId > 0 ? testAppId : 5023800u);

    // ── Steam 콜백 펌핑 ───────────────────────────────
    // 왜: 이걸 매 프레임 돌리지 않으면 CreateItem/Submit 결과 콜백이 영원히 안 온다.
    private void Update()
    {
        if (manageSteamLifecycle && steamStartedByUs && !usingExternalSteamManager)
        {
            SteamAPI.RunCallbacks();
        }

        if (phase == Phase.Creating && createBusyRetryAt >= 0f
            && Time.realtimeSinceStartup >= createBusyRetryAt)
        {
            createBusyRetryAt = -1f;
            BeginCreateItem();
        }

        // 업로드 중이면 진행률을 UI 로 방출
        if (phase == Phase.Submitting && updateHandle != UGCUpdateHandle_t.Invalid)
        {
            EItemUpdateStatus st = SteamUGC.GetItemUpdateProgress(updateHandle, out ulong done, out ulong total);
            if (total > 0) ProgressChanged?.Invoke(done / (float)total);
            SetStatus(L("업로드 중: ", "Uploading: ") + Describe(st));
        }
    }

    private void OnDestroy()
    {
        // 왜: 우리가 켠 Steam 연결은 우리가 닫아야 다음 Play 에서 깨끗이 다시 열린다.
        if (steamStartedByUs && !usingExternalSteamManager)
        {
            try { SteamAPI.Shutdown(); } catch (Exception e) { Debug.LogWarning("Steam Shutdown: " + e.Message); }
            steamStartedByUs = false;
        }
    }

    // ── 버튼이 부르는 진입점 ─────────────────────────
    public void ValidateAndUpload()
    {
        if (IsBusy) return;                                   // 중복 클릭 방지
        if (mapEditor == null) { Fail(L("MapEditorManager 미연결.", "MapEditorManager is not assigned.")); return; }
        if (!EnsureSteamReady()) return;                      // Steam 연결 확보(실패 시 내부에서 Fail)

        createBusyRetryCount = 0;
        createBusyRetryAt = -1f;
        previewFallbackAttempted = false;
        CompletedWithoutSteamPreview = false;

        // (1) 검증
        SetStatus(L("맵 검증 중...", "Validating map..."));
        PixelChromaMapValidationReport report = mapEditor.ValidateForWorkshop();
        if (report == null || !report.isValid)
        {
            string errs = report == null ? L("검증 실패", "Validation failed") : string.Join("\n- ", report.errors);
            Fail(L("검증 오류로 중단:\n- ", "Upload stopped due to validation errors:\n- ") + errs);
            return;
        }

        // (2) export
        SetStatus(L("패키지 생성 중...", "Creating package..."));
        if (!mapEditor.ExportWorkshopPackageForUpload(out exportFolder) || !Directory.Exists(exportFolder))
        {
            Fail(L("패키지 export 실패. package_report.json 확인.", "Package export failed. Check package_report.json."));
            return;
        }

        // (3) 업로드 시작
        try
        {
            BeginCreateItem();
        }
        catch (Exception e)
        {
            Fail(L("Steam 창작마당 업로드 시작 중 예외가 발생했습니다: ", "An exception occurred while starting the Steam Workshop upload: ") + e.Message);
        }
    }

    // ── Steam 연결 ───────────────────────────────────
    private bool EnsureSteamReady()
    {
        // 콜백 수신기는 최초 1회만 생성
        if (createItemCallResult == null)
        {
            createItemCallResult = CallResult<CreateItemResult_t>.Create(OnCreateItem);
            submitItemCallResult = CallResult<SubmitItemUpdateResult_t>.Create(OnSubmitItemUpdate);
        }

        SteamManager externalManager = UnityEngine.Object.FindFirstObjectByType<SteamManager>();
        if (externalManager != null)
        {
            if (!SteamManager.Initialized)
            {
                Fail(L("씬의 SteamManager가 SteamAPI 초기화에 실패했습니다. Steam 실행, 로그인, App ID와 계정 라이선스를 확인하세요.", "The scene SteamManager failed to initialize SteamAPI. Check Steam, sign-in, the App ID, and the account license."));
                return false;
            }

            usingExternalSteamManager = true;
            return ValidateSteamSession();
        }

        if (!manageSteamLifecycle)
        {
            Fail(L("Steam 콜백 관리자가 없습니다. manageSteamLifecycle을 켜거나 씬에 SteamManager를 추가하세요.", "No Steam callback manager is available. Enable manageSteamLifecycle or add SteamManager to the scene."));
            return false;
        }

        if (steamStartedByUs) return ValidateSteamSession();

        uint appId = EffectiveAppId;
        EnsureSteamAppIdFile(appId);

        try
        {
            if (!SteamAPI.Init())
            {
                Fail(L("SteamAPI.Init 실패. Steam 실행/로그인 및 steam_appid.txt(=", "SteamAPI.Init failed. Check Steam, sign-in, and steam_appid.txt (=") + appId + L(")를 확인하세요.", ")."));
                return false;
            }
        }
        catch (DllNotFoundException e)
        {
            Fail(L("Steam 네이티브 DLL 없음. Steamworks.NET 설치 확인.\n", "Steam native DLL is missing. Check the Steamworks.NET installation.\n") + e.Message);
            return false;
        }
        catch (Exception e)
        {
            Fail(L("SteamAPI 초기화 중 예외가 발생했습니다: ", "An exception occurred while initializing SteamAPI: ") + e.Message);
            return false;
        }

        steamStartedByUs = true;
        return ValidateSteamSession();
    }

    private bool ValidateSteamSession()
    {
        uint expectedAppId = EffectiveAppId;
        uint activeAppId = SteamUtils.GetAppID().m_AppId;
        if (activeAppId != expectedAppId)
        {
            Fail(L("Steam이 다른 App ID로 초기화되었습니다. 현재=", "Steam was initialized with a different App ID. Current=")
                + activeAppId + L(", 필요=", ", expected=") + expectedAppId
                + L(". 실행 중인 Steam 게임이나 업로더를 닫고 다시 실행하세요.",
                    ". Close other running Steam games or uploaders, then restart."));
            return false;
        }

        if (!SteamUser.BLoggedOn())
        {
            Fail(L("Steam 서버에 로그인되어 있지 않습니다. Steam 온라인 상태를 확인하세요.",
                "Steam is not logged on to its servers. Check that Steam is online."));
            return false;
        }

        PrepareSteamCloud();
        Debug.Log("[RuntimeWorkshopUploader] Steam session ready. App ID=" + activeAppId);
        return true;
    }

    private void PrepareSteamCloud()
    {
        bool accountEnabled = SteamRemoteStorage.IsCloudEnabledForAccount();
        bool appEnabled = SteamRemoteStorage.IsCloudEnabledForApp();
        if (accountEnabled && !appEnabled)
        {
            SteamRemoteStorage.SetCloudEnabledForApp(true);
            appEnabled = SteamRemoteStorage.IsCloudEnabledForApp();
        }

        if (SteamRemoteStorage.GetQuota(out ulong totalBytes, out ulong availableBytes))
        {
            Debug.Log("[RuntimeWorkshopUploader] Steam Cloud: account=" + accountEnabled
                + ", app=" + appEnabled
                + ", available=" + FormatBytes(availableBytes)
                + "/" + FormatBytes(totalBytes));
        }
        else
        {
            Debug.LogWarning("[RuntimeWorkshopUploader] Steam Cloud quota could not be queried. account="
                + accountEnabled + ", app=" + appEnabled);
        }
    }

    // 왜: SteamAPI.Init 은 실행 폴더의 steam_appid.txt 를 읽어 "어떤 앱으로 붙을지" 판단한다.
    // Steam API가 PixelChroma 앱 컨텍스트로 초기화되도록 프로젝트 루트에 App ID를 기록한다.
    private void EnsureSteamAppIdFile(uint appId)
    {
        Environment.SetEnvironmentVariable("SteamAppId", appId.ToString());
        WriteSteamAppIdFile(Path.Combine(Directory.GetCurrentDirectory(), "steam_appid.txt"), appId);

        string executableFolder = Path.GetDirectoryName(Application.dataPath);
        if (!string.IsNullOrWhiteSpace(executableFolder))
        {
            WriteSteamAppIdFile(Path.Combine(executableFolder, "steam_appid.txt"), appId);
        }
    }

    private static void WriteSteamAppIdFile(string path, uint appId)
    {
        try
        {
            if (!File.Exists(path) || File.ReadAllText(path).Trim() != appId.ToString())
            {
                File.WriteAllText(path, appId.ToString());
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("steam_appid.txt 쓰기 실패 (" + path + "): " + e.Message);
        }
    }

    // ── 업로드 3단계 ─────────────────────────────────
    // 1) 아이템 슬롯 생성
    private void BeginCreateItem()
    {
        phase = Phase.Creating;
        SetStatus(L("창작마당 아이템 생성 중...", "Creating Workshop item..."));
        SteamAPICall_t call = SteamUGC.CreateItem(new AppId_t(EffectiveAppId), EWorkshopFileType.k_EWorkshopFileTypeCommunity);
        if (call == SteamAPICall_t.Invalid)
        {
            Fail(L("Steam이 창작마당 아이템 생성 요청을 시작하지 못했습니다. App ID의 창작마당 기능 설정을 확인하세요.", "Steam could not start the Workshop item creation request. Check the App ID's Workshop configuration."));
            return;
        }

        createItemCallResult.Set(call);   // 결과는 OnCreateItem 으로
    }

    private void OnCreateItem(CreateItemResult_t r, bool ioFailure)
    {
        if (!ioFailure && r.m_eResult == EResult.k_EResultBusy && ScheduleCreateBusyRetry())
        {
            return;
        }

        if (ioFailure || r.m_eResult != EResult.k_EResultOK)
        {
            Fail(L("아이템 생성 실패: ", "Item creation failed: ") + DescribeFailure(r.m_eResult, ioFailure));
            return;
        }

        createBusyRetryAt = -1f;
        publishedFileId = r.m_nPublishedFileId;   // 결과: 이제 이 맵의 창작마당 고유 ID 확보

        // 최초 업로드 시 계정이 창작마당 약관에 동의해야 공개된다.
        if (r.m_bUserNeedsToAcceptWorkshopLegalAgreement)
        {
            SetStatus(L("창작마당 약관 동의 필요. 아이템 페이지에서 동의 후 재시도.", "Workshop agreement acceptance is required. Accept it on the item page, then retry."));
            Application.OpenURL("steam://url/CommunityFilePage/" + publishedFileId.m_PublishedFileId);
        }

        try
        {
            SubmitContent(true);
        }
        catch (Exception e)
        {
            Fail(L("창작마당 콘텐츠 제출 준비 중 예외가 발생했습니다: ", "An exception occurred while preparing the Workshop submission: ") + e.Message);
        }
    }

    // 2) 메타데이터 + 콘텐츠 폴더 붙여서 제출
    private void SubmitContent(bool includePreviews)
    {
        phase = Phase.Submitting;
        SetStatus(L("콘텐츠 업로드 준비 중...", "Preparing content upload..."));

        updateHandle = SteamUGC.StartItemUpdate(new AppId_t(EffectiveAppId), publishedFileId);
        if (updateHandle == UGCUpdateHandle_t.Invalid)
        {
            Fail(L("Steam이 아이템 업데이트를 시작하지 못했습니다. App ID와 게시물 소유권을 확인하세요.", "Steam could not start the item update. Check the App ID and item ownership."));
            return;
        }

        // steam_upload.json(우리 export 가 만든 파일)에서 제목/설명/태그를 읽어 채운다.
        PixelChromaSteamWorkshopUploadConfig cfg = ReadConfig();
        if (!SteamUGC.SetItemTitle(updateHandle, cfg?.title ?? cfg?.mapId ?? "map")
            || !SteamUGC.SetItemDescription(updateHandle, cfg?.description ?? string.Empty)
            || !SteamUGC.SetItemVisibility(updateHandle, ResolveVisibility(cfg)))
        {
            Fail(L("Steam 창작마당 제목, 설명 또는 공개 범위를 설정하지 못했습니다.", "Could not set the Steam Workshop title, description, or visibility."));
            return;
        }

        if (cfg?.tags != null && cfg.tags.Length > 0)
        {
            if (!SteamUGC.SetItemTags(updateHandle, cfg.tags))
            {
                Fail(L("Steam 창작마당 태그를 설정하지 못했습니다.", "Could not set the Steam Workshop tags."));
                return;
            }
        }

        // 미리보기 이미지(≤1MB). 왜: 창작마당/메뉴 썸네일로 쓰임.
        string preview = Path.GetFullPath(Path.Combine(exportFolder, "preview.png"));
        if (includePreviews && File.Exists(preview) && new FileInfo(preview).Length <= 1024 * 1024
            && !SteamUGC.SetItemPreview(updateHandle, preview))
        {
            Fail(L("Steam 창작마당 미리보기 이미지를 설정하지 못했습니다.", "Could not set the Steam Workshop preview image."));
            return;
        }

        if (includePreviews && cfg?.additionalPreviewFiles != null)
        {
            for (int i = 0; i < cfg.additionalPreviewFiles.Length; i++)
            {
                string additional = Path.GetFullPath(Path.Combine(exportFolder, cfg.additionalPreviewFiles[i] ?? string.Empty));
                if (!File.Exists(additional) || new FileInfo(additional).Length > 1024 * 1024) continue;
                if (!SteamUGC.AddItemPreviewFile(updateHandle, additional, EItemPreviewType.k_EItemPreviewType_Image))
                {
                    Fail(L("Steam 창작마당 추가 미리보기를 등록하지 못했습니다: ", "Could not add a Steam Workshop preview: ") + Path.GetFileName(additional));
                    return;
                }
            }
        }

        // 핵심: 업로드할 "폴더" 지정. Steam 이 이 폴더 전체를 압축/전송한다.
        if (!SteamUGC.SetItemContent(updateHandle, Path.GetFullPath(exportFolder)))
        {
            Fail(L("Steam에 업로드할 콘텐츠 폴더를 설정하지 못했습니다: ", "Could not set the Steam upload content folder: ") + exportFolder);
            return;
        }

        SteamAPICall_t call = SteamUGC.SubmitItemUpdate(updateHandle, "In-game map upload");
        if (call == SteamAPICall_t.Invalid)
        {
            Fail(L("Steam이 콘텐츠 제출 요청을 시작하지 못했습니다.", "Steam could not start the content submission request."));
            return;
        }

        submitItemCallResult.Set(call);   // 결과는 OnSubmitItemUpdate 으로
    }

    // 3) 완료 콜백
    private void OnSubmitItemUpdate(SubmitItemUpdateResult_t r, bool ioFailure)
    {
        if (!ioFailure
            && !previewFallbackAttempted
            && (r.m_eResult == EResult.k_EResultLimitExceeded || r.m_eResult == EResult.k_EResultAccessDenied))
        {
            previewFallbackAttempted = true;
            CompletedWithoutSteamPreview = true;
            SetStatus(L(
                "Steam이 대표 이미지 저장을 거부했습니다. 같은 게시물에 이미지 없이 맵 콘텐츠를 다시 제출합니다.",
                "Steam rejected preview storage. Retrying the same item without Steam preview images."));
            try
            {
                SubmitContent(false);
            }
            catch (Exception e)
            {
                Fail(L("대표 이미지 제외 재제출 중 예외가 발생했습니다: ",
                    "An exception occurred while resubmitting without previews: ") + e.Message);
            }
            return;
        }

        if (ioFailure || r.m_eResult != EResult.k_EResultOK)
        {
            Fail(L("업로드 실패: ", "Upload failed: ") + DescribeFailure(r.m_eResult, ioFailure));
            return;
        }

        phase = Phase.Done;
        ProgressChanged?.Invoke(1f);
        SetStatus((CompletedWithoutSteamPreview
            ? L("맵 업로드 완료(대표 이미지는 Steam 권한/할당량 문제로 제외). PublishedFileId = ",
                "Map upload complete (Steam preview omitted due to permission/quota). PublishedFileId = ")
            : L("업로드 완료! PublishedFileId = ", "Upload complete! PublishedFileId = "))
            + publishedFileId.m_PublishedFileId);
        UploadSucceeded?.Invoke(publishedFileId.m_PublishedFileId);  // 결과: UI 가 성공 처리
    }

    // ── 보조 ────────────────────────────────────────
    private PixelChromaSteamWorkshopUploadConfig ReadConfig()
    {
        string p = Path.Combine(exportFolder, "steam_upload.json");
        if (!File.Exists(p)) return null;
        try { return JsonUtility.FromJson<PixelChromaSteamWorkshopUploadConfig>(File.ReadAllText(p)); }
        catch (Exception e) { Debug.LogWarning("steam_upload.json 파싱 실패: " + e.Message); return null; }
    }

    private ERemoteStoragePublishedFileVisibility ResolveVisibility(PixelChromaSteamWorkshopUploadConfig cfg)
    {
        if (forceUnlistedForTest)                              // 테스트 업로드 공개 방지
            return ERemoteStoragePublishedFileVisibility.k_ERemoteStoragePublishedFileVisibilityUnlisted;

        switch ((cfg?.visibility ?? "Unlisted").Trim().ToLowerInvariant())
        {
            case "public":      return ERemoteStoragePublishedFileVisibility.k_ERemoteStoragePublishedFileVisibilityPublic;
            case "friendsonly": return ERemoteStoragePublishedFileVisibility.k_ERemoteStoragePublishedFileVisibilityFriendsOnly;
            case "private":     return ERemoteStoragePublishedFileVisibility.k_ERemoteStoragePublishedFileVisibilityPrivate;
            default:            return ERemoteStoragePublishedFileVisibility.k_ERemoteStoragePublishedFileVisibilityUnlisted;
        }
    }

    private static string Describe(EItemUpdateStatus s)
    {
        switch (s)
        {
            case EItemUpdateStatus.k_EItemUpdateStatusPreparingConfig:      return L("설정 준비 중", "Preparing configuration");
            case EItemUpdateStatus.k_EItemUpdateStatusPreparingContent:     return L("콘텐츠 준비 중", "Preparing content");
            case EItemUpdateStatus.k_EItemUpdateStatusUploadingContent:     return L("콘텐츠 업로드 중", "Uploading content");
            case EItemUpdateStatus.k_EItemUpdateStatusUploadingPreviewFile: return L("미리보기 업로드 중", "Uploading preview");
            case EItemUpdateStatus.k_EItemUpdateStatusCommittingChanges:    return L("커밋 중", "Committing changes");
            default:                                                        return L("대기 중", "Waiting");
        }
    }

    private static string DescribeFailure(EResult result, bool ioFailure)
    {
        if (ioFailure)
        {
            return L("Steam 네트워크 IO 오류", "Steam network IO error");
        }

        switch (result)
        {
            case EResult.k_EResultAccessDenied:
                return L("접근 거부. App ID의 창작마당 설정과 계정 권한을 확인하세요.", "Access denied. Check the App ID's Workshop configuration and account permissions.");
            case EResult.k_EResultInsufficientPrivilege:
                return L("권한 부족. Steam 계정 제한 또는 창작마당 약관 동의를 확인하세요.", "Insufficient privilege. Check Steam account restrictions and Workshop agreement acceptance.");
            case EResult.k_EResultTimeout:
                return L("Steam 서버 응답 시간 초과. 네트워크를 확인하고 다시 시도하세요.", "Steam server timed out. Check the network and try again.");
            case EResult.k_EResultNotLoggedOn:
                return L("Steam에 로그인되어 있지 않습니다.", "Not signed in to Steam.");
            case EResult.k_EResultBusy:
                return L(
                    "Steam 창작마당이 다른 작업을 처리 중이라 요청을 받지 않았습니다. 자동 재시도 후에도 계속되면 Steam과 다른 업로더를 완전히 종료한 뒤 다시 실행하세요.",
                    "Steam Workshop did not accept the request because it is busy. If automatic retries also fail, fully close Steam and other uploaders, then restart.");
            case EResult.k_EResultLimitExceeded:
                return L(
                    "Steam 대표 이미지가 1MB를 넘었거나 이 계정의 Steam Cloud 할당량이 부족합니다.",
                    "The Steam preview exceeds 1 MB or this account lacks available Steam Cloud quota.");
            default:
                return result.ToString();
        }
    }

    private static string FormatBytes(ulong bytes)
    {
        const double megabyte = 1024d * 1024d;
        return (bytes / megabyte).ToString("0.##") + " MB";
    }

    private bool ScheduleCreateBusyRetry()
    {
        if (createBusyRetryCount >= MaxCreateBusyRetries)
        {
            return false;
        }

        createBusyRetryCount++;
        float delay = CreateBusyRetryBaseDelaySeconds * createBusyRetryCount;
        createBusyRetryAt = Time.realtimeSinceStartup + delay;
        SetStatus(L("Steam 창작마당이 처리 중입니다. ", "Steam Workshop is busy. Retrying in ")
            + delay.ToString("0")
            + L("초 후 자동 재시도합니다. (", " seconds. (")
            + createBusyRetryCount + "/" + MaxCreateBusyRetries + ")");
        return true;
    }

    private void SetStatus(string message)
    {
        if (string.Equals(lastStatus, message, StringComparison.Ordinal))
        {
            return;
        }

        lastStatus = message;
        Debug.Log("[RuntimeWorkshopUploader] " + message);
        StatusChanged?.Invoke(message);
    }

    private static string L(string korean, string english)
    {
        return MapEditorLocalization.Choose(korean, english);
    }

    private void Fail(string m)
    {
        createBusyRetryAt = -1f;
        phase = Phase.Error;
        SetStatus(m);
        Debug.LogError("[RuntimeWorkshopUploader] " + m);
        UploadFailed?.Invoke(m);
    }
}
#else
// STEAMWORKS_NET 미정의 시의 스텁. 왜: SDK/심볼 없이도 컴파일되고 씬 참조가 살아있게 한다.
public sealed class PixelChromaRuntimeWorkshopUploader : MonoBehaviour
{
    public MapEditorManager mapEditor;
    public uint testAppId = 5023800u;
    public bool forceUnlistedForTest;
    public bool manageSteamLifecycle = true;

    public event Action<string> StatusChanged;
    public event Action<float>  ProgressChanged;
    public event Action<ulong>  UploadSucceeded;
    public event Action<string> UploadFailed;

    public bool IsBusy => false;
    public bool CompletedWithoutSteamPreview => false;
    public uint EffectiveAppId => mapEditor != null && mapEditor.steamAppId > 0
        ? mapEditor.steamAppId
        : (testAppId > 0 ? testAppId : 5023800u);

    public void ValidateAndUpload()
    {
        string m = MapEditorLocalization.Choose(
            "Steamworks.NET 미설치. STEAMWORKS_NET 심볼을 정의하면 실제 업로드가 켜집니다.",
            "Steamworks.NET is not installed. Define STEAMWORKS_NET to enable real uploads.");
        Debug.LogWarning("[RuntimeWorkshopUploader] " + m);
        StatusChanged?.Invoke(m);
        UploadFailed?.Invoke(m);
    }
}
#endif
