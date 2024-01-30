﻿// Copyright (c) David Pine. All rights reserved.
// Licensed under the MIT License.

namespace ProfanityFilter.Services;

internal record class ProfaneFilter(
    string SourceName,
    FrozenSet<string> ProfaneWords)
{
    public string RegexPattern { get; } =
        $"\\b({string.Join('|', ProfaneWords)})\\b";
}
