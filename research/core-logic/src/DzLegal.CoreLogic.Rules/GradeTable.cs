using DzLegal.CoreLogic.Contracts;

namespace DzLegal.CoreLogic.Rules;

/// <summary>
/// From the declared effect to the effect the app shows (rules/GRADES.md): the grade table first, then the origin rule.
/// "To check" is a mark, never a weaker effect: a grade D item that declares a block or a warning comes out as a warning
/// with the mark, and its questions and listed documents keep their effect and carry the mark too.
/// </summary>
public static class GradeTable
{
    /// <summary>The grade of an item: its grading's grade, or D when it has none (or one the table does not know).</summary>
    public static string GradeOf(Grading? grading) =>
        grading?.Grade is Grades.A or Grades.B or Grades.C ? grading.Grade : Grades.D;

    /// <param name="declared">What the record's item says should happen for this case.</param>
    /// <param name="grade">"A" to "D".</param>
    /// <param name="usesProposedSlot">True when a slot the finding used was proposed by the reader and not confirmed.</param>
    public static (FindingEffect Effect, bool ToCheck) Apply(FindingEffect declared, string grade, bool usesProposedSlot)
    {
        // Only grade A may block; every other grade turns a block into a warning. Warn, ask and none are never changed.
        var effect = declared == FindingEffect.BlockCleanExport && grade != Grades.A ? FindingEffect.Warn : declared;

        // A value the lawyer has not confirmed never blocks, whatever the grade.
        if (effect == FindingEffect.BlockCleanExport && usesProposedSlot)
        {
            effect = FindingEffect.Warn;
        }

        return (effect, grade is not (Grades.A or Grades.B or Grades.C));
    }
}
