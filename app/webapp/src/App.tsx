import { BrowserRouter, Routes, Route, Outlet } from "react-router"
import { ThemeProvider } from "./components/theme-provider"
import { MainLayout } from "./components/layout/MainLayout"
import { AdminLayout } from "./components/layout/AdminLayout"
import { AdminGuard } from "./components/auth/AdminGuard"

import HomePage from "./pages/home"
import HotPage from "./pages/hot"
import AdminDashboard from "./pages/admin/dashboard"
import AdminUsersPage from "./pages/admin/users"
import AdminPostsPage from "./pages/admin/posts"
import AdminCommentsPage from "./pages/admin/comments"
import AdminLogsPage from "./pages/admin/logs"
import AdminBlacklistPage from "./pages/admin/blacklist"
import LoginPage from "./pages/login"
import LoginCallbackPage from "./pages/login/callback"
import RegisterPage from "./pages/register"
import PostDetailPage from "./pages/post/detail"
import CreatePostPage from "./pages/post/create"
import NotificationsPage from "./pages/notifications"
import ProfilePage from "./pages/profile"
import PublicProfilePage from "./pages/profile/public"
import ForgotPasswordPage from "./pages/forgot-password"
import ResetPasswordPage from "./pages/profile/reset-password"
import { useEffect } from "react"
import { useDispatch, useSelector } from "react-redux"
import type { RootState } from "./stores"
import { userService } from "./services/userService"
import { setUser, logout } from "./stores/authSlice"

export function App() {
  const dispatch = useDispatch()
  const { isAuthenticated, user } = useSelector((state: RootState) => state.auth)

  useEffect(() => {
    const initUser = async () => {
      if (isAuthenticated && !user) {
        try {
          const response = await userService.getProfile()
          if (response.data) {
            dispatch(setUser(response.data))
          }
        } catch (error) {
          console.error("Failed to initialize user:", error)
          dispatch(logout())
        }
      }
    }
    initUser()
  }, [isAuthenticated, user, dispatch])

  return (
    <ThemeProvider defaultTheme="system" storageKey="marketours-theme">
      <BrowserRouter>
        <Routes>
          {/* Public Routes with MainLayout */}
          <Route element={<MainLayout><Outlet /></MainLayout>}>
            <Route path="/" element={<HomePage />} />
            <Route path="/hot" element={<HotPage />} />
            <Route path="/post/:id" element={<PostDetailPage />} />
            <Route path="/post/create" element={<CreatePostPage />} />
            <Route path="/notifications" element={<NotificationsPage />} />
            <Route path="/login" element={<LoginPage />} />
            <Route path="/login-callback" element={<LoginCallbackPage />} />
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
            <Route path="comments" element={<AdminCommentsPage />} />
            <Route path="logs" element={<AdminLogsPage />} />
            <Route path="blacklist" element={<AdminBlacklistPage />} />
          </Route>
        </Routes>
      </BrowserRouter>
    </ThemeProvider>
  )
}
