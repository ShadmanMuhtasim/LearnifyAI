import { type FormEvent, useEffect, useMemo, useState } from 'react';
import axios from 'axios';
import apiClient from '../services/api';
import { courseService, type Course } from '../services/courseService';
import { quizService, type Quiz } from '../services/quizService';
import {
  studyPlannerService,
  type CreateStudyPlanItemRequest,
  type StudyPlanItem,
  type StudyPlanStatus,
  type StudyPlanSummary,
  type StudySuggestion,
  type UpdateStudyPlanItemRequest,
} from '../services/studyPlannerService';
import { AppButton, Badge, Card, EmptyState, ErrorState, LoadingState, PageHeader, StatCard } from '../components/UI/Primitives';

interface NoteOption {
  id: string;
  content: string;
  courseId: string;
  courseTitle?: string | null;
}

interface ApiResponse<T> {
  success: boolean;
  data: T;
  message?: string;
}

type PlannerFormState = {
  title: string;
  description: string;
  planType: string;
  courseId: string;
  noteId: string;
  quizId: string;
  scheduledFor: string;
  estimatedMinutes: number;
  priority: string;
};

const planTypes = ['ReviewNote', 'TakeQuiz', 'ReviseCourse', 'FlashcardSession', 'Custom'];
const priorities = ['Low', 'Medium', 'High'];
const emptySummary: StudyPlanSummary = {
  pendingCount: 0,
  completedCount: 0,
  todayCount: 0,
  overdueCount: 0,
  totalEstimatedMinutesToday: 0,
  nextItem: null,
  suggestions: [],
};

function toDateTimeInput(value: Date) {
  const local = new Date(value.getTime() - value.getTimezoneOffset() * 60000);
  return local.toISOString().slice(0, 16);
}

function defaultForm(): PlannerFormState {
  const scheduled = new Date();
  scheduled.setHours(scheduled.getHours() + 1, 0, 0, 0);

  return {
    title: '',
    description: '',
    planType: 'Custom',
    courseId: '',
    noteId: '',
    quizId: '',
    scheduledFor: toDateTimeInput(scheduled),
    estimatedMinutes: 30,
    priority: 'Medium',
  };
}

function getApiError(error: unknown, fallback: string) {
  if (axios.isAxiosError(error)) {
    const data = error.response?.data as { message?: string; errors?: string[] } | undefined;
    return data?.errors?.[0] || data?.message || fallback;
  }

  return error instanceof Error ? error.message : fallback;
}

function toRequest(form: PlannerFormState): CreateStudyPlanItemRequest {
  return {
    title: form.title.trim(),
    description: form.description.trim() || null,
    planType: form.planType,
    courseId: form.courseId || null,
    noteId: form.noteId || null,
    quizId: form.quizId || null,
    scheduledFor: new Date(form.scheduledFor).toISOString(),
    estimatedMinutes: form.estimatedMinutes,
    priority: form.priority,
    source: 'Manual',
  };
}

function formFromItem(item: StudyPlanItem): PlannerFormState {
  return {
    title: item.title,
    description: item.description || '',
    planType: item.planType || 'Custom',
    courseId: item.courseId || '',
    noteId: item.noteId || '',
    quizId: item.quizId || '',
    scheduledFor: toDateTimeInput(new Date(item.scheduledFor)),
    estimatedMinutes: item.estimatedMinutes,
    priority: item.priority || 'Medium',
  };
}

function formatWhen(value: string) {
  return new Date(value).toLocaleString(undefined, {
    month: 'short',
    day: 'numeric',
    hour: 'numeric',
    minute: '2-digit',
  });
}

function preview(value?: string | null, maxLength = 90) {
  if (!value) {
    return '';
  }

  return value.length <= maxLength ? value : `${value.slice(0, maxLength)}...`;
}

function statusTone(status: StudyPlanStatus) {
  if (status === 'Completed') return 'success';
  if (status === 'Skipped') return 'warning';
  return 'primary';
}

export default function StudyPlanner() {
  const [items, setItems] = useState<StudyPlanItem[]>([]);
  const [summary, setSummary] = useState<StudyPlanSummary>(emptySummary);
  const [courses, setCourses] = useState<Course[]>([]);
  const [notes, setNotes] = useState<NoteOption[]>([]);
  const [quizzes, setQuizzes] = useState<Quiz[]>([]);
  const [form, setForm] = useState<PlannerFormState>(() => defaultForm());
  const [editingItem, setEditingItem] = useState<StudyPlanItem | null>(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [formError, setFormError] = useState<string | null>(null);
  const [activeFilter, setActiveFilter] = useState<'All' | StudyPlanStatus | 'Overdue'>('All');

  const loadPlanner = async () => {
    try {
      setLoading(true);
      setError(null);
      const [plannerItems, plannerSummary, courseList, notesResponse, quizList] = await Promise.all([
        studyPlannerService.getItems(),
        studyPlannerService.getSummary(),
        courseService.getAll(),
        apiClient.get<ApiResponse<NoteOption[]>>('/api/notes'),
        quizService.getQuizzes(),
      ]);

      setItems(plannerItems);
      setSummary(plannerSummary);
      setCourses(courseList);
      setNotes(notesResponse.data.data || []);
      setQuizzes(quizList);
    } catch (err: unknown) {
      setError(getApiError(err, 'Unable to load your study planner.'));
      setItems([]);
      setSummary(emptySummary);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    void loadPlanner();
  }, []);

  const visibleItems = useMemo(() => {
    const now = Date.now();

    return items
      .filter((item) => {
        if (activeFilter === 'All') return true;
        if (activeFilter === 'Overdue') return item.status === 'Pending' && new Date(item.scheduledFor).getTime() < now;
        return item.status === activeFilter;
      })
      .slice()
      .sort((first, second) => new Date(first.scheduledFor).getTime() - new Date(second.scheduledFor).getTime());
  }, [activeFilter, items]);

  const resetForm = () => {
    setForm(defaultForm());
    setEditingItem(null);
    setFormError(null);
  };

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    if (!form.title.trim()) {
      setFormError('Title is required.');
      return;
    }

    try {
      setSaving(true);
      setFormError(null);

      if (editingItem) {
        const request: UpdateStudyPlanItemRequest = {
          ...toRequest(form),
          status: editingItem.status,
          source: editingItem.source,
        };
        await studyPlannerService.update(editingItem.id, request);
      } else {
        await studyPlannerService.create(toRequest(form));
      }

      resetForm();
      await loadPlanner();
    } catch (err: unknown) {
      setFormError(getApiError(err, 'Unable to save this study plan item.'));
    } finally {
      setSaving(false);
    }
  };

  const handleComplete = async (item: StudyPlanItem) => {
    try {
      setError(null);
      await studyPlannerService.complete(item.id);
      await loadPlanner();
    } catch (err: unknown) {
      setError(getApiError(err, 'Unable to mark this task complete.'));
    }
  };

  const handleDelete = async (item: StudyPlanItem) => {
    if (!window.confirm(`Delete "${item.title}"?`)) {
      return;
    }

    try {
      setError(null);
      await studyPlannerService.delete(item.id);
      await loadPlanner();
    } catch (err: unknown) {
      setError(getApiError(err, 'Unable to delete this study plan item.'));
    }
  };

  const handleAddSuggestion = async (suggestion: StudySuggestion) => {
    const scheduled = new Date();
    scheduled.setDate(scheduled.getDate() + 1);
    scheduled.setHours(9, 0, 0, 0);

    try {
      setError(null);
      await studyPlannerService.create({
        title: suggestion.title,
        description: suggestion.description || null,
        planType: suggestion.planType,
        courseId: suggestion.courseId || null,
        noteId: suggestion.noteId || null,
        quizId: suggestion.quizId || null,
        scheduledFor: scheduled.toISOString(),
        estimatedMinutes: suggestion.estimatedMinutes,
        priority: suggestion.priority,
        source: 'Suggested',
      });
      await loadPlanner();
    } catch (err: unknown) {
      setError(getApiError(err, 'Unable to add this suggestion.'));
    }
  };

  if (loading) {
    return <LoadingState label="Loading study planner..." />;
  }

  return (
    <div className="stack">
      <PageHeader
        eyebrow="Study Planner"
        title="Study Planner"
        subtitle="Plan review sessions, quizzes, and focused study tasks."
        actions={<Badge tone="primary">{summary.pendingCount} pending</Badge>}
      />

      {error && <ErrorState message={error} />}

      <div className="grid grid-4">
        <StatCard label="Today's tasks" value={summary.todayCount} detail="scheduled today" tone="primary" />
        <StatCard label="Pending" value={summary.pendingCount} detail="open study tasks" tone="warning" />
        <StatCard label="Completed" value={summary.completedCount} detail="finished tasks" tone="success" />
        <StatCard label="Overdue" value={summary.overdueCount} detail="need attention" tone={summary.overdueCount ? 'danger' : 'default'} />
        <StatCard label="Minutes today" value={summary.totalEstimatedMinutesToday} detail="estimated focus time" tone="default" />
      </div>

      <div className="page-two-column planner-layout">
        <div className="stack">
          <Card>
            <div className="split mb-4">
              <div>
                <h2>{editingItem ? 'Edit plan item' : 'Create plan item'}</h2>
                <p className="muted mt-3">Schedule a real task linked to your courses, notes, or quizzes.</p>
              </div>
              {editingItem && (
                <AppButton type="button" variant="secondary" onClick={resetForm}>
                  Cancel edit
                </AppButton>
              )}
            </div>

            <form className="field-grid" onSubmit={(event) => void handleSubmit(event)}>
              <label>
                <span className="form-label">Title</span>
                <input
                  value={form.title}
                  maxLength={300}
                  required
                  onChange={(event) => setForm((current) => ({ ...current, title: event.target.value }))}
                  placeholder="Review graph traversal notes"
                />
              </label>

              <label>
                <span className="form-label">Description</span>
                <textarea
                  rows={3}
                  value={form.description}
                  maxLength={1000}
                  onChange={(event) => setForm((current) => ({ ...current, description: event.target.value }))}
                  placeholder="Optional focus details"
                />
              </label>

              <div className="grid grid-3">
                <label>
                  <span className="form-label">Type</span>
                  <select value={form.planType} onChange={(event) => setForm((current) => ({ ...current, planType: event.target.value }))}>
                    {planTypes.map((type) => <option key={type}>{type}</option>)}
                  </select>
                </label>

                <label>
                  <span className="form-label">Scheduled</span>
                  <input
                    type="datetime-local"
                    value={form.scheduledFor}
                    onChange={(event) => setForm((current) => ({ ...current, scheduledFor: event.target.value }))}
                  />
                </label>

                <label>
                  <span className="form-label">Estimated minutes</span>
                  <input
                    type="number"
                    min={1}
                    max={600}
                    value={form.estimatedMinutes}
                    onChange={(event) => setForm((current) => ({ ...current, estimatedMinutes: Number(event.target.value) }))}
                  />
                </label>
              </div>

              <div className="grid grid-3">
                <label>
                  <span className="form-label">Course</span>
                  <select value={form.courseId} onChange={(event) => setForm((current) => ({ ...current, courseId: event.target.value }))}>
                    <option value="">No course link</option>
                    {courses.map((course) => <option key={course.id} value={course.id}>{course.title}</option>)}
                  </select>
                </label>

                <label>
                  <span className="form-label">Note</span>
                  <select value={form.noteId} onChange={(event) => setForm((current) => ({ ...current, noteId: event.target.value }))}>
                    <option value="">No note link</option>
                    {notes.map((note) => (
                      <option key={note.id} value={note.id}>
                        {(note.courseTitle || 'Note')} - {preview(note.content, 50)}
                      </option>
                    ))}
                  </select>
                </label>

                <label>
                  <span className="form-label">Quiz</span>
                  <select value={form.quizId} onChange={(event) => setForm((current) => ({ ...current, quizId: event.target.value }))}>
                    <option value="">No quiz link</option>
                    {quizzes.map((quiz) => <option key={quiz.id} value={quiz.id}>{quiz.title}</option>)}
                  </select>
                </label>
              </div>

              <label>
                <span className="form-label">Priority</span>
                <select value={form.priority} onChange={(event) => setForm((current) => ({ ...current, priority: event.target.value }))}>
                  {priorities.map((priority) => <option key={priority}>{priority}</option>)}
                </select>
              </label>

              {formError && <ErrorState message={formError} />}

              <AppButton type="submit" disabled={saving} style={{ justifySelf: 'start' }}>
                {saving ? 'Saving...' : editingItem ? 'Save changes' : 'Add to planner'}
              </AppButton>
            </form>
          </Card>

          <Card className="stack">
            <div className="split">
              <h2>Planner list</h2>
              <div className="cluster" role="tablist" aria-label="Planner filters">
                {(['All', 'Pending', 'Overdue', 'Completed'] as const).map((filter) => (
                  <button
                    type="button"
                    key={filter}
                    className={`ui-button ${activeFilter === filter ? 'ui-button-primary' : 'ui-button-ghost'}`}
                    onClick={() => setActiveFilter(filter)}
                  >
                    {filter}
                  </button>
                ))}
              </div>
            </div>

            {items.length === 0 ? (
              <EmptyState
                title="Create your first study plan item."
                message="Schedule a review, quiz, flashcard session, or custom study task."
              />
            ) : visibleItems.length === 0 ? (
              <EmptyState title="No matching tasks" message="Try a different planner filter." />
            ) : (
              <div className="stack">
                {visibleItems.map((item) => (
                  <Card key={item.id} className="planner-item">
                    <div className="split">
                      <div>
                        <div className="cluster mb-2">
                          <Badge tone={statusTone(item.status)}>{item.status}</Badge>
                          <Badge tone="muted">{item.planType}</Badge>
                          <Badge tone={item.priority === 'High' ? 'warning' : 'default'}>{item.priority}</Badge>
                          {item.source === 'Suggested' && <Badge tone="primary">Suggested</Badge>}
                        </div>
                        <h3>{item.title}</h3>
                        {item.description && <p className="muted mt-3 planner-description">{item.description}</p>}
                      </div>
                      <div className="planner-item-meta">
                        <strong>{formatWhen(item.scheduledFor)}</strong>
                        <span className="muted text-small">{item.estimatedMinutes} min</span>
                      </div>
                    </div>

                    <div className="cluster">
                      {item.courseTitle && <Badge tone="primary">{item.courseTitle}</Badge>}
                      {item.notePreview && <span className="muted text-small">Note: {preview(item.notePreview, 80)}</span>}
                      {item.quizTitle && <span className="muted text-small">Quiz: {item.quizTitle}</span>}
                    </div>

                    <div className="cluster">
                      {item.status !== 'Completed' && (
                        <AppButton type="button" onClick={() => void handleComplete(item)}>
                          Complete
                        </AppButton>
                      )}
                      <AppButton
                        type="button"
                        variant="secondary"
                        onClick={() => {
                          setEditingItem(item);
                          setForm(formFromItem(item));
                          setFormError(null);
                        }}
                      >
                        Edit
                      </AppButton>
                      <AppButton type="button" variant="danger" onClick={() => void handleDelete(item)}>
                        Delete
                      </AppButton>
                    </div>
                  </Card>
                ))}
              </div>
            )}
          </Card>
        </div>

        <div className="stack">
          <Card>
            <div className="eyebrow mb-3">Next study</div>
            {summary.nextItem ? (
              <div className="stack">
                <h2>{summary.nextItem.title}</h2>
                <p className="muted">{formatWhen(summary.nextItem.scheduledFor)}</p>
                <Badge tone="primary">{summary.nextItem.estimatedMinutes} min</Badge>
              </div>
            ) : (
              <p className="muted">No upcoming pending study task yet.</p>
            )}
          </Card>

          <Card className="stack">
            <div>
              <div className="eyebrow mb-3">Smart suggestions</div>
              <h2>Based on your data</h2>
            </div>

            {summary.suggestions.length === 0 ? (
              <p className="muted">Suggestions appear after you add notes, quizzes, or pending planner items.</p>
            ) : (
              summary.suggestions.map((suggestion) => (
                <div key={`${suggestion.title}-${suggestion.noteId || suggestion.quizId || suggestion.courseId || 'custom'}`} className="concept-card">
                  <strong>{suggestion.title}</strong>
                  {suggestion.description && <p className="muted text-small">{suggestion.description}</p>}
                  <div className="split">
                    <Badge tone="muted">{suggestion.planType}</Badge>
                    <AppButton type="button" variant="secondary" onClick={() => void handleAddSuggestion(suggestion)}>
                      Add
                    </AppButton>
                  </div>
                </div>
              ))
            )}
          </Card>
        </div>
      </div>
    </div>
  );
}
