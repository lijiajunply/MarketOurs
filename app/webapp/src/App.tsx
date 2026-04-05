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
            <Route path="/profile" element={<div className="text-center py-20 text-2xl font-bold text-muted-foreground animate-in fade-in duration-700">Profile Page Coming Soon</div>} />
          </Routes>
        </MainLayout>
      </BrowserRouter>
    </ThemeProvider>
  )
}

