using System;
using System.Collections.Generic;

[Serializable]
public class LanguageInfo
{
    public string name;
}

[Serializable]
public class WordColumn
{
    public List<string> items = new List<string>(); 
}

[Serializable]
public class LocalizationData
{
    public List<LanguageInfo> languages = new List<LanguageInfo>(); 
    public List<WordColumn> words = new List<WordColumn>();         
    public int selectedLanguageIndex = 0;                            
}
