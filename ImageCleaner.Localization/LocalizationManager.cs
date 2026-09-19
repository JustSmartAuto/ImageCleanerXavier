using System;

namespace ImageCleaner.Localization;

/// <summary>
/// 兼容外观：内部委托给 I18n 字典，保留旧调用点。
/// </summary>
public static class LocalizationManager
{
    public static string CurrentLanguage => I18n.Language == "en" ? "en-US" : "zh-CN";

    public static void Initialize(string language)
    {
        SetLanguage(language);
    }

    public static void SetLanguage(string language)
    {
        I18n.SetLanguage(language == "en-US" ? "en" : "zh");
    }

    public static string Get(string key)
    {
        return I18n.T(key);
    }

    public static event Action LanguageChanged
    {
        add { I18n.LanguageChanged += value; }
        remove { I18n.LanguageChanged -= value; }
    }
}
