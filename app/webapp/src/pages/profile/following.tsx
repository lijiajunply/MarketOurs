import { useState, useEffect, useCallback } from "react";
import { Link, useSearchParams } from "react-router";
import { useSelector } from "react-redux";
import { UserMinus, Ban, Loader2, Users, ArrowLeft } from "lucide-react";
import { followService } from "../../services/followService";
import type { RootState } from "../../stores";
import type { UserSimpleDto } from "../../types";

type Tab = "following" | "blocked";

export default function FollowingPage() {
  const [searchParams] = useSearchParams();
  const initialTab = searchParams.get("tab") === "blocked" ? "blocked" : "following";
  const [activeTab, setActiveTab] = useState<Tab>(initialTab);
  const currentUser = useSelector((state: RootState) => state.auth.user);

  const [followingList, setFollowingList] = useState<UserSimpleDto[]>([]);
  const [blockedList, setBlockedList] = useState<UserSimpleDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [actionLoading, setActionLoading] = useState<string | null>(null);

  const loadData = useCallback(async () => {
    if (!currentUser) return;
    setLoading(true);
    try {
      if (activeTab === "following") {
        const res = await followService.getFollowing(currentUser.id, 1, 50);
        setFollowingList(res.data?.items ?? []);
      } else {
        const res = await followService.getBlocked();
        setBlockedList(res.data?.items ?? []);
      }
    } catch (err) {
      console.error("Failed to load list", err);
    } finally {
      setLoading(false);
    }
  }, [currentUser, activeTab]);

  useEffect(() => {
    loadData();
  }, [loadData]);

  const handleUnfollow = async (userId: string) => {
    setActionLoading(userId);
    try {
      await followService.toggleFollow(userId);
      setFollowingList((prev) => prev.filter((u) => u.id !== userId));
    } catch (err) {
      console.error("Failed to unfollow", err);
    } finally {
      setActionLoading(null);
    }
  };

  const handleUnblock = async (userId: string) => {
    setActionLoading(userId);
    try {
      await followService.unblockUser(userId);
      setBlockedList((prev) => prev.filter((u) => u.id !== userId));
    } catch (err) {
      console.error("Failed to unblock", err);
    } finally {
      setActionLoading(null);
    }
  };

  if (!currentUser) {
    return (
      <div className="flex min-h-[60vh] items-center justify-center">
        <p className="text-muted-foreground">请先登录</p>
      </div>
    );
  }

  const list = activeTab === "following" ? followingList : blockedList;

  return (
    <div className="mx-auto max-w-2xl space-y-6 px-4 py-12">
      <div className="flex items-center gap-4">
        <Link
          to="/profile"
          className="rounded-full p-2 text-muted-foreground transition-colors hover:bg-muted"
        >
          <ArrowLeft size={20} />
        </Link>
        <h1 className="text-2xl font-bold tracking-tight">社交管理</h1>
      </div>

      <div className="flex gap-2 rounded-2xl bg-muted/50 p-1.5">
        <button
          onClick={() => setActiveTab("following")}
          className={`flex-1 rounded-xl px-4 py-2.5 text-sm font-semibold transition-all ${
            activeTab === "following"
              ? "bg-background text-foreground shadow-sm"
              : "text-muted-foreground hover:text-foreground"
          }`}
        >
          <Users size={16} className="mr-2 inline-block" />
          我的关注
        </button>
        <button
          onClick={() => setActiveTab("blocked")}
          className={`flex-1 rounded-xl px-4 py-2.5 text-sm font-semibold transition-all ${
            activeTab === "blocked"
              ? "bg-background text-foreground shadow-sm"
              : "text-muted-foreground hover:text-foreground"
          }`}
        >
          <Ban size={16} className="mr-2 inline-block" />
          屏蔽列表
        </button>
      </div>

      {loading ? (
        <div className="flex justify-center py-16">
          <Loader2 className="animate-spin text-muted-foreground" size={32} />
        </div>
      ) : list.length === 0 ? (
        <div className="rounded-[2rem] border border-dashed border-border bg-card px-6 py-16 text-center">
          <p className="text-muted-foreground">
            {activeTab === "following" ? "还没有关注任何人" : "没有屏蔽任何人"}
          </p>
        </div>
      ) : (
        <div className="space-y-3">
          {list.map((user) => (
            <div
              key={user.id}
              className="flex items-center gap-4 rounded-2xl border border-border/50 bg-card p-4 transition-all hover:shadow-sm"
            >
              <Link to={`/user/${user.id}`} className="shrink-0">
                <img
                  src={user.avatar || `https://api.dicebear.com/9.x/avataaars/svg?seed=${user.id}`}
                  alt={user.name}
                  className="h-12 w-12 rounded-full object-cover"
                />
              </Link>

              <Link to={`/user/${user.id}`} className="min-w-0 flex-1">
                <p className="truncate font-semibold">{user.name || "未设置昵称"}</p>
              </Link>

              <button
                onClick={() =>
                  activeTab === "following"
                    ? handleUnfollow(user.id)
                    : handleUnblock(user.id)
                }
                disabled={actionLoading === user.id}
                className={`inline-flex items-center gap-1.5 rounded-xl px-4 py-2 text-sm font-semibold transition-all disabled:opacity-50 ${
                  activeTab === "following"
                    ? "border border-border bg-muted text-muted-foreground hover:bg-muted/80"
                    : "border border-destructive/30 bg-destructive/10 text-destructive hover:bg-destructive/20"
                }`}
              >
                {activeTab === "following" ? (
                  <>
                    <UserMinus size={14} />
                    取消关注
                  </>
                ) : (
                  <>
                    <Ban size={14} />
                    取消屏蔽
                  </>
                )}
              </button>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
