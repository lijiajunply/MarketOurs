import { useState } from "react"
import { Save } from "lucide-react"

export default function AdminSettingsPage() {
  const [settings, setSettings] = useState({
    siteName: "MarketOurs",
    allowRegistration: true,
    maintenanceMode: false,
    maxPostImages: 5,
    autoApprovePosts: true,
  })

  const handleSave = (e: React.FormEvent) => {
    e.preventDefault()
    // Mock save
    alert("Settings saved successfully!")
  }

  return (
    <div className="space-y-8">
      <header>
        <h1 className="text-3xl font-bold tracking-tight">System Settings</h1>
        <p className="text-muted-foreground mt-1">Configure global platform behavior.</p>
      </header>

      <form onSubmit={handleSave} className="space-y-8">
        <div className="bg-card border border-border/50 rounded-[2.5rem] p-8 space-y-8">
          
          <div className="space-y-6">
            <h2 className="text-xl font-bold tracking-tight">General</h2>
            
            <div className="grid gap-2">
              <label htmlFor="siteName" className="text-sm font-medium">Site Name</label>
              <input 
                id="siteName"
                type="text" 
                value={settings.siteName}
                onChange={(e) => setSettings({...settings, siteName: e.target.value})}
                className="w-full md:w-1/2 px-4 py-3 bg-muted/50 border border-border/50 rounded-2xl focus:outline-none focus:ring-2 focus:ring-primary/20 transition-all"
              />
            </div>
            
            <div className="flex items-center justify-between py-4 border-b border-border/50">
              <div>
                <p className="font-medium">Allow New Registrations</p>
                <p className="text-sm text-muted-foreground">When disabled, no new users can sign up.</p>
              </div>
              <label className="relative inline-flex items-center cursor-pointer">
                <input 
                  type="checkbox" 
                  className="sr-only peer" 
                  checked={settings.allowRegistration}
                  onChange={(e) => setSettings({...settings, allowRegistration: e.target.checked})}
                />
                <div className="w-11 h-6 bg-muted peer-focus:outline-none rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-gray-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-primary"></div>
              </label>
            </div>
            
            <div className="flex items-center justify-between py-4">
              <div>
                <p className="font-medium">Maintenance Mode</p>
                <p className="text-sm text-muted-foreground">Show maintenance page to all non-admin users.</p>
              </div>
              <label className="relative inline-flex items-center cursor-pointer">
                <input 
                  type="checkbox" 
                  className="sr-only peer" 
                  checked={settings.maintenanceMode}
                  onChange={(e) => setSettings({...settings, maintenanceMode: e.target.checked})}
                />
                <div className="w-11 h-6 bg-muted peer-focus:outline-none rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-gray-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-primary"></div>
              </label>
            </div>
          </div>
          
          <div className="space-y-6 pt-4 border-t border-border/50">
            <h2 className="text-xl font-bold tracking-tight">Content</h2>
            
            <div className="grid gap-2">
              <label htmlFor="maxPostImages" className="text-sm font-medium">Max Images per Post</label>
              <input 
                id="maxPostImages"
                type="number" 
                min="1"
                max="20"
                value={settings.maxPostImages}
                onChange={(e) => setSettings({...settings, maxPostImages: parseInt(e.target.value)})}
                className="w-full md:w-1/4 px-4 py-3 bg-muted/50 border border-border/50 rounded-2xl focus:outline-none focus:ring-2 focus:ring-primary/20 transition-all"
              />
            </div>
            
            <div className="flex items-center justify-between py-4">
              <div>
                <p className="font-medium">Auto-Approve Posts</p>
                <p className="text-sm text-muted-foreground">If disabled, all new posts require admin review.</p>
              </div>
              <label className="relative inline-flex items-center cursor-pointer">
                <input 
                  type="checkbox" 
                  className="sr-only peer" 
                  checked={settings.autoApprovePosts}
                  onChange={(e) => setSettings({...settings, autoApprovePosts: e.target.checked})}
                />
                <div className="w-11 h-6 bg-muted peer-focus:outline-none rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-gray-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-primary"></div>
              </label>
            </div>
          </div>

        </div>
        
        <div className="flex justify-end">
          <button 
            type="submit"
            className="flex items-center gap-2 bg-primary text-primary-foreground px-6 py-3 rounded-2xl font-bold hover:opacity-90 transition-opacity"
          >
            <Save size={18} />
            Save Settings
          </button>
        </div>
      </form>
    </div>
  )
}
