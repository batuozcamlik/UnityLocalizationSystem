using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class Test : MonoBehaviour
{
    #region Runtime Functions
    public void GetString(string word)
    {
        if (LocalizationManager.Instance == null)
        {
            Debug.LogWarning("LocalizationManager instance not found.");
            return;
        }
        Debug.Log(LocalizationManager.Instance.Get(word));
    }


    public void ChangeDil(int index, string languageName)
    {
        if (LocalizationManager.Instance == null)
        {
            Debug.LogWarning("LocalizationManager instance not found.");
            return;
        }

        LocalizationManager.Instance.SetLanguage(index);

      
        Debug.Log($"Change Language : {languageName}");
    }
    #endregion
}

[CustomEditor(typeof(Test))]
public class TestCodeEditor : Editor
{
    #region Fields
    private string word = "Hello";
    private int languageIndex = 0;
    private string[] languageNames = new string[0];
    private string relativeJsonPath = "Localization/localization.json";
    #endregion

    #region Inspector GUI
    public override void OnInspectorGUI()
    {
        GUILayout.Space(5);

        GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        EditorGUILayout.LabelField("Test System", titleStyle);
        GUILayout.Space(8);

        Test testCode = (Test)target;
        RefreshLanguageList();

        EditorGUILayout.BeginHorizontal();
        word = EditorGUILayout.TextField(word, GUILayout.ExpandWidth(true));
        if (GUILayout.Button("Get", GUILayout.Width(70)))
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
            EditorGUILayout.LabelField("No languages found", GUILayout.ExpandWidth(true));
            EditorGUI.BeginDisabledGroup(true);
            if (GUILayout.Button("Change", GUILayout.Width(70))) { }
            EditorGUI.EndDisabledGroup();
        }
        else
        {
            int newIndex = EditorGUILayout.Popup(languageIndex, languageNames, GUILayout.ExpandWidth(true));
            if (newIndex != languageIndex)
                languageIndex = newIndex;

            if (GUILayout.Button("Change", GUILayout.Width(70)))
            {
                if (LocalizationManager.Instance == null)
                {
                    Debug.LogWarning("LocalizationManager instance not found. (ChangeDil call failed.)");
                }
                else
                {
                    string selectedLangName = languageNames[languageIndex];
                    testCode.ChangeDil(languageIndex, selectedLangName);
                }
            }
        }

        EditorGUILayout.EndHorizontal();

        GUILayout.Space(10);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        GUILayout.Space(5);

        GUIStyle footerStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12,
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold
        };
        EditorGUILayout.LabelField("Created by Batu Özçamlýk", footerStyle);
        GUILayout.Space(5);

        GUIStyle signatureStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleRight,
            fontStyle = FontStyle.Italic
        };
        EditorGUILayout.LabelField("www.batuozcamlik.com", signatureStyle);
        GUILayout.Space(5);
    }
    #endregion

    #region Language Handling
    private void RefreshLanguageList()
    {
        if (LocalizationManager.Instance != null && LocalizationManager.Instance.Data != null)
        {
            var langs = LocalizationManager.Instance.Data.languages;
            if (langs != null && langs.Count > 0)
            {
                EnsureLanguageNamesFrom(langs);
                if (languageIndex < 0 || languageIndex >= languageNames.Length)
                {
                    try
                    {
                        languageIndex = LocalizationManager.Instance.SelectedLanguageIndex;
                    }
                    catch { languageIndex = 0; }
                }
                return;
            }
        }

      
        string fullPath = Path.Combine(Application.dataPath, "..", relativeJsonPath).Replace("\\", "/");
        if (File.Exists(fullPath))
        {
            try
            {
                string json = File.ReadAllText(fullPath);
                var loaded = JsonUtility.FromJson<LocalizationData>(json);
                if (loaded != null && loaded.languages != null && loaded.languages.Count > 0)
                {
                    EnsureLanguageNamesFrom(loaded.languages);
                    if (languageIndex < 0 || languageIndex >= languageNames.Length)
                    {
                        languageIndex = Mathf.Clamp(loaded.selectedLanguageIndex, 0, languageNames.Length - 1);
                    }
                    return;
                }
            }
            catch { }
        }

        languageNames = new string[0];
    }

    private void EnsureLanguageNamesFrom(IList langs)
    {
        var list = langs as System.Collections.IList;
        if (list == null)
        {
            languageNames = new string[0];
            return;
        }

        languageNames = new string[list.Count];
        for (int i = 0; i < list.Count; i++)
        {
            object li = list[i];
            if (li == null)
            {
                languageNames[i] = $"Lang {i}";
                continue;
            }

            var type = li.GetType();
            var field = type.GetField("name");
            if (field != null)
            {
                var val = field.GetValue(li) as string;
                languageNames[i] = string.IsNullOrEmpty(val) ? $"Lang {i}" : val;
            }
            else
            {
                var prop = type.GetProperty("name");
                if (prop != null)
                {
                    var val = prop.GetValue(li, null) as string;
                    languageNames[i] = string.IsNullOrEmpty(val) ? $"Lang {i}" : val;
                }
                else
                {
                    languageNames[i] = $"Lang {i}";
                }
            }
        }
    }
    #endregion
}
