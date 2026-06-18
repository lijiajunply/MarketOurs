import { Link, useParams } from "react-router";
import { useEffect, useMemo, useRef, useState } from "react";
import { useTranslation } from "react-i18next";
import { useSelector } from "react-redux";
import { ArrowRight, Calendar, FileText, Loader2, Sparkles, UserPlus, UserMinus, Ban } from "lucide-react";
import { userService } from "../../services/userService";
import { postService } from "../../services/postService";
import { followService } from "../../services/followService";
import { toast } from "../../lib/toast";
import { extractUserMessage } from "../../services/errorCodes";
import type { RootState } from "../../stores";
import type { PagedResult, PostDto, PublicUserProfileDto } from "../../types";
import { PostTagBadge } from "../../components/post/PostTagBadge";
import { formatLocalDate } from "../../lib/dateTime";

const RECENT_POST_FETCH_SIZE = 10;

export default function PublicProfilePage() {
  const { id } = useParams();
  const { t, i18n } = useTranslation();
  const currentUser = useSelector((state: RootState) => state.auth.user);

  const [profile, setProfile] = useState<PublicUserProfileDto | null>(null);
  const [recentPosts, setRecentPosts] = useState<PostDto[]>([]);
  const [postsTotalCount, setPostsTotalCount] = useState(0);
  const [postsPageIndex, setPostsPageIndex] = useState(1);
  const [hasNextPage, setHasNextPage] = useState(false);
  const [isLoadingMore, setIsLoadingMore] = useState(false);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const loadMoreRef = useRef<HTMLDivElement | null>(null);
  const pageIndexRef = useRef(1);
  const totalCountRef = useRef(0);

  useEffect(() => {
    pageIndexRef.current = postsPageIndex;
  }, [postsPageIndex]);

  useEffect(() => {
    totalCountRef.current = postsTotalCount;
  }, [postsTotalCount]);

  useEffect(() => {
    if (!id) {
      setError(t("profile.public_not_found"));
      setProfile(null);
      setRecentPosts([]);
      setPostsTotalCount(0);
      setPostsPageIndex(1);
      pageIndexRef.current = 1;
      totalCountRef.current = 0;
      setHasNextPage(false);
      setIsLoadingMore(false);
      setLoading(false);
      return;
    }

    let cancelled = false;

    const fetchPublicProfile = async () => {
      setLoading(true);
      setError(null);
      setProfile(null);
      setRecentPosts([]);
      setPostsTotalCount(0);
      setPostsPageIndex(1);
      pageIndexRef.current = 1;
      totalCountRef.current = 0;
      setHasNextPage(false);
      setIsLoadingMore(false);

      try {
        const [userResponse, postsResponse] = await Promise.all([
          userService.getPublicProfile(id),
          postService.getUserPosts(id, 1, RECENT_POST_FETCH_SIZE),
        ]);

        if (cancelled) {
          return;
        }

        const userData = userResponse.data;
        const postsPage = postsResponse.data as PagedResult<PostDto> | undefined;
        const posts = postsPage?.items ?? [];

        setProfile(userData);
        setRecentPosts(posts);
        setPostsTotalCount(postsPage?.totalCount ?? posts.length);
        setPostsPageIndex(postsPage?.pageIndex ?? 1);
        pageIndexRef.current = postsPage?.pageIndex ?? 1;
        totalCountRef.current = postsPage?.totalCount ?? posts.length;
        setHasNextPage(postsPage?.hasNextPage ?? false);
      } catch (err) {
        console.error("Failed to fetch public profile", err);
        if (!cancelled) {
          setError(t("profile.public_fetch_error"));
        }
      } finally {
        if (!cancelled) {
          setLoading(false);
        }
      }
    };

    fetchPublicProfile();

    return () => {
      cancelled = true;
    };
  }, [id, t]);

  useEffect(() => {
    if (!id || !hasNextPage || isLoadingMore || loading) {
      return;
    }

    const target = loadMoreRef.current;
    if (!target) {
      return;
    }

    const observer = new IntersectionObserver(
      (entries) => {
        const entry = entries[0];
        if (entry?.isIntersecting) {
          void loadMorePosts();
        }
      },
      { rootMargin: "240px 0px" },
    );

    observer.observe(target);
    return () => observer.disconnect();
  }, [id, hasNextPage, isLoadingMore, loading, postsPageIndex]);

  const loadMorePosts = async () => {
    if (!id || isLoadingMore || !hasNextPage) {
      return;
    }

    setIsLoadingMore(true);
    try {
      const response = await postService.getUserPosts(id, pageIndexRef.current + 1, RECENT_POST_FETCH_SIZE);
      const page = response.data;
      const items = page?.items ?? [];

      setRecentPosts((current) => {
        const existingIds = new Set(current.map((post) => post.id));
        const nextItems = items.filter((post) => !existingIds.has(post.id));
        return [...current, ...nextItems];
      });
      const nextTotalCount = page?.totalCount ?? totalCountRef.current;
      const nextPageIndex = page?.pageIndex ?? pageIndexRef.current + 1;
      totalCountRef.current = nextTotalCount;
      pageIndexRef.current = nextPageIndex;
      setPostsTotalCount(nextTotalCount);
      setPostsPageIndex(nextPageIndex);
      setHasNextPage(page?.hasNextPage ?? false);
    } catch (err) {
      console.error("Failed to load more public posts", err);
    } finally {
      setIsLoadingMore(false);
    }
  };

  const [isFollowing, setIsFollowing] = useState(false);
  const [isBlocked, setIsBlocked] = useState(false);
  const [followerCount, setFollowerCount] = useState(0);
  const [followingCount, setFollowingCount] = useState(0);
  const [followLoading, setFollowLoading] = useState(false);

  const isCurrentUser = useMemo(() => {
    if (!currentUser || !profile) {
      return false;
    }

    return currentUser.id.toLowerCase() === profile.id.toLowerCase();
  }, [currentUser, profile]);

  useEffect(() => {
    if (profile) {
      setFollowerCount(profile.followerCount ?? 0);
      setFollowingCount(profile.followingCount ?? 0);
      setIsFollowing(profile.relationshipStatus?.isFollowing ?? false);
      setIsBlocked(profile.relationshipStatus?.isBlocked ?? false);
    }
  }, [profile]);

  const handleToggleFollow = async () => {
    if (!id || followLoading) return;
    setFollowLoading(true);
    try {
      const result = await followService.toggleFollow(id);
      setIsFollowing(result.data.isFollowing);
      setFollowerCount(result.data.followerCount);
    } catch (err) {
      toast.error(extractUserMessage(err, t("profile.follow_error")));
    } finally {
      setFollowLoading(false);
    }
  };

  const handleToggleBlock = async () => {
    if (!id || followLoading) return;
    setFollowLoading(true);
    try {
      if (isBlocked) {
        await followService.unblockUser(id);
        setIsBlocked(false);
      } else {
        await followService.blockUser(id);
        setIsBlocked(true);
        setIsFollowing(false);
      }
    } catch (err) {
      toast.error(extractUserMessage(err, t("profile.block_error")));
    } finally {
      setFollowLoading(false);
    }
  };

  if (loading) {
    return (
      <div className="flex min-h-[60vh] items-center justify-center">
        <Loader2 className="animate-spin text-primary" size={40} />
      </div>
    );
  }

  if (error || !profile) {
    return (
      <div className="mx-auto flex min-h-[60vh] max-w-2xl flex-col items-center justify-center gap-4 px-4 text-center">
        <div className="rounded-full bg-destructive/10 p-4 text-destructive">
          <Sparkles size={24} />
        </div>
        <h1 className="text-2xl font-bold">{t("profile.public_empty_title")}</h1>
        <p className="text-muted-foreground">{error ?? t("profile.public_not_found")}</p>
        <Link
          to="/"
          className="inline-flex items-center gap-2 rounded-2xl bg-primary px-5 py-3 font-semibold text-primary-foreground transition-opacity hover:opacity-90"
        >
          {t("post.back_to_feed")}
        </Link>
      </div>
    );
  }

  const profileAvatar = profile.avatar || `https://api.dicebear.com/9.x/avataaars/svg?seed=${profile.id}`;

  return (
    <div className="mx-auto max-w-4xl space-y-8 px-4 py-12">
      <section className="overflow-hidden rounded-[2.5rem] border border-border/50 bg-card shadow-sm">
        <div className="h-36 bg-linear-to-r from-primary/20 via-primary/10 to-transparent" />

        <div className="space-y-8 px-8 pb-8 pt-0">
          <div className="-mt-16 flex flex-col gap-6 md:flex-row md:items-end md:justify-between">
            <div className="flex flex-col gap-5 md:flex-row md:items-end">
              <div className="h-32 w-32 overflow-hidden rounded-[2rem] border-4 border-background bg-muted shadow-2xl">
                <img src={profileAvatar} alt={profile.name} className="h-full w-full object-cover" />
              </div>

              <div className="space-y-3">
                <div className="flex flex-wrap items-center gap-3">
                  <h1 className="text-3xl font-black tracking-tight">{profile.name}</h1>
                  <span className="rounded-full bg-primary/10 px-3 py-1 text-xs font-bold uppercase tracking-wide text-primary">
                    {profile.role}
                  </span>
                  <span className="rounded-full bg-muted px-3 py-1 text-xs font-semibold text-muted-foreground">
                    {t("profile.public_badge")}
                  </span>
                </div>
                <p className="max-w-2xl text-sm leading-7 text-muted-foreground">
                  {profile.info || t("profile.public_bio_empty")}
                </p>
              </div>
            </div>

            {isCurrentUser && (
              <Link
                to="/profile"
                className="inline-flex items-center gap-2 self-start rounded-2xl bg-primary px-5 py-3 font-semibold text-primary-foreground transition-opacity hover:opacity-90"
              >
                {t("profile.manage_profile")}
                <ArrowRight size={18} />
              </Link>
            )}

            {!isCurrentUser && currentUser && (
              <div className="flex items-center gap-3 self-start">
                <button
                  onClick={handleToggleFollow}
                  disabled={followLoading || isBlocked}
                  className={`inline-flex items-center gap-2 rounded-2xl px-5 py-3 font-semibold transition-all disabled:opacity-50 ${
                    isFollowing
                      ? "border border-border bg-muted text-foreground hover:bg-muted/80"
                      : "bg-primary text-primary-foreground hover:opacity-90"
                  }`}
                >
                  {isFollowing ? <UserMinus size={18} /> : <UserPlus size={18} />}
                  {isFollowing ? "已关注" : "关注"}
                </button>
                <button
                  onClick={handleToggleBlock}
                  disabled={followLoading}
                  className={`inline-flex items-center gap-2 rounded-2xl px-4 py-3 font-semibold transition-all disabled:opacity-50 ${
                    isBlocked
                      ? "border border-destructive/30 bg-destructive/10 text-destructive"
                      : "border border-border bg-muted text-muted-foreground hover:bg-muted/80"
                  }`}
                >
                  <Ban size={18} />
                  {isBlocked ? "已屏蔽" : "屏蔽"}
                </button>
              </div>
            )}
          </div>

          <div className="grid gap-4 md:grid-cols-4">
            <div className="rounded-[2rem] border border-border/50 bg-muted/30 p-5">
              <div className="mb-3 flex items-center gap-3 text-muted-foreground">
                <UserPlus size={18} />
                <span className="text-sm font-medium">粉丝</span>
              </div>
              <p className="text-lg font-bold">{followerCount}</p>
            </div>

            <div className="rounded-[2rem] border border-border/50 bg-muted/30 p-5">
              <div className="mb-3 flex items-center gap-3 text-muted-foreground">
                <UserPlus size={18} />
                <span className="text-sm font-medium">关注</span>
              </div>
              <p className="text-lg font-bold">{followingCount}</p>
            </div>

            <div className="rounded-[2rem] border border-border/50 bg-muted/30 p-5">
              <div className="mb-3 flex items-center gap-3 text-muted-foreground">
                <Calendar size={18} />
                <span className="text-sm font-medium">{t("profile.joined_at")}</span>
              </div>
              <p className="text-lg font-bold">{formatLocalDate(profile.createdAt, i18n.resolvedLanguage)}</p>
            </div>

            <div className="rounded-[2rem] border border-border/50 bg-muted/30 p-5">
              <div className="mb-3 flex items-center gap-3 text-muted-foreground">
                <FileText size={18} />
                <span className="text-sm font-medium">{t("profile.recent_posts")}</span>
              </div>
              <p className="text-lg font-bold">{postsTotalCount}</p>
            </div>
          </div>
        </div>
      </section>

      <section className="space-y-4">
        <div className="flex items-center justify-between gap-4">
          <div>
            <h2 className="text-2xl font-bold tracking-tight">{t("profile.recent_posts")}</h2>
            <p className="text-sm text-muted-foreground">{t("profile.public_posts_hint")}</p>
          </div>
        </div>

        {recentPosts.length > 0 ? (
          <div className="grid gap-4">
            {recentPosts.map((post) => (
              <Link
                key={post.id}
                to={`/post/${post.id}`}
                className="rounded-[2rem] border border-border/50 bg-card p-6 transition-all hover:border-primary/30 hover:shadow-lg hover:shadow-primary/5"
              >
                <div className="flex items-start justify-between gap-4">
                  <div className="space-y-3">
                    <PostTagBadge tag={post.tag} />
                    <h3 className="text-xl font-bold tracking-tight">{post.title}</h3>
                    <p className="line-clamp-2 whitespace-pre-wrap text-sm leading-6 text-muted-foreground">
                      {post.content}
                    </p>
                  </div>
                  <ArrowRight className="mt-1 shrink-0 text-muted-foreground" size={18} />
                </div>
              </Link>
            ))}
            <div ref={loadMoreRef} className="flex min-h-12 items-center justify-center">
              {isLoadingMore ? <Loader2 className="animate-spin text-primary" size={20} /> : null}
              {!isLoadingMore && !hasNextPage ? (
                <p className="text-sm text-muted-foreground">{t("common.no_more_posts")}</p>
              ) : null}
            </div>
          </div>
        ) : (
          <div className="rounded-[2rem] border border-dashed border-border bg-card px-6 py-10 text-center text-muted-foreground">
            {t("profile.no_public_posts")}
          </div>
        )}
      </section>
    </div>
  );
}
