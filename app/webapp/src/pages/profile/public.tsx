import { Link, useParams } from "react-router";
import { useEffect, useMemo, useState } from "react";
import { useTranslation } from "react-i18next";
import { useSelector } from "react-redux";
import { ArrowRight, Calendar, FileText, Loader2, Shield, Sparkles } from "lucide-react";
import { userService } from "../../services/userService";
import { postService } from "../../services/postService";
import type { RootState } from "../../stores";
import type { PostDto, PublicUserProfileDto } from "../../types";

const RECENT_POST_FETCH_SIZE = 10;

export default function PublicProfilePage() {
  const { id } = useParams();
  const { t } = useTranslation();
  const currentUser = useSelector((state: RootState) => state.auth.user);

  const [profile, setProfile] = useState<PublicUserProfileDto | null>(null);
  const [recentPosts, setRecentPosts] = useState<PostDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!id) {
      setError(t("profile.public_not_found"));
      setLoading(false);
      return;
    }

    let cancelled = false;

    const fetchPublicProfile = async () => {
      setLoading(true);
      setError(null);

      try {
        const [userResponse, postsResponse] = await Promise.all([
          userService.getPublicProfile(id),
          postService.getUserPosts(id, 1, RECENT_POST_FETCH_SIZE),
        ]);

        if (cancelled) {
          return;
        }

        const userData = userResponse.data;
        const posts = postsResponse.data?.items ?? [];

        setProfile(userData);
        setRecentPosts(posts);
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

  const isCurrentUser = useMemo(() => {
    if (!currentUser || !profile) {
      return false;
    }

    return currentUser.id.toLowerCase() === profile.id.toLowerCase();
  }, [currentUser, profile]);

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
          </div>

          <div className="grid gap-4 md:grid-cols-3">
            <div className="rounded-[2rem] border border-border/50 bg-muted/30 p-5">
              <div className="mb-3 flex items-center gap-3 text-muted-foreground">
                <Calendar size={18} />
                <span className="text-sm font-medium">{t("profile.joined_at")}</span>
              </div>
              <p className="text-lg font-bold">{new Date(profile.createdAt).toLocaleDateString()}</p>
            </div>

            <div className="rounded-[2rem] border border-border/50 bg-muted/30 p-5">
              <div className="mb-3 flex items-center gap-3 text-muted-foreground">
                <Shield size={18} />
                <span className="text-sm font-medium">{t("profile.role")}</span>
              </div>
              <p className="text-lg font-bold">{profile.role}</p>
            </div>

            <div className="rounded-[2rem] border border-border/50 bg-muted/30 p-5">
              <div className="mb-3 flex items-center gap-3 text-muted-foreground">
                <FileText size={18} />
                <span className="text-sm font-medium">{t("profile.recent_posts")}</span>
              </div>
              <p className="text-lg font-bold">{recentPosts.length}</p>
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
                    <h3 className="text-xl font-bold tracking-tight">{post.title}</h3>
                    <p className="line-clamp-2 whitespace-pre-wrap text-sm leading-6 text-muted-foreground">
                      {post.content}
                    </p>
                  </div>
                  <ArrowRight className="mt-1 shrink-0 text-muted-foreground" size={18} />
                </div>
              </Link>
            ))}
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
