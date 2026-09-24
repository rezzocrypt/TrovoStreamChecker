const API = 'https://kick.com/api/v2/channels'

const LOGO =
  '<svg viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg"><path fill="#53FC18" d="M18.94 3.06a10.5 10.5 0 0 0-1.71 20.32l-4.2-5.56h2.62v-4.56h-4.9L9.6 3.06h9.34z"/></svg>'

export async function checkChannels(logins) {
  const cleaned = logins.map((l) => String(l).trim()).filter(Boolean)

  return Promise.all(
    cleaned.map(async (login) => {
      const res = await fetch(`${API}/${encodeURIComponent(login.toLowerCase())}`)

      if (res.status === 404) {
        return {
          login,
          exists: false,
          online: false,
          viewers: null,
          title: null,
          game: null,
          avatar: null,
        }
      }

      if (!res.ok) {
        const err = new Error(`HTTP ${res.status}`)
        err.status = res.status
        throw err
      }

      const data = await res.json()
      const stream = data?.livestream ?? null

      return {
        login,
        exists: true,
        online: !!stream,
        viewers: stream?.viewer_count ?? null,
        title: stream?.session_title ?? null,
        game: stream?.categories?.[0]?.name ?? null,
        avatar: data?.user?.profile_pic ?? null,
      }
    }),
  )
}

export const kick = {
  id: 'kick',
  name: 'Kick',
  logo: LOGO,
  checkChannels,
  channelUrl: (name) => `https://kick.com/${encodeURIComponent(name)}`,
}