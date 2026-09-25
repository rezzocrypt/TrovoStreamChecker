const API = 'https://kick.com/api/v2/channels'

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
          startedAt: null,
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
        startedAt: stream?.start_time ?? null,
        avatar: data?.user?.profile_pic ?? null,
      }
    }),
  )
}

export const kick = {
  id: 'kick',
  name: 'Kick',
  checkChannels,
  channelUrl: (name) => `https://kick.com/${encodeURIComponent(name)}`,
}