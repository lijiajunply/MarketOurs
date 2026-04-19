import { formatDistanceToNow } from "date-fns"
import { enUS, zhCN } from "date-fns/locale"
import type { i18n } from "i18next"
import type { PostDto } from "../types"

export function formatPostRelativeDate(
  dateString: string,
  i18nInstance: i18n,
  updatedAtString?: string,
  editedLabel?: string,
) {
  try {
    const date = new Date(dateString)
    const display = formatDistanceToNow(date, {
      addSuffix: true,
      locale: i18nInstance.language === "zh" ? zhCN : enUS,
    })

    if (updatedAtString && editedLabel) {
      const updatedDate = new Date(updatedAtString)
      if (updatedDate.getTime() - date.getTime() > 5000) {
        return `${display} (${editedLabel})`
      }
    }

    return display
  } catch {
    return dateString
  }
}

export function getPostExcerpt(content: string, maxLength = 150) {
  if (content.length <= maxLength) {
    return content
  }

  return `${content.slice(0, maxLength)}...`
}

export function getPostAuthorName(post: PostDto, fallbackUserLabel: string) {
  return post.author?.name || `${fallbackUserLabel} ${post.userId.slice(0, 4)}`
}
