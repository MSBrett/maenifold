using Maenifold.Tools;
using NUnit.Framework;

namespace Maenifold.Tests;

public class ContentPreviewTests
{
    // RTM: BUILDCONTEXT-PREVIEW-UX-001 - Test coverage for sentence-aware content preview

    [Test]
    public void CreateSmartPreview_ShortContent_ReturnsFullContent()
    {
        var content = "Short content.";
        var result = GraphTools.CreateSmartPreview(content, 200, 50);
        Assert.That(result, Is.EqualTo(content));
    }

    [Test]
    public void CreateSmartPreview_WithCompleteSentence_TruncatesAtSentence()
    {
        var content = "First sentence. Second sentence. Third sentence that goes on and on.";
        var result = GraphTools.CreateSmartPreview(content, 30, 20);
        // With targetLength=30 and tolerance=20, maxLength=50
        // "First sentence. Second sentence." is 32 chars, fits within range
        Assert.That(result, Is.EqualTo("First sentence. Second sentence."));
    }

    [Test]
    public void CreateSmartPreview_NoSentenceBoundary_UsesParagraph()
    {
        var content = "First paragraph without ending\n\nSecond paragraph here";
        var result = GraphTools.CreateSmartPreview(content, 20, 15);
        Assert.That(result, Does.EndWith("..."));
        Assert.That(result, Does.Contain("First paragraph"));
    }

    [Test]
    public void CreateSmartPreview_NoParagraphBoundary_UsesWordBoundary()
    {
        var content = "ThisIsAVeryLongWordWithNoSpaces FollowedByMore Words";
        var result = GraphTools.CreateSmartPreview(content, 35, 10);
        Assert.That(result, Does.EndWith("..."));
        Assert.That(result, Does.Not.Contain("FollowedByMore"));
    }

    [Test]
    public void CreateSmartPreview_NoBoundaries_HardTruncates()
    {
        var content = "NoSpacesOrPunctuationHereJustOneVeryLongString";
        var result = GraphTools.CreateSmartPreview(content, 20, 5);
        Assert.That(result, Does.EndWith("..."));
        Assert.That(result.Length, Is.LessThanOrEqualTo(24)); // 20 + "..."
    }

    [Test]
    public void CreateSmartPreview_WithWikiLinks_PreservesLinks()
    {
        var content = "The [[authentication]] system uses [[JWT]] tokens. More content here.";
        var result = GraphTools.CreateSmartPreview(content, 60, 20);
        Assert.That(result, Does.Contain("[[authentication]]"));
        Assert.That(result, Does.Contain("[[JWT]]"));
    }

    [Test]
    public void CreateSmartPreview_MultipleShortSentences_TruncatesAtNaturalBoundary()
    {
        var content = "First. Second. Third. Fourth. Fifth. Sixth. Seventh.";
        var result = GraphTools.CreateSmartPreview(content, 25, 10);
        // Should include at least "First. Second. Third." (21 chars)
        Assert.That(result, Does.Contain("First."));
        Assert.That(result, Does.Contain("Second."));
    }

    [Test]
    public void CreateSmartPreview_SentenceEndingAtLimit_ReturnsFullSentence()
    {
        var content = "This is exactly fifty characters for testing now. And more after.";
        var result = GraphTools.CreateSmartPreview(content, 50, 10);
        Assert.That(result, Does.Contain("This is exactly fifty characters for testing now."));
        Assert.That(result, Does.Not.Contain("And more"));
    }
}
