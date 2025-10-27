using Maenifold.Utils;
using NUnit.Framework;

namespace Maenifold.Tests;

public class MarkdownWriterTests
{
    // RTM: NORMALIZE-HYPHEN-COMPRESSION-001 - Test coverage for hyphen compression

    [Test]
    public void NormalizeConcept_DoubleSpace_CompressesToSingleHyphen()
    {
        var result = MarkdownWriter.NormalizeConcept("API  Gateway");
        Assert.That(result, Is.EqualTo("api-gateway"));
    }

    [Test]
    public void NormalizeConcept_DoubleUnderscore_CompressesToSingleHyphen()
    {
        var result = MarkdownWriter.NormalizeConcept("user__profile");
        Assert.That(result, Is.EqualTo("user-profile"));
    }

    [Test]
    public void NormalizeConcept_MultipleSlashes_CompressesToSingleHyphen()
    {
        var result = MarkdownWriter.NormalizeConcept("auth / / token");
        Assert.That(result, Is.EqualTo("auth-token"));
    }

    [Test]
    public void NormalizeConcept_MixedSeparators_CompressesToSingleHyphen()
    {
        var result = MarkdownWriter.NormalizeConcept("API  __  //  Gateway");
        Assert.That(result, Is.EqualTo("api-gateway"));
    }

    [Test]
    public void NormalizeConcept_LeadingTrailingSeparators_TrimsHyphens()
    {
        var result = MarkdownWriter.NormalizeConcept("  concept  ");
        Assert.That(result, Is.EqualTo("concept"));
    }

    [Test]
    public void NormalizeConcept_OnlySeparators_ReturnsEmpty()
    {
        var result = MarkdownWriter.NormalizeConcept("   ___  ///  ");
        Assert.That(result, Is.EqualTo(string.Empty));
    }

    [Test]
    public void NormalizeConcept_SingleSeparators_WorksAsExpected()
    {
        var result = MarkdownWriter.NormalizeConcept("REST API Gateway");
        Assert.That(result, Is.EqualTo("rest-api-gateway"));
    }

    [Test]
    public void NormalizeConcept_NoSeparators_WorksAsExpected()
    {
        var result = MarkdownWriter.NormalizeConcept("Authentication");
        Assert.That(result, Is.EqualTo("authentication"));
    }

    [Test]
    public void NormalizeConcept_TripleHyphens_CompressesToSingleHyphen()
    {
        var result = MarkdownWriter.NormalizeConcept("test---concept");
        Assert.That(result, Is.EqualTo("test-concept"));
    }

    [Test]
    public void NormalizeConcept_ManyConsecutiveHyphens_CompressesToSingleHyphen()
    {
        var result = MarkdownWriter.NormalizeConcept("test----------concept");
        Assert.That(result, Is.EqualTo("test-concept"));
    }
}
