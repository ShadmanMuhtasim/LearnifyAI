using Learnify.Application.DTOs;
using Learnify.Application.Services;
using Learnify.Core.Entities;
using Learnify.Core.Interfaces;
using Learnify.Core.Models;
using Moq;
using Xunit;

namespace Learnify.Tests;

public class QuizServiceTests
{
    [Fact]
    public async Task SubmitAttempt_ScoresSupportedQuestionTypesAndCalculatesPercentage()
    {
        var userId = Guid.NewGuid();
        var quiz = BuildQuiz(userId);
        var setup = BuildService(quiz);

        var result = await setup.Service.SubmitAttemptAsync(userId, quiz.Id, new SubmitQuizAttemptRequest
        {
            Answers = new List<SubmitQuizAnswerDto>
            {
                new() { QuestionId = quiz.Questions.ElementAt(0).Id, UserAnswer = "B" },
                new() { QuestionId = quiz.Questions.ElementAt(1).Id, UserAnswer = "true" },
                new() { QuestionId = quiz.Questions.ElementAt(2).Id, UserAnswer = " pyruvate " },
                new() { QuestionId = quiz.Questions.ElementAt(3).Id, UserAnswer = "  PROTON   GRADIENT " }
            }
        });

        Assert.NotNull(result);
        Assert.Equal(4, result.Score);
        Assert.Equal(4, result.TotalPoints);
        Assert.Equal(100m, result.Percentage);
        Assert.All(result.Answers, answer => Assert.True(answer.IsCorrect));
        Assert.Equal("ATP synthase uses a proton gradient.", result.Answers.Last().Explanation);
        Assert.Equal(4, setup.CapturedAttempt!.Answers.Count);
    }

    [Fact]
    public async Task SubmitAttempt_EmptyAndWrongAnswersScoreZero()
    {
        var userId = Guid.NewGuid();
        var quiz = BuildQuiz(userId);
        var setup = BuildService(quiz);

        var result = await setup.Service.SubmitAttemptAsync(userId, quiz.Id, new SubmitQuizAttemptRequest
        {
            Answers = new List<SubmitQuizAnswerDto>
            {
                new() { QuestionId = quiz.Questions.ElementAt(0).Id, UserAnswer = "" },
                new() { QuestionId = quiz.Questions.ElementAt(1).Id, UserAnswer = "False" }
            }
        });

        Assert.NotNull(result);
        Assert.Equal(0, result.Score);
        Assert.Equal(4, result.TotalPoints);
        Assert.Equal(0m, result.Percentage);
        Assert.Equal(4, result.Answers.Count);
        Assert.All(result.Answers, answer => Assert.False(answer.IsCorrect));
    }

    [Fact]
    public async Task SubmitAttempt_ReturnsNullWhenQuizIsNotOwnedByUser()
    {
        var userId = Guid.NewGuid();
        var setup = BuildService(null);

        var result = await setup.Service.SubmitAttemptAsync(userId, Guid.NewGuid(), new SubmitQuizAttemptRequest
        {
            Answers = new List<SubmitQuizAnswerDto>
            {
                new() { QuestionId = Guid.NewGuid(), UserAnswer = "anything" }
            }
        });

        Assert.Null(result);
        setup.Attempts.Verify(repo => repo.AddAsync(It.IsAny<QuizAttempt>()), Times.Never);
    }

    [Fact]
    public async Task GetQuiz_HidesAnswersUnlessPracticeModeRequested()
    {
        var userId = Guid.NewGuid();
        var quiz = BuildQuiz(userId);
        var setup = BuildService(quiz);

        var exam = await setup.Service.GetQuizAsync(userId, quiz.Id, includeAnswers: false);
        var practice = await setup.Service.GetQuizAsync(userId, quiz.Id, includeAnswers: true);

        Assert.NotNull(exam);
        Assert.All(exam.Questions, q => Assert.Null(q.CorrectAnswer));
        Assert.NotNull(practice);
        Assert.All(practice.Questions, q => Assert.False(string.IsNullOrWhiteSpace(q.CorrectAnswer)));
    }

    [Fact]
    public async Task GenerateQuiz_PersistsTimerAndRejectsMalformedAiQuestions()
    {
        var userId = Guid.NewGuid();
        var noteId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        Quiz? capturedQuiz = null;

        var notes = new Mock<INoteRepository>();
        notes.Setup(repo => repo.GetByIdAsync(noteId))
            .ReturnsAsync(new Note { Id = noteId, CourseId = courseId, Content = "Cellular respiration content." });

        var courses = new Mock<ICourseRepository>();
        courses.Setup(repo => repo.GetByIdAsync(courseId))
            .ReturnsAsync(new Course { Id = courseId, UserId = userId, Title = "Biology" });

        var quizzes = new Mock<IQuizRepository>();
        quizzes.Setup(repo => repo.AddAsync(It.IsAny<Quiz>()))
            .Callback<Quiz>(quiz => capturedQuiz = quiz)
            .Returns(Task.CompletedTask);

        var ai = new Mock<IAiService>();
        ai.Setup(service => service.GenerateQuizAsync(
                It.IsAny<string>(),
                It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GeneratedQuizResult
            {
                Title = "Generated",
                Questions = new List<GeneratedQuizQuestionResult>
                {
                    new()
                    {
                        Type = "FillInTheBlank",
                        QuestionText = "ATP synthase uses the ____ gradient.",
                        CorrectAnswer = "proton",
                        Explanation = "A proton gradient powers ATP synthase."
                    },
                    new()
                    {
                        Type = "MultipleChoice",
                        QuestionText = "",
                        CorrectAnswer = ""
                    }
                }
            });

        var unitOfWork = BuildUnitOfWork(
            notes: notes.Object,
            courses: courses.Object,
            quizzes: quizzes.Object);
        var service = new QuizService(unitOfWork.Object, ai.Object);

        var dto = await service.GenerateQuizAsync(userId, new GenerateQuizRequest
        {
            NoteId = noteId,
            Difficulty = "Hard",
            NumberOfQuestions = 2,
            QuestionTypes = new List<string> { "FillInTheBlank" },
            TimeLimitMinutes = 500
        });

        Assert.NotNull(capturedQuiz);
        Assert.Equal(240, capturedQuiz.TimeLimitMinutes);
        Assert.Single(capturedQuiz.Questions);
        Assert.Single(dto.Questions);
        Assert.Equal("FillInTheBlank", dto.Questions[0].Type);
        Assert.Equal("proton", dto.Questions[0].CorrectAnswer);
    }

    [Fact]
    public async Task GenerateQuiz_ThrowsWhenAiReturnsNoValidQuestions()
    {
        var userId = Guid.NewGuid();
        var noteId = Guid.NewGuid();
        var courseId = Guid.NewGuid();

        var notes = new Mock<INoteRepository>();
        notes.Setup(repo => repo.GetByIdAsync(noteId))
            .ReturnsAsync(new Note { Id = noteId, CourseId = courseId, Content = "Content" });

        var courses = new Mock<ICourseRepository>();
        courses.Setup(repo => repo.GetByIdAsync(courseId))
            .ReturnsAsync(new Course { Id = courseId, UserId = userId, Title = "Biology" });

        var ai = new Mock<IAiService>();
        ai.Setup(service => service.GenerateQuizAsync(
                It.IsAny<string>(),
                It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GeneratedQuizResult
            {
                Questions = new List<GeneratedQuizQuestionResult>
                {
                    new() { QuestionText = "", CorrectAnswer = "" }
                }
            });

        var unitOfWork = BuildUnitOfWork(notes: notes.Object, courses: courses.Object);
        var service = new QuizService(unitOfWork.Object, ai.Object);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.GenerateQuizAsync(userId, new GenerateQuizRequest { NoteId = noteId }));

        Assert.Contains("valid quiz question", ex.Message);
    }

    private static Quiz BuildQuiz(Guid userId)
    {
        var quiz = new Quiz
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = "Respiration Quiz",
            QuestionTypes = "MultipleChoice,TrueFalse,ShortAnswer,FillInTheBlank"
        };

        quiz.Questions.Add(new Question
        {
            Id = Guid.NewGuid(),
            QuizId = quiz.Id,
            Type = "MultipleChoice",
            QuestionText = "Which option is correct?",
            OptionsJson = "[\"A\",\"B\",\"C\",\"D\"]",
            CorrectAnswer = "B",
            Explanation = "B is correct.",
            Points = 1,
            OrderIndex = 0
        });
        quiz.Questions.Add(new Question
        {
            Id = Guid.NewGuid(),
            QuizId = quiz.Id,
            Type = "TrueFalse",
            QuestionText = "Oxygen is the final electron acceptor.",
            OptionsJson = "[\"True\",\"False\"]",
            CorrectAnswer = "True",
            Explanation = "Oxygen accepts electrons.",
            Points = 1,
            OrderIndex = 1
        });
        quiz.Questions.Add(new Question
        {
            Id = Guid.NewGuid(),
            QuizId = quiz.Id,
            Type = "ShortAnswer",
            QuestionText = "What does glycolysis produce?",
            CorrectAnswer = "pyruvate",
            Explanation = "Glycolysis produces pyruvate.",
            Points = 1,
            OrderIndex = 2
        });
        quiz.Questions.Add(new Question
        {
            Id = Guid.NewGuid(),
            QuizId = quiz.Id,
            Type = "FillInTheBlank",
            QuestionText = "ATP synthase uses the ____.",
            CorrectAnswer = "proton gradient",
            Explanation = "ATP synthase uses a proton gradient.",
            Points = 1,
            OrderIndex = 3
        });

        return quiz;
    }

    private static QuizServiceSetup BuildService(Quiz? ownedQuiz)
    {
        var quizzes = new Mock<IQuizRepository>();
        quizzes.Setup(repo => repo.GetOwnedQuizAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(ownedQuiz);

        QuizAttempt? capturedAttempt = null;
        var attempts = new Mock<IQuizAttemptRepository>();
        attempts.Setup(repo => repo.AddAsync(It.IsAny<QuizAttempt>()))
            .Callback<QuizAttempt>(attempt => capturedAttempt = attempt)
            .Returns(Task.CompletedTask);

        var unitOfWork = BuildUnitOfWork(quizzes: quizzes.Object, attempts: attempts.Object);
        var service = new QuizService(unitOfWork.Object, Mock.Of<IAiService>());

        return new QuizServiceSetup(service, attempts, () => capturedAttempt);
    }

    private static Mock<IUnitOfWork> BuildUnitOfWork(
        INoteRepository? notes = null,
        ICourseRepository? courses = null,
        IQuizRepository? quizzes = null,
        IQuizAttemptRepository? attempts = null)
    {
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.SetupGet(uow => uow.Notes).Returns(notes ?? Mock.Of<INoteRepository>());
        unitOfWork.SetupGet(uow => uow.Courses).Returns(courses ?? Mock.Of<ICourseRepository>());
        unitOfWork.SetupGet(uow => uow.Quizzes).Returns(quizzes ?? Mock.Of<IQuizRepository>());
        unitOfWork.SetupGet(uow => uow.QuizAttempts).Returns(attempts ?? Mock.Of<IQuizAttemptRepository>());
        unitOfWork.Setup(uow => uow.SaveChangesAsync()).ReturnsAsync(1);
        return unitOfWork;
    }

    private sealed class QuizServiceSetup
    {
        private readonly Func<QuizAttempt?> _getCapturedAttempt;

        public QuizServiceSetup(
            QuizService service,
            Mock<IQuizAttemptRepository> attempts,
            Func<QuizAttempt?> getCapturedAttempt)
        {
            Service = service;
            Attempts = attempts;
            _getCapturedAttempt = getCapturedAttempt;
        }

        public QuizService Service { get; }
        public Mock<IQuizAttemptRepository> Attempts { get; }
        public QuizAttempt? CapturedAttempt => _getCapturedAttempt();
    }
}
