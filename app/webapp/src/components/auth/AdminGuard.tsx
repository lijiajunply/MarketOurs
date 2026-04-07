import { Navigate } from "react-router"
import { useSelector } from "react-redux"
import type { RootState } from "../../stores"

interface AdminGuardProps {
  children: React.ReactNode
}

export function AdminGuard({ children }: AdminGuardProps) {
  const { isAuthenticated, user } = useSelector((state: RootState) => state.auth)

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />
  }

  if (user?.role !== "Admin") {
    // If not admin, redirect to home page
    return <Navigate to="/" replace />
  }

  return <>{children}</>
}
