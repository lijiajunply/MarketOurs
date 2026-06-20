import type { PostDto } from "../types";
import i18n from "./i18n";

type ShareOutcome = "shared" | "copied" | "cancelled";

type SharePayload = {
  title: string;
  text: string;
  url: string;
};

export function buildPostShareUrl(postId: string, origin = window.location.origin) {
  return new URL(`/post/${postId}`, origin).toString();
}

export function buildPostSharePayload(post: PostDto, origin = window.location.origin): SharePayload {
  const title = post.title.trim() || i18n.t("post.share_fallback_title");
  const url = buildPostShareUrl(post.id, origin);

  return {
    title,
    text: url,
    url,
  };
}

export async function sharePost(post: PostDto): Promise<ShareOutcome> {
  const payload = buildPostSharePayload(post);

  if (typeof navigator !== "undefined" && typeof navigator.share === "function") {
    try {
      await navigator.share(payload);
      return "shared";
    } catch (error) {
      if (error instanceof DOMException && error.name === "AbortError") {
        return "cancelled";
      }
    }
  }

  if (typeof navigator !== "undefined" && navigator.clipboard?.writeText) {
    await navigator.clipboard.writeText(payload.url);
    return "copied";
  }

  if (typeof document !== "undefined") {
    const textarea = document.createElement("textarea");
    textarea.value = payload.url;
    textarea.setAttribute("readonly", "");
    textarea.style.position = "absolute";
    textarea.style.left = "-9999px";
    document.body.appendChild(textarea);
    textarea.select();
    document.execCommand("copy");
    document.body.removeChild(textarea);
    return "copied";
  }

  throw new Error("Sharing is not supported in this browser.");
}
