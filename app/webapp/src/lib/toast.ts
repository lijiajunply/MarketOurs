import { toast as sonnerToast } from "sonner"

type ToastOptions = Parameters<typeof sonnerToast>[1]

/**
 * Display a success toast notification.
 */
export const toast = {
  success: (message: string, options?: ToastOptions) =>
    sonnerToast.success(message, options),

  error: (message: string, options?: ToastOptions) =>
    sonnerToast.error(message, options),

  info: (message: string, options?: ToastOptions) =>
    sonnerToast.info(message, options),

  warning: (message: string, options?: ToastOptions) =>
    sonnerToast.warning(message, options),

  loading: (message: string, options?: ToastOptions) =>
    sonnerToast.loading(message, options),

  /** Dismiss all toasts */
  dismiss: () => sonnerToast.dismiss(),

  /** Dismiss a specific toast by id */
  dismissById: (id: string | number) => sonnerToast.dismiss(id),

  /** Promise-based toast for async operations */
  promise: sonnerToast.promise,

  /** Custom toast */
  custom: sonnerToast,
}
