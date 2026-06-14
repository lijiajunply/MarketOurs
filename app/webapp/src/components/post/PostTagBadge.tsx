import type { PostTagDto } from "../../types"

export function PostTagBadge({ tag, fallback }: { tag?: PostTagDto | null; fallback?: string }) {
  if (!tag) return null

  return (
    <span
      className="inline-flex w-fit items-center rounded-full border px-2.5 py-1 text-xs font-bold"
      style={{
        borderColor: `${tag.color}55`,
        backgroundColor: `${tag.color}18`,
        color: tag.color,
      }}
    >
      {tag.name || fallback}
    </span>
  )
}
