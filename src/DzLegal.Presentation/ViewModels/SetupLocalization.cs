using System.ComponentModel;
using System.Globalization;

namespace DzLegal.Presentation.ViewModels;

/// <summary>Temporary Arabic/English fallbacks over the shared application localization.</summary>
public sealed class SetupLocalization : INotifyPropertyChanged
{
    public SetupLocalization(Loc? source = null)
    {
        Source = source ?? new Loc();
        // A closed screen must not be retained by the shared localization.
        var weak = new WeakReference<SetupLocalization>(this);
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
            ["SetupTitle"] = ("مرحبا بك في مساعد صياغة العرائض", "Welcome to DZ Legal Assistant"),
            ["SetupIntro"] = ("أدخل بياناتك المهنية مرة واحدة؛ تُملأ بها كل عريضة جديدة تلقائيا. تُحفظ على هذا الحاسوب فقط.", "Enter your professional details once; they fill every new petition automatically. They are stored on this computer only."),
            ["SetupStepLawyer"] = ("أنت", "You"),
            ["SetupStepOffice"] = ("المكتب والوثائق", "Office and documents"),
            ["SetupStepPrefs"] = ("التفضيلات", "Preferences"),
            ["SetupLater"] = ("لاحقا", "Later"),
            ["SetupFinish"] = ("ابدأ", "Start"),
            ["SetupProgress"] = ("الخطوة {0} من {1}", "Step {0} of {1}"),
            ["ProfileTitle"] = ("الملف المهني والتفضيلات", "Profile and settings"),
            ["ProfileNameAr"] = ("الاسم واللقب (بالعربية)", "Full name (Arabic)"),
            ["ProfileNameFr"] = ("الاسم واللقب (بالفرنسية، اختياري)", "Full name (French, optional)"),
            ["ProfileGender"] = ("صيغة المخاطبة", "Form of address"),
            ["ProfileGenderMale"] = ("الأستاذ", "Counsel (man)"),
            ["ProfileGenderFemale"] = ("الأستاذة", "Counsel (woman)"),
            ["ProfileCouncil"] = ("المجلس القضائي المسجّل به", "Judicial council where you are registered"),
            ["ProfileCouncilHint"] = ("مثال: الجزائر، وهران، قسنطينة", "e.g. Alger, Oran, Constantine"),
            ["ProfileBar"] = ("منظمة المحامين", "Bar"),
            ["ProfileBarHint"] = ("مثال: منظمة المحامين لناحية الجزائر", "e.g. Bar of the Algiers region"),
            ["ProfileSupremeCourt"] = ("معتمد(ة) لدى المحكمة العليا ومجلس الدولة", "Accredited at the Supreme Court and the Council of State"),
            ["ProfileSupremeCourtMale"] = ("معتمد لدى المحكمة العليا ومجلس الدولة", "Accredited at the Supreme Court and the Council of State"),
            ["ProfileSupremeCourtFemale"] = ("معتمدة لدى المحكمة العليا ومجلس الدولة", "Accredited at the Supreme Court and the Council of State"),
            ["ProfileStanding"] = ("الصفة كما تُكتب في العريضة", "Standing as written in the petition"),
            ["ProfileStandingNote"] = ("تُقترح تلقائيا؛ يمكنك تعديلها.", "Suggested automatically; you can edit it."),
            ["ProfileStandingRestore"] = ("استعادة الصيغة المقترحة", "Restore suggestion"),
            ["ProfileAddress"] = ("عنوان المكتب", "Office address"),
            ["ProfilePhone"] = ("الهاتف", "Phone"),
            ["ProfileEmail"] = ("البريد الإلكتروني", "Email"),
            ["ProfileFirm"] = ("اسم المكتب (اختياري)", "Firm name (optional)"),
            ["ProfileLogo"] = ("شعار المكتب (اختياري)", "Firm logo (optional)"),
            ["ProfileLogoNote"] = ("محفوظ للاستعمال لاحقا؛ لا يظهر على العرائض حاليا.", "Kept for later use; it does not appear on petitions for now."),
            ["ProfileLogoChoose"] = ("اختر صورة", "Choose image"),
            ["ProfileLogoRemove"] = ("إزالة", "Remove"),
            ["DefaultsPlace"] = ("مكان التحرير المعتاد", "Usual place of drafting"),
            ["DefaultsCouncil"] = ("المجلس القضائي المعتاد", "Usual judicial council"),
            ["DefaultsCourt"] = ("المحكمة المعتادة", "Usual court"),
            ["DefaultsWatermark"] = ("وسم المسودة مفعّل افتراضيا", "Draft mark on by default"),
            ["DefaultsFileName"] = ("تسمية الملفات المصدّرة", "Naming of exported files"),
            ["FileNamingCurrent"] = ("نوع الوثيقة - المدعي (الحالي)", "Document type - plaintiff (current)"),
            ["FileNamingParties"] = ("المدعي ضد المدعى عليه - التاريخ", "Plaintiff v defendant - date"),
            ["PrefsLanguage"] = ("لغة الواجهة", "Interface language"),
            ["PrefsTheme"] = ("المظهر", "Appearance"),
            ["PrefsThemeLight"] = ("فاتح", "Light"),
            ["PrefsThemeDark"] = ("داكن", "Dark"),
            ["PrefsEngine"] = ("محرك الصياغة الافتراضي", "Default drafting engine"),
            ["PrefsEngineLocal"] = ("النموذج المحلي", "Local model"),
            ["PrefsBackground"] = ("تحليل الوقائع في الخلفية عند مغادرة خطوة الوقائع", "Analyse the facts in the background when leaving the Facts step"),
            ["PrefsSaveFolder"] = ("مجلد حفظ القضايا", "Folder for saved cases"),
            ["PrefsSaveFolderChoose"] = ("اختر مجلدا", "Choose folder"),
            ["PrivacyNote"] = ("تُحفظ هذه البيانات على هذا الحاسوب فقط ولا تُرسل إلى أي جهة.", "These details are stored on this computer only and are never sent anywhere."),
            ["ProfileAppliesToNew"] = ("تُطبّق التغييرات على القضايا الجديدة فقط؛ لا تتغير القضايا المحفوظة.", "Changes apply to new cases only; saved cases are not changed."),
            ["ProfileSave"] = ("حفظ", "Save"),
            ["ProfileSaved"] = ("حُفظ الملف المهني.", "Profile saved."),
            ["ProfileUnsaved"] = ("تعديلات ملفك المهني غير محفوظة. هل تريد حفظها؟", "Your profile changes are not saved. Save them?"),
            ["ProfileDiscard"] = ("تجاهل التعديلات", "Discard changes"),
            ["ProfileExport"] = ("تصدير الملف المهني", "Export profile"),
            ["ProfileImport"] = ("استيراد الملف المهني", "Import profile"),
            ["ProfileImportConfirm"] = ("سيحلّ الملف المستورد محلّ ملفك المهني وتفضيلاتك الحالية. هل تريد المتابعة؟", "The imported file will replace your current profile and preferences. Continue?"),
            ["ProfileImportInvalid"] = ("تعذّرت قراءة الملف المهني. لم يتغير شيء.", "The profile file could not be read. Nothing was changed."),
            ["ProfileImportSummary"] = ("الاسم: {0} · المجلس: {1} · اللغة: {2}", "Name: {0} · Council: {1} · Language: {2}"),
            ["ProfileExported"] = ("صُدّر الملف المهني.", "Profile exported."),
            ["ProfileImported"] = ("استُورد الملف المهني. اضغط حفظ لتأكيد التغييرات.", "Profile imported. Press Save to keep the changes."),
            ["ImageTooLarge"] = ("الصورة أكبر من 2 ميغابايت. اختر صورة أصغر.", "The image is larger than 2 MB. Choose a smaller one."),
            ["ImageInvalid"] = ("تعذّرت قراءة الصورة. اختر ملف PNG أو JPG.", "The image could not be read. Choose a PNG or JPG file."),
            ["EmailInvalid"] = ("صيغة البريد الإلكتروني غير صحيحة.", "This email address does not look right."),
            ["ProfileNameTooLong"] = ("الاسم أطول من 120 حرفا. يرجى تقصيره قبل الحفظ.", "The name exceeds 120 characters. Shorten it before saving."),
            ["ProfileSaveFailed"] = ("تعذّر الحفظ. تحقق من صلاحية الكتابة ومساحة القرص ثم أعد المحاولة. بقيت تعديلاتك هنا.", "Could not save. Check write permission and disk space, then retry. Your edits remain here."),
            ["SetupMarkerFailed"] = ("تعذّر تسجيل إتمام الإعداد. أعد المحاولة، أو افتح مساحة العمل؛ قد يظهر الإعداد في المرة القادمة.", "Could not remember setup completion. Retry, or open the workspace; setup may appear next time."),
            ["SetupContinueAnyway"] = ("فتح مساحة العمل", "Open workspace"),
            ["ProfilePreferenceFailed"] = ("تعذّر حفظ اختيار اللغة أو المظهر. سيُعاد المحاولة عند الحفظ.", "Could not remember the language or appearance. Saving will retry."),
            ["ProfileValidationFailed"] = ("راجع الحقول المبيّنة قبل الحفظ.", "Check the highlighted fields before saving."),
            ["ProfileClose"] = ("إغلاق", "Close"),
            ["ProfileContinue"] = ("متابعة", "Continue"),
        };
}
