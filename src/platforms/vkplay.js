const BASE = '/vkplay-api'

function streamUrl(slug) {
  return `${BASE}/blog/${encodeURIComponent(slug)}/public_video_stream`
}

async function checkChannels(logins) {
  const cleaned = logins.map((l) => String(l).trim()).filter(Boolean)

  return Promise.all(
    cleaned.map(async (login) => {
      const res = await fetch(streamUrl(login))

      if (!res.ok) {
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

      const data = await res.json()

      const online = !!data?.isOnline && !data?.isEnded
      const user = data?.user || null
      const counts = data?.count || null

      return {
        login,
        exists: true,
        online,
        viewers: online ? (counts?.viewers ?? counts?.views ?? null) : null,
        title: online ? (data.title ?? null) : null,
        game: online ? (data.category?.title ?? null) : null,
        startedAt: online && data.startTime ? new Date(Number(data.startTime) * 1000).toISOString() : null,
        avatar: user?.avatarUrl ?? null,
      }
    }),
  )
}

export const vkplay = {
  id: 'vkplay',
  name: 'VK Play',
  checkChannels,
  channelUrl: (name) => `https://live.vkvideo.ru/${encodeURIComponent(name)}`,
}