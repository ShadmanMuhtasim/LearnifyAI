using Learnify.Application.DTOs;
using Learnify.Application.Services;
using Learnify.Core.Entities;
using Learnify.Core.Interfaces;
using Learnify.Core.Models;
using System.Linq.Expressions;
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
        Assert.Equal(0, setup.Attempts.AddedCount);
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
    public async Task GenerateQuiz_PersistsTimerAndFiltersMalformedExtraQuestions()
    {
        var userId = Guid.NewGuid();
        var noteId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        Quiz? capturedQuiz = null;

        var notes = new FakeNoteRepository(new Note { Id = noteId, CourseId = courseId, Content = "Cellular respiration content." });
        var courses = new FakeCourseRepository(new Course { Id = courseId, UserId = userId, Title = "Biology" });
        var quizzes = new FakeQuizRepository
        {
            OnAdd = quiz => capturedQuiz = quiz
        };
        var ai = new FakeAiService
        {
            QuizResult = new GeneratedQuizResult
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
            }
        };

        var unitOfWork = BuildUnitOfWork(
            notes: notes,
            courses: courses,
            quizzes: quizzes);
        var service = new QuizService(unitOfWork, ai);

        var dto = await service.GenerateQuizAsync(userId, new GenerateQuizRequest
        {
            NoteId = noteId,
            Difficulty = "Hard",
            NumberOfQuestions = 1,
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
    public async Task GenerateQuiz_ThrowsWhenAiReturnsFewerValidQuestionsThanRequested()
    {
        var userId = Guid.NewGuid();
        var noteId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var addedQuiz = false;

        var notes = new FakeNoteRepository(new Note { Id = noteId, CourseId = courseId, Content = "Cellular respiration content." });
        var courses = new FakeCourseRepository(new Course { Id = courseId, UserId = userId, Title = "Biology" });
        var quizzes = new FakeQuizRepository
        {
            OnAdd = _ => addedQuiz = true
        };
        var ai = new FakeAiService
        {
            QuizResult = new GeneratedQuizResult
            {
                Title = "Generated",
                Questions = new List<GeneratedQuizQuestionResult>
                {
                    new()
                    {
                        Type = "MultipleChoice",
                        QuestionText = "What powers ATP synthase?",
                        Options = new List<string> { "Proton gradient", "Glucose", "Oxygen", "Carbon dioxide" },
                        CorrectAnswer = "Proton gradient",
                        Explanation = "A proton gradient powers ATP synthase."
                    }
                }
            }
        };

        var unitOfWork = BuildUnitOfWork(
            notes: notes,
            courses: courses,
            quizzes: quizzes);
        var service = new QuizService(unitOfWork, ai);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.GenerateQuizAsync(userId, new GenerateQuizRequest
            {
                NoteId = noteId,
                Difficulty = "Hard",
                NumberOfQuestions = 2,
                QuestionTypes = new List<string> { "MultipleChoice" }
            }));

        Assert.Contains("requested quiz size", ex.Message);
        Assert.False(addedQuiz);
    }

    [Fact]
    public async Task GenerateQuiz_ThrowsWhenAiReturnsNoValidQuestions()
    {
        var userId = Guid.NewGuid();
        var noteId = Guid.NewGuid();
        var courseId = Guid.NewGuid();

        var notes = new FakeNoteRepository(new Note { Id = noteId, CourseId = courseId, Content = "Content" });
        var courses = new FakeCourseRepository(new Course { Id = courseId, UserId = userId, Title = "Biology" });
        var ai = new FakeAiService
        {
            QuizResult = new GeneratedQuizResult
            {
                Questions = new List<GeneratedQuizQuestionResult>
                {
                    new() { QuestionText = "", CorrectAnswer = "" }
                }
            }
        };

        var unitOfWork = BuildUnitOfWork(notes: notes, courses: courses);
        var service = new QuizService(unitOfWork, ai);

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
        var quizzes = new FakeQuizRepository { OwnedQuiz = ownedQuiz };
        var attempts = new FakeQuizAttemptRepository();
        var unitOfWork = BuildUnitOfWork(quizzes: quizzes, attempts: attempts);
        var service = new QuizService(unitOfWork, new FakeAiService());

        return new QuizServiceSetup(service, attempts);
    }

    private static FakeUnitOfWork BuildUnitOfWork(
        INoteRepository? notes = null,
        ICourseRepository? courses = null,
        IQuizRepository? quizzes = null,
        IQuizAttemptRepository? attempts = null)
    {
        return new FakeUnitOfWork(
            notes ?? new FakeNoteRepository(),
            courses ?? new FakeCourseRepository(),
            quizzes ?? new FakeQuizRepository(),
            attempts ?? new FakeQuizAttemptRepository());
    }

    private sealed class QuizServiceSetup
    {
        public QuizServiceSetup(QuizService service, FakeQuizAttemptRepository attempts)
        {
            Service = service;
            Attempts = attempts;
        }

        public QuizService Service { get; }
        public FakeQuizAttemptRepository Attempts { get; }
        public QuizAttempt? CapturedAttempt => Attempts.CapturedAttempt;
    }

    private abstract class FakeRepository<T> : IRepository<T> where T : class
    {
        public virtual Task<IEnumerable<T>> GetAllAsync() => Task.FromResult(Enumerable.Empty<T>());
        public virtual Task<IEnumerable<T>> GetAllNoTrackingAsync() => Task.FromResult(Enumerable.Empty<T>());
        public virtual Task<T?> GetByIdAsync(object id) => Task.FromResult<T?>(null);
        public virtual Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate) => Task.FromResult(Enumerable.Empty<T>());
        public virtual Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate) => Task.FromResult<T?>(null);
        public virtual Task<bool> AnyAsync(Expression<Func<T, bool>> predicate) => Task.FromResult(false);
        public virtual Task<int> CountAsync(Expression<Func<T, bool>> predicate) => Task.FromResult(0);
        public virtual Task AddAsync(T entity) => Task.CompletedTask;
        public virtual Task AddRangeAsync(IEnumerable<T> entities) => Task.CompletedTask;
        public virtual void Update(T entity) { }
        public virtual void Remove(T entity) { }
        public virtual Task DeleteAsync(Guid id) => Task.CompletedTask;
        public virtual void RemoveRange(IEnumerable<T> entities) { }
    }

    private sealed class FakeNoteRepository : FakeRepository<Note>, INoteRepository
    {
        private readonly Note? _note;

        public FakeNoteRepository(Note? note = null)
        {
            _note = note;
        }

        public override Task<Note?> GetByIdAsync(object id)
            => Task.FromResult(_note != null && Equals(_note.Id, id) ? _note : null);

        public Task<IEnumerable<Note>> FindByCourseIdAsync(Guid courseId) => Task.FromResult(Enumerable.Empty<Note>());
        public Task<IEnumerable<Note>> FindByUserIdAsync(Guid userId) => Task.FromResult(Enumerable.Empty<Note>());
        public Task<int> CountByCourseIdAsync(Guid courseId) => Task.FromResult(0);
    }

    private sealed class FakeCourseRepository : FakeRepository<Course>, ICourseRepository
    {
        private readonly Course? _course;

        public FakeCourseRepository(Course? course = null)
        {
            _course = course;
        }

        public override Task<Course?> GetByIdAsync(object id)
            => Task.FromResult(_course != null && Equals(_course.Id, id) ? _course : null);

        public Task<IEnumerable<Course>> FindByUserIdAsync(Guid userId) => Task.FromResult(Enumerable.Empty<Course>());
        public Task<IEnumerable<Course>> SearchByTitleAsync(string searchTerm) => Task.FromResult(Enumerable.Empty<Course>());
        public Task<int> CountByUserIdAsync(Guid userId) => Task.FromResult(0);
    }

    private sealed class FakeQuizRepository : FakeRepository<Quiz>, IQuizRepository
    {
        public Quiz? OwnedQuiz { get; set; }
        public Action<Quiz>? OnAdd { get; set; }

        public override Task AddAsync(Quiz entity)
        {
            OnAdd?.Invoke(entity);
            return Task.CompletedTask;
        }

        public Task<IEnumerable<Quiz>> FindByUserIdAsync(Guid userId) => Task.FromResult(Enumerable.Empty<Quiz>());
        public Task<Quiz?> GetOwnedQuizAsync(Guid quizId, Guid userId) => Task.FromResult(OwnedQuiz);
    }

    private sealed class FakeQuizAttemptRepository : FakeRepository<QuizAttempt>, IQuizAttemptRepository
    {
        public int AddedCount { get; private set; }
        public QuizAttempt? CapturedAttempt { get; private set; }

        public override Task AddAsync(QuizAttempt entity)
        {
            AddedCount++;
            CapturedAttempt = entity;
            return Task.CompletedTask;
        }

        public Task<IEnumerable<QuizAttempt>> FindByQuizAndUserAsync(Guid quizId, Guid userId) => Task.FromResult(Enumerable.Empty<QuizAttempt>());
        public Task<QuizAttempt?> GetOwnedAttemptAsync(Guid attemptId, Guid userId) => Task.FromResult<QuizAttempt?>(null);
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public FakeUnitOfWork(
            INoteRepository notes,
            ICourseRepository courses,
            IQuizRepository quizzes,
            IQuizAttemptRepository attempts)
        {
            Notes = notes;
            Courses = courses;
            Quizzes = quizzes;
            QuizAttempts = attempts;
        }

        public IUserRepository Users => null!;
        public ICourseRepository Courses { get; }
        public INoteRepository Notes { get; }
        public ILessonRepository Lessons => null!;
        public IUserAiSettingsRepository UserAiSettings => null!;
        public IQuizRepository Quizzes { get; }
        public IQuizAttemptRepository QuizAttempts { get; }
        public Task<int> SaveChangesAsync() => Task.FromResult(1);
        public Task BeginTransactionAsync() => Task.CompletedTask;
        public Task CommitTransactionAsync() => Task.CompletedTask;
        public void RollbackTransaction() { }
        public void Dispose() { }
    }

    private sealed class FakeAiService : IAiService
    {
        public GeneratedQuizResult QuizResult { get; set; } = new();

        public Task<string> SummarizeNoteAsync(string content, CancellationToken ct = default) => Task.FromResult("");
        public Task<IReadOnlyList<FlashcardResult>> GenerateFlashcardsAsync(string content, int count = 5, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<FlashcardResult>>(Array.Empty<FlashcardResult>());
        public Task<GeneratedQuizResult> GenerateQuizAsync(string content, IReadOnlyList<string> questionTypes, string difficulty, int numberOfQuestions, CancellationToken ct = default)
            => Task.FromResult(QuizResult);
        public Task<string> GetStudyTipsAsync(string topic, CancellationToken ct = default) => Task.FromResult("");
        public Task<NoteAnalysisResult> AnalyzeDocumentAsync(string content, string fileName, CancellationToken ct = default)
            => Task.FromResult(new NoteAnalysisResult());
    }
}
