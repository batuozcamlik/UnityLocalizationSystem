using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour
{

    public string kelime;
    public int dilIndex;

    [ContextMenu("String")]
    public void String()
    {
        Debug.Log(LocalizationManager.Instance.Get(kelime));
    }
    [ContextMenu("ChangeDil")]
    public void ChangeDil()
    {
        LocalizationManager.Instance.SetLanguage(dilIndex);
    }

}
