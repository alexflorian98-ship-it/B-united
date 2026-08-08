namespace BUnited.Modules.Questionnaires.Application;

/// <summary>docs/PROMPT.md §35: "explicit questionnaire consent with versioning". A version
/// bump here requires re-consent from every user before they can start a new submission
/// (existing submissions are unaffected — consent is per-submission-start, not retroactive).</summary>
public static class QuestionnaireConsent
{
    public const string ConsentType = "questionnaire_data_processing";

    public const int CurrentVersion = 1;
}
