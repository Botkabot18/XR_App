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

    [Header("AR Context")]
    public string currentSubject = "";

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

    // ── Prompt Builder ────────────────────────────────────────────────────────
    string BuildPrompt(string userQuestion)
    {
        return "You are an educational AR assistant embedded in a science learning app for students.and also make it feel like an app, don't just reply like great AR model or great question, just pure answer " +
               "A student is looking at an AR model of " + currentSubject + " through their phone camera. " +
               "YOUR RULES follow these strictly: " +
               "1. Answer ONLY about the subject: " + currentSubject + ". " +
               "2. IGNORE everything else visible in the image, the background, the phone screen, UI buttons, hands, furniture, cables, or any real-world objects. " +
               "3. Do describe what you see in the image. Just answer the question based on what you actually see. " +
               "4. Keep your answer clear, factual, and engaging for a student aged 10-16. " +
               "5. Use short paragraphs. No bullet points. " +
               "Student question: " + userQuestion;
    }

    // ── Button Handlers ───────────────────────────────────────────────────────
    void OnTalkClick() { StartCoroutine(SnapAndAppend()); }

    void OnCloseClick()
    {
        StartCoroutine(SlidePanel(hiddenPos));
        talkButton.gameObject.SetActive(true);
    }

    void OnSendClick()
    {
        string userMessage = inputField.text;
        if (string.IsNullOrEmpty(userMessage)) return;
        AddToChat("You", userMessage);
        inputField.text = "";
        if (latestImage == null)
        {
            AddToChat("Error", "No image captured yet. Tap the scan button first.");
            return;
        }
        StartCoroutine(AskGemini(BuildPrompt(userMessage), latestImage));
    }

    // ── Snap & Send ───────────────────────────────────────────────────────────
    IEnumerator SnapAndAppend()
    {
        talkButton.gameObject.SetActive(false);
        yield return new WaitForEndOfFrame();
        latestImage = ScreenCapture.CaptureScreenshotAsTexture();
        StartCoroutine(SlidePanel(visiblePos));
        AddToChat("System", "New scan captured...");
        string autoPrompt = BuildPrompt("Give a comprehensive educational overview of " + currentSubject);
        StartCoroutine(AskGemini(autoPrompt, latestImage));
    }

    // ── Chat UI ───────────────────────────────────────────────────────────────
    void AddToChat(string sender, string message)
    {
        string color = "#FFFFFF";
        if (sender == "You") color = "#00FF00";
        if (sender == "System") color = "#FFFF00";
        if (sender == "Error") color = "#FF4444";

        resultText.text += "\n<color=" + color + "><b>" + sender + ":</b></color> " + message + "\n";
        StartCoroutine(ScrollToBottom());
    }

    IEnumerator ScrollToBottom()
    {
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        if (scrollView != null)
            scrollView.verticalNormalizedPosition = 0f;
    }

    // ── Gemini API Call ───────────────────────────────────────────────────────
    IEnumerator AskGemini(string prompt, Texture2D image)
    {
        if (image == null)
        {
            AddToChat("Error", "No image captured. Try pressing Talk to AI again.");
            yield break;
        }

        string safePrompt = prompt
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r");

        byte[] imageBytes = image.EncodeToJPG(50);
        string base64Image = System.Convert.ToBase64String(imageBytes);

        string json = "{\"contents\":[{\"parts\":[" +
                      "{\"text\":\"" + safePrompt + "\"}," +
                      "{\"inline_data\":{\"mime_type\":\"image/jpeg\",\"data\":\"" + base64Image + "\"}}" +
                      "]}]}";

        AddToChat("System", "Sending to Gemini...");

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("X-goog-api-key", apiKey);

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                AddToChat("Error", "Network failed: " + request.error);
                yield break;
            }

            string rawResponse = request.downloadHandler.text;
            Debug.Log("[Gemini Raw] " + rawResponse);

            GeminiResponse response = JsonUtility.FromJson<GeminiResponse>(rawResponse);

            if (response == null || response.candidates == null || response.candidates.Length == 0)
            {
                AddToChat("Error", "Bad response: " + rawResponse);
                yield break;
            }

            string geminiReply = response.candidates[0].content.parts[0].text;
            AddToChat("Gemini", geminiReply);
        }
    }

    // ── Panel Animation ───────────────────────────────────────────────────────
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

// ── Gemini Data Classes ───────────────────────────────────────────────────────
[System.Serializable] public class GeminiResponse { public Candidate[] candidates; }
[System.Serializable] public class Candidate { public Content content; }
[System.Serializable] public class Content { public Part[] parts; }
[System.Serializable] public class Part { public string text; }