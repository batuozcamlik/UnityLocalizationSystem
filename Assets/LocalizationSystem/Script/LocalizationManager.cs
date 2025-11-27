using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class LocalizationManager : MonoBehaviour
{
    #region Fields

    public static LocalizationManager Instance;

    public string relativeJsonPath = "localization.json";

    public bool createSampleIfMissing = true;
    public string defaultLanguageName = "English";

    [SerializeField] private LocalizationData data = new LocalizationData();

    private Dictionary<string, int> defaultIndexMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
    public int DefaultLanguageIndex => 0;
    public int SelectedLanguageIndex => data.selectedLanguageIndex;
    public LocalizationData Data => data;

    #endregion

    #region Path

    public string FullPath
    {
        get
        {
#if UNITY_EDITOR
            string resourcesPath = Path.Combine(Application.dataPath, "Resources");
            string combinedPath = Path.Combine(resourcesPath, relativeJsonPath).Replace("\\", "/");

            return combinedPath;
#else
            return Path.Combine(Application.persistentDataPath, relativeJsonPath);
#endif
        }
    }

    #endregion

    #region Lifecycle

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadOrCreate();
        RebuildDefaultIndexMap();
    }

    #endregion

    #region Load / Save

    public void LoadOrCreate()
    {
        if (File.Exists(FullPath))
        {
            try
            {
                string json = File.ReadAllText(FullPath);
                data = JsonUtility.FromJson<LocalizationData>(json) ?? new LocalizationData();
            }
            catch
            {
                data = new LocalizationData();
            }
        }
        else
        {
            string resourceName = Path.GetFileNameWithoutExtension(relativeJsonPath);
            TextAsset textAsset = Resources.Load<TextAsset>(resourceName);

            if (textAsset != null)
            {
                data = JsonUtility.FromJson<LocalizationData>(textAsset.text) ?? new LocalizationData();
                Save();
            }
            else
            {
                if (createSampleIfMissing)
                {
                    CreateSampleData();
                    Save();
                }
                else
                {
                    data = new LocalizationData();
                    AddLanguage(defaultLanguageName);
                }
            }
        }

        if (data.selectedLanguageIndex < 0 || data.selectedLanguageIndex >= data.languages.Count)
            data.selectedLanguageIndex = 0;

        NormalizeLengths();
    }

    public void Save()
    {
        try
        {
            var json = JsonUtility.ToJson(data, true);

            string directory = Path.GetDirectoryName(FullPath);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllText(FullPath, json);

#if UNITY_EDITOR
            AssetDatabase.Refresh();
#endif
        }
        catch (Exception e)
        {
            Debug.LogError("Localization Save Error: " + e.Message);
        }
    }

    private void CreateSampleData()
    {
        data = new LocalizationData();

        data.languages.Add(new LanguageInfo { name = defaultLanguageName });
        data.languages.Add(new LanguageInfo { name = "Turkish" });
        data.languages.Add(new LanguageInfo { name = "Deutsch" });
        data.languages.Add(new LanguageInfo { name = "Japanese" });

        data.words.Add(new WordColumn
        {
            items = new List<string> { "Hello", "How are you", "Quit", "Settings", "Play" }
        });

        data.words.Add(new WordColumn
        {
            items = new List<string> { "Merhaba", "Nasılsın", "Çıkış", "Ayarlar", "Oyna" }
        });

        data.words.Add(new WordColumn
        {
            items = new List<string> { "Hallo", "Wie geht's", "Beenden", "Einstellungen", "Spielen" }
        });

        data.words.Add(new WordColumn
        {
            items = new List<string> { "こんにちは", "お元気ですか", "終了", "設定", "プレイ" }
        });

        data.selectedLanguageIndex = 0;
    }

    #endregion

    #region Data Helpers

    private void RebuildDefaultIndexMap()
    {
        defaultIndexMap.Clear();
        defaultIndexMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        if (data.words.Count == 0) return;

        var def = data.words[DefaultLanguageIndex].items;
        for (int i = 0; i < def.Count; i++)
        {
            string raw = def[i] ?? "";
            string w = raw.Trim();

            if (!string.IsNullOrEmpty(w) && !defaultIndexMap.ContainsKey(w))
                defaultIndexMap[w] = i;
        }
    }

    public void NormalizeLengths()
    {
        while (data.words.Count < data.languages.Count)
            data.words.Add(new WordColumn());

        int maxLen = 0;
        for (int l = 0; l < data.languages.Count; l++)
            maxLen = Mathf.Max(maxLen, data.words[l].items.Count);

        for (int l = 0; l < data.languages.Count; l++)
        {
            while (data.words[l].items.Count < maxLen)
                data.words[l].items.Add("__MISSING__");
        }
    }

    #endregion

    #region Language / Word API

    public void SetLanguage(int languageIndex)
    {
        if (languageIndex < 0 || languageIndex >= data.languages.Count)
            return;

        data.selectedLanguageIndex = languageIndex;
        Save();
    }

    public void AddLanguage(string name)
    {
        data.languages.Add(new LanguageInfo { name = name });

        int wordCount = data.words.Count > 0 ? data.words[0].items.Count : 0;
        var newCol = new WordColumn();

        for (int i = 0; i < wordCount; i++)
            newCol.items.Add("__MISSING__");

        data.words.Add(newCol);

        Save();
    }

    public int AddWordToDefault(string newWord)
    {
        if (data.words.Count == 0)
            data.words.Add(new WordColumn());

        data.words[0].items.Add(newWord);
        int newIndex = data.words[0].items.Count - 1;

        for (int l = 1; l < data.languages.Count; l++)
        {
            while (data.words.Count <= l)
                data.words.Add(new WordColumn());

            data.words[l].items.Add("__MISSING__");
        }

        Save();

        if (!string.IsNullOrEmpty(newWord) && !defaultIndexMap.ContainsKey(newWord))
            defaultIndexMap.Add(newWord, newIndex);

        return newIndex;
    }

    public void SetWordAt(int languageIndex, int wordIndex, string value)
    {
        if (languageIndex < 0 || languageIndex >= data.languages.Count) return;
        if (wordIndex < 0 || wordIndex >= data.words[languageIndex].items.Count) return;

        data.words[languageIndex].items[wordIndex] = value ?? "";
        Save();
    }

    #endregion

    #region Lookup

    public string Get(string wordInDefault)
    {
        if (string.IsNullOrEmpty(wordInDefault))
            return "";

        string searchKey = wordInDefault.Trim();

        if (!defaultIndexMap.TryGetValue(searchKey, out int idx))
        {
            idx = FindIndexInDefault(searchKey);
            if (idx == -1)
                return wordInDefault;

            defaultIndexMap[searchKey] = idx;
        }

        return GetByIndex(idx);
    }

    public string GetByIndex(int wordIndex)
    {
        int lang = data.selectedLanguageIndex;
        if (lang < 0 || lang >= data.languages.Count) return "";
        if (wordIndex < 0 || wordIndex >= data.words[lang].items.Count) return "";

        string val = data.words[lang].items[wordIndex];
        if (string.IsNullOrEmpty(val) || val == "__MISSING__")
        {
            if (wordIndex >= 0 && wordIndex < data.words[DefaultLanguageIndex].items.Count)
                return data.words[DefaultLanguageIndex].items[wordIndex];
            return "";
        }
        return val;
    }

    public int FindIndexInDefault(string wordInDefault)
    {
        if (data.words.Count == 0) return -1;

        string searchKey = (wordInDefault ?? "").Trim();

        var def = data.words[DefaultLanguageIndex].items;
        for (int i = 0; i < def.Count; i++)
        {
            string item = (def[i] ?? "").Trim();
            if (string.Equals(item, searchKey, StringComparison.OrdinalIgnoreCase))
                return i;
        }
        return -1;
    }

    public List<(int index, string word)> SearchInDefault(string contains, bool caseSensitive = false)
    {
        var result = new List<(int, string)>();
        if (data.words.Count == 0) return result;

        var def = data.words[DefaultLanguageIndex].items;
        StringComparison cmp = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

        for (int i = 0; i < def.Count; i++)
        {
            var w = def[i] ?? "";
            if (w.IndexOf(contains ?? "", cmp) >= 0)
                result.Add((i, w));
        }
        return result;
    }

    #endregion
}

#region Custom Inspector

#if UNITY_EDITOR
[CustomEditor(typeof(LocalizationManager))]
[CanEditMultipleObjects]
public class LocalizationInspector : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GUILayout.Space(10);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        GUILayout.Space(5);
        GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = 14;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        EditorGUILayout.LabelField("Created by Batu Özçamlık", titleStyle);

        GUILayout.Space(5);

        GUIStyle textStyle = new GUIStyle(GUI.skin.label);
        textStyle.wordWrap = true;
        textStyle.richText = true;

        GUIStyle signatureStyle = new GUIStyle(GUI.skin.label);
        signatureStyle.alignment = TextAnchor.MiddleRight;
        signatureStyle.fontStyle = FontStyle.Italic;
        EditorGUILayout.LabelField("www.batuozcamlik.com", signatureStyle);
    }
}
#endif
#endregion