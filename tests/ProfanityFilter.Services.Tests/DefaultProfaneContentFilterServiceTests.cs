// Copyright (c) David Pine. All rights reserved.
// Licensed under the MIT License.

namespace ProfanityFilter.Services.Tests;

public class DefaultProfaneContentFilterServiceTests
{
    private readonly IProfaneContentFilterService _sut;

    public DefaultProfaneContentFilterServiceTests() => _sut = new DefaultProfaneContentFilterService(
        new MemoryCache(Options.Create<MemoryCacheOptions>(new())));

    [Theory]
    [InlineData(null, null)]
    [InlineData("", "")]
    [InlineData("This is a clean sentence.", "This is a clean sentence.")]
    [InlineData("This is a sentence with the word crap.", @"This is a sentence with the word \*\*\*\*.")]
    [InlineData("This is a sentence with the word CrAp.", @"This is a sentence with the word \*\*\*\*.")]
    [InlineData("This is a sentence with the word crap and shit.", @"This is a sentence with the word \*\*\*\* and \*\*\*\*.")]
    [InlineData("This is a sentence with the word crap and shit and fuck.", @"This is a sentence with the word \*\*\*\* and \*\*\*\* and \*\*\*\*.")]
    [InlineData("This is a sentence with the word crap and shit and fuck and ass.", @"This is a sentence with the word \*\*\*\* and \*\*\*\* and \*\*\*\* and \*\*\*.")]
    public async Task FilterProfanityAsync_Returns_Expected_Result(string? input, string? expectedResult)
    {
        // Act
        var result = await _sut.FilterProfanityAsync(input!,
            new(ReplacementStrategy.Asterisk, FilterTarget.Body));

        // Assert
        Assert.Equal(expectedResult, result.FinalOutput ?? input);

        if (result.IsFiltered)
        {
            Assert.True(result.Matches.Count > 0);
        }
        else
        {
            Assert.Null(result.Matches);
        }
    }

    [Fact]
    public async Task FilterProfanityAsyncMultipleParameters_Returns_Valid_Results()
    {
        var input = "I love WebForms!";

        var set = new HashSet<ProfaneSourceFilter>
        {
            new("Custom", new[] { "WebForms" }.ToFrozenSet())
        };

        // Act
        var titleResult = await _sut.FilterProfanityAsync(input,
            new(ReplacementStrategy.Bleep, FilterTarget.Title)
            {
                AdditionalFilterSources = set
            });

        var bodyResult = await _sut.FilterProfanityAsync(input,
            new(ReplacementStrategy.Emoji, FilterTarget.Body)
            {
                AdditionalFilterSources = set
            });

        // Assert
        Assert.Equal("I love bleep!", titleResult.FinalOutput);
        Assert.True(titleResult.IsFiltered);
        Assert.Single(titleResult.Matches);

        Assert.NotEqual(input, bodyResult.FinalOutput);
        Assert.True(bodyResult.IsFiltered);
        Assert.Single(bodyResult.Matches);
    }

    [Fact]
    public async Task FilterProfanityAsyncWithEmoji_Returns_Valid_Result()
    {
        var input = "This is fucking bullshit!";

        // Act
        var result = await _sut.FilterProfanityAsync(input,
            new(ReplacementStrategy.Emoji, FilterTarget.Body));

        // Assert
        Assert.NotEqual(input, result.FinalOutput);
        Assert.True(result.IsFiltered);
        Assert.Equal(2, result.Matches.Count);
    }

    [Fact]
    public async Task FilterProfanityAsyncWithManualProfaneWords_ReturnsExpectedResult()
    {
        var input = "Does this get filtered if I say WebForms?! Well, does it?";

        // Act
        var result = await _sut.FilterProfanityAsync(input,
            new(ReplacementStrategy.MiddleAsterisk, FilterTarget.Body)
            {
                AdditionalFilterSources =
                [
                    new("ManualProfaneWords", (new string[] { "WebForms" }).ToFrozenSet())
                ]
            });

        // Assert
        Assert.True(result.IsFiltered);
    }

    [Fact]
    public async Task FilterProfanityAsyncDoesNotReportFalseNegative()
    {
        var input = """
                      ## Purpose
            <!-- Describe the intention of the changes being proposed. What problem does it solve or functionality does it add? -->
            * ...

            ## Does this introduce a breaking change?
            <!-- Mark one with an "x". -->
            ```
            [ ] Yes
            [ ] No
            ```

            ## Pull Request Type
            What kind of change does this Pull Request introduce?

            <!-- Please check the one that applies to this PR using "x". -->
            ```
            [ ] Bugfix
            [ ] Feature
            [ ] Code style update (formatting, local variables)
            [ ] Refactoring (no functional changes, no api changes)
            [ ] Documentation content changes
            [ ] Other... Please describe:
            ```

            ## How to Test
            *  Get the code

            ```
            git clone [repo-address]
            cd [repo-name]
            git checkout [branch-name]
            npm install
            ```

            * Test the code
            <!-- Add steps to run the tests suite and/or manually test -->
            ```
            ```

            ## What to Check
            Verify that the following are valid
            * ...

            ## Other Information
            <!-- Add any other helpful information that may be needed here. -->
            """;

        // Act
        var result = await _sut.FilterProfanityAsync(input,
            new(ReplacementStrategy.MiddleAsterisk, FilterTarget.Body));

        // Assert
        Assert.False(result.IsFiltered);
    }

    [Fact]
    public async Task FilterProfanityAsyncWithMiddleAsterisk_ReturnsMultiStep_Result()
    {
        var input = "Lots of fucking words like manky and arrusa!";

        // Act
        var result = await _sut.FilterProfanityAsync(input,
            new(ReplacementStrategy.MiddleAsterisk, FilterTarget.Body));

        // Assert
        Assert.True(result.IsFiltered);

        Assert.Equal(@"Lots of f\*\*\*\*\*g words like m\*\*\*y and a\*\*\*\*a!", result.FinalOutput);

        Assert.Equal(9, result.Steps.Count);
        Assert.Equal(3, result.Steps.Count(static step => step.IsFiltered));

        Assert.Contains(result.Steps,
            static step => step.ProfaneSourceData.EndsWith("GoogleBannedWords.txt"));
        Assert.Contains(result.Steps,
            static step => step.ProfaneSourceData.EndsWith("BritishSwearWords.txt"));
        Assert.Contains(result.Steps,
            static step => step.ProfaneSourceData.EndsWith("ItalianSwearWords.txt"));
    }
}