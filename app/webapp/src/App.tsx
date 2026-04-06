import { BrowserRouter, Routes, Route } from "react-router"
import { ThemeProvider } from "./components/theme-provider"
import { MainLayout } from "./components/layout/MainLayout"
import HomePage from "./pages/home"
import AdminDashboard from "./pages/admin/dashboard"
import LoginPage from "./pages/login"
import RegisterPage from "./pages/register"
import PostDetailPage from "./pages/post/detail"
import CreatePostPage from "./pages/post/create"
import NotificationsPage from "./pages/notifications"
import ProfilePage from "./pages/profile"
import ForgotPasswordPage from "./pages/forgot-password"
import ResetPasswordPage from "./pages/profile/reset-password"

export function App() {
  return (
    <ThemeProvider defaultTheme="system" storageKey="marketours-theme">
      <BrowserRouter>
        <MainLayout>
          <Routes>
            <Route path="/" element={<HomePage />} />
            <Route path="/post/:id" element={<PostDetailPage />} />
            <Route path="/post/create" element={<CreatePostPage />} />
            <Route path="/notifications" element={<NotificationsPage />} />
            <Route path="/admin" element={<AdminDashboard />} />
            <Route path="/login" element={<LoginPage />} />
            <Route path="/register" element={<RegisterPage />} />
            <Route path="/forgot-password" element={<ForgotPasswordPage />} />
            <Route path="/profile" element={<ProfilePage />} />
            <Route path="/profile/reset-password" element={<ResetPasswordPage />} />
          </Routes>
        </MainLayout>
      </BrowserRouter>
    </ThemeProvider>
  )
}

