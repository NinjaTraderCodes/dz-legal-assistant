// No browser, network or model behavior: only the process name is a test fixture.
if (args.Length != 1)
{
    return 2;
}

var deadline = DateTimeOffset.UtcNow.AddSeconds(30);
while (DateTimeOffset.UtcNow < deadline && !File.Exists(args[0]))
{
    await Task.Delay(50);
}

return 0;
