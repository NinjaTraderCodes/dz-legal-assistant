using System.Text.Json;
using DzLegal.Law.Corpus;

if (args.Length != 1)
{
    Console.Error.WriteLine("Usage: dotnet run --project tools/law-corpus/Validate -- <corpus-directory>");
    return 2;
}

try
{
    var corpus = CorpusLoader.Load(args[0]);
    var summary = corpus.Codes.Select(code => new
    {
        codeId = code.Ledger.Id,
        scope = code.Ledger.Scope.Kind,
        complete = code.Ledger.Scope.Complete,
        articles = code.Content.Articles.Count,
        versions = code.Content.Articles.Sum(a => a.Versions.Count),
        countsByState = code.Report.CountsByState,
        gaps = code.Report.Inventory.Gaps,
    });
    Console.WriteLine(JsonSerializer.Serialize(summary, new JsonSerializerOptions { WriteIndented = true }));
    return 0;
}
catch (CorpusValidationException error)
{
    foreach (var item in error.Errors)
    {
        Console.Error.WriteLine(item);
    }

    return 1;
}
