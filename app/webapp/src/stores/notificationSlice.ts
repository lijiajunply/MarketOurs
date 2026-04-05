import { createSlice, createAsyncThunk, type PayloadAction } from "@reduxjs/toolkit";
import { notificationService } from "../services/notificationService";
import type { NotificationDto } from "../types";

interface NotificationState {
  notifications: NotificationDto[];
  unreadCount: number;
  totalCount: number;
  loading: boolean;
  error: string | null;
}

const initialState: NotificationState = {
  notifications: [],
  unreadCount: 0,
  totalCount: 0,
  loading: false,
  error: null,
};

export const fetchUnreadCount = createAsyncThunk(
  "notification/fetchUnreadCount",
  async (_, { rejectWithValue }) => {
    try {
      const response = await notificationService.getUnreadCount();
      return response.data;
    } catch (err: any) {
      return rejectWithValue(err.message || "Failed to fetch unread count");
    }
  }
);

export const fetchNotifications = createAsyncThunk(
  "notification/fetchNotifications",
  async ({ pageIndex, pageSize }: { pageIndex: number; pageSize: number }, { rejectWithValue }) => {
    try {
      const response = await notificationService.getNotifications(pageIndex, pageSize);
      return response.data;
    } catch (err: any) {
      return rejectWithValue(err.message || "Failed to fetch notifications");
    }
  }
);

const notificationSlice = createSlice({
  name: "notification",
  initialState,
  reducers: {
    setUnreadCount: (state, action: PayloadAction<number>) => {
      state.unreadCount = action.payload;
    },
    markReadLocal: (state, action: PayloadAction<string>) => {
      const notification = state.notifications.find((n) => n.id === action.payload);
      if (notification && !notification.isRead) {
        notification.isRead = true;
        state.unreadCount = Math.max(0, state.unreadCount - 1);
      }
    },
    markAllReadLocal: (state) => {
      state.notifications.forEach((n) => (n.isRead = true));
      state.unreadCount = 0;
    },
  },
  extraReducers: (builder) => {
    builder
      .addCase(fetchUnreadCount.fulfilled, (state, action) => {
        state.unreadCount = action.payload ?? 0;
      })
      .addCase(fetchNotifications.pending, (state) => {
        state.loading = true;
      })
      .addCase(fetchNotifications.fulfilled, (state, action) => {
        state.loading = false;
        state.notifications = action.payload?.items || [];
        state.totalCount = action.payload?.totalCount || 0;
      })
      .addCase(fetchNotifications.rejected, (state, action) => {
        state.loading = false;
        state.error = action.payload as string;
      });
  },
});

export const { setUnreadCount, markReadLocal, markAllReadLocal } = notificationSlice.actions;
export default notificationSlice.reducer;
