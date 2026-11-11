using System.Text.Json;
using FluentAssertions;
using Hive.Infrastructure.Services;

namespace Hive.Tests.Infrastructure.Services;

/// <summary>
/// Tests for AtlassianDocumentText: Jira rich text to Markdown.
/// </summary>
public class AtlassianDocumentTextTests
{
    [Fact]
    public void ToMarkdown_KeepsParagraphsHeadingsAndLineBreaks()
    {
        var text = Convert("""
            { "type": "doc", "version": 1, "content": [
              { "type": "heading", "attrs": { "level": 2 }, "content": [ { "type": "text", "text": "Context" } ] },
              { "type": "paragraph", "content": [
                { "type": "text", "text": "First line", "marks": [ { "type": "strong" } ] },
                { "type": "hardBreak" },
                { "type": "text", "text": "second line" } ] },
              { "type": "paragraph", "content": [ { "type": "text", "text": "Next paragraph" } ] }
            ] }
            """);

        text.Should().Be("## Context\n\n**First line**\nsecond line\n\nNext paragraph");
    }

    [Fact]
    public void ToMarkdown_WritesListsAsDashesAndNumbers()
    {
        var text = Convert("""
            { "type": "doc", "version": 1, "content": [
              { "type": "bulletList", "content": [
                { "type": "listItem", "content": [ { "type": "paragraph", "content": [ { "type": "text", "text": "One" } ] } ] },
                { "type": "listItem", "content": [ { "type": "paragraph", "content": [ { "type": "text", "text": "Two" } ] } ] } ] },
              { "type": "orderedList", "content": [
                { "type": "listItem", "content": [ { "type": "paragraph", "content": [ { "type": "text", "text": "First" } ] } ] },
                { "type": "listItem", "content": [ { "type": "paragraph", "content": [ { "type": "text", "text": "Second" } ] } ] } ] }
            ] }
            """);

        text.Should().Be("- One\n- Two\n\n1. First\n2. Second");
    }

    [Fact]
    public void ToMarkdown_KeepsMentionsLinksAndTablesAndDropsMedia()
    {
        var text = Convert("""
            { "type": "doc", "version": 1, "content": [
              { "type": "paragraph", "content": [
                { "type": "text", "text": "Ask " },
                { "type": "mention", "attrs": { "id": "1", "text": "@Alice" } },
                { "type": "text", "text": " about " },
                { "type": "inlineCard", "attrs": { "url": "https://example.com/doc" } } ] },
              { "type": "mediaSingle", "content": [ { "type": "media", "attrs": { "id": "x", "type": "file" } } ] },
              { "type": "table", "content": [
                { "type": "tableRow", "content": [
                  { "type": "tableHeader", "content": [ { "type": "paragraph", "content": [ { "type": "text", "text": "Env" } ] } ] },
                  { "type": "tableHeader", "content": [ { "type": "paragraph", "content": [ { "type": "text", "text": "Status" } ] } ] } ] },
                { "type": "tableRow", "content": [
                  { "type": "tableCell", "content": [ { "type": "paragraph", "content": [ { "type": "text", "text": "prod" } ] } ] },
                  { "type": "tableCell", "content": [ { "type": "paragraph", "content": [ { "type": "text", "text": "ok" } ] } ] } ] } ] }
            ] }
            """);

        text.Should().Be("Ask @Alice about https://example.com/doc\n\n| Env | Status |\n| --- | --- |\n| prod | ok |");
    }

    [Fact]
    public void ToMarkdown_WritesTaskListsCodeBlocksAndQuotes()
    {
        var text = Convert("""
            { "type": "doc", "version": 1, "content": [
              { "type": "heading", "attrs": { "level": 3 }, "content": [ { "type": "text", "text": "Acceptance criteria" } ] },
              { "type": "taskList", "content": [
                { "type": "taskItem", "attrs": { "state": "TODO" }, "content": [ { "type": "text", "text": "Saves the draft" } ] },
                { "type": "taskItem", "attrs": { "state": "DONE" }, "content": [ { "type": "text", "text": "Shows a toast" } ] } ] },
              { "type": "codeBlock", "attrs": { "language": "json" }, "content": [ { "type": "text", "text": "{ \"a\": 1 }" } ] },
              { "type": "blockquote", "content": [ { "type": "paragraph", "content": [
                { "type": "text", "text": "Use " },
                { "type": "text", "text": "the API", "marks": [ { "type": "code" } ] } ] } ] }
            ] }
            """);

        text.Should().Be("### Acceptance criteria\n\n- [ ] Saves the draft\n- [x] Shows a toast\n\n```json\n{ \"a\": 1 }\n```\n\n> Use `the API`");
    }

    [Fact]
    public void ToMarkdown_WithNullOrEmptyDocument_IsEmpty()
    {
        Convert("null").Should().BeEmpty();
        Convert("""{ "type": "doc", "version": 1, "content": [] }""").Should().BeEmpty();
    }

    private static string Convert(string json) => AtlassianDocumentText.ToMarkdown(JsonDocument.Parse(json).RootElement);
}
