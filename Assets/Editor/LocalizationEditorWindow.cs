#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;

public class LocalizationEditorWindow : EditorWindow
{
    #region Fields

    private string relativeJsonPath = "Localization/localization.json";
    private LocalizationData data = new LocalizationData();

    private Vector2 scrollPosHeader;
    private Vector2 scrollPosGrid;
    private float firstColWidth = 320f;
    private float cellWidth = 300f;
    private float rowHeight = 22f;

    private string newLanguageName = "";
    //private string newDefaultWord = "";
    private string filterText = "";
    private int filterLanguageIndex = 0;

    private LocalizationManager detectedManager;
    private string fallbackDefaultName = "English";

    private bool isDirty = false;

    private static bool _isReopening = false;
    private static string _reopenJsonCache = null;

    #endregion


    #region Menu

    [MenuItem("Window/Localization/Editor")]
    public static void ShowWindow()
    {
        var win = GetWindow<LocalizationEditorWindow>();
        win.UpdateTitle();
        win.Show();
    }

    #endregion


    #region Init

    private void OnEnable()
    {
        DetectManagerAndPath();
        SafeLoad();
        UpdateTitle();
    }

    private void DetectManagerAndPath()
    {
        detectedManager = FindObjectOfType<LocalizationManager>();
        if (detectedManager != null)
        {
            relativeJsonPath = detectedManager.relativeJsonPath;
            if (!string.IsNullOrWhiteSpace(detectedManager.defaultLanguageName))
                fallbackDefaultName = detectedManager.defaultLanguageName;
        }
    }

    private string FullPath
    {
        get
        {
            string projectPath = Application.dataPath;
            return Path.Combine(projectPath, "..", relativeJsonPath).Replace("\\", "/");
        }
    }

    private void UpdateTitle()
    {
        titleContent = new GUIContent(isDirty ? "Localization Grid *" : "Localization Grid");
    }

    private void MarkDirty()
    {
        if (!isDirty)
        {
            isDirty = true;
            UpdateTitle();
            Repaint();
        }
    }

    #endregion


    #region IO

    private void SafeLoad()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FullPath));
            if (File.Exists(FullPath))
            {
                var json = File.ReadAllText(FullPath);
                var loaded = JsonUtility.FromJson<LocalizationData>(json);
                data = loaded ?? new LocalizationData();

                if (data.languages.Count == 0)
                {
                    data.languages.Add(new LanguageInfo { name = fallbackDefaultName });
                    data.words.Add(new WordColumn());
                }

                NormalizeLengths();
                isDirty = false;
                UpdateTitle();
            }
            else
            {
                if (data.languages.Count == 0)
                {
                    data.languages.Add(new LanguageInfo { name = fallbackDefaultName });
                    data.words.Add(new WordColumn());
                }

                NormalizeLengths();
                Save();
            }

            if (filterLanguageIndex < 0 || filterLanguageIndex >= data.languages.Count)
                filterLanguageIndex = 0;
        }
        catch
        {
            data = new LocalizationData();
            isDirty = false;
            UpdateTitle();
        }
    }

    private void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FullPath));
            var json = JsonUtility.ToJson(data, true);
            File.WriteAllText(FullPath, json);
            AssetDatabase.Refresh();

            isDirty = false;
            UpdateTitle();
            Repaint();
        }
        catch { }
    }

    private void NormalizeLengths()
    {
        while (data.words.Count < data.languages.Count)
            data.words.Add(new WordColumn());

        int maxLen = 0;
        for (int l = 0; l < data.languages.Count; l++)
            maxLen = Mathf.Max(maxLen, data.words[l].items.Count);

        for (int l = 0; l < data.languages.Count; l++)
            while (data.words[l].items.Count < maxLen)
                data.words[l].items.Add("__MISSING__");
    }

    #endregion


    #region GUI

    private void OnGUI()
    {
        DrawToolbar();
        EditorGUILayout.Space(4);
        DrawHeader();
        DrawGrid();

        UpdateTitle();

        float padX = 10f;
        float padY = 8f;

        string leftText = "Created by Batu Özçamlık";
        string rightText = " |   www.batuozcamlik.com";

        GUIStyle leftStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleRight,
            fontSize = 13,
            fontStyle = FontStyle.Bold
        };

        GUIStyle rightStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleRight,
            fontSize = 13,
            fontStyle = FontStyle.Normal
        };

      
        Vector2 sizeLeft = leftStyle.CalcSize(new GUIContent(leftText));
        Vector2 sizeRight = rightStyle.CalcSize(new GUIContent(rightText));

        float totalW = sizeLeft.x + sizeRight.x;
        float lineH = Mathf.Max(sizeLeft.y, sizeRight.y);

        Rect area = new Rect(
            position.width - totalW - padX,
            position.height - lineH - padY,
            totalW,
            lineH
        );

    
        GUI.Label(
            new Rect(area.x, area.y, sizeLeft.x, lineH),
            leftText,
            leftStyle
        );

      
        Color prev = GUI.color;
        GUI.color = new Color(1f, 1f, 1f, 0.40f);

        GUI.Label(
            new Rect(area.x + sizeLeft.x, area.y, sizeRight.x, lineH),
            rightText,
            rightStyle
        );

        GUI.color = prev;

      
    }



    private void DrawToolbar()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            EditorGUILayout.LabelField("JSON Path:", GUILayout.Width(70));
            string newPath = EditorGUILayout.TextField(relativeJsonPath, GUILayout.MinWidth(220));
            if (newPath != relativeJsonPath)
                relativeJsonPath = newPath;

            if (GUILayout.Button("Yükle", EditorStyles.toolbarButton, GUILayout.Width(60)))
                SafeLoad();

            if (GUILayout.Button("Kaydet", EditorStyles.toolbarButton, GUILayout.Width(60)))
                Save();

            GUILayout.FlexibleSpace();

            int clamped = Mathf.Clamp(
                data.selectedLanguageIndex,
                0,
                Mathf.Max(0, data.languages.Count - 1)
            );

            int newSel = EditorGUILayout.Popup(
                "Seçili Dil",
                clamped,
                GetLangNames(),
                GUILayout.Width(280)
            );

            if (newSel != data.selectedLanguageIndex)
            {
                data.selectedLanguageIndex = newSel;
                Repaint();
            }

            if (isDirty)
            {
                GUILayout.Space(12);
                GUILayout.Label("⚠ Kaydedilmedi", EditorStyles.boldLabel, GUILayout.Width(120));
            }
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            using (new EditorGUI.DisabledScope(data.languages.Count == 0))
            {
                string current = (data.languages.Count > 0) ? (data.languages[0].name ?? "") : "";
                string edited = EditorGUILayout.TextField("Default Dil Adı (Index 0)", current, GUILayout.MinWidth(220));
                if (data.languages.Count > 0 && edited != current)
                {
                    data.languages[0].name = edited;
                    MarkDirty();
                    Repaint();
                }
            }
            GUILayout.FlexibleSpace();
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            newLanguageName = EditorGUILayout.TextField("Yeni Dil Adı", newLanguageName);
            if (GUILayout.Button("Dili Ekle", GUILayout.Width(100)))
            {
                if (!string.IsNullOrWhiteSpace(newLanguageName))
                {
                    data.languages.Add(new LanguageInfo { name = newLanguageName.Trim() });
                    var newCol = new WordColumn();
                    int wordCount = data.words.Count > 0 ? data.words[0].items.Count : 0;

                    for (int i = 0; i < wordCount; i++)
                        newCol.items.Add("__MISSING__");

                    data.words.Add(newCol);
                    newLanguageName = "";
                    MarkDirty();

                    if (filterLanguageIndex < 0 || filterLanguageIndex >= data.languages.Count)
                        filterLanguageIndex = 0;

                    Repaint();
                }
            }

            GUILayout.FlexibleSpace();

            using (new EditorGUI.DisabledScope(data.languages.Count == 0))
            {
                filterLanguageIndex = EditorGUILayout.Popup(
                    new GUIContent("Filtre Dili"),
                    Mathf.Clamp(
                        filterLanguageIndex,
                        0,
                        Mathf.Max(0, data.languages.Count - 1)
                    ),
                    GetLangNames(),
                    GUILayout.Width(260)
                );

                filterText = EditorGUILayout.TextField(
                    $"Filtre ({SafeLangName(filterLanguageIndex)})",
                    filterText,
                    GUILayout.Width(280)
                );
            }
        }
    }

    private string SafeLangName(int idx)
    {
        if (data.languages == null || data.languages.Count == 0) return "—";
        if (idx < 0 || idx >= data.languages.Count) idx = 0;
        return data.languages[idx].name;
    }

    private string[] GetLangNames()
    {
        var arr = new string[data.languages.Count];
        for (int i = 0; i < arr.Length; i++)
            arr[i] = $"[{i}] {data.languages[i].name}";
        return arr;
    }

    private void DrawHeader()
    {
        float headerH = 24f;
        Rect rect = GUILayoutUtility.GetRect(0, headerH, GUILayout.ExpandWidth(true));

        string defName = (data.languages.Count > 0) ? data.languages[0].name : "Default";

        GUI.Box(new Rect(rect.x, rect.y, firstColWidth, rect.height), $"Default (Index 0) - {defName}", EditorStyles.helpBox);

        float headerWidth = data.languages.Count > 1
            ? (data.languages.Count - 1) * cellWidth
            : 0f;

        Rect svRect = new Rect(rect.x + firstColWidth, rect.y, rect.width - firstColWidth, rect.height);
        Rect contentRect = new Rect(0, 0, headerWidth, rect.height);

        scrollPosHeader = GUI.BeginScrollView(
            svRect,
            scrollPosHeader,
            contentRect,
            false, false,
            GUIStyle.none,
            GUIStyle.none
        );

        float x = 0f;
        for (int l = 1; l < data.languages.Count; l++)
        {
            Rect hRect = new Rect(x, 0, cellWidth - 4, rect.height);
            GUI.Box(hRect, GUIContent.none, EditorStyles.helpBox);

            var nameRect = new Rect(hRect.x + 6, hRect.y + 2, hRect.width - 60, hRect.height - 4);
            EditorGUI.LabelField(nameRect, $"[{l}] {data.languages[l].name}");

            var delRect = new Rect(hRect.xMax - 50, hRect.y + 2, 44, hRect.height - 4);
            using (new EditorGUI.DisabledScope(l == 0))
            {
                if (GUI.Button(delRect, "Sil"))
                {
                    if (EditorUtility.DisplayDialog(
                        "Dili sil",
                        $"'{data.languages[l].name}' dilini silmek istediğine emin misin?",
                        "Evet, sil", "Vazgeç"))
                    {
                        RemoveLanguageAt(l);
                        MarkDirty();
                        scrollPosHeader.x = Mathf.Clamp(scrollPosHeader.x, 0, Mathf.Max(0, headerWidth - svRect.width));
                        GUI.EndScrollView();
                        Repaint();
                        return;
                    }
                }
            }
            x += cellWidth;
        }

        GUI.EndScrollView();
    }

    private void DrawGrid()
    {
        int rowCount = data.words.Count > 0 ? data.words[0].items.Count : 0;

        List<int> visibleRows = new List<int>(rowCount);
        string filter = filterText?.Trim() ?? "";
        int langIdx = Mathf.Clamp(filterLanguageIndex, 0, Mathf.Max(0, data.languages.Count - 1));

        for (int r = 0; r < rowCount; r++)
        {
            bool show = true;
            if (!string.IsNullOrEmpty(filter))
            {
                var col = (langIdx < data.words.Count) ? data.words[langIdx].items : null;
                string w = (col != null && r < col.Count) ? (col[r] ?? "") : "";
                if (w.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0)
                    show = false;
            }
            if (show) visibleRows.Add(r);
        }

        float gridHeight = Mathf.Max(position.height - 120f, 100f);
        Rect totalRect = GUILayoutUtility.GetRect(0, gridHeight, GUILayout.ExpandWidth(true));

        Rect leftRect = new Rect(totalRect.x, totalRect.y, firstColWidth, totalRect.height);
        GUI.Box(leftRect, GUIContent.none);

        Rect rightRect = new Rect(totalRect.x + firstColWidth, totalRect.y, totalRect.width - firstColWidth, totalRect.height);
        float contentWidth = Mathf.Max((data.languages.Count - 1) * cellWidth, rightRect.width + 1);
        float contentHeight = visibleRows.Count * rowHeight;

        using (new GUI.GroupScope(leftRect))
        {
            var v = new Vector2(0, scrollPosGrid.y);
            scrollPosGrid = new Vector2(scrollPosGrid.x, v.y);

            Rect view = new Rect(0, 0, leftRect.width - 16, contentHeight);
            Rect scroll = new Rect(0, 0, view.width, contentHeight);
            var vScroll = GUI.BeginScrollView(new Rect(0, 0, leftRect.width, leftRect.height), v, scroll, false, true);

            for (int i = 0; i < visibleRows.Count; i++)
            {
                int r = visibleRows[i];
                float btnW = 48f;

                Rect rowRect = new Rect(4, i * rowHeight, view.width - 8 - btnW - 4, rowHeight - 2);

                string oldVal = data.words[0].items[r];
                string newVal = EditorGUI.TextField(rowRect, oldVal);
                if (newVal != oldVal)
                {
                    data.words[0].items[r] = newVal;
                    MarkDirty();
                }

                Rect delRect = new Rect(rowRect.xMax + 4, rowRect.y, btnW, rowHeight - 2);
                if (GUI.Button(delRect, "Sil"))
                {
                    if (EditorUtility.DisplayDialog(
                        "Kelimeyi sil",
                        $"Bu satırı (index {r}) tüm dillerden silmek istediğine emin misin?",
                        "Evet, sil", "Vazgeç"))
                    {
                        RemoveWordAt(r);
                        MarkDirty();
                        GUI.EndScrollView();
                        Repaint();
                        return;
                    }
                }
            }

            GUI.EndScrollView();
            scrollPosGrid = new Vector2(scrollPosGrid.x, vScroll.y);
        }

        GUI.BeginGroup(rightRect);
        var h = new Vector2(scrollPosHeader.x, scrollPosGrid.y);
        Rect viewRect = new Rect(0, 0, rightRect.width, rightRect.height);
        Rect contentRect = new Rect(0, 0, contentWidth, contentHeight);
        var newScroll = GUI.BeginScrollView(viewRect, h, contentRect, true, true);

        float x = 0f;
        for (int l = 1; l < data.languages.Count; l++)
        {
            float colX = x;
            for (int i = 0; i < visibleRows.Count; i++)
            {
                int r = visibleRows[i];
                Rect cellRect = new Rect(colX + 4, i * rowHeight, cellWidth - 8, rowHeight - 2);

                string oldVal = data.words[l].items[r];
                string newVal = EditorGUI.TextField(cellRect, oldVal);
                if (newVal != oldVal)
                {
                    data.words[l].items[r] = newVal;
                    MarkDirty();
                }
            }
            x += cellWidth;
        }

        GUI.EndScrollView();
        GUI.EndGroup();

        scrollPosHeader = new Vector2(newScroll.x, scrollPosHeader.y);
        scrollPosGrid = new Vector2(scrollPosGrid.x, newScroll.y);

        EditorGUILayout.Space(6);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Yeni Kelime Ekle"))
            {
                if (data.words.Count == 0) data.words.Add(new WordColumn());
                data.words[0].items.Add("YeniKelime");

                for (int l = 1; l < data.languages.Count; l++)
                    data.words[l].items.Add("__MISSING__");

                MarkDirty();
                Repaint();
            }

            if (GUILayout.Button("Kaydet"))
                Save();

            GUILayout.FlexibleSpace();
        }
    }

    #endregion


    #region Helpers

    private void RemoveLanguageAt(int langIndex)
    {
        if (langIndex <= 0 || langIndex >= data.languages.Count) return;

        data.languages.RemoveAt(langIndex);
        if (langIndex < data.words.Count)
            data.words.RemoveAt(langIndex);

        if (data.selectedLanguageIndex == langIndex)
            data.selectedLanguageIndex = 0;
        else if (langIndex < data.selectedLanguageIndex)
            data.selectedLanguageIndex = Mathf.Max(0, data.selectedLanguageIndex - 1);

        if (filterLanguageIndex >= data.languages.Count)
            filterLanguageIndex = Mathf.Clamp(filterLanguageIndex, 0, Mathf.Max(0, data.languages.Count - 1));

        NormalizeLengths();
    }

    private void RemoveWordAt(int wordIndex)
    {
        if (wordIndex < 0) return;

        for (int l = 0; l < data.languages.Count; l++)
        {
            var col = data.words[l].items;
            if (wordIndex < col.Count)
                col.RemoveAt(wordIndex);
        }
        NormalizeLengths();
    }

    #endregion


    #region Closing

    private void OnDestroy()
    {
        if (_isReopening || EditorApplication.isCompiling)
            return;

        if (!isDirty)
            return;

        string currentJson = JsonUtility.ToJson(data, true);

        int choice = EditorUtility.DisplayDialogComplex(
            "Kaydedilmemiş Değişiklikler",
            "Kaydetmeden kapatmak üzeresin. Ne yapmak istersin?",
            "Kaydet ve Kapat",
            "Kapat (Kaydetme)",
            "İptal"
        );

        if (choice == 0)
        {
            Save();
        }
        else if (choice == 1)
        {
        }
        else
        {
            _isReopening = true;
            _reopenJsonCache = currentJson;

            EditorApplication.delayCall += () =>
            {
                var win = GetWindow<LocalizationEditorWindow>();
                if (!string.IsNullOrEmpty(_reopenJsonCache))
                {
                    try
                    {
                        var restored = JsonUtility.FromJson<LocalizationData>(_reopenJsonCache);
                        win.data = restored ?? new LocalizationData();
                        win.NormalizeLengths();
                        win.isDirty = true;
                        win.UpdateTitle();
                    }
                    catch { }
                }

                _reopenJsonCache = null;
                _isReopening = false;
                win.Show();
                win.Focus();
            };
        }
    }

    #endregion
}
#endif
