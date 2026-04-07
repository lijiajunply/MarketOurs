import { BrowserRouter, Routes, Route, Outlet } from "react-router"
import { ThemeProvider } from "./components/theme-provider"
import { MainLayout } from "./components/layout/MainLayout"
import { AdminLayout } from "./components/layout/AdminLayout"
import { AdminGuard } from "./components/auth/AdminGuard"

import HomePage from "./pages/home"
import AdminDashboard from "./pages/admin/dashboard"
import AdminUsersPage from "./pages/admin/users"
import AdminPostsPage from "./pages/admin/posts"
import AdminSettingsPage from "./pages/admin/settings"
import LoginPage from "./pages/login"
import RegisterPage from "./pages/register"
import PostDetailPage from "./pages/post/detail"
import CreatePostPage from "./pages/post/create"
import NotificationsPage from "./pages/notifications"
import ProfilePage from "./pages/profile"
import PublicProfilePage from "./pages/profile/public"
import ForgotPasswordPage from "./pages/forgot-password"
import ResetPasswordPage from "./pages/profile/reset-password"

export function App() {
  return (
    <ThemeProvider defaultTheme="system" storageKey="marketours-theme">
      <BrowserRouter>
        <Routes>
          {/* Public Routes with MainLayout */}
          <Route element={<MainLayout><Outlet /></MainLayout>}>
            <Route path="/" element={<HomePage />} />
            <Route path="/post/:id" element={<PostDetailPage />} />
            <Route path="/post/create" element={<CreatePostPage />} />
            <Route path="/notifications" element={<NotificationsPage />} />
            <Route path="/login" element={<LoginPage />} />
            <Route path="/register" element={<RegisterPage />} />
            <Route path="/forgot-password" element={<ForgotPasswordPage />} />
            <Route path="/profile" element={<ProfilePage />} />
            <Route path="/user/:id" element={<PublicProfilePage />} />
            <Route path="/profile/reset-password" element={<ResetPasswordPage />} />
          </Route>

          {/* Admin Routes with AdminLayout and AdminGuard */}
          <Route 
            path="/admin" 
            element={
              <AdminGuard>
                <AdminLayout>
                  <Outlet />
                </AdminLayout>
              </AdminGuard>
            }
          >
            <Route index element={<AdminDashboard />} />
            <Route path="users" element={<AdminUsersPage />} />
            <Route path="posts" element={<AdminPostsPage />} />
            <Route path="settings" element={<AdminSettingsPage />} />
          </Route>
        </Routes>
      </BrowserRouter>
    </ThemeProvider>
  )
}
