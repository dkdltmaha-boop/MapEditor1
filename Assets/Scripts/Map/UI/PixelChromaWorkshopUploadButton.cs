using UnityEngine;
using UnityEngine.UI;

// UI 버튼 바인더: uGUI Button 클릭을 런타임 업로더에 연결하고,
// 업로더의 상태/진행률/성공/실패 이벤트를 Text/Slider/버튼 활성상태에 반영한다.
// 왜 별도 파일: UI 배선과 업로드 로직을 분리. 이 스크립트는 Steam 타입을 전혀 안 써서
// STEAMWORKS_NET 없이도 항상 컴파일된다(씬 참조가 안 깨짐).
public sealed class PixelChromaWorkshopUploadButton : MonoBehaviour
{
    [Tooltip("실제 업로드를 수행하는 컴포넌트.")]
    public PixelChromaRuntimeWorkshopUploader uploader;

    [Tooltip("눌렀을 때 검증 + 업로드를 시작하는 버튼.")]
    public Button uploadButton;

    [Tooltip("상태 메시지를 표시할 Text (선택, 레거시 UnityEngine.UI.Text).")]
    public Text statusText;

    [Tooltip("업로드 진행률 슬라이더 (선택, 0~1).")]
    public Slider progressSlider;

    private void Awake()
    {
        // uploader 를 인스펙터에서 안 넣었으면 부모에서 자동 탐색(편의).
        if (uploader == null)
        {
            uploader = GetComponentInParent<PixelChromaRuntimeWorkshopUploader>();
        }
    }

    private void OnEnable()
    {
        if (uploadButton != null)
        {
            uploadButton.onClick.AddListener(OnClick);
        }

        if (uploader != null)
        {
            uploader.StatusChanged   += OnStatus;
            uploader.ProgressChanged += OnProgress;
            uploader.UploadSucceeded += OnSucceeded;
            uploader.UploadFailed    += OnFailed;
        }

        if (progressSlider != null)
        {
            progressSlider.value = 0f;
        }
    }

    private void OnDisable()
    {
        if (uploadButton != null)
        {
            uploadButton.onClick.RemoveListener(OnClick);
        }

        if (uploader != null)
        {
            uploader.StatusChanged   -= OnStatus;
            uploader.ProgressChanged -= OnProgress;
            uploader.UploadSucceeded -= OnSucceeded;
            uploader.UploadFailed    -= OnFailed;
        }
    }

    private void OnClick()
    {
        if (uploader == null || uploader.IsBusy)
        {
            return;
        }

        SetInteractable(false);           // 왜: 업로드 중 재클릭 막기
        uploader.ValidateAndUpload();
    }

    private void OnStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }

    private void OnProgress(float value)
    {
        if (progressSlider != null)
        {
            progressSlider.value = Mathf.Clamp01(value);
        }
    }

    private void OnSucceeded(ulong publishedFileId)
    {
        SetInteractable(true);
    }

    private void OnFailed(string message)
    {
        SetInteractable(true);            // 실패해도 다시 누를 수 있게 복구
    }

    private void SetInteractable(bool value)
    {
        if (uploadButton != null)
        {
            uploadButton.interactable = value;
        }
    }
}
