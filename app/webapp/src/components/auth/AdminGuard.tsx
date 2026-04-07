import { Navigate } from "react-router"
import { useSelector, useDispatch } from "react-redux"
import { useEffect, useState } from "react"
import type { RootState } from "../../stores"
import { userService } from "../../services/userService"
import { setUser, logout } from "../../stores/authSlice"
import { Loader2 } from "lucide-react"

interface AdminGuardProps {
  children: React.ReactNode
}

export function AdminGuard({ children }: AdminGuardProps) {
  const { isAuthenticated, user } = useSelector((state: RootState) => state.auth)
  const dispatch = useDispatch()
  const [isLoading, setIsLoading] = useState(isAuthenticated && !user)

  useEffect(() => {
    const fetchUser = async () => {
      if (isAuthenticated && !user) {
        try {
          const response = await userService.getProfile()
          if (response.data) {
            dispatch(setUser(response.data))
          }
        } catch (error) {
          console.error("AdminGuard: Failed to fetch user profile:", error)
          dispatch(logout())
        } finally {
          setIsLoading(false)
        }
      }
    };

    if (isAuthenticated && !user) {
      fetchUser()
    } else {
      setIsLoading(false)
    }
  }, [isAuthenticated, user, dispatch])

  if (isLoading) {
    return (
      <div className="flex items-center justify-center min-h-screen">
        <Loader2 className="animate-spin text-primary" size={48} />
      </div>
    )
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />
  }

  if (user?.role !== "Admin") {
    // If not admin, redirect to home page
    return <Navigate to="/" replace />
  }

  return <>{children}</>
}
