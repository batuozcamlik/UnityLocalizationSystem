using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class Test : MonoBehaviour
{
    [Header("UI Settings")]
    public TextMeshProUGUI allWordsText;

    private IEnumerator Start()
    {
        yield return null;

        if (LocalizationManager.Instance == null)
        {
            Debug.LogWarning("LocalizationManager not found in the scene!");
            yield break;
        }

        PrintAllWords();
    }

    public void PrintAllWords()
    {
        if (allWordsText == null)
        {
            Debug.LogWarning("TextMeshPro object not assigned!");
            return;
        }

        if (LocalizationManager.Instance == null || LocalizationManager.Instance.Data == null)
            return;

        var manager = LocalizationManager.Instance;
        var data = manager.Data;

        if (data.words == null || data.words.Count == 0)
        {
            allWordsText.text = "List is empty or could not be loaded.";
            return;
        }

        int selectedIndex = manager.SelectedLanguageIndex;
        int defaultIndex = manager.DefaultLanguageIndex;

        if (selectedIndex < 0 || selectedIndex >= data.languages.Count) selectedIndex = 0;

        List<string> selectedList = data.words[selectedIndex].items;
        List<string> defaultList = data.words[defaultIndex].items;

        string selectedLangName = data.languages[selectedIndex].name;
        string defaultLangName = data.languages[defaultIndex].name;

        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"<b>--- TRANSLATION CHECK ---</b>");
        sb.AppendLine($"<color=yellow>Key ({defaultLangName})</color> => <color=green>Value ({selectedLangName})</color>");
        sb.AppendLine("-------------------------");

        int count = Mathf.Min(selectedList.Count, defaultList.Count);

        for (int i = 0; i < count; i++)
        {
            string key = defaultList[i];
            string value = selectedList[i];

            if (string.IsNullOrEmpty(key)) key = "[EMPTY]";
            if (string.IsNullOrEmpty(value)) value = "[EMPTY]";

            sb.AppendLine($"<b>[{i}]</b> {key}  =>  {value}");
        }

        if (selectedList.Count != defaultList.Count)
        {
            sb.AppendLine("\n<color=red>ERROR: Language lists are not of equal length!</color>");
        }

        allWordsText.text = sb.ToString();
    }

    public void GetString(string word)
    {
        if (LocalizationManager.Instance != null)
            Debug.Log(LocalizationManager.Instance.Get(word));
    }

    public void ChangeLanguage(int index, string languageName)
    {
        if (LocalizationManager.Instance == null) return;

        LocalizationManager.Instance.SetLanguage(index);
        Debug.Log($"Language Changed : {languageName}");

        PrintAllWords();
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(Test))]
public class TestCodeEditor : Editor
{
    private string word = "Hello";
    private int languageIndex = 0;
    private string[] languageNames = new string[0];
    private string jsonFileName = "localization.json";

    public override void OnInspectorGUI()
    {
        GUILayout.Space(5);

        GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        EditorGUILayout.LabelField("Test & Debug System", titleStyle);
        GUILayout.Space(8);

        Test testCode = (Test)target;
        RefreshLanguageList();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Text Output:", GUILayout.Width(80));
        testCode.allWordsText = (TextMeshProUGUI)EditorGUILayout.ObjectField(testCode.allWordsText, typeof(TextMeshProUGUI), true);
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(5);

        EditorGUILayout.BeginHorizontal();
        word = EditorGUILayout.TextField(word, GUILayout.ExpandWidth(true));
        if (GUILayout.Button("Get Log", GUILayout.Width(70)))
        {
            if (LocalizationManager.Instance == null)
                Debug.LogWarning("LocalizationManager instance not found.");
            else
                testCode.GetString(word);
        }
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(6);

        EditorGUILayout.BeginHorizontal();
        if (languageNames == null || languageNames.Length == 0)
        {
            EditorGUILayout.LabelField("Language not found", GUILayout.ExpandWidth(true));
        }
        else
        {
            int newIndex = EditorGUILayout.Popup(languageIndex, languageNames, GUILayout.ExpandWidth(true));
            if (newIndex != languageIndex)
                languageIndex = newIndex;

            if (GUILayout.Button("Change", GUILayout.Width(70)))
            {
                if (LocalizationManager.Instance != null)
                {
                    string selectedLangName = languageNames[languageIndex];
                    testCode.ChangeLanguage(languageIndex, selectedLangName);
                }
            }
        }
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(10);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        GUILayout.Space(5);
        EditorGUILayout.LabelField("Created by Batu Özçamlýk", new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 10 });
    }

    private void RefreshLanguageList()
    {
        if (LocalizationManager.Instance != null && LocalizationManager.Instance.Data != null)
        {
            var langs = LocalizationManager.Instance.Data.languages;
            if (langs != null && langs.Count > 0)
            {
                EnsureLanguageNamesFrom(langs);
                return;
            }
        }

        string fullPath = Path.Combine(Application.dataPath, "Resources", jsonFileName);

        if (File.Exists(fullPath))
        {
            try
            {
                string json = File.ReadAllText(fullPath);
                var loaded = JsonUtility.FromJson<LocalizationData>(json);
                if (loaded != null && loaded.languages != null && loaded.languages.Count > 0)
                {
                    EnsureLanguageNamesFrom(loaded.languages);
                    return;
                }
            }
            catch { }
        }

        languageNames = new string[0];
    }

    private void EnsureLanguageNamesFrom(IList langs)
    {
        if (langs == null) { languageNames = new string[0]; return; }

        languageNames = new string[langs.Count];
        for (int i = 0; i < langs.Count; i++)
        {
            object li = langs[i];
            if (li == null)
            {
                languageNames[i] = $"Lang {i}";
                continue;
            }

            var field = li.GetType().GetField("name");
            if (field != null)
            {
                var val = field.GetValue(li) as string;
                languageNames[i] = string.IsNullOrEmpty(val) ? $"Lang {i}" : val;
            }
            else
            {
                languageNames[i] = $"Lang {i}";
            }
        }
    }
}
#endif