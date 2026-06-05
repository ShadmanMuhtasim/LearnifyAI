using Learnify.Core.Models;
using Learnify.Infrastructure.AI;
using Learnify.Infrastructure.AI.Providers;
using Xunit;

namespace Learnify.Tests;

public class QuizResponseParserTests
{
    [Fact]
    public void Parse_AcceptsRawValidJsonObject()
    {
        var result = QuizResponseParser.Parse("""
        {
          "title": "Hashing Quiz",
          "questions": [
            {
              "type": "MultipleChoice",
              "questionText": "What resolves hash collisions?",
              "options": ["Chaining", "Sorting", "Polling", "Paging"],
              "correctAnswer": "Chaining",
              "explanation": "Chaining stores colliding entries in a bucket list."
            }
          ]
        }
        """);

        Assert.Equal("Hashing Quiz", result.Title);
        var question = Assert.Single(result.Questions);
        Assert.Equal("MultipleChoice", question.Type);
        Assert.Equal("Chaining", question.CorrectAnswer);
    }

    [Fact]
    public void Parse_AcceptsRawQuestionArray()
    {
        var result = QuizResponseParser.Parse("""
        [
          {
            "type": "TrueFalse",
            "questionText": "Hash maps use keys.",
            "options": ["True", "False"],
            "correctAnswer": "true",
            "explanation": "Keys identify entries."
          }
        ]
        """);

        var question = Assert.Single(result.Questions);
        Assert.Equal("TrueFalse", question.Type);
        Assert.Equal("True", question.CorrectAnswer);
    }

    [Fact]
    public void Parse_AcceptsJsonInsideMarkdownFence()
    {
        var result = QuizResponseParser.Parse("""
        ```json
        {
          "questions": [
            {
              "type": "ShortAnswer",
              "questionText": "Name one collision strategy.",
              "options": [],
              "correctAnswer": "chaining",
              "explanation": "Separate chaining is a common strategy."
            }
          ]
        }
        ```
        """);

        var question = Assert.Single(result.Questions);
        Assert.Equal("ShortAnswer", question.Type);
        Assert.Equal("chaining", question.CorrectAnswer);
    }

    [Fact]
    public void Parse_ExtractsJsonWithLeadingAndTrailingText()
    {
        var result = QuizResponseParser.Parse("""
        Sure, here is the quiz:
        {
          "questions": [
            {
              "type": "FillInTheBlank",
              "questionText": "A hash map stores values by ____.",
              "options": [],
              "correctAnswer": "key",
              "explanation": "Keys map to values."
            }
          ]
        }
        Good luck!
        """);

        var question = Assert.Single(result.Questions);
        Assert.Equal("FillInTheBlank", question.Type);
        Assert.Equal("key", question.CorrectAnswer);
    }

    [Fact]
    public void Parse_IgnoresThinkBlockBeforeJson()
    {
        var result = QuizResponseParser.Parse("""
        <think>I should plan the quiz first and then output JSON.</think>
        {
          "questions": [
            {
              "type": "MultipleChoice",
              "questionText": "Which structure stores key-value pairs?",
              "choices": ["Hash Map", "Stack", "Queue"],
              "answer": "Hash Map",
              "explanation": "Hash maps associate keys with values."
            }
          ]
        }
        """);

        var question = Assert.Single(result.Questions);
        Assert.Equal("Which structure stores key-value pairs?", question.QuestionText);
        Assert.Equal("Hash Map", question.CorrectAnswer);
        Assert.Equal(3, question.Options.Count);
    }

    [Fact]
    public void Parse_MapsCommonAliases()
    {
        var result = QuizResponseParser.Parse("""
        {
          "questions": [
            {
              "type": "multiple_choice",
              "question": "Which method probes another slot on collision?",
              "choices": ["Open addressing", "Chaining"],
              "correct_answer": "Open addressing",
              "explanation": "Open addressing stores entries in the bucket array."
            }
          ]
        }
        """);

        var question = Assert.Single(result.Questions);
        Assert.Equal("MultipleChoice", question.Type);
        Assert.Equal("Which method probes another slot on collision?", question.QuestionText);
        Assert.Equal("Open addressing", question.CorrectAnswer);
    }

    [Fact]
    public void Parse_InvalidSchemaThrowsClearFailure()
    {
        var ex = Assert.Throws<AiQuizFormatException>(() =>
            QuizResponseParser.Parse("""
            {
              "questions": [
                {
                  "type": "MultipleChoice",
                  "questionText": "Incomplete options",
                  "options": ["Only one"],
                  "correctAnswer": "Only one"
                }
              ]
            }
            """));

        Assert.Contains("invalid quiz format", ex.Message);
        Assert.Contains("fewer than 2 options", ex.Reason);
        Assert.True(ex.ResponseLength > 0);
    }

    [Fact]
    public async Task Parse_AcceptsMockProviderQuizOutput()
    {
        var provider = new MockAiProvider();
        var response = await provider.CompleteAsync(
            "Generate a quiz with questions",
            new AiRequestOptions(),
            CancellationToken.None);

        var result = QuizResponseParser.Parse(response);

        Assert.NotEmpty(result.Questions);
        Assert.All(result.Questions, question =>
        {
            Assert.False(string.IsNullOrWhiteSpace(question.QuestionText));
            Assert.False(string.IsNullOrWhiteSpace(question.CorrectAnswer));
        });
    }
}
