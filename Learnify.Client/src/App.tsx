import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import Login from './pages/Login';
import Register from './pages/Register';
import Dashboard from './pages/Dashboard';
import Courses from './pages/Courses';
import CourseDetail from './pages/CourseDetail';
import NotesList from './pages/NotesList';
import NoteDetail from './pages/NoteDetail';
import Lessons from './pages/Lessons';
import Flashcards from './pages/Flashcards';
import Quizzes from './pages/Quizzes';
import QuizTaking from './pages/QuizTaking';
import QuizResult from './pages/QuizResult';
import Settings from './pages/Settings';
import PrivateRoute from './components/PrivateRoute';
import Layout from './components/Layout/Layout';
import { Toaster } from 'react-hot-toast';

function App() {
  return (
    <BrowserRouter>
      <Toaster position="top-right" />
      <Routes>
        <Route path="/login" element={<Login />} />
        <Route path="/register" element={<Register />} />
        <Route
          path="/dashboard"
          element={
            <PrivateRoute>
              <Layout><Dashboard /></Layout>
            </PrivateRoute>
          }
        />
        <Route
          path="/courses"
          element={
            <PrivateRoute>
              <Layout><Courses /></Layout>
            </PrivateRoute>
          }
        />
        <Route
          path="/courses/:id"
          element={
            <PrivateRoute>
              <Layout><CourseDetail /></Layout>
            </PrivateRoute>
          }
        />
        <Route
          path="/notes"
          element={
            <PrivateRoute>
              <Layout><NotesList /></Layout>
            </PrivateRoute>
          }
        />
        <Route
          path="/notes/:id"
          element={
            <PrivateRoute>
              <Layout><NoteDetail /></Layout>
            </PrivateRoute>
          }
        />
        <Route
          path="/courses/:courseId/lessons"
          element={
            <PrivateRoute>
              <Layout><Lessons /></Layout>
            </PrivateRoute>
          }
        />
        <Route
          path="/flashcards"
          element={
            <PrivateRoute>
              <Layout><Flashcards /></Layout>
            </PrivateRoute>
          }
        />
        <Route
          path="/quizzes"
          element={
            <PrivateRoute>
              <Layout><Quizzes /></Layout>
            </PrivateRoute>
          }
        />
        <Route
          path="/quizzes/:id"
          element={
            <PrivateRoute>
              <Layout><QuizTaking /></Layout>
            </PrivateRoute>
          }
        />
        <Route
          path="/quizzes/:id/result"
          element={
            <PrivateRoute>
              <Layout><QuizResult /></Layout>
            </PrivateRoute>
          }
        />
        <Route
          path="/settings"
          element={
            <PrivateRoute>
              <Layout><Settings /></Layout>
            </PrivateRoute>
          }
        />
        <Route path="/" element={<Navigate to="/login" replace />} />
        <Route path="*" element={<Navigate to="/login" replace />} />
      </Routes>
    </BrowserRouter>
  );
}

export default App;
