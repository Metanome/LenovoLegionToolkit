using System;

namespace LenovoLegionToolkit.Lib.Scripting;

public sealed record ScriptResult(
    string? Output,
    object? ReturnValue,
    string? Error,
    TimeSpan Elapsed
);
