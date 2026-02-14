using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;
using System.Collections;
using System.Text;

public class LensManager : MonoBehaviour
{
    [Header("API Settings")]
    [SerializeField] private string apiKey = "YOUR_API_KEY_HERE";
    private string url = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent";

    [Header("UI Elements")]
    public RectTransform resultPanel;
    public TMP_InputField inputField;
    public TextMeshProUGUI resultText;
    public ScrollRect scrollView;

    [Header("Buttons")]
    public Button talkButton;
    public Button closeButton;
    public Button sendButton;

    [Header("Animation")]
    public float animationSpeed = 0.5f;
    private Vector2 hiddenPos;
    private Vector2 visiblePos;

    private Texture2D latestImage;

    void Start()
    {
        talkButton.onClick.AddListener(OnTalkClick);
        closeButton.onClick.AddListener(OnCloseClick);
        sendButton.onClick.AddListener(OnSendClick);

        visiblePos = new Vector2(0, 0);
        hiddenPos = new Vector2(0, -resultPanel.rect.height);
        resultPanel.anchoredPosition = hiddenPos;
    }

    void OnTalkClick() { StartCoroutine(SnapAndAppend()); }
    void OnCloseClick() { StartCoroutine(SlidePanel(hiddenPos)); talkButton.gameObject.SetActive(true); }

    void OnSendClick()
    {
        string userMessage = inputField.text;
        if (string.IsNullOrEmpty(userMessage)) return;
        AddToChat("You", userMessage);
        inputField.text = "";
        StartCoroutine(AskGemini(userMessage, latestImage));
    }

    IEnumerator SnapAndAppend()
    {
        talkButton.gameObject.SetActive(false);
        yield return new WaitForEndOfFrame();
        latestImage = ScreenCapture.CaptureScreenshotAsTexture();
        StartCoroutine(SlidePanel(visiblePos));
        AddToChat("System", "New scan captured...");
        StartCoroutine(AskGemini("What is in this new image?", latestImage));
    }

    // --- UPDATED: ADDS TEXT AND TRIGGERS SCROLL ---
    void AddToChat(string sender, string message)
    {
        string color = (sender == "You") ? "#00FF00" : "#FFFFFF";
        if (sender == "System") color = "#FFFF00";

        resultText.text += $"\n<color={color}><b>{sender}:</b></color> {message}\n";

        // Trigger the safe auto-scroll
        StartCoroutine(ScrollToBottom());
    }

    // --- NEW: THE SMOOTH SCROLL LOGIC ---
    IEnumerator ScrollToBottom()
    {
        // Wait for end of frame TWICE to ensure the Content Size Fitter 
        // has finished calculating the new height.
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();

        if (scrollView != null)
        {
            scrollView.verticalNormalizedPosition = 0f; // 0f means "Bottom"
        }
    }

    IEnumerator AskGemini(string prompt, Texture2D image)
    {
        byte[] imageBytes = image.EncodeToJPG(50);
        string base64Image = System.Convert.ToBase64String(imageBytes);
        string json = "{ \"contents\": [{ \"parts\": [ { \"text\": \"" + prompt + "\" }, { \"inline_data\": { \"mime_type\": \"image/jpeg\", \"data\": \"" + base64Image + "\" } } ] }] }";

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("X-goog-api-key", apiKey);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                GeminiResponse response = JsonUtility.FromJson<GeminiResponse>(request.downloadHandler.text);
                if (response != null && response.candidates.Length > 0)
                {
                    AddToChat("Gemini", response.candidates[0].content.parts[0].text);
                }
            }
        }
    }

    IEnumerator SlidePanel(Vector2 targetPos)
    {
        float elapsed = 0f;
        Vector2 startPos = resultPanel.anchoredPosition;
        while (elapsed < animationSpeed)
        {
            resultPanel.anchoredPosition = Vector2.Lerp(startPos, targetPos, elapsed / animationSpeed);
            elapsed += Time.deltaTime;
            yield return null;
        }
        resultPanel.anchoredPosition = targetPos;
    }
}

// --- DATA CLASSES ---
[System.Serializable] public class GeminiResponse { public Candidate[] candidates; }
[System.Serializable] public class Candidate { public Content content; }
[System.Serializable] public class Content { public Part[] parts; }
[System.Serializable] public class Part { public string text; }