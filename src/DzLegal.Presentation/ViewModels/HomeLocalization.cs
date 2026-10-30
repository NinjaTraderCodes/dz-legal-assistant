using System.ComponentModel;
using System.Globalization;

namespace DzLegal.Presentation.ViewModels;

/// <summary>Temporary Arabic/English fallbacks over the shared application localization.</summary>
public sealed class HomeLocalization : INotifyPropertyChanged
{
    public HomeLocalization(Loc? source = null)
    {
        Source = source ?? new Loc();
        // A closed screen must not be retained by the shared localization.
        var weak = new WeakReference<HomeLocalization>(this);
        PropertyChangedEventHandler? handler = null;
        handler = (sender, args) =>
        {
            if (weak.TryGetTarget(out var current)) current.PropertyChanged?.Invoke(current, args);
            else if (sender is Loc localization) localization.PropertyChanged -= handler;
        };
        Source.PropertyChanged += handler;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public Loc Source { get; }
    public string Language => Source.Language;
    public bool IsRightToLeft => Source.IsRightToLeft;
    public CultureInfo Culture => Source.Culture;
    public string this[string key]
    {
        get
        {
            var existing = Source[key];
            if (existing != $"[{key}]") return existing;
            return Entries.TryGetValue(key, out var text) ? Language == "ar" ? text.Arabic : text.English : string.Empty;
        }
    }
    public string Format(string key, params object?[] values) => string.Format(Culture, this[key], values);
    public void SetLanguage(string language) => Source.SetLanguage(language);
    public static IReadOnlyDictionary<string, (string Arabic, string English)> Entries { get; } =
        new Dictionary<string, (string, string)>(StringComparer.Ordinal)
        {
            ["Home"] = ("الرئيسية", "Home"),
            ["BackToCase"] = ("العودة إلى القضية", "Back to case"),
            ["StartNew"] = ("عريضة جديدة", "New petition"),
            ["StartOpen"] = ("فتح قضية…", "Open a case…"),
            ["StartSample"] = ("جرّب قضية مثال", "Try a sample case"),
            ["StartSampleDesc"] = ("قضية وهمية ترافقك خطوة بخطوة من الوقائع إلى التصدير.", "A fictitious case that walks you step by step from the facts to export."),
            ["StartResume"] = ("استئناف الجلسة السابقة", "Resume previous session"),
            ["StartResumeDesc"] = ("وُجدت نسخة تلقائية من آخر جلسة عمل.", "An automatic copy of your last session was found."),
            ["StartResumeLater"] = ("ليس الآن", "Not now"),
            ["StartRecent"] = ("القضايا الأخيرة", "Recent cases"),
            ["StartRecentEmpty"] = ("لا توجد قضايا حديثة بعد. ستظهر هنا القضايا التي تحفظها أو تفتحها.", "No recent cases yet. Cases you save or open will appear here."),
            ["RecentMissing"] = ("لم يعد الملف موجودا في مكانه: {0}", "The file is no longer where it was: {0}"),
            ["RecentRemove"] = ("إزالة من القائمة", "Remove from list"),
            ["RecentClear"] = ("مسح القائمة", "Clear list"),
            ["RecentOpened"] = ("آخر فتح: {0}", "Last opened: {0}"),
            ["StartTour"] = ("جولة تعريفية", "Take the tour"),
            ["HomeHelp"] = ("المساعدة", "Help"),
            ["HomeLibrary"] = ("المكتبة القانونية", "Law library"),
            ["CompleteProfileBanner"] = ("أكمل ملفك المهني ليُملأ التوقيع تلقائيا في كل عريضة.", "Complete your profile so the signature fills itself in every petition."),
            ["CompleteProfileAction"] = ("إكمال الملف", "Complete profile"),
            ["HomeComingLater"] = ("غير متاح بعد", "Not available yet"),
            ["HomeWorkspaceUnavailable"] = ("فتح مساحة العمل غير متاح حاليا.", "The drafting workspace is currently unavailable."),
            ["HomeRecentUnavailable"] = ("فتح القضايا الأخيرة غير متاح بعد. استخدم «فتح قضية…» لاختيار الملف.", "Recent-file opening is not available yet. Use “Open a case…” to choose the file."),
            ["HomeClearConfirm"] = ("هل تريد مسح قائمة القضايا الأخيرة؟ لن تُحذف الملفات.", "Clear the recent cases list? Your files will not be deleted."),
            ["HomeCancel"] = ("إلغاء", "Cancel"),
            ["HomeStorageFailed"] = ("تعذّر حفظ القائمة. أعد المحاولة بعد التحقق من صلاحية الكتابة.", "Could not save the list. Check write permission and try again."),
            ["HomeActionFailed"] = ("تعذّر فتح مساحة العمل أو الملف. لم تُستبدل القضية من هذه الشاشة.", "Could not open the workspace or file. This screen did not replace your case."),
            ["HomeLanguage"] = ("لغة الرئيسية", "Home language"),
            ["HomeArabic"] = ("العربية", "العربية"),
            ["HomeEnglish"] = ("English", "English"),
        };
}
