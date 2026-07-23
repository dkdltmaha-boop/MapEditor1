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
    [Tooltip("맵 데이터와 창작마당 메타데이터를 가진 매니저.")]
    public MapEditorManager mapEditor;

    [Tooltip("테스트 대상 App ID. 480 = SpaceWar 공용 테스트 앱. 실제 App ID가 생기면 교체.")]
    public uint testAppId = 480;

    [Tooltip("테스트 중엔 항상 Unlisted 로 올려 공개 노출을 막는다.")]
    public bool forceUnlistedForTest = true;

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
    private string exportFolder = string.Empty;

    private PublishedFileId_t publishedFileId;
    private UGCUpdateHandle_t updateHandle;

    // 비동기 결과 수신기. 왜: CreateItem/SubmitItemUpdate 는 즉시 끝나지 않고
    // 나중에 콜백으로 결과가 오므로, 그 콜백을 받을 핸들러를 미리 만들어 둔다.
    private CallResult<CreateItemResult_t>        createItemCallResult;
    private CallResult<SubmitItemUpdateResult_t>  submitItemCallResult;

    public bool IsBusy => phase == Phase.Creating || phase == Phase.Submitting;

    // ── Steam 콜백 펌핑 ───────────────────────────────
    // 왜: 이걸 매 프레임 돌리지 않으면 CreateItem/Submit 결과 콜백이 영원히 안 온다.
    private void Update()
    {
        if (manageSteamLifecycle && steamStartedByUs)
        {
            SteamAPI.RunCallbacks();
        }

        // 업로드 중이면 진행률을 UI 로 방출
        if (phase == Phase.Submitting && updateHandle != UGCUpdateHandle_t.Invalid)
        {
            EItemUpdateStatus st = SteamUGC.GetItemUpdateProgress(updateHandle, out ulong done, out ulong total);
            if (total > 0) ProgressChanged?.Invoke(done / (float)total);
            SetStatus("업로드 중: " + Describe(st));
        }
    }

    private void OnDestroy()
    {
        // 왜: 우리가 켠 Steam 연결은 우리가 닫아야 다음 Play 에서 깨끗이 다시 열린다.
        if (steamStartedByUs)
        {
            try { SteamAPI.Shutdown(); } catch (Exception e) { Debug.LogWarning("Steam Shutdown: " + e.Message); }
            steamStartedByUs = false;
        }
    }

    // ── 버튼이 부르는 진입점 ─────────────────────────
    public void ValidateAndUpload()
    {
        if (IsBusy) return;                                   // 중복 클릭 방지
        if (mapEditor == null) { Fail("MapEditorManager 미연결."); return; }
        if (!EnsureSteamReady()) return;                      // Steam 연결 확보(실패 시 내부에서 Fail)

        // (1) 검증
        SetStatus("맵 검증 중...");
        PixelChromaMapValidationReport report = mapEditor.ValidateForWorkshop();
        if (report == null || !report.isValid)
        {
            string errs = report == null ? "검증 실패" : string.Join("\n- ", report.errors);
            Fail("검증 오류로 중단:\n- " + errs);            // 결과: 잘못된 맵은 여기서 멈춤
            return;
        }

        // (2) export
        SetStatus("패키지 생성 중...");
        if (!mapEditor.ExportWorkshopPackageForUpload(out exportFolder) || !Directory.Exists(exportFolder))
        {
            Fail("패키지 export 실패. package_report.json 확인.");
            return;
        }

        // (3) 업로드 시작
        BeginCreateItem();
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

        if (!manageSteamLifecycle) return true;  // 외부 SteamManager 가 이미 Init/RunCallbacks 담당
        if (steamStartedByUs)      return true;

        EnsureSteamAppIdFile();   // 480 을 Steam 에 알림(아래 함수 참고)

        try
        {
            if (!SteamAPI.Init())
            {
                Fail("SteamAPI.Init 실패. Steam 실행/로그인 및 steam_appid.txt(=" + testAppId + ") 확인.");
                return false;
            }
        }
        catch (DllNotFoundException e)
        {
            Fail("Steam 네이티브 DLL 없음. Steamworks.NET 설치 확인.\n" + e.Message);
            return false;
        }

        steamStartedByUs = true;
        return true;
    }

    // 왜: SteamAPI.Init 은 실행 폴더의 steam_appid.txt 를 읽어 "어떤 앱으로 붙을지" 판단한다.
    // 이 파일이 480 이어야 SpaceWar 로 연결된다. (에디터에선 working dir = 프로젝트 루트)
    private void EnsureSteamAppIdFile()
    {
        string path = Path.Combine(Directory.GetCurrentDirectory(), "steam_appid.txt");
        try
        {
            if (!File.Exists(path) || File.ReadAllText(path).Trim() != testAppId.ToString())
                File.WriteAllText(path, testAppId.ToString());
        }
        catch (Exception e) { Debug.LogWarning("steam_appid.txt 쓰기 실패: " + e.Message); }
    }

    // ── 업로드 3단계 ─────────────────────────────────
    // 1) 아이템 슬롯 생성
    private void BeginCreateItem()
    {
        phase = Phase.Creating;
        SetStatus("창작마당 아이템 생성 중...");
        SteamAPICall_t call = SteamUGC.CreateItem(new AppId_t(testAppId), EWorkshopFileType.k_EWorkshopFileTypeCommunity);
        createItemCallResult.Set(call);   // 결과는 OnCreateItem 으로
    }

    private void OnCreateItem(CreateItemResult_t r, bool ioFailure)
    {
        if (ioFailure || r.m_eResult != EResult.k_EResultOK)
        {
            Fail("아이템 생성 실패: " + (ioFailure ? "IO 오류" : r.m_eResult.ToString()));
            return;
        }

        publishedFileId = r.m_nPublishedFileId;   // 결과: 이제 이 맵의 창작마당 고유 ID 확보

        // 최초 업로드 시 계정이 창작마당 약관에 동의해야 공개된다.
        if (r.m_bUserNeedsToAcceptWorkshopLegalAgreement)
        {
            SetStatus("창작마당 약관 동의 필요. 아이템 페이지에서 동의 후 재시도.");
            Application.OpenURL("https://steamcommunity.com/sharedfiles/filedetails/?id=" + publishedFileId.m_PublishedFileId);
        }

        SubmitContent();
    }

    // 2) 메타데이터 + 콘텐츠 폴더 붙여서 제출
    private void SubmitContent()
    {
        phase = Phase.Submitting;
        SetStatus("콘텐츠 업로드 준비 중...");

        updateHandle = SteamUGC.StartItemUpdate(new AppId_t(testAppId), publishedFileId);

        // steam_upload.json(우리 export 가 만든 파일)에서 제목/설명/태그를 읽어 채운다.
        PixelChromaSteamWorkshopUploadConfig cfg = ReadConfig();
        SteamUGC.SetItemTitle(updateHandle,       cfg?.title ?? cfg?.mapId ?? "map");
        SteamUGC.SetItemDescription(updateHandle, cfg?.description ?? string.Empty);
        SteamUGC.SetItemVisibility(updateHandle,  ResolveVisibility(cfg));
        if (cfg?.tags != null && cfg.tags.Length > 0)
            SteamUGC.SetItemTags(updateHandle, cfg.tags);

        // 미리보기 이미지(≤1MB). 왜: 창작마당/메뉴 썸네일로 쓰임.
        string preview = Path.GetFullPath(Path.Combine(exportFolder, "preview.png"));
        if (File.Exists(preview) && new FileInfo(preview).Length <= 1024 * 1024)
            SteamUGC.SetItemPreview(updateHandle, preview);

        // 핵심: 업로드할 "폴더" 지정. Steam 이 이 폴더 전체를 압축/전송한다.
        SteamUGC.SetItemContent(updateHandle, Path.GetFullPath(exportFolder));

        SteamAPICall_t call = SteamUGC.SubmitItemUpdate(updateHandle, "In-game map upload");
        submitItemCallResult.Set(call);   // 결과는 OnSubmitItemUpdate 으로
    }

    // 3) 완료 콜백
    private void OnSubmitItemUpdate(SubmitItemUpdateResult_t r, bool ioFailure)
    {
        if (ioFailure || r.m_eResult != EResult.k_EResultOK)
        {
            Fail("업로드 실패: " + (ioFailure ? "IO 오류" : r.m_eResult.ToString()));
            return;
        }

        phase = Phase.Done;
        ProgressChanged?.Invoke(1f);
        SetStatus("업로드 완료! PublishedFileId = " + publishedFileId.m_PublishedFileId);
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
        if (forceUnlistedForTest)                              // 480 샌드박스 보호
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
            case EItemUpdateStatus.k_EItemUpdateStatusPreparingConfig:      return "설정 준비 중";
            case EItemUpdateStatus.k_EItemUpdateStatusPreparingContent:     return "콘텐츠 준비 중";
            case EItemUpdateStatus.k_EItemUpdateStatusUploadingContent:     return "콘텐츠 업로드 중";
            case EItemUpdateStatus.k_EItemUpdateStatusUploadingPreviewFile: return "미리보기 업로드 중";
            case EItemUpdateStatus.k_EItemUpdateStatusCommittingChanges:    return "커밋 중";
            default:                                                        return "대기 중";
        }
    }

    private void SetStatus(string m) => StatusChanged?.Invoke(m);

    private void Fail(string m)
    {
        phase = Phase.Error;
        Debug.LogError("[RuntimeWorkshopUploader] " + m);
        UploadFailed?.Invoke(m);
    }
}
#else
// STEAMWORKS_NET 미정의 시의 스텁. 왜: SDK/심볼 없이도 컴파일되고 씬 참조가 살아있게 한다.
public sealed class PixelChromaRuntimeWorkshopUploader : MonoBehaviour
{
    public MapEditorManager mapEditor;
    public uint testAppId = 480;
    public bool forceUnlistedForTest = true;
    public bool manageSteamLifecycle = true;

    public event Action<string> StatusChanged;
    public event Action<float>  ProgressChanged;
    public event Action<ulong>  UploadSucceeded;
    public event Action<string> UploadFailed;

    public bool IsBusy => false;

    public void ValidateAndUpload()
    {
        const string m = "Steamworks.NET 미설치. STEAMWORKS_NET 심볼을 정의하면 실제 업로드가 켜집니다.";
        Debug.LogWarning("[RuntimeWorkshopUploader] " + m);
        StatusChanged?.Invoke(m);
        UploadFailed?.Invoke(m);
    }
}
#endif
