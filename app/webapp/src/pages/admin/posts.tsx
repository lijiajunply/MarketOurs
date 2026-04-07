import { useState } from "react"
import { Search, Eye, Trash2, ShieldAlert } from "lucide-react"
import { useTranslation } from "react-i18next"

// Mock data
const MOCK_POSTS = [
  { id: "101", title: "Vintage Camera for Sale", author: "Alice Johnson", status: "Active", date: "2026-04-05", comments: 12 },
  { id: "102", title: "Looking for a roommate", author: "Bob Smith", status: "Active", date: "2026-04-06", comments: 5 },
  { id: "103", title: "Inappropriate Content", author: "Charlie Brown", status: "Flagged", date: "2026-04-06", comments: 0 },
  { id: "104", title: "Free desk (must pick up)", author: "Diana Prince", status: "Active", date: "2026-04-07", comments: 24 },
]

export default function AdminPostsPage() {
  const { t } = useTranslation()
  const [searchTerm, setSearchTerm] = useState("")
  
  const filteredPosts = MOCK_POSTS.filter(post => 
    post.title.toLowerCase().includes(searchTerm.toLowerCase()) || 
    post.author.toLowerCase().includes(searchTerm.toLowerCase())
  )

  return (
    <div className="space-y-8">
      <header className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">{t("admin.posts.title")}</h1>
          <p className="text-muted-foreground mt-1">{t("admin.posts.subtitle")}</p>
        </div>
        
        <div className="relative">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 text-muted-foreground" size={18} />
          <input 
            type="text" 
            placeholder={t("admin.posts.search_placeholder")}
            className="pl-10 pr-4 py-2 bg-muted/50 border border-border/50 rounded-xl focus:outline-none focus:ring-2 focus:ring-primary/20 w-full sm:w-64"
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
          />
        </div>
      </header>

      <div className="bg-card border border-border/50 rounded-[2rem] overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full text-sm text-left">
            <thead className="text-xs text-muted-foreground uppercase bg-muted/30">
              <tr>
                <th className="px-6 py-4 font-semibold">{t("admin.posts.table_title")}</th>
                <th className="px-6 py-4 font-semibold">{t("admin.posts.table_author")}</th>
                <th className="px-6 py-4 font-semibold">{t("admin.posts.table_status")}</th>
                <th className="px-6 py-4 font-semibold">{t("admin.posts.table_date")}</th>
                <th className="px-6 py-4 font-semibold text-right">{t("admin.posts.table_actions")}</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-border/50">
              {filteredPosts.map((post) => (
                <tr key={post.id} className="hover:bg-muted/30 transition-colors">
                  <td className="px-6 py-4">
                    <span className="font-bold text-foreground line-clamp-1">{post.title}</span>
                    <span className="text-xs text-muted-foreground">{t("admin.posts.comments_count", { count: post.comments })}</span>
                  </td>
                  <td className="px-6 py-4 text-muted-foreground">
                    {post.author}
                  </td>
                  <td className="px-6 py-4">
                    <span className={`px-2.5 py-1 rounded-full text-xs font-bold flex items-center gap-1 w-fit ${
                      post.status === 'Active' ? 'bg-emerald-500/10 text-emerald-500' : 'bg-orange-500/10 text-orange-500'
                    }`}>
                      {post.status === 'Flagged' && <ShieldAlert size={12} />}
                      {post.status === 'Active' ? t("admin.posts.status_active") : t("admin.posts.status_flagged")}
                    </span>
                  </td>
                  <td className="px-6 py-4 text-muted-foreground">
                    {post.date}
                  </td>
                  <td className="px-6 py-4 text-right">
                    <div className="flex items-center justify-end gap-2">
                      <button className="p-2 hover:bg-muted rounded-lg text-muted-foreground hover:text-foreground transition-colors" title={t("admin.posts.action_view")}>
                        <Eye size={18} />
                      </button>
                      <button className="p-2 hover:bg-destructive/10 rounded-lg text-muted-foreground hover:text-destructive transition-colors" title={t("admin.posts.action_delete")}>
                        <Trash2 size={18} />
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          
          {filteredPosts.length === 0 && (
            <div className="p-8 text-center text-muted-foreground">
              {t("admin.posts.no_posts_found", { searchTerm })}
            </div>
          )}
        </div>
      </div>
    </div>
  )
}
