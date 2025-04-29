using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using UnityEngine.UI;

public class SimpleTextToImage : MonoBehaviour
{
    public TMP_InputField inputField;
    public RawImage imageDisplay;
    public GameObject imagePlane;

    private bool isTyping = false;

    private string TEXT_API_URL = "https://api.openai.com/v1/chat/completions";
    private string IMAGE_API_URL = "https://api.openai.com/v1/images/generations";
    private string API_KEY = "your_token";

    private string SYSTEM_PROMPT = "You are a prompt engineering specialist for DALL-E 3. When users describe an image concept, always generate a visualization prompt that strictly follows this template: 'A simple line drawing of [SUBJECT DESCRIPTION]. [SUBJECT] has [DEFINING CHARACTERISTIC], with [OPTIONAL DETAIL]. [ADD MORE NECESSARY DETAILS IF NEEDED]. [Add composition and background information if necessary]. The drawing is minimalistic, using only black lines on a white background.' Key requirements: 1. Maintain this exact sentence structure. 2. Stick to the user's description and avoid adding unnecessary details.";

    void Start()
    {
        // Check if image plane is set up
        if (imagePlane == null)
        {
            // Try to find Image object by name
            imagePlane = GameObject.Find("Image");
            if (imagePlane != null)
            {
                Debug.Log("Automatically found object named 'Image'");
            }
        }
    }

    public void Activate()
    {
        // Get text from InputField's placeholder
        string userInput = "";
        if (inputField != null && inputField.placeholder != null)
        {
            TextMeshProUGUI placeholderText = inputField.placeholder as TextMeshProUGUI;
            if (placeholderText != null)
            {
                userInput = placeholderText.text;
            }
        }

        // Fallback to InputField text if placeholder is empty
        if (string.IsNullOrEmpty(userInput))
        {
            userInput = inputField.text;
        }

        if (!string.IsNullOrEmpty(userInput))
        {
            Debug.Log("Text obtained from Activate method: " + userInput);
            StartCoroutine(GeneratePrompt(userInput));
        }
        else
        {
            Debug.LogWarning("Failed to get text content, ensure input field has text");
        }
    }

    void ShowInputField()
    {
        inputField.gameObject.SetActive(true);
        inputField.text = "";
        inputField.ActivateInputField();
        isTyping = true;
    }

    // Use String.Format instead of JsonUtility to ensure correct JSON formatting
    IEnumerator GeneratePrompt(string userInput)
    {
        string requestJson = string.Format("{{\"model\":\"gpt-4\",\"temperature\":0.7,\"max_tokens\":150,\"messages\":[{{\"role\":\"system\",\"content\":\"{0}\"}},{{\"role\":\"user\",\"content\":\"{1}\"}}]}}",
            EscapeJsonString(SYSTEM_PROMPT),
            EscapeJsonString(userInput));

        Debug.Log("Sent JSON: " + requestJson);

        UnityWebRequest request = new UnityWebRequest(TEXT_API_URL, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(requestJson);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + API_KEY);

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Prompt error: " + request.error);
            Debug.LogError("Response: " + request.downloadHandler.text);
            yield break;
        }

        string responseText = request.downloadHandler.text;
        Debug.Log("Full response: " + responseText);
        
        string prompt = ExtractMessageFromJsonImproved(responseText);
        
        if (string.IsNullOrEmpty(prompt))
        {
            Debug.LogError("Failed to extract prompt from response");
            yield break;
        }
        
        Debug.Log("Generated image prompt: " + prompt);
        StartCoroutine(GenerateImage(prompt));
    }

    IEnumerator GenerateImage(string prompt)
    {
        string requestJson = string.Format("{{\"model\":\"dall-e-3\",\"prompt\":\"{0}\",\"n\":1,\"size\":\"1024x1024\"}}",
            EscapeJsonString(prompt));

        Debug.Log("Sent image request JSON: " + requestJson);

        UnityWebRequest request = new UnityWebRequest(IMAGE_API_URL, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(requestJson);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + API_KEY);

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Image error: " + request.error);
            Debug.LogError("Response: " + request.downloadHandler.text);
            yield break;
        }

        string responseText = request.downloadHandler.text;
        Debug.Log("Full image response: " + responseText);
        
        string imageUrl = ExtractImageUrlFromJsonImproved(responseText);
        
        if (string.IsNullOrEmpty(imageUrl))
        {
            Debug.LogError("Failed to extract image URL from response");
            yield break;
        }
        
        Debug.Log("Image URL: " + imageUrl);
        StartCoroutine(LoadImage(imageUrl));
    }

    IEnumerator LoadImage(string url)
    {
        UnityWebRequest www = UnityWebRequestTexture.GetTexture(url);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Image load error: " + www.error);
            yield break;
        }

        Texture2D texture = ((DownloadHandlerTexture)www.downloadHandler).texture;
        
        if (imageDisplay != null)
        {
            imageDisplay.texture = texture;
        }
        
        if (imagePlane != null)
        {
            Renderer planeRenderer = imagePlane.GetComponent<Renderer>();
            if (planeRenderer != null)
            {
                Material mat = new Material(Shader.Find("Transparent/Diffuse"));
                mat.mainTexture = texture;
                Color color = mat.color;
                color.a = 0.6f;
                mat.color = color;
                planeRenderer.material = mat;
                
                Debug.Log("Successfully applied image to Image plane with 60% transparency");
            }
            else
            {
                Debug.LogError("Image plane missing Renderer component");
            }
        }
        else
        {
            Debug.LogWarning("Image plane not set - cannot display in 3D space");
        }
    }

    // JSON string escaping helper
    string EscapeJsonString(string str)
    {
        if (string.IsNullOrEmpty(str))
            return "";
        
        return str.Replace("\\", "\\\\")
                 .Replace("\"", "\\\"")
                 .Replace("\n", "\\n")
                 .Replace("\r", "\\r")
                 .Replace("\t", "\\t")
                 .Replace("\b", "\\b")
                 .Replace("\f", "\\f");
    }

    // Improved JSON parsing for OpenAI response
    string ExtractMessageFromJsonImproved(string json)
    {
        try {
            Debug.Log("Attempting to parse JSON response:");
            int maxLogLength = Mathf.Min(json.Length, 500);
            Debug.Log(json.Substring(0, maxLogLength) + (json.Length > maxLogLength ? "..." : ""));
            
            int choicesIndex = json.IndexOf("\"choices\"");
            if (choicesIndex < 0) {
                Debug.LogError("No choices field in response");
                return "";
            }
            
            int messageIndex = json.IndexOf("\"message\"", choicesIndex);
            if (messageIndex < 0) {
                Debug.LogError("No message field in response");
                return "";
            }
            
            int contentIndex = json.IndexOf("\"content\"", messageIndex);
            if (contentIndex < 0) {
                Debug.LogError("No content field in response");
                return "";
            }
            
            int valueStartIndex = json.IndexOf(":", contentIndex) + 1;
            while (valueStartIndex < json.Length && (json[valueStartIndex] == ' ' || json[valueStartIndex] == '\n' || json[valueStartIndex] == '\r' || json[valueStartIndex] == '\t'))
                valueStartIndex++;
                
            if (valueStartIndex >= json.Length || json[valueStartIndex] != '"') {
                Debug.LogError("Content value not starting with quote");
                return "";
            }
            
            valueStartIndex++;
            int valueEndIndex = valueStartIndex;
            bool escaped = false;
            
            while (valueEndIndex < json.Length) {
                char c = json[valueEndIndex];
                
                if (escaped) {
                    escaped = false;
                } else if (c == '\\') {
                    escaped = true;
                } else if (c == '"') {
                    break;
                }
                
                valueEndIndex++;
            }
            
            if (valueEndIndex >= json.Length) {
                Debug.LogError("Missing closing quote for content");
                return "";
            }
            
            string content = json.Substring(valueStartIndex, valueEndIndex - valueStartIndex);
            content = content.Replace("\\\"", "\"").Replace("\\n", "\n").Replace("\\r", "\r")
                           .Replace("\\t", "\t").Replace("\\\\", "\\");
                           
            Debug.Log("Extracted content: " + content);
            return content;
        }
        catch (System.Exception e) {
            Debug.LogError("Content extraction error: " + e.Message + "\n" + e.StackTrace);
            return "";
        }
    }

    string ExtractImageUrlFromJsonImproved(string json)
    {
        try {
            Debug.Log("Attempting to parse image URL from JSON:");
            int maxLogLength = Mathf.Min(json.Length, 500);
            Debug.Log(json.Substring(0, maxLogLength) + (json.Length > maxLogLength ? "..." : ""));
            
            int dataIndex = json.IndexOf("\"data\"");
            if (dataIndex < 0) {
                Debug.LogError("No data field in response");
                return "";
            }
            
            int urlIndex = json.IndexOf("\"url\"", dataIndex);
            if (urlIndex < 0) {
                Debug.LogError("No url field in response");
                return "";
            }
            
            int valueStartIndex = json.IndexOf(":", urlIndex) + 1;
            while (valueStartIndex < json.Length && (json[valueStartIndex] == ' ' || json[valueStartIndex] == '\n' || json[valueStartIndex] == '\r' || json[valueStartIndex] == '\t'))
                valueStartIndex++;
                
            if (valueStartIndex >= json.Length || json[valueStartIndex] != '"') {
                Debug.LogError("URL value not starting with quote");
                return "";
            }
            
            valueStartIndex++;
            int valueEndIndex = json.IndexOf("\"", valueStartIndex);
            
            if (valueEndIndex < 0) {
                Debug.LogError("Missing closing quote for URL");
                return "";
            }
            
            string url = json.Substring(valueStartIndex, valueEndIndex - valueStartIndex);
            Debug.Log("Extracted URL: " + url);
            return url;
        }
        catch (System.Exception e) {
            Debug.LogError("URL extraction error: " + e.Message + "\n" + e.StackTrace);
            return "";
        }
    }
}